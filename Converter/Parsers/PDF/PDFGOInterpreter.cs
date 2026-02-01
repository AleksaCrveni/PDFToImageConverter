using Converter.Converters;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.Rasterizers;
using Converter.StaticData;
using System.Buffers;
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
  public ref struct PDFGOInterpreter
  {
    private ReadOnlySpan<byte> _buffer;
    private int _pos = 0;
    private int _readPos = 0;
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
    private GraphicsState currentGS;
    private PDFGI_PathConstruction currentPC;
    private PDFGI_TextObject currentTextObject;
    private Span<byte> _fourByteSlice;
    private PDF_ResourceDict _resourceDict;
    private long tokensParsed = 0; // for debugging
    private double[,] textRenderingMatrix;
    private double[,] reUsableMatrix1;
    private double[,] reUsableMatrix2;
    private IConverter _converter;
    private (int Height, int Width) _targetSize;
    public byte[] _outputBuffer;

    // TODO: maybe NULL check is redundant if we let it throw to end?
    public PDFGOInterpreter(ReadOnlySpan<byte> contentBuffer, ref PDF_ResourceDict resourceDict, ref Span<byte> fourByteSlice, IConverter converter)
    {
      _buffer = contentBuffer;
      intOperands = new Stack<int>();
      realOperands = new Stack<double>();
      operandTypes = new Stack<OperandType>();
      arrayLengths = new Stack<int>();
      stringOperands = new Stack<string>();
      currentTextObject = new PDFGI_TextObject();
      GSS = new Stack<GraphicsState>();
      currentGS = new GraphicsState();
      currentPC = new PDFGI_PathConstruction();
      _resourceDict = resourceDict;
      // should be always 4 bytes
      if (fourByteSlice.Length != 4)
        throw new Exception("Four byte slice must be 4 in length!");
      _fourByteSlice = fourByteSlice;
      textRenderingMatrix = new double[3,3];
      reUsableMatrix1 = new double[3, 3];
      reUsableMatrix2 = new double[3, 3];
      _converter = converter;
      _targetSize = (_converter.GetHeight(), _converter.GetWidth());
      _outputBuffer = new byte[_targetSize.Height * _targetSize.Width];
    }
    public void InitGS()
    {
      currentGS = new GraphicsState();
      currentGS.CTM = MyMath.RealIdentityMatrix3x3();
    }
    public void ConvertToPixelData()
    {
      InitGS();

      uint val = ReadNext();
      // TODO: instead of string, we can return hexadecimals or numbers
      // since they are all less than 4char strings
      // TODO: move these to constants file
      // TODO: load all as double or float and then cast to int if needed?
      PDFGI_Point mp;
      string literal;
      tokensParsed++;
      // val != because last token value might not be 0 so it won't account it
      while (_char != PDFConstants.NULL || (_char == PDFConstants.NULL && val != 0))
      {
        switch (val)
        {
          #region gss&sgs
          case 0x77: // w
            currentGS.LineWidth = GetNextStackValAsDouble();
            break;
          case 0x4a: // J
                     // valid values 0, 1, 2
            currentGS.LineCap = GetNextStackValAsInt();
            break;
          case 0x6a: // j
                     // valid values 0, 1, 2
            currentGS.LineJoin = GetNextStackValAsInt();
            break;
          case 0x4d: // M
            currentGS.MiterLimit = GetNextStackValAsDouble();
            break;
          case 0x64: // d
            PDFGI_DashPattern dashPattern = new PDFGI_DashPattern();
            int phase = GetNextStackValAsInt();
            int[] dashArr = new int[arrayLengths.Pop()];
            for (int dpIndex = dashArr.Length; dpIndex >= 0; dpIndex--)
            {
              dashArr[dpIndex] = GetNextStackValAsInt();
            }

            currentGS.DashPattern = dashPattern;
            break;
          case 0x6972: // ri
            operandTypes.Pop();
            string renderingIntentString = stringOperands.Pop();

            bool valid = Enum.TryParse(renderingIntentString, out PDFGI_RenderingIntent ri);
            if (!valid)
              ri = PDFGI_RenderingIntent.Null;

            currentGS.RenderingIntent = ri;
            break;
          case 0x69: // i
            currentGS.Flatness = GetNextStackValAsDouble();
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
            GSS.Push(currentGS.DeepCopy());
            break;
          case 0x51: // Q
            currentGS = GSS.Pop().DeepCopy();
            break;
          case 0x6d63: // cm
            // a b c e d f - real numbers but can be saved as ints
            double f = GetNextStackValAsDouble();
            double d = GetNextStackValAsDouble();
            double e = GetNextStackValAsDouble();
            double c = GetNextStackValAsDouble();
            double b = GetNextStackValAsDouble();
            double a = GetNextStackValAsDouble();
            UpdateCTM(a, b, c, e, d, f);
            break;
          #endregion gss&sgs
          #region pathConstruction
          case 0x6d: // m
            currentPC.PathConstructs.Clear();
            mp = new PDFGI_Point();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PDFGI_PathConstructOperator.m, mp));
            break;
          case 0x6c: // l
            mp = new PDFGI_Point();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PDFGI_PathConstructOperator.l, mp));
            break;
          case 0x63: // c
            mp = new PDFGI_Point();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y2 = GetNextStackValAsInt();
            mp.X2 = GetNextStackValAsInt();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PDFGI_PathConstructOperator.c, mp));
            break;
          case 0x76: // v
            mp = new PDFGI_Point();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y2 = GetNextStackValAsInt();
            mp.X2 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PDFGI_PathConstructOperator.v, mp));
            break;
          case 0x79: // y
            mp = new PDFGI_Point();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PDFGI_PathConstructOperator.y, mp));
            break;
          case 0x68: // h
            mp = new PDFGI_Point();
            currentPC.PathConstructs.Add((PDFGI_PathConstructOperator.h, mp));
            break;
          case 0x6572: // re
            mp = new PDFGI_Point();

            // use this as width
            mp.X3 = GetNextStackValAsInt();
            /// use this as height
            mp.Y3 = GetNextStackValAsInt();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PDFGI_PathConstructOperator.re, mp));
            break;
          #endregion pathConstruction
          #region pathPainting
          case 0x53: // S
            throw new NotImplementedException("Operator not i implemented");
          case 0x73: // s
            throw new NotImplementedException("Operator not i implemented");
          case 0x66: // f

            currentPC.PathConstructs.Clear();
            currentPC.EvenOddClippingPath = false;
            currentPC.NonZeroClippingPath = false;
            break;
          case 0x46: // F
            throw new NotImplementedException("Operator not i implemented");
          case 0x2a66: // f*
            throw new NotImplementedException("Operator not i implemented");
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
            currentPC.PathConstructs.Add((PDFGI_PathConstructOperator.n, mp));       // CHECK CLIPPING PATH SECTION
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
            currentTextObject.Active = true;
            currentTextObject.InitMatrixes();
            break;
          case 0x5445: // ET
            currentTextObject.Active = false;
            break;
          #endregion textObjects
          #region textState
          case 0x6354: // Tc
            currentTextObject.Tc = GetNextStackValAsDouble();
            break;
          case 0x7754: // Tw
            currentTextObject.Tw = GetNextStackValAsDouble();
            break;
          case 0x7a54: // Tz
            currentTextObject.Th = GetNextStackValAsDouble() / 100;
            break;
          case 0x4c54: // TL
            currentTextObject.Tl = GetNextStackValAsDouble();
            break;
          case 0x6654: // Tf
            currentTextObject.FontScaleFactor = GetNextStackValAsDouble();
            currentTextObject.FontRef = GetNextStackValAsString();
            break;
          case 0x7254: // Tr
            currentTextObject.TMode = GetNextStackValAsInt();
            break;
          case 0x7354: // Ts
            currentTextObject.TRise = GetNextStackValAsDouble();
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

            MyMath.MultiplyMatrixes3x3(reUsableMatrix1, currentTextObject.TextLineMatrix, currentTextObject.TextMatrix);
            MyMath.CopyMatrix3x3Data(currentTextObject.TextLineMatrix, currentTextObject.TextMatrix);
            break;
          case 0x4454: // TD
            ty = GetNextStackValAsDouble();
            tx = GetNextStackValAsDouble();
            currentTextObject.TL = -ty;

            reUsableMatrix1[0, 0] = 1;
            reUsableMatrix1[0, 1] = 0;
            reUsableMatrix1[0, 2] = 0;

            reUsableMatrix1[1, 0] = 0;
            reUsableMatrix1[1, 1] = 1;
            reUsableMatrix1[1, 2] = 0;

            reUsableMatrix1[2, 0] = tx;
            reUsableMatrix1[2, 1] = ty;
            reUsableMatrix1[2, 2] = 1;

            MyMath.MultiplyMatrixes3x3(reUsableMatrix1, currentTextObject.TextLineMatrix, currentTextObject.TextMatrix);
            MyMath.CopyMatrix3x3Data(currentTextObject.TextLineMatrix, currentTextObject.TextMatrix);
            break;
          case 0x6d54: // Tm
            double tmOp = GetNextStackValAsDouble();
            currentTextObject.TextMatrix[2, 1] = tmOp;
            currentTextObject.TextLineMatrix[2, 1] = tmOp;
            // e
            tmOp = GetNextStackValAsDouble();
            currentTextObject.TextMatrix[2, 0] = tmOp;
            currentTextObject.TextLineMatrix[2, 0] = tmOp;
            // d
            tmOp = GetNextStackValAsDouble();
            currentTextObject.TextMatrix[1, 1] = tmOp;
            currentTextObject.TextLineMatrix[1, 1] = tmOp;
            // c
            tmOp = GetNextStackValAsDouble();
            currentTextObject.TextMatrix[1, 0] = tmOp;
            currentTextObject.TextLineMatrix[1, 0] = tmOp;
            // b
            tmOp = GetNextStackValAsDouble();
            currentTextObject.TextMatrix[0, 1] = tmOp;
            currentTextObject.TextLineMatrix[0, 1] = tmOp;
            // a
            tmOp = GetNextStackValAsDouble();
            currentTextObject.TextMatrix[0, 0] = tmOp;
            currentTextObject.TextLineMatrix[0, 0] = tmOp;
            break;
          case 0x2a54: // T*
                       // text positioning
            break;
          #endregion textPositioning;
          #region textShowing
          case 0x6a54: // Tj
            literal = GetNextStackValAsString();
            PDFGI_DrawState state = new PDFGI_DrawState();
            state.CTM = currentGS.CTM;
            state.TextObject = currentTextObject;
            state.TextRenderingMatrix = textRenderingMatrix;
            PDF_DrawText(currentTextObject.FontRef, literal, state);
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
              // TODO: fix this, just a workaround
              state = new PDFGI_DrawState();
              state.CTM = currentGS.CTM;
              state.TextObject = currentTextObject;
              state.TextRenderingMatrix = textRenderingMatrix;
              // read in proper order
              for (int i = literalsList.Count - 1; i >= 0; i--)
              {
                PDF_DrawText(currentTextObject.FontRef, literalsList[i].Literal, state, literalsList[i].PosCorrection);
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
            throw new NotImplementedException("Operator not i implemented");
          case 0x7363: // cs
            PDF_ColorSpaceInfo info = new PDF_ColorSpaceInfo();
            bool found = false;
            string key = GetNextStackValAsString();
            for (int i =0; i < _resourceDict.ColorSpace.Count; i++)
            {
              if (key == _resourceDict.ColorSpace[i].Key)
              {
                // DO 0 for now, but check if there can be multiple and which to choose then!
                info = _resourceDict.ColorSpace[i].ColorSpaceInfo[0];
                found = true;
                break;
              }
            }
            if (!found)
              throw new InvalidDataException("Missing color space information!");
            currentGS.ColorSpaceInfo = info;
            break;
          case 0x4353: // SC
            throw new NotImplementedException("Operator not i implemented");
          case 0x4e4353: // SCN
            throw new NotImplementedException("Operator not i implemented");
          case 0x6373: // sc
            int N = currentGS.ColorSpaceInfo.Dict.N;
            for (int i =0; i < N; i++)
            {
              GetNextStackValAsInt();
            }
            break;
          case 0x6e6373: // scn
            throw new NotImplementedException("Operator not i implemented");
          case 0x47: // G
            GetNextStackValAsDouble();
            break;
          case 0x67: // g
            GetNextStackValAsDouble();
            break;
          case 0x4752: // RG
            throw new NotImplementedException("Operator not i implemented");
          case 0x6772: // rg
            throw new NotImplementedException("Operator not i implemented");
          case 0x4b: // K
            throw new NotImplementedException("Operator not i implemented");
          case 0x6b: // k
                     // color
            throw new NotImplementedException("Operator not i implemented");
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
            throw new NotImplementedException("Operator not i implemented");
            // XObject
            break;
          case 0x504d: // MP
            throw new NotImplementedException("Operator not i implemented");
          case 0x5044: // DP
            throw new NotImplementedException("Operator not i implemented");
          case 0x434d42: // BMC
            throw new NotImplementedException("Operator not i implemented");
          case 0x434442: // BDC
            throw new NotImplementedException("Operator not i implemented");
          case 0x434d45: // EMC
            throw new NotImplementedException("Operator not i implemented");
            // Marked content
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
    private uint ReadNext()
    {
      SkipWhiteSpace();
     

      if (IsCurrentCharPartOfOperator() && _char != PDFConstants.NULL)
      {
        int startPos = _pos;
        while (!IsCurrentCharPDFWhitespaceOrNewLine())
          ReadChar();
        _fourByteSlice.Fill(0);
        for (int i = 0; i < _pos - startPos; i++)
          _fourByteSlice[i] = _buffer[startPos + i];
        return BitConverter.ToUInt32(_fourByteSlice);
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

      // array
      if (_char == '[')
      {
        ReadChar();
        int count = 0;
        SkipWhiteSpace();
        while (_char != ']' && _char != PDFConstants.NULL)
        {
          if (IsCurrentCharDigit() || _char == '-')
            GetNumber();
          else if (_char == '/')
            GetName();
          else if (_char == '(')
            GetStringLiteral();

          count++;
          SkipWhiteSpace();
        }
        ReadChar(); // move off ']'
        operandTypes.Push(OperandType.ARRAY);
        arrayLengths.Push(count);
        return 0;
      }

      return 0;
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
      while (!IsCurrentCharPDFWhitespaceOrNewLine() && _char != PDFConstants.NULL)
      {
        ReadChar();
      }

     stringOperands.Push(Encoding.Default.GetString(_buffer.Slice(startPos, _pos - startPos)));
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

        sb.Append((char) c);
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
    public void ComputeTextRenderingMatrix()
    {
      // Set initial value to first matrix
      reUsableMatrix1[0, 0] = currentTextObject.FontScaleFactor * currentTextObject.Th;
      reUsableMatrix1[0, 1] = 0;
      reUsableMatrix1[0, 2] = 0;
      reUsableMatrix1[1, 0] = 0;
      reUsableMatrix1[1, 1] = currentTextObject.FontScaleFactor;
      reUsableMatrix1[1, 2] = 0;
      reUsableMatrix1[2, 0] = 0;
      reUsableMatrix1[2, 1] = currentTextObject.TRise;
      reUsableMatrix1[2, 2] = 1;

      MyMath.MultiplyMatrixes3x3(reUsableMatrix1, currentTextObject.TextMatrix, reUsableMatrix2);
      MyMath.MultiplyMatrixes3x3(reUsableMatrix2, currentGS.CTM, textRenderingMatrix);
    }
    // Should this be removed?
    public void UpdateTextMatrixAfterGlyphRender(int charWidth, int charHeight, int posAdjustment)
    {
      double tx = ((charWidth - posAdjustment / 1000) * currentTextObject.FontScaleFactor + currentTextObject.Tc + currentTextObject.Tw) * (currentTextObject.Th);
      double ty = (charHeight - posAdjustment / 1000) * currentTextObject.FontScaleFactor + currentTextObject.Tc + currentTextObject.Tw;
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
      MyMath.MultiplyMatrixes3x3(reUsableMatrix1, currentTextObject.TextMatrix, reUsableMatrix2);

      // asign value to textMatrix
      currentTextObject.TextMatrix[0, 0] = reUsableMatrix2[0, 0];
      currentTextObject.TextMatrix[0, 1] = reUsableMatrix2[0, 1];
      currentTextObject.TextMatrix[0, 2] = reUsableMatrix2[0, 2];

      currentTextObject.TextMatrix[1, 0] = reUsableMatrix2[1, 0];
      currentTextObject.TextMatrix[1, 1] = reUsableMatrix2[1, 1];
      currentTextObject.TextMatrix[1, 2] = reUsableMatrix2[1, 2];

      currentTextObject.TextMatrix[2, 0] = reUsableMatrix2[2, 0];
      currentTextObject.TextMatrix[2, 1] = reUsableMatrix2[2, 1];
      currentTextObject.TextMatrix[2, 2] = reUsableMatrix2[2, 2];

    }

    public void PDF_DrawText(string font, string textToWrite, PDFGI_DrawState state, int positionAdjustment = 0)
    {
      // TODO: just do this once at opettor and assign ref to glolbal obj
      // Get right rasterizer
      PDF_FontData fd = GetFontDataFromKey(currentTextObject.FontRef);
      IRasterizer activeParser = fd.Rasterizer;
      double[] activeWidths = fd.FontInfo.Widths;

      char c;
      int glyphIndex;
      int baseline = 0;
      GlyphInfo glyphInfo = new GlyphInfo(); // make global??
      // Account for position adjustment
      state.TextObject.TextMatrix[2, 0] -= (positionAdjustment / 1000f) * state.TextObject.TextMatrix[0, 0] * state.TextObject.FontScaleFactor;

      for (int i = 0; i < textToWrite.Length; i++)
      {
        c = textToWrite[i];
        activeParser.SetDefaultGlyphInfoValues(ref glyphInfo);
        // TODO: use this instead of c, FIX 
        activeParser.GetGlyphInfo(c, ref glyphInfo);

        ComputeTextRenderingMatrix(state.TextObject, state.CTM, ref state.TextRenderingMatrix);

        // rounding makes it look a bit better?
        int X = (int)MathF.Round((float)state.TextRenderingMatrix[2, 0]);
        // because origin is bottom-left we have do bitmapHeight - , to get position on the top
        int Y = _targetSize.Height - (int)(state.TextRenderingMatrix[2, 1]);

        #region width calculation

        int idx = (int)c - fd.FontInfo.FirstChar;
        float width = 0;
        if (idx < activeWidths.Length)
          width = (float)activeWidths[idx] / 1000f;
        else
          width = fd.FontInfo.FontDescriptor.MissingWidth / 1000f;
        #endregion

        (float scaleX, float scaleY) s = activeParser.GetScale(glyphInfo.Index, state.TextRenderingMatrix, width);

        #region asserts
        Debug.Assert(X > 0, $"X is negative at index {i}. Lit: {textToWrite}");
        Debug.Assert(Y > 0, $"Y is negative at index {i}. Lit: {textToWrite}");
        Debug.Assert(X < _targetSize.Width, $"X must be within bounds.X: {X} - Width: {_targetSize.Width}. Lit: {textToWrite}");
        Debug.Assert(Y < _targetSize.Height, $"Y must be within bounds.Y: {Y} - Height: {_targetSize.Height}. Lit: {textToWrite}");
        Debug.Assert(s.scaleX > 0, $"Scale factor X must be higher than 0! sfX: {s.scaleX}. Lit: {textToWrite}. Ind : {i}");
        Debug.Assert(s.scaleY > 0, $"Scale factor Y must be higher than 0! sfY: {s.scaleY}. Lit: {textToWrite}.Ind : {i}");
        #endregion asserts

        int ascent = (int)Math.Round(fd.FontInfo.FontDescriptor.Ascent * s.scaleY);
        int descent = (int)Math.Round(fd.FontInfo.FontDescriptor.Descent * s.scaleY);

        #region glyph metrics

        int c_x0 = 0;
        int c_y0 = 0;
        int c_x1 = 0;
        int c_y1 = 0;
        activeParser.GetGlyphBoundingBox(ref glyphInfo, s.scaleX, s.scaleY, ref c_x0, ref c_y0, ref c_x1, ref c_y1);
        // char height - different than bounding box height
        int y = Y + c_y0;
        int glyphWidth = c_x1 - c_x0; // I think that this should be replaced from value in Widths array
        int glyphHeight = c_y1 - c_y0;

        #endregion

        int byteOffset = X + (y * _targetSize.Width);
        int shiftX = 0;
        int shiftY = 0;

        activeParser.RasterizeGlyph(_outputBuffer, byteOffset, glyphWidth, glyphHeight, _targetSize.Width, s.scaleX, s.scaleY, shiftX, shiftY, ref glyphInfo);

        #region Advance
        double advanceX = width * state.TextObject.FontScaleFactor + state.TextObject.Tc;
        double advanceY = 0 + state.TextObject.FontScaleFactor; // This wont work for vertical fonts

        if (c == ' ')
          advanceX += state.TextObject.Tw;
        advanceX *= state.TextObject.Th;

        // TODO: this really depends on what type of CTM it is. i.e is there shear, transaltion, rotation etc
        // I should detect this and save state somewhere
        // for now just support translate and scale
        // NOTE: actually I think I can just multiply matrix, and this is done to avoid matrix multiplciation
        state.TextObject.TextMatrix[2, 0] = advanceX * state.TextRenderingMatrix[0, 0] + state.TextObject.TextMatrix[2, 0];
        state.TextObject.TextMatrix[2, 1] = 0 * state.TextObject.TextMatrix[1, 1] + state.TextObject.TextMatrix[2, 1];
        #endregion
      }
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

      MyMath.MultiplyMatrixes3x3(reUsableMatrix1, currentGS.CTM, reUsableMatrix2);

      currentGS.CTM[0, 0] = reUsableMatrix2[0, 0];
      currentGS.CTM[0, 1] = reUsableMatrix2[0, 1];
      currentGS.CTM[0, 2] = reUsableMatrix2[0, 2];

      currentGS.CTM[1, 0] = reUsableMatrix2[1, 0];
      currentGS.CTM[1, 1] = reUsableMatrix2[1, 1];
      currentGS.CTM[1, 2] = reUsableMatrix2[1, 2];

      currentGS.CTM[2, 0] = reUsableMatrix2[2, 0];
      currentGS.CTM[2, 1] = reUsableMatrix2[2, 1];
      currentGS.CTM[2, 2] = reUsableMatrix2[2, 2];
    }

    // TODO: optimize
    public void ComputeTextRenderingMatrix(PDFGI_TextObject currentTextObject, double[,] CTM, ref double[,] textRenderingMatrix)
    {
      // Set initial value to first matrix
      double[,] identity = new double[3, 3];
      identity[0, 0] = currentTextObject.FontScaleFactor * currentTextObject.Th;
      identity[0, 1] = 0;
      identity[0, 2] = 0;
      identity[1, 0] = 0;
      identity[1, 1] = currentTextObject.FontScaleFactor;
      identity[1, 2] = 0;
      identity[2, 0] = 0;
      identity[2, 1] = currentTextObject.TRise;
      identity[2, 2] = 1;

      double[,] mid = new double[3, 3];
      MyMath.MultiplyMatrixes3x3(identity, currentTextObject.TextMatrix, mid);
      MyMath.MultiplyMatrixes3x3(mid, CTM, textRenderingMatrix);
    }

    private PDF_FontData GetFontDataFromKey(string searchKey)
    {
      foreach (PDF_FontData fd in _resourceDict.Font)
        if (fd.Key == searchKey)
          return fd;

      return new PDF_FontData();
    }
  }

  public enum OperandType
  {
    INT,
    DOUBLE,
    STRING,
    ARRAY,
    INSTRUCTION
  }
}
