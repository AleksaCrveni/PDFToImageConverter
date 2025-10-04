using Converter.FileStructures;
using Converter.FIleStructures;
using Converter.Parsers.Fonts;
using Converter.Writers.TIFF;
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
    private PathConstruction currentPC;
    private TextObject currentTextObject;
    private List<FontData> _fontInfo;
    private ITIFFWriter _writer;
    private Span<byte> _fourByteSlice;
    // TODO: maybe NULL check is redundant if we let it throw to end?
    public PDFGOInterpreter(ReadOnlySpan<byte> buffer, List<FontData> fontInfo, ITIFFWriter tiffWriter, ref Span<byte> fourByteSlice)
    {
      _buffer = buffer;
      intOperands = new Stack<int>(100);
      realOperands = new Stack<double>(100);
      operandTypes = new Stack<OperandType>();
      arrayLengths = new Stack<int>();
      stringOperands = new Stack<string>();
      currentTextObject = new TextObject();
      GSS = new Stack<GraphicsState>();
      currentGS = new GraphicsState();
      currentPC = new PathConstruction();
      _fontInfo = fontInfo;
      _writer = tiffWriter;
      // should be always 4 bytes
      if (fourByteSlice.Length != 4)
        throw new Exception("Four byte slice must be 4 in length!");
      _fourByteSlice = fourByteSlice;
    }

    public void ParseAll()
    {
      currentGS = new GraphicsState();

      uint val = ReadNext();
      // TODO: instead of string, we can return hexadecimals or numbers
      // since they are all less than 4char strings
      // TODO: move these to constants file
      // TODO: load all as double or float and then cast to int if needed?
      MyPoint mp;
      string literal;
      while (_char != PDFConstants.NULL)
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
            DashPattern dashPattern = new DashPattern();
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

            bool valid = Enum.TryParse(renderingIntentString, out RenderingIntent ri);
            if (!valid)
              ri = RenderingIntent.Null;

            currentGS.RenderingIntent = ri;
            break;
          case 0x69: // i
            currentGS.Flatness = GetNextStackValAsDouble();
            break;
          case 0x7371: // qs
                       // Name of gs paramter dict that is in ExtGState subdict in current resorouceDict
                       // do it later
            throw new Exception("Implement dictname (gs)");
            break;

          // special graphics states
          case 0x71: // q
            GSS.Push(currentGS);
            break;
          case 0x51: // Q
            currentGS = GSS.Pop();
            break;
          case 0x6d63: // cm
                       // a b c e d f - real numbers but can be saved as ints
            CTM newCtm = new CTM();
            // get it as reverse since its stack

            newCtm.YLen = GetNextStackValAsDouble();
            newCtm.XxLen = GetNextStackValAsDouble();
            newCtm.YOrientation = GetNextStackValAsDouble();
            newCtm.XOrientation = GetNextStackValAsDouble();
            newCtm.YLocation = GetNextStackValAsDouble();
            newCtm.XLocation = GetNextStackValAsDouble();

            currentGS.CTM = newCtm;
            break;
          #endregion gss&sgs
          #region pathConstruction
          case 0x6d: // m
            currentPC.PathConstructs.Clear();
            mp = new MyPoint();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PathConstructOperator.m, mp));
            break;
          case 0x6c: // l
            mp = new MyPoint();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PathConstructOperator.l, mp));
            break;
          case 0x63: // c
            mp = new MyPoint();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y2 = GetNextStackValAsInt();
            mp.X2 = GetNextStackValAsInt();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PathConstructOperator.c, mp));
            break;
          case 0x76: // v
            mp = new MyPoint();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y2 = GetNextStackValAsInt();
            mp.X2 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PathConstructOperator.v, mp));
            break;
          case 0x79: // y
            mp = new MyPoint();
            mp.Y3 = GetNextStackValAsInt();
            mp.X3 = GetNextStackValAsInt();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PathConstructOperator.y, mp));
            break;
          case 0x68: // h
            mp = new MyPoint();
            currentPC.PathConstructs.Add((PathConstructOperator.h, mp));
            break;
          case 0x6572: // re
            mp = new MyPoint();

            // use this as width
            mp.X3 = GetNextStackValAsInt();
            /// use this as height
            mp.Y3 = GetNextStackValAsInt();
            mp.Y1 = GetNextStackValAsInt();
            mp.X1 = GetNextStackValAsInt();
            currentPC.PathConstructs.Add((PathConstructOperator.re, mp));
            break;
          #endregion pathConstruction
          #region pathPainting
          case 0x53: // S
          case 0x73: // s
          case 0x66: // f

            currentPC.PathConstructs.Clear();
            currentPC.EvenOddClippingPath = false;
            currentPC.NonZeroClippingPath = false;
            break;
          case 0x46: // F
          case 0x2a66: // f*
          case 0x42: // B
          case 0x2a42: // B*
          case 0x62: // b
          case 0x2a62: // b*
          case 0x6e: // n
                     // CHECK CLIPPING PATH SECTION
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
            currentTextObject.Active = false;
            currentTextObject.InitMatrixes();
            break;
          case 0x5445: // ET
            currentTextObject.Active = true;
            break;
          #endregion textObjects
          #region textState
          case 0x6354: // Tc
          case 0x7754: // Tw
          case 0x7a54: // Tz
          case 0x4c54: // TL
          case 0x6654: // Tf
            currentTextObject.FontScaleFactor = GetNextStackValAsDouble();
            currentTextObject.FontRef = GetNextStackValAsString();
            break;
          case 0x7254: // Tr
          case 0x7354: // Ts
            break;
          #endregion textState
          #region textPositioning;
          case 0x6454: // Td
          case 0x4454: // TD
          case 0x6d54: // Tm
            double tmOp = GetNextStackValAsDouble();
            currentTextObject.TextMatrix[2, 1] = tmOp;
            currentTextObject.TextLineMatrix[2, 1] = tmOp;
            // e
            tmOp = GetNextStackValAsDouble();
            currentTextObject.TextMatrix[2, 0] = tmOp;
            currentTextObject.TextLineMatrix[1, 0] = tmOp;
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
            break;
          case 0x4a54: // TJ
            break;
          case 0x27:   // '
          case 0x22:   // "
                       // text showing
            break;
          #endregion textShowing
          case 0x3064: // d0
          case 0x3164: // d1
                       // type 3 fonts
            break;
          case 0x5343: // CS
          case 0x7363: // cs
            GetNextStackValAsString();
            break;
          case 0x4353: // SC
          case 0x4e4353: // SCN
          case 0x6373: // sc
          case 0x6e6373: // scn
          case 0x47: // G
          case 0x67: // g
          case 0x4752: // RG
          case 0x6772: // rg
          case 0x4b: // K
          case 0x6b: // k
                     // color
            break;
          case 0x6873: // sh
                       // shading patterns
            break;
          case 0x4972: // BI
          case 0x4449: // ID
          case 0x4945: // EI
                       // inline images
            break;
          case 0x6f44: // Do
                       // XObject
            break;
          case 0x504d: // MP
          case 0x5044: // DP
          case 0x434d42: // BMC
          case 0x434442: // BDC
          case 0x434d45: // EMC
                         // Marked content
            break;
          case 0x5842: // BX
          case 0x5845: // EX
                       // Compatibility
            break;
          default:
            break;
        }

        val = ReadNext();
      }
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
        int count = 0;
        while (_char != ']' && _char != PDFConstants.NULL)
        {
          SkipWhiteSpace();

          if (IsCurrentCharDigit() || _char == '-')
            GetNumber();

          // is a name
          if (_char == '/')
            GetName();

          // string literal
          if (_char == '(')
            GetStringLiteral();

          count++;
        }
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
    private void GetStringLiteral()
    {
      ReadChar();
      int startPos = _pos;
      while (_char != ')' || _char == PDFConstants.NULL)
      {
        ReadChar();
      }

      stringOperands.Push(Encoding.Default.GetString(_buffer.Slice(startPos, _pos - startPos)));
      operandTypes.Push(OperandType.STRING);
      ReadChar(); // skip ')'
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
  }

  public enum OperandType
  {
    INT,
    DOUBLE,
    STRING,
    ARRAY,
  }
}
