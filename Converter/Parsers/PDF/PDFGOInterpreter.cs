using Converter.Converters;
using Converter.FileStructures;
using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.Rasterizers;
using Converter.StaticData;
using Converter.Utils;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Converter.Parsers.PDF
{
  /// <summary>
  /// PDF Graphics Objects Parser
  /// Parses PDF Content stream that contains many operators
  /// Stack based parser
  /// Just have 3 Stacks to store operands, for now because we don't have discrimated unions and I don't want to use object
  /// TODO: see max values for operands, if they are small then we can use single 32 bit unsigned integer to store any data
  /// regardless if its decimal or not
  /// TODO: See if building AST first would be better and faster
  /// </summary>
  public class PDFGOInterpreter
  {
    public byte[] _buffer;
    public int _pos = 0;
    public int _readPos = 0;
    private byte _char = PDFConstants.SP; // current char, init it to non null val
    private Stack<int> intOperands;
    private Stack<double> realOperands;
    private Stack<string> stringOperands;
    // TODO: maybe this stack isn't *really* needed
    private Stack<OperandType> operandTypes;
    // could be wrapped into struct and put inside operand types, but then all of the other would have it
    // this is more memory efficient I think
    private Stack<int> arrayLengths;
    private Stack<GraphicsState> GSS;
    public GraphicsState GS;
    public PDFGI_PathConstruction currentPC;
    public PDF_ResourceDict _resourceDict;
    private long tokensParsed = 0; // for debugging
    private double[,] textRenderingMatrix;
    private double[,] reUsableMatrix1;
    private double[,] reUsableMatrix2;
    public IConverter _converter;
    private (int Height, int Width) _targetSize;
    public byte[] _outputBuffer;
    public bool _debug;
    public PDFGO_DEBUG_STATE _debugState;
    private PathRasterizer _shapeRasterizer;
    public byte[] _delimiters = [(byte)'(', (byte)')', (byte)'/', (byte)'[', (byte)']', (byte)'<', (byte)'>'];
    public int _byteSize = 2; // Used only for type0 fonts
    public PDFLogger _pathLogger;
    public MyColor defaultColor;
    // this variable is used in specific case where cm is seen yet and we want to draw shape
    // because then it will move start at the end of the byte array instead of start because we use thin our origin TOP-LEFT and PDF does it BOTTOM-LEFT
    public bool _cmExecuted = false; 


    // TODO: maybe NULL check is redundant if we let it throw to end?
    public PDFGOInterpreter(byte[] contentBuffer, PDF_ResourceDict resourceDict, IConverter converter, bool debug = false)
    {
      _buffer = contentBuffer;
      intOperands = new Stack<int>();
      realOperands = new Stack<double>();
      operandTypes = new Stack<OperandType>();
      arrayLengths = new Stack<int>();
      stringOperands = new Stack<string>();
      GS.TextState = new PDFGI_TextObject();
      GSS = new Stack<GraphicsState>();
      GS = new GraphicsState();
      currentPC = new PDFGI_PathConstruction();
      _resourceDict = resourceDict;
      textRenderingMatrix = new double[3, 3];
      reUsableMatrix1 = new double[3, 3];
      reUsableMatrix2 = new double[3, 3];
      _converter = converter;
      _targetSize = (_converter.GetHeight(), _converter.GetWidth());
      _outputBuffer = _converter.CreateBuffer();
      _debug = debug;
      _shapeRasterizer = new PathRasterizer(Array.Empty<byte>(), "");
      _pathLogger = new PDFLogger();
      if (debug)
        _debugState = new PDFGO_DEBUG_STATE();
      defaultColor = new MyColor();
      defaultColor.SetColor(0, 0, 0, 1);

      InitGS();
    }
    public void InitGS(int pageHeight = 0)
    {
      GS = new GraphicsState();
      GS.CTM = MyMath.RealIdentityMatrix3x3();
    }
    public void ConvertToPixelData()
    {

      if (_debug)
        _debugState.Literals.Clear();
      uint val = ReadNext();
      // TODO: instead of string, we can return hexadecimals or numbers
      // since they are all less than 4char strings
      // TODO: move these to constants file
      // TODO: load all as double or float and then cast to int if needed?
      PDFGI_Point mp;
      string literal;
      tokensParsed++;
      // used for tracking points of currently drawn path
      PointD currPoint = new PointD();

      // val != because last token value might not be 0 so it won't account it
      while (_char != PDFConstants.NULL || (_char == PDFConstants.NULL && val != 0))
      {
        switch (val)
        {
          #region gss&sgs
          case 0x77: // w
            GS.LineWidth = GetNextStackValAsDouble();
            break;
          case 0x4a: // J
                     // valid values 0, 1, 2
            GS.LineCap = GetNextStackValAsInt();
            break;
          case 0x6a: // j
                     // valid values 0, 1, 2
            GS.LineJoin = GetNextStackValAsInt();
            break;
          case 0x4d: // M
            GS.MiterLimit = GetNextStackValAsDouble();
            break;
          case 0x64: // d
            PDFGI_DashPattern dashPattern = new PDFGI_DashPattern();
            int phase = GetNextStackValAsInt();
            int[] dashArr = new int[arrayLengths.Pop()];
            for (int dpIndex = dashArr.Length - 1; dpIndex >= 0; dpIndex--)
            {
              dashArr[dpIndex] = GetNextStackValAsInt();
            }
            Debug.Assert(arrayLengths.Count == 0, "currently only support solid line");
            GS.DashPattern = dashPattern;
            break;
          case 0x6972: // ri
            operandTypes.Pop();
            string renderingIntentString = stringOperands.Pop();

            bool valid = Enum.TryParse(renderingIntentString, out PDFGI_RenderingIntent ri);
            if (!valid)
              ri = PDFGI_RenderingIntent.Null;

            GS.RenderingIntent = ri;
            break;
          case 0x69: // i
            GS.Flatness = GetNextStackValAsDouble();
            break;
          case 0x7371: // qs
                       // Name of gs paramter dict that is in ExtGState subdict in current resorouceDict
                       // do it later
            throw new NotImplementedException("Operator not i implemented");
            break;

          // special graphics states
          // NOTE: This may not work with multiple cm, they may need to be stored and calculated at TJ
          // because of order between different types of transformations
          case 0x71: // q
            GSS.Push(GS.DeepCopy());
            break;
          case 0x51: // Q
            GS = GSS.Pop().DeepCopy();
            break;
          case 0x6d63: // cm
            double f = GetNextStackValAsDouble();
            double d = GetNextStackValAsDouble();
            double e = GetNextStackValAsDouble();
            double c = GetNextStackValAsDouble();
            double b = GetNextStackValAsDouble();
            double a = GetNextStackValAsDouble();
            UpdateCTM(a, b, c, e, d, f);
            _cmExecuted = true;
            break;
          #endregion gss&sgs
          #region pathConstruction
          case 0x6d: // m
            // this is relavive to current point
            double y = GetNextStackValAsDouble();
            double x = GetNextStackValAsDouble();
            m(x, y);
            break;
          case 0x6c: // l
            y = GetNextStackValAsDouble();
            x = GetNextStackValAsDouble();
            l(x, y);
            break;
          case 0x63: // c
            mp = new PDFGI_Point();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y2 = GetNextStackValAsInt();
            mp.X2 = GetNextStackValAsInt();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            //throw new NotImplementedException();
            break;
          case 0x76: // v
            mp = new PDFGI_Point();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y2 = GetNextStackValAsInt();
            mp.X2 = GetNextStackValAsInt();
            // throw new NotImplementedException();
            break;
          case 0x79: // y
            mp = new PDFGI_Point();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            //  throw new NotImplementedException();
            break;
          case 0x68: // h
            h();
            break;
          case 0x6572: // re
            mp = new PDFGI_Point();

            double height = GetNextStackValAsDouble();
            double width = GetNextStackValAsDouble();
            y = GetNextStackValAsDouble();
            x = GetNextStackValAsDouble();
            _pathLogger.Log(" re ");
            m(x, y);
            l(x + width, y);
            l(x + width, y + height);
            l(x, y + height);
            h();
            break;
          #endregion pathConstruction
          #region pathPainting
          case 0x53: // S
            if (_debug && _debugState.SkipPath == false)
            {
              // shape and logger reset have to be handled by debugger now
              _debugState.isPath = true;
              return;
            }
            StrokePath();
            break;
          case 0x73: // s
            h();
            if (_debug && _debugState.SkipPath == false)
            {
              // shape and logger reset have to be handled by debugger now
              _debugState.isPath = true;
              return;
            }
            StrokePath();
            break;
          case 0x66: // f
            currentPC.EvenOddClippingPath = false;
            currentPC.NonZeroClippingPath = true;
            if (_debug && _debugState.SkipPath == false)
            {
              // shape and logger reset have to be handled by debugger now
              _debugState.isPath = true;
              return;
            }
            StrokePath();
            break;
          case 0x46: // F
            currentPC.EvenOddClippingPath = false;
            currentPC.NonZeroClippingPath = true;
            if (_debug && _debugState.SkipPath == false)
            {
              // shape and logger reset have to be handled by debugger now
              _debugState.isPath = true;
              return;
            }
            StrokePath();
            break;
          case 0x2a66: // f*
            currentPC.EvenOddClippingPath = true;
            currentPC.NonZeroClippingPath = false;
            if (_debug && _debugState.SkipPath == false)
            {
              // shape and logger reset have to be handled by debugger now
              _debugState.isPath = true;
              return;
            }
            StrokePath();
            break;
          case 0x42: // B
            throw new NotImplementedException("Operator not i implemented");
          case 0x2a42: // B*
            throw new NotImplementedException("Operator not i implemented");
          case 0x62: // b
            throw new NotImplementedException("Operator not i implemented");
          case 0x2a62: // b*
            throw new NotImplementedException("Operator not i implemented");
          case 0x6e: // n
            mp = new PDFGI_Point();
            // throw new NotImplementedException();// CHECK CLIPPING PATH SECTION
            // path painting
            break;
          #endregion pathPainting
          #region clippingPath
          case 0x57: // W
            currentPC.NonZeroClippingPath = true;
            break;
          case 0x2a57: // W*
            currentPC.EvenOddClippingPath = true;
            // clipping paths
            break;
          #endregion clippingPath
          #region textObjects
          case 0x5442: // BT
            GS.TextState.Active = true;
            GS.TextState.InitMatrixes();
            break;
          case 0x5445: // ET
            GS.TextState.Active = false;
            break;
          #endregion textObjects
          #region textState
          case 0x6354: // Tc
            GS.TextState.Tc = GetNextStackValAsDouble();
            break;
          case 0x7754: // Tw
            GS.TextState.Tw = GetNextStackValAsDouble();
            break;
          case 0x7a54: // Tz
            GS.TextState.Th = GetNextStackValAsDouble() / 100;
            break;
          case 0x4c54: // TL
            GS.TextState.Tl = GetNextStackValAsDouble();
            break;
          case 0x6654: // Tf
            GS.TextState.FontScaleFactor = GetNextStackValAsDouble();
            GS.TextState.FontRef = GetNextStackValAsString();
            if (GS.TextState.Font == null || GS.TextState.FontRef != GS.TextState.Font.Key)
              SetupFont();
            break;
          case 0x7254: // Tr
            GS.TextState.TMode = GetNextStackValAsInt();
            break;
          case 0x7354: // Ts
            GS.TextState.TRise = GetNextStackValAsDouble();
            break;
          #endregion textState
          #region textPositioning;
          case 0x6454: // Td
            double ty = GetNextStackValAsDouble();
            double tx = GetNextStackValAsDouble();

            reUsableMatrix1[0, 0] = 1;
            reUsableMatrix1[0, 1] = 0;
            reUsableMatrix1[0, 2] = 0;

            reUsableMatrix1[1, 0] = 0;
            reUsableMatrix1[1, 1] = 1;
            reUsableMatrix1[1, 2] = 0;

            reUsableMatrix1[2, 0] = tx;
            reUsableMatrix1[2, 1] = ty;
            reUsableMatrix1[2, 2] = 1;

            MyMath.MultiplyMatrixes3x3(reUsableMatrix1, GS.TextState.TextLineMatrix, GS.TextState.TextMatrix);
            MyMath.CopyMatrix3x3Data(GS.TextState.TextLineMatrix, GS.TextState.TextMatrix);
            break;
          case 0x4454: // TD
            ty = GetNextStackValAsDouble();
            tx = GetNextStackValAsDouble();
            GS.TextState.TL = -ty;

            reUsableMatrix1[0, 0] = 1;
            reUsableMatrix1[0, 1] = 0;
            reUsableMatrix1[0, 2] = 0;

            reUsableMatrix1[1, 0] = 0;
            reUsableMatrix1[1, 1] = 1;
            reUsableMatrix1[1, 2] = 0;

            reUsableMatrix1[2, 0] = tx;
            reUsableMatrix1[2, 1] = ty;
            reUsableMatrix1[2, 2] = 1;

            MyMath.MultiplyMatrixes3x3(reUsableMatrix1, GS.TextState.TextLineMatrix, GS.TextState.TextMatrix);
            MyMath.CopyMatrix3x3Data(GS.TextState.TextLineMatrix, GS.TextState.TextMatrix);
            break;
          case 0x6d54: // Tm
            double tmOp = GetNextStackValAsDouble();
            GS.TextState.TextMatrix[2, 1] = tmOp;
            GS.TextState.TextLineMatrix[2, 1] = tmOp;
            // e
            tmOp = GetNextStackValAsDouble();
            GS.TextState.TextMatrix[2, 0] = tmOp;
            GS.TextState.TextLineMatrix[2, 0] = tmOp;
            // d
            tmOp = GetNextStackValAsDouble();
            GS.TextState.TextMatrix[1, 1] = tmOp;
            GS.TextState.TextLineMatrix[1, 1] = tmOp;
            // c
            tmOp = GetNextStackValAsDouble();
            GS.TextState.TextMatrix[1, 0] = tmOp;
            GS.TextState.TextLineMatrix[1, 0] = tmOp;
            // b
            tmOp = GetNextStackValAsDouble();
            GS.TextState.TextMatrix[0, 1] = tmOp;
            GS.TextState.TextLineMatrix[0, 1] = tmOp;
            // a
            tmOp = GetNextStackValAsDouble();
            GS.TextState.TextMatrix[0, 0] = tmOp;
            GS.TextState.TextLineMatrix[0, 0] = tmOp;
            break;
          case 0x2a54: // T*
                       // text positioning
            break;
          #endregion textPositioning;
          #region textShowing
          // NOTE:
          // When the current font is composite,
          // the text-showing operators shall behave differently than with simple fonts.
          // For simple fonts, each byte of a string to be shown selects one glyph, whereas for composite fonts,
          // a sequence of one or more bytes are decoded to select a glyph from the descendant CIDFont.
          case 0x6a54: // Tj
            literal = GetNextStackValAsString();
            // for debug we willr return to give back control to the called
            // we could also impelment semaphoreslim to stop processing but this is simpler/dumber version
            if (_debug)
            {
              _debugState.FontRef = GS.TextState.FontRef;
              LiteralToDrawState lState = new LiteralToDrawState(literal, 0);
              _debugState.Literals.Add(lState);
              return;
            }

            PDF_DrawText(literal);

            break;
          case 0x4a54: // TJ
            //TODO: This is very bad
            //~I should actually load it into list while parsing arrays
            //  Load as a human
            if (operandTypes.Peek() == OperandType.ARRAY)
            {
              operandTypes.Pop();
              int arrLen = arrayLengths.Pop();
              List<(string Literal, int PosCorrection)> literalsList = new List<(string, int)>();


              for (int i = 0; i < arrLen; i++)
              {
                operandTypes.Pop();// this should always pop string
                literal = stringOperands.Pop();

                int posCorrection = 0;
                if (operandTypes.Count > 0 && operandTypes.Peek() == OperandType.INT)
                {
                  posCorrection = intOperands.Pop();
                  operandTypes.Pop();
                  i++;
                }

                literalsList.Add((literal, posCorrection));

              }

              // for debug we willr return to give back control to the called
              // we could also impelment semaphoreslim to stop processing but this is simpler/dumber version
              if (_debug)
              {
                _debugState.FontRef = GS.TextState.FontRef;

                for (int i = literalsList.Count - 1; i >= 0; i--)
                {
                  LiteralToDrawState lState = new LiteralToDrawState(literalsList[i].Literal, literalsList[i].PosCorrection);
                  _debugState.Literals.Add(lState);
                }
                return;
              }


              // read in proper order
              for (int i = literalsList.Count - 1; i >= 0; i--)
              {
                PDF_DrawText(literalsList[i].Literal, literalsList[i].PosCorrection);
              }
            }
            break;
          case 0x27:   // '
            throw new NotImplementedException("Operator not i implemented");
          case 0x22:   // "
            throw new NotImplementedException("Operator not i implemented");
            // text showing

            break;
          #endregion textShowing
          case 0x3064: // d0
            throw new NotImplementedException("Operator not i implemented");
          case 0x3164: // d1
            throw new NotImplementedException("Operator not i implemented");
            // type 3 fonts
            break;
          case 0x5343: // CS
            GS.StrokingColorSpace = InitCurrentColorSpace();
            break;
          case 0x7363: // cs
            GS.NonStrokingColorSpace = InitCurrentColorSpace();
            break;
          case 0x4353: // SC
            SetColor(GS.StrokingColorSpace);
            break;
          case 0x4e4353: // SCN
            SetColorAndName(GS.StrokingColorSpace);
            break;
          case 0x6373: // sc
            SetColor(GS.NonStrokingColorSpace);
            break;
          case 0x6e6373: // scn
            SetColorAndName(GS.NonStrokingColorSpace);
            break;
          case 0x47: // G
            GetNextStackValAsDouble();
            break;
          case 0x67: // g
            GetNextStackValAsDouble();
            break;
          case 0x4752: // RG
            GS.StrokingColorSpace = InitCurrentColorSpace(PDF_ColorSpaceFamily.DeviceRGB);
            SetColor(GS.StrokingColorSpace);
            break;
          case 0x6772: // rg
            GS.NonStrokingColorSpace = InitCurrentColorSpace(PDF_ColorSpaceFamily.DeviceRGB);
            SetColor(GS.NonStrokingColorSpace);
            break;
          case 0x4b: // K
            GS.StrokingColorSpace = InitCurrentColorSpace(PDF_ColorSpaceFamily.DeviceCMYK);
            SetColor(GS.StrokingColorSpace);
            break;
          case 0x6b: // k
            GS.NonStrokingColorSpace = InitCurrentColorSpace(PDF_ColorSpaceFamily.DeviceCMYK);
            SetColor(GS.NonStrokingColorSpace);
            break;
          case 0x6873: // sh
                       // shading patterns
            throw new NotImplementedException("Operator not i implemented");
            break;
          case 0x4972: // BI
            throw new NotImplementedException("Operator not i implemented");
          case 0x4449: // ID
            throw new NotImplementedException("Operator not i implemented");
          case 0x4945: // EI
            throw new NotImplementedException("Operator not i implemented");
            // inline images
            break;
          case 0x6f44: // Do
                       // XObject
                       // throw new NotImplementedException("Operator not i implemented");
            break;
          case 0x504d: // MP
            throw new NotImplementedException("Operator not i implemented");
          case 0x5044: // DP
            throw new NotImplementedException("Operator not i implemented");
          // Marked content
          // It seems like this isnt required for rendering and we can just pop it off the stack
          case 0x434d42: // BMC
            PopString();
            break;
          case 0x434442: // BDC
            if (operandTypes.Peek() == OperandType.DICT)
              VoidDict();
            else
              PopString();
            PopString();
            break;
          case 0x434d45: // EMC
            // do nothing since we dont support marked content 
            break;
          case 0x5842: // BX
            throw new NotImplementedException("Operator not i implemented");
          case 0x5845: // EX
            throw new NotImplementedException("Operator not i implemented");
            // Compatibility
            break;
          default:
            break;
        }

        val = ReadNext();
        tokensParsed++;
      }
      // TODO: remove this eventually
      Debug.Assert(_outputBuffer.Select(x => x > 0).Any());
      _converter.Save(_outputBuffer);
    }
    /// <summary>
    /// Read to next byte and see if its:
    ///  digit -> can be number or real
    ///  [ -> array
    ///  / -> name
    ///  ( -> string literal (ENCODE THIS WITH UNICODE or something global)
    ///  other char -> most likely operator
    ///  if its operator return uint value of it otherwise return 0
    /// </summary>
    /// <returns></returns>
    // TODO: think about storing byte[] instead of string, not sure if it matters
    private uint ReadNext(int depth = 0)
    {
      SkipWhiteSpace();


      if (IsCurrentCharPartOfOperator() && _char != PDFConstants.NULL)
      {
        int startPos = _pos;
        // TODO: this may have to be all more delimiters than '/'
        while (!IsCurrentCharPDFWhitespaceOrNewLine() && !_delimiters.Contains(_char))
          ReadChar();

        Span<byte> fourByteSlice = stackalloc byte[4];
        // we have to pass 4 bytes to bitconverter but some of the comands are 3 bytes so we need this buffer
        for (int i = 0; i < _pos - startPos; i++)
          fourByteSlice[i] = _buffer[startPos + i];
        return BitConverter.ToUInt32(fourByteSlice);
      }

      // is a number, either real or int
      if (IsCurrentCharDigit() || _char == '-')
      {
        GetNumber();
        return 0;
      }

      // is a name
      if (_char == '/')
      {
        GetName();
        return 0;
      }

      // string literal
      if (_char == '(')
      {
        GetStringLiteral();
        return 0;
      }

      if (_char == '<')
      {
        if (_buffer[_readPos] == '<')
        {
          ReadChar();
          ReadChar();
          int count = 0;
          SkipWhiteSpace();
          depth++;
          // not sure if i should ever put some limit here
          if (depth > 10)
            throw new StackOverflowException("Depth too large!");

          while (_char != PDFConstants.NULL)
          {
            ReadNext(depth);
            count++;
            SkipWhiteSpace();
            if (_char == '>' && _buffer[_readPos] == '>')
              break;
          }
          depth--;
          ReadChar(); // move off 1st '>'
          ReadChar(); // move off 2nd '>'
          operandTypes.Push(OperandType.DICT);
          Debug.Assert(count % 2 == 0);
          // count only keys because if there are nested arrays or dicts they will their own count
          arrayLengths.Push(count);
          return 0;
        }
        else
        {
          GetHexString();
          return 0;
        }
      }

      // array
      if (_char == '[')
      {
        ReadChar();
        int count = 0;
        SkipWhiteSpace();
        depth++;
        // not sure if i should ever put some limit here
        if (depth > 10)
          throw new StackOverflowException("Depth too large!");

        while (_char != ']' && _char != PDFConstants.NULL)
        {
          ReadNext(depth);
          count++;
          SkipWhiteSpace();
        }
        depth--;
        ReadChar(); // move off ']'
        operandTypes.Push(OperandType.ARRAY);
        arrayLengths.Push(count);
        return 0;
      }

      return 0;
    }
    /// <summary>
    /// We should probably interpret this and push it to number stack instead of string stack
    /// </summary>
    private void GetHexString()
    {
      // we are including <>
      int startPos = _pos;
      while (_char != PDFConstants.NULL && _char != '>')
      {
        ReadChar();
      }
      ReadChar();

      stringOperands.Push(Encoding.Default.GetString(_buffer.AsSpan().Slice(startPos, _pos - startPos)));
      operandTypes.Push(OperandType.STRING);
    }
    private void GetNumber()
    {
      int startPos = _pos;
      int negativeMulti = 1;
      if (_char == '-')
      {
        negativeMulti = -1;
        ReadChar();
      }

      int integer = 0;
      int baseIndex = -1;
      while ((IsCurrentCharDigit() || _char == '.') && _char != PDFConstants.NULL && !IsCurrentCharPDFWhitespaceOrNewLine())
      {
        if (_char == '.')
        {
          baseIndex = _pos - startPos;
          ReadChar();
          continue;
        }

        integer = integer * 10 + CharUnicodeInfo.GetDecimalDigitValue((char)_char);
        ReadChar();
      }


      if (baseIndex == -1)
      {
        // number is integer
        intOperands.Push(integer * negativeMulti);
        operandTypes.Push(OperandType.INT);
      }
      else
      {
        // number has no decimals but is expected to be double
        // i.e 253.0
        if (_pos - baseIndex - startPos == 1)
          realOperands.Push((double)(integer * negativeMulti));
        else
          realOperands.Push((integer / Math.Pow(10, _pos - baseIndex - startPos - 1)) * negativeMulti);
        operandTypes.Push(OperandType.DOUBLE);
      }
    }

    private double GetNextStackValAsDouble()
    {
      if (operandTypes.Pop() == OperandType.INT)
        return intOperands.Pop();
      else
        return realOperands.Pop();
    }

    private int GetNextStackValAsInt()
    {
      if (operandTypes.Count == 0)
      {
        int x = 0;
      }

      if (operandTypes.Pop() == OperandType.INT)
        return intOperands.Pop();
      else
        return (int)realOperands.Pop();
    }

    private string GetNextStackValAsString()
    {
      OperandType t = operandTypes.Pop();
      if (t == OperandType.STRING)
        return stringOperands.Pop();
      if (t == OperandType.INT)
        return intOperands.Pop().ToString();
      if (t == OperandType.DOUBLE)
        return realOperands.Pop().ToString();
      return "";
    }
    // we are at '/'
    private void GetName()
    {
      ReadChar();
      int startPos = _pos;
      while (!IsCurrentCharPDFWhitespaceOrNewLine() && !_delimiters.Contains(_char) && _char != PDFConstants.NULL)
      {
        ReadChar();
      }
      stringOperands.Push(Encoding.Default.GetString(_buffer, startPos, _pos - startPos));
      operandTypes.Push(OperandType.STRING);
    }

    // we are at '('
    // we need to use string builder here because of the escape characters
    private void GetStringLiteral()
    {
      ReadChar();
      int c = 0;
      int depth = 1;

      // TODO: Use pool for this
      StringBuilder sb = new StringBuilder();
      while (depth != 0 && _char != PDFConstants.NULL)
      {
        // octal representation page 16
        c = _char;
        if (_char == '(')
        {
          depth++;
        }
        else if (_char == ')')
        {
          depth--;
          if (depth == 0)
            break;
        }
        else if (_char == '\\')
        {
          ReadChar();
          if (_char >= '0' && _char < '8')
          {
            int count = 0;
            int val = 0;
            while (_char >= '0' && _char < '8' && count < 3)
            {
              val = val * 8 + _char - '0';
              ReadChar();
              count++;
            }
            // return it back since we will read char at the end
            _pos--;
            _readPos--;
            c = val;
          }
          else if (c == 'n')
          {
            c = '\n';
          }
          else if (c == 'r')
          {
            c = '\r';
          }
          else if (c == 't')
          {
            c = '\t';
          }
          else if (c == 'b')
          {
            c = '\b';
          }
          else if (c == 'f')
          {
            c = '\f';
          }
          else if (c == '\n' || c == '\r')
          {
            ReadChar();
            continue;
          }
        }

        sb.Append((char)c);
        ReadChar();
      }

      stringOperands.Push(sb.ToString());
      operandTypes.Push(OperandType.STRING);
      ReadChar();
    }

    private void ReadChar()
    {
      if (_readPos >= _buffer.Length)
        _char = PDFConstants.NULL;
      else
        _char = _buffer[_readPos];

      // set curr and go next
      _pos = _readPos++;
    }

    /// <summary>
    /// True means that its part of operand and false means that its part of operator
    /// </summary>
    /// <returns></returns>
    private bool IsCurrentCharPartOfOperator()
    {
      if ((_char >= 65 && _char <= 90) || (_char >= 97 && _char <= 122) || _char == '\'' || _char == '\"')
        return true;
      return false;
    }
    private bool IsCurrentCharPDFWhitespaceOrNewLine()
    {
      return _char == PDFConstants.SP || _char == PDFConstants.LF || _char == PDFConstants.CR || _char == PDFConstants.NULL;
    }
    private void SkipWhiteSpace()
    {
      while (IsCurrentCharPDFWhitespaceOrNewLine() && _char != PDFConstants.NULL)
        ReadChar();
    }
    private bool IsCurrentCharDigit()
    {
      return (_char >= 48 && _char <= 57);
    }

    // Should this be removed?
    public void UpdateTextMatrixAfterGlyphRender(int charWidth, int charHeight, int posAdjustment)
    {
      double tx = ((charWidth - posAdjustment / 1000) * GS.TextState.FontScaleFactor + GS.TextState.Tc + GS.TextState.Tw) * (GS.TextState.Th);
      double ty = (charHeight - posAdjustment / 1000) * GS.TextState.FontScaleFactor + GS.TextState.Tc + GS.TextState.Tw;
      // init first matrix
      reUsableMatrix1[0, 0] = 1;
      reUsableMatrix1[0, 1] = 0;
      reUsableMatrix1[0, 2] = 0;

      reUsableMatrix1[1, 0] = 0;
      reUsableMatrix1[1, 1] = 1;
      reUsableMatrix1[1, 2] = 0;

      reUsableMatrix1[2, 0] = tx;
      reUsableMatrix1[2, 1] = ty;
      reUsableMatrix1[2, 2] = 1;
      // multiply
      MyMath.MultiplyMatrixes3x3(reUsableMatrix1, GS.TextState.TextMatrix, reUsableMatrix2);

      // asign value to textMatrix
      GS.TextState.TextMatrix[0, 0] = reUsableMatrix2[0, 0];
      GS.TextState.TextMatrix[0, 1] = reUsableMatrix2[0, 1];
      GS.TextState.TextMatrix[0, 2] = reUsableMatrix2[0, 2];

      GS.TextState.TextMatrix[1, 0] = reUsableMatrix2[1, 0];
      GS.TextState.TextMatrix[1, 1] = reUsableMatrix2[1, 1];
      GS.TextState.TextMatrix[1, 2] = reUsableMatrix2[1, 2];

      GS.TextState.TextMatrix[2, 0] = reUsableMatrix2[2, 0];
      GS.TextState.TextMatrix[2, 1] = reUsableMatrix2[2, 1];
      GS.TextState.TextMatrix[2, 2] = reUsableMatrix2[2, 2];

    }

    /// <summary>
    /// Make ShapeRasterizer and use it for this, we need accesds to STBRasterizer
    /// Path/Shape points are defined in user (absolute) space so we dont have to apply any scalling
    /// or calculate offsets or anything similar
    /// </summary>
    public void PDF_RasterShape()
    {

      // TODO: still cant figure out winding rules stuff so ignore for now and get all fonts in order
      return;
      //currentPC.Shape.SaveAbsolute("shapeExport");
      // rounding makes it look a bit better?
      int X = (int)MathF.Round((float)GS.CTM[2, 0]);
      // because origin is bottom-left we have do bitmapHeight - , to get position on the top
      int Y;

      if (_cmExecuted)
        Y = _targetSize.Height - (int)(GS.CTM[2, 1]);
      else
        Y = 0;

      float scaleX = (float)GS.CTM[0, 0];
      float scaleY = (float)GS.CTM[1, 1];

      // do one scale for now
      float scale = scaleX > scaleY ? scaleX : scaleY;

      int y = Y;
      int byteOffset = X + (y * _targetSize.Width);
      _shapeRasterizer.RasterizeShape(_outputBuffer, byteOffset, _targetSize.Width, _targetSize.Height, currentPC.Shape, scale);
    }

    public void PDF_DrawText(string textToWrite, int positionAdjustment = 0)
    {
      
      GlyphInfo glyphInfo = new GlyphInfo(); // make global??
      if (GS.NonStrokingColorSpace == null || GS.NonStrokingColorSpace.Color == null)
        glyphInfo.Color = defaultColor;
      else 
        glyphInfo.Color = GS.NonStrokingColorSpace.Color;
      // Account for position adjustment
      GS.TextState.TextMatrix[2, 0] -= (positionAdjustment / 1000f) * GS.TextState.TextMatrix[0, 0] * GS.TextState.FontScaleFactor;
      // Type0 encodes characters in special way and CID can map to either ligature (multiple characters) or single char
      // So not to make PDF_DrawGlyph weird and those set of interfaces really weird, we will check CID here specifically for Type0 font
      // since afaik this is the only font that does this
      if (GS.TextState.Font.FontInfo.SubType == PDF_FontType.Type0)
      {
        PDF_DrawTextMultiByte(textToWrite, ref glyphInfo);
      }
      else
      {
        for (int i = 0; i < textToWrite.Length; i++)
        {
          PDF_DrawGlyph(textToWrite[i], ref glyphInfo, textToWrite, i);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="c"></param>
    /// <param name="glyphInfo"></param>
    /// <param name="textToWrite">For debug</param>
    /// <param name="index">For debug</param>
    public void PDF_DrawTextMultiByte(string textToWrite, ref GlyphInfo glyphInfo)
    {
      char? c;
      char CID;

      Debug.Assert(textToWrite.Length > 0);
      if (textToWrite == " ")
      {
        PDF_DrawGlyph(' ', ref glyphInfo, textToWrite, 0, ' ');
        return;
      }

      // support only 2 or 4 bytes for now
      for (int i = 1; i < textToWrite.Length - 1; i += (_byteSize * 2))
      {
        // this doesnt work i think...?
        if (_byteSize == 4)
        {
          CID = (char)UInt32.Parse(textToWrite.AsSpan().Slice(i, _byteSize * 2), NumberStyles.HexNumber);
        }
        else if (_byteSize == 2)
        {
          CID = (char)UInt16.Parse(textToWrite.AsSpan().Slice(i, _byteSize * 2), NumberStyles.HexNumber);
        }
        else
        {
          throw new NotSupportedException("Other byte size not supported yet!");
        }

        c = GS.TextState.Font.Rasterizer.FindCharFromCID(CID);

        // Page 271 -> Even though the CIDs are not used to select glyphs in a Type 2 CIDFont, they shall always be used to determine the glyph metrics, as described in the next sub-clause.
        // not sure fi this is right....
        if (GS.TextState.Font.FontInfo.DescendantFontsInfo[0].DescendantDict.Subtype == PDF_FontType.CIDFontType2)
          c = CID;

        if (c != null)
        {
          PDF_DrawGlyph((char)c, ref glyphInfo, textToWrite, 0, CID);
          continue;
        }

        // Not sure if ligatures should be printed separatetly or I should advance manually for each char
        // just do this for now and see how it works
        List<char> ligature = GS.TextState.Font.Rasterizer.FindLigatureFromCID(CID);
        if (ligature.Count == 0)
        {
          char character = '0';
          PDF_DrawGlyph((char)0, ref glyphInfo, textToWrite, 0, CID);
          continue;
        }
        else
        {
          for (int j = 0; j < ligature.Count; j++)
          {
            // NOTE:
            // Not sure if i should pass CID of ligature of current glyph in ligature
            // Because we will get width based on CID and then chars in ligature may appear to wide/sparse
            PDF_DrawGlyph(ligature[j], ref glyphInfo, textToWrite, j, CID);
          }
        }
      }
    }

    /// <summary>
    /// Even though we have some global state, for now i will keep those as parameters since it might be easier to use it debug state?
    /// </summary>
    /// <param name="c"></param>
    /// <param name="glyphInfo"></param>
    /// <param name="rasterizer"></param>
    /// <param name="state"></param>
    /// <param name="fd"></param>
    /// <param name="widths"></param>
    /// <param name="literal"></param>
    /// <param name="index"></param>
    /// <param name="CID">// CID is only used for Composite fonts</param>
    public void PDF_DrawGlyph(char c, ref GlyphInfo glyphInfo, string literal, int index, char CID = ' ')
    {
      // just copy ref since lines can get too long
      PDF_FontData fd = GS.TextState.Font;
      if (GS.TextState.Font.Rasterizer == null)
        return;
      GS.TextState.Font.Rasterizer.SetDefaultGlyphInfoValues(ref glyphInfo);
      // TODO: use this instead of c, FIX 
      GS.TextState.Font.Rasterizer.GetGlyphInfo(c, ref glyphInfo);

      ComputeTextRenderingMatrix();

      // rounding makes it look a bit better?
      int X = (int)MathF.Round((float)textRenderingMatrix[2, 0]);
      // because origin is bottom-left we have do bitmapHeight - , to get position on the top
      int Y = _targetSize.Height - (int)(textRenderingMatrix[2, 1]);

      #region width calculation

      float width = 0;
      if (fd.FontInfo.SubType == PDF_FontType.Type0)
      {
        width = RasterHelper.GetCompositeWidth(CID, fd.FontInfo.DescendantFontsInfo![0].DescendantDict);
      }
      else
      {
        // Does this work for all charcaters
        int idx = (int)c - fd.FontInfo.FirstChar;
        
        if (fd.FontInfo.Widths != null && idx < fd.FontInfo.Widths.Length)
          width = (float)fd.FontInfo.Widths[idx] / 1000f;
        else
          width = fd.FontInfo.FontDescriptor.MissingWidth / 1000f;
      }
      //Debug.Assert(width != 0);
      #endregion

      (float scaleX, float scaleY) s = GS.TextState.Font.Rasterizer.GetScale(glyphInfo.Index, textRenderingMatrix, width);

      #region asserts
      Debug.Assert(X > 0, $"X is negative at index {index}. Lit: {literal}");
      Debug.Assert(Y > 0, $"Y is negative at index {index}. Lit: {literal}");
      Debug.Assert(X < _targetSize.Width, $"X must be within bounds.X: {X} - Width: {_targetSize.Width}. Lit: {literal}");
      Debug.Assert(Y < _targetSize.Height, $"Y must be within bounds.Y: {Y} - Height: {_targetSize.Height}. Lit: {literal}");
      Debug.Assert(s.scaleX > 0, $"Scale factor X must be higher than 0! sfX: {s.scaleX}. Lit: {literal}. Ind : {index}");
      Debug.Assert(s.scaleY > 0, $"Scale factor Y must be higher than 0! sfY: {s.scaleY}. Lit: {literal}.Ind : {index}");
      #endregion asserts

      int ascent = 0;
      int descent = 0;
      if (fd.FontInfo.SubType == PDF_FontType.Type0)
      {
        ascent = (int)Math.Round(fd.FontInfo.DescendantFontsInfo![0].DescendantDict.FontDescriptor.Ascent * s.scaleY);
        descent = (int)Math.Round(fd.FontInfo.DescendantFontsInfo![0].DescendantDict.FontDescriptor.Descent * s.scaleY);
      }
      else
      {
        ascent = (int)Math.Round(fd.FontInfo.FontDescriptor.Ascent * s.scaleY);
        descent = (int)Math.Round(fd.FontInfo.FontDescriptor.Descent * s.scaleY);
      }

      #region glyph metrics

      int c_x0 = 0;
      int c_y0 = 0;
      int c_x1 = 0;
      int c_y1 = 0;
      GS.TextState.Font.Rasterizer.GetGlyphBoundingBox(ref glyphInfo, s.scaleX, s.scaleY, ref c_x0, ref c_y0, ref c_x1, ref c_y1);

      Debug.Assert(c_x0 != int.MaxValue && c_x0 != int.MinValue);
      Debug.Assert(c_y0 != int.MaxValue && c_y0 != int.MinValue);
      Debug.Assert(c_x1 != int.MaxValue && c_x1 != int.MinValue);
      Debug.Assert(c_y1 != int.MaxValue && c_y1 != int.MinValue);

      // char height - different than bounding box height
      int y = Y + c_y0;
      // I think that this should be replaced from value in Widths array
      // NOTE: widths array wont work since this width is not in units but in pixels after its been scaled down
      int glyphWidth = c_x1 - c_x0;
      int glyphHeight = c_y1 - c_y0;

      #endregion

      int byteOffset = X + (y * _targetSize.Width);
      int shiftX = 0;
      int shiftY = 0;

      GS.TextState.Font.Rasterizer.RasterizeGlyph(_outputBuffer, byteOffset, glyphWidth, glyphHeight, _targetSize.Width, s.scaleX, s.scaleY, shiftX, shiftY, ref glyphInfo);

      AdvanceDrawPos(c, width);
    }

    private void UpdateCTM(double a, double b, double c, double d, double e, double f)
    {
      reUsableMatrix1[0, 0] = a;
      reUsableMatrix1[0, 1] = b;
      reUsableMatrix1[0, 2] = 0;

      reUsableMatrix1[1, 0] = c;
      reUsableMatrix1[1, 1] = d;
      reUsableMatrix1[1, 2] = 0;

      reUsableMatrix1[2, 0] = e;
      reUsableMatrix1[2, 1] = f;
      reUsableMatrix1[2, 2] = 1;

      MyMath.MultiplyMatrixes3x3(reUsableMatrix1, GS.CTM, reUsableMatrix2);

      GS.CTM[0, 0] = reUsableMatrix2[0, 0];
      GS.CTM[0, 1] = reUsableMatrix2[0, 1];
      GS.CTM[0, 2] = reUsableMatrix2[0, 2];

      GS.CTM[1, 0] = reUsableMatrix2[1, 0];
      GS.CTM[1, 1] = reUsableMatrix2[1, 1];
      GS.CTM[1, 2] = reUsableMatrix2[1, 2];

      GS.CTM[2, 0] = reUsableMatrix2[2, 0];
      GS.CTM[2, 1] = reUsableMatrix2[2, 1];
      GS.CTM[2, 2] = reUsableMatrix2[2, 2];
    }

    // TODO: optimize
    public void ComputeTextRenderingMatrix()
    {
      // Set initial value to first matrix
      double[,] identity = new double[3, 3];
      identity[0, 0] = GS.TextState.FontScaleFactor * GS.TextState.Th;
      identity[0, 1] = 0;
      identity[0, 2] = 0;
      identity[1, 0] = 0;
      identity[1, 1] = GS.TextState.FontScaleFactor;
      identity[1, 2] = 0;
      identity[2, 0] = 0;
      identity[2, 1] = GS.TextState.TRise;
      identity[2, 2] = 1;

      double[,] mid = new double[3, 3];
      MyMath.MultiplyMatrixes3x3(identity, GS.TextState.TextMatrix, mid);
      MyMath.MultiplyMatrixes3x3(mid, GS.CTM, textRenderingMatrix);
    }

    public void AdvanceDrawPos(char c, double width)
    {
      #region Advance
      // double advanceX = width * state.TextObject.FontScaleFactor + state.TextObject.Tc;
      // double advanceY = 0 + state.TextObject.FontScaleFactor; // This wont work for vertical fonts
      double advanceX = width + GS.TextState.Tc;
      double advanceY = 0; // This wont work for vertical fonts

      if (c == ' ')
        advanceX += GS.TextState.Tw;
      advanceX *= GS.TextState.Th;

      // TODO: this really depends on what type of CTM it is. i.e is there shear, transaltion, rotation etc
      // I should detect this and save state somewhere
      // for now just support translate and scale
      // NOTE: actually I think I can just multiply matrix, and this is done to avoid matrix multiplciation
      GS.TextState.TextMatrix[2, 0] = advanceX * textRenderingMatrix[0, 0] + GS.TextState.TextMatrix[2, 0];
      GS.TextState.TextMatrix[2, 1] = 0 * textRenderingMatrix[1, 1] + GS.TextState.TextMatrix[2, 1];
      #endregion
    }

    public void SetupFont()
    {
      foreach (PDF_FontData fd in _resourceDict.Font)
        if (fd.Key == GS.TextState.FontRef)
        {
          if (fd.FontInfo.SubType == PDF_FontType.Type0)
            SetEncByteSize(fd.FontInfo.EncodingData.BaseEncoding);
          GS.TextState.Font = fd;
          return;
        }

      GS.TextState.Font = new PDF_FontData();
    }

    public void SetEncByteSize(string encoding)
    {
      _byteSize = encoding switch
      {
        "Identity-H" => 2,
        _ => 2
      };
    }

    /// <summary>
    /// There are 3 ways to init CS.
    /// 1. Reference actual color space with the key
    /// 2. Reference key in the ResourceDict ColorSpaces
    /// 3. Use default ColorSpace based on operator i.e (rg will expect DeviceRGB and only operands it has are r g b intensities)
    /// </summary>
    /// <param name="defaultCS"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public PDFGI_ColorState InitCurrentColorSpace(PDF_ColorSpaceFamily defaultCS = PDF_ColorSpaceFamily.NULL)
    {
      PDFGI_ColorState state = new PDFGI_ColorState();
     
      if (defaultCS != PDF_ColorSpaceFamily.NULL)
      {
        state.Cs = new PDF_ColorSpace();
        state.Cs.Family = defaultCS;
      }
      else
      {
        bool isCS = false;
        PDF_ColorSpaceFamily csf;
        string key = string.Empty;
        key = GetNextStackValAsString();
        isCS = Enum.TryParse<PDF_ColorSpaceFamily>(key, out csf);

        if (isCS)
        {
          foreach (PDF_ColorSpace cs in _resourceDict.ColorSpace)
          {
            if (cs.Family == csf)
            {
              state.Cs = cs;
              break;
            }
          }
        }
        else
        {
          foreach (PDF_ColorSpace cs in _resourceDict.ColorSpace)
          {
            if (cs.Key == key)
            {
              state.Cs = cs;
              break;
            }
          }
        }
      }

      // not sure if it has to be defined, if it does then remove this
      if (state.Cs == null)
        throw new InvalidDataException("Defined ColorSpace not found!");

      MyColor c = new MyColor();
      // init color 
      switch (state.Cs.Family)
      {
        case PDF_ColorSpaceFamily.NULL:
          throw new InvalidDataException("Defined ColorSpace not found!");
          break;
        case PDF_ColorSpaceFamily.DeviceGray:
          c.SetColor(0, 0, 0, 1);
          break;
        case PDF_ColorSpaceFamily.DeviceRGB:
          c.SetColor(0, 0, 0, 1);
          break;
        case PDF_ColorSpaceFamily.DeviceCMYK:
          c.SetColor(0, 0, 0, 1);
          break;
        case PDF_ColorSpaceFamily.CalGray:
          c.SetColor(0, 0, 0, 1);
          break;
        case PDF_ColorSpaceFamily.CalRGB:
          c.SetColor(0, 0, 0, 1);
          break;
        case PDF_ColorSpaceFamily.Lab:
          if (state.Cs.HasExtraData == false)
          {
            c.SetColor(0, 0, 0, 1);
          }
          else
          {
            // Implement this when we have extra data and know how Range looks like Table74 CS operator
            throw new NotImplementedException();
          }
          break;
        case PDF_ColorSpaceFamily.ICCBased:
          if (state.Cs.HasExtraData == false)
          {
            c.SetColor(0, 0, 0, 1);
          }
          else
          {
            PDF_ICCExtraData ICCExtra = new PDF_ICCExtraData();
            if (ICCExtra.Range == null || ICCExtra.Range.Length == 0)
              c.SetColor(0, 0, 0, 1);
            else
              // Implement this when we have extra data and know how Range looks like Table74 CS operator
              throw new NotImplementedException();
          }
          break;
        case PDF_ColorSpaceFamily.Indexed:
          state.IndexColor = 0;
          break;
        case PDF_ColorSpaceFamily.Pattern:
          state.Pattern = new PDFGI_Pattern();
          break;
        case PDF_ColorSpaceFamily.Separation:
          state.Tint = 1;
          break;
        case PDF_ColorSpaceFamily.DeviceN:
          state.Tint = 1;
          break;
      }
      state.Color = c;
      return state;
    }
    // sc 
    public void SetColor(PDFGI_ColorState state)
    {
      // i dont think all of the color spaces use this command, some use scn 
      // 0 usually means least intensitiy and 1 means most (0 black 1 white)
      // Do not normalize here and let rasterize normalize it OR find out color range before and use it to normalize here
      // color  range is pretty much channel size
      switch (state.Cs.Family)
      {
        case PDF_ColorSpaceFamily.NULL:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.DeviceGray:
          // values between 0.0 and 1.0 that will somehow need to be normalized
          double gray = GetNextStackValAsDouble();
          state.Color.SetColor(gray, gray, gray, 1);
          break;
        case PDF_ColorSpaceFamily.DeviceRGB:
          double blue = GetNextStackValAsDouble();
          double green = GetNextStackValAsDouble();
          double red = GetNextStackValAsDouble();
          state.Color.SetColor(red, green, blue, 1);
          break;
        case PDF_ColorSpaceFamily.DeviceCMYK:
          double black = GetNextStackValAsDouble();
          double yellow = GetNextStackValAsDouble();
          double magenta = GetNextStackValAsDouble();
          double cyan = GetNextStackValAsDouble();
          ColorHelper.ConvertCMYKtoRGBbyIntensity(cyan, magenta, yellow, black, state.Color);
          break;
        case PDF_ColorSpaceFamily.CalGray:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.CalRGB:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.Lab:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.ICCBased:
          if (!state.Cs.HasExtraData)
            throw new InvalidDataException("No extra data for ICC profile!");
          PDF_ICCExtraData extra = (PDF_ICCExtraData)state.Cs.ExtraCSData;
          if (extra.N == 1)
          {
            gray = GetNextStackValAsDouble();
            state.Color.SetColor(gray, gray, gray, 1);
          }
          else if (extra.N == 3)
          {
            blue = GetNextStackValAsDouble();
            green = GetNextStackValAsDouble();
            red = GetNextStackValAsDouble();
            state.Color.SetColor(red, green, blue, 1);
          }
          else if (extra.N == 4)
          {
            black = GetNextStackValAsDouble();
            yellow = GetNextStackValAsDouble();
            magenta = GetNextStackValAsDouble();
            cyan = GetNextStackValAsDouble();
            ColorHelper.ConvertCMYKtoRGBbyIntensity(cyan, magenta, yellow, black, state.Color);
          }
          else
          {
            throw new NotSupportedException("N value not supported"); // shouldn't this be in the parser
          }
          break;
        case PDF_ColorSpaceFamily.Indexed:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.Pattern:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.Separation:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.DeviceN:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
      }
    }

    public void SetColorAndName(PDFGI_ColorState state)
    {
      // Pattern, Separation, DeviceN and ICCBased colour spaces
      switch (state.Cs.Family)
      {
        case PDF_ColorSpaceFamily.NULL:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.DeviceGray:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.DeviceRGB:
          SetColor(state);
          break;
        case PDF_ColorSpaceFamily.DeviceCMYK:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.CalGray:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.CalRGB:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.Lab:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.ICCBased:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.Indexed:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.Pattern:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.Separation:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
        case PDF_ColorSpaceFamily.DeviceN:
          throw new NotImplementedException("Invalid ColorSpace!");
          break;
      }
    }

    public void VoidDict()
    {
      if (operandTypes.Peek() != OperandType.DICT)
        return;

      operandTypes.Pop();
      int keyNum = arrayLengths.Pop();
      OperandType op;
      for (int i = 0; i < keyNum; i++)
      {
        op = operandTypes.Peek();
        if (op == OperandType.INT)
        {
          operandTypes.Pop();
          intOperands.Pop();
        }
        else if (op == OperandType.DOUBLE)
        {
          operandTypes.Pop();
          realOperands.Pop();
        }
        else if (op == OperandType.STRING)
        {
          operandTypes.Pop();
          stringOperands.Pop();
        }
        else if (op == OperandType.DICT)
          VoidDict();
        else if (op == OperandType.ARRAY)
          VoidArray();
      }
    }

    public void VoidArray()
    {
      if (operandTypes.Peek() != OperandType.ARRAY)
        return;

      operandTypes.Pop();
      int keyNum = arrayLengths.Pop();
      OperandType op;
      for (int i = 0; i < keyNum; i++)
      {
        op = operandTypes.Peek();
        if (op == OperandType.INT)
        {
          operandTypes.Pop();
          intOperands.Pop();
        }
        else if (op == OperandType.DOUBLE)
        {
          operandTypes.Pop();
          realOperands.Pop();
        }
        else if (op == OperandType.STRING)
        {
          operandTypes.Pop();
          stringOperands.Pop();
        }
        else if (op == OperandType.DICT)
          throw new InvalidDataException("Invalid state");
        else if (op == OperandType.ARRAY)
          VoidArray();
      }
    }

    public string PopString()
    {
      OperandType op = operandTypes.Pop();
      Debug.Assert(op == OperandType.STRING);
      return stringOperands.Pop();
    }

    public void StrokePath()
    {
      try
      {
        PDF_RasterShape();
      }
      catch (Exception ex)
      {
#if DEBUG
        if (!_debug) // handle this somehow better
        throw ex;
#endif
      }
      currentPC.Shape = new PSShape();
      _pathLogger.Clear();
    }

    public void m(double x, double y)
    {
      _pathLogger.MoveToLog(x, y);
      currentPC.Shape.MoveTo(x, y);
    }

    public void l(double x, double y)
    {
      _pathLogger.LineToLog(x, y);
      currentPC.Shape.LineTo(x, y);
    }

    // this may produce double CLOSEPATH logs but w/e
    public void h()
    {
      currentPC.Shape.CloseShape();
      if (currentPC.Shape._shapePoints.Count > 1)
      {
        double y = currentPC.Shape._shapePoints[currentPC.Shape._shapePoints.Count - 1];
        double x = currentPC.Shape._shapePoints[currentPC.Shape._shapePoints.Count - 2];
        _pathLogger.MoveToLog(x, y);
      }
    }
  }

}