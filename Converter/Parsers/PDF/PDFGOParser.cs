using Converter.FileStructures;
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
  /// </summary>
  public ref struct PDFGOParser
  {
    private ReadOnlySpan<byte> _buffer;
    private int _pos = 0;
    private int _readPos = 0;
    private byte _char; // current char
    private Stack<int> intOperands;
    private Stack<double> realOperands;
    private Stack<string> stringOperands;
    private Stack<OperandType> operandTypes;
    private Stack<GraphicsState> GSS;
    public PDFGOParser(ReadOnlySpan<byte> buffer)
    {
      _buffer = buffer;
      intOperands = new Stack<int>();
      realOperands = new Stack<double>();
      operandTypes = new Stack<OperandType>();
      GSS = new Stack<GraphicsState>();
    }

    public PDFGOParser(Span<byte> buffer)
    {
      _buffer = buffer;
    }

    public void ParseAll()
    {
      uint val = ReadNextString();
      // TODO: instead of string, we can return hexadecimals or numbers
      // since they are all less than 4char strings
      // TODO: move these to constants file
      switch (val)
      {
        case 0x77: // w
        case 0x4a: // J
        case 0x6a: // j
        case 0x4d: // M
        case 0x64: // d
        case 0x7269: // ri
        case 0x69: // i
        case 0x7173: // qs
          // graphics state
          break;
        case 0x71: // q
        case 0x51: // Q
        case 0x636d: // cm
          // special graphics state
          break;
        case 0x6d: // m
        case 0x49:  // I
        case 0x63: // c
        case 0x76: // v
        case 0x79: // y
        case 0x68: // h
        case 0x7265: // re
          // path construction
          break;
        case 0x53: // S
        case 0x73: // s
        case 0x66: // f
        case 0x46: // F
        case 0x662a: // f*
        case 0x42: // B
        case 0x422a: // B*
        case 0x62: // b
        case 0x622a: // b*
        case 0x6e: // n
          // path painting
          break;
        case 0x57: // W
        case 0x572a: // W*
          // clipping paths
          break;
        case 0x4254: // BT
        case 0x4554: // ET
          // text objects
          break;
        case 0x5463: // Tc
        case 0x5477: // Tw
        case 0x547a: // Tz
        case 0x544c: // TL
        case 0x5466: // Tf
        case 0x5472: // Tr
        case 0x5473: // Ts
          // text state
          break;
        case 0x5464: // Td
        case 0x5444: // TD
        case 0x546d: // Tm
        case 0x542a: // T*
          // text positioning
          break;
        case 0x546a: // Tj
        case 0x544a: // TJ
        case 0x27: // '
        case 0x22: // "
          // text showing
          break;
        case 0x6430: // d0
        case 0x6431: // d1
          // type 3 fonts
          break;
        case 0x4353: // CS
        case 0x6373: // cs
        case 0x5343: // SC
        case 0x53434e: // SCN
        case 0x7363: // sc
        case 0x73636e: // scn
        case 0x47: // G
        case 0x67: // g
        case 0x5247: // RG
        case 0x7267: // rg
        case 0x4b: // K
        case 0x6b: // k
          // color
          break;
        case 0x7368: // sh
          // shading patterns
          break;
        case 0x7249: // BI
        case 0x4944: // ID
        case 0x4549: // EI
          // inline images
          break;
        case 0x446f: // Do
          // XObject
          break;
        case 0x4d50: // MP
        case 0x4450: // DP
        case 0x424d43: // BMC
        case 0x424443: // BDC
        case 0x454d43: // EMC
          // Marked content
          break;
        case 0x4258: // BX
        case 0x4558: // EX
          // Compatibility
          break;
        default:
          break;
      }
    }
    /// <summary>
    /// Read to next byte and see if its:
    ///  digit -> can be number or real
    ///  [ -> array
    ///  / -> name
    ///  ( -> string literal
    ///  other char -> most likely operator
    ///  if its operator return uint value of it otherwise return 0
    /// </summary>
    /// <returns></returns>
    private uint ReadNext()
    {
      SkipWhiteSpace();
      int startPos = _pos;
      while (!IsCurrentCharPDFWhitespaceOrNewLine())
        ReadChar();

      return 0;
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

    private bool IsCurrentCharPDFWhitespaceOrNewLine()
    {
      return _char == PDFConstants.SP || _char == PDFConstants.LF || _char == PDFConstants.CR;
    }
    private void SkipWhiteSpace()
    {
      while (IsCurrentCharPDFWhitespaceOrNewLine())
        ReadChar();
    }
  }

  public enum OperandType
  {
    INT,
    DOUBLE,
    STRING,
  }
}
