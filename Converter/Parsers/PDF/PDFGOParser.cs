using System.Text;

namespace Converter.Parsers.PDF
{
  /// <summary>
  /// PDF Graphics Objects Parser
  /// Parses PDF Content stream that contains many operators
  /// </summary>
  public ref struct PDFGOParser
  {
    private ReadOnlySpan<byte> _buffer;
    private int _pos = 0;
    private int _readPos = 0;
    private byte _char; // current char
    public PDFGOParser(ReadOnlySpan<byte> buffer)
    {
      _buffer = buffer;
    }

    public PDFGOParser(Span<byte> buffer)
    {
      _buffer = buffer;
    }

    public void ParseAll()
    {
      string val = ReadNextString();
      // TODO: instead of string, we can return hexadecimals or numbers
      // since they are all less than 4char strings
      switch (val)
      {
        case "w":
        case "J":
        case "j":
        case "M":
        case "d":
        case "ri":
        case "i":
        case "qs":
          // graphics state
          break;
        case "q":
        case "Q":
        case "cm":
          // special graphics state
          break;
        case "m":
        case "I":
        case "c":
        case "v":
        case "y":
        case "h":
        case "re":
          // path construction
          break;
        case "S":
        case "s":
        case "f":
        case "F":
        case "f*":
        case "B":
        case "B*":
        case "b":
        case "b*":
        case "n":
          // path painting
          break;
        case "W":
        case "W*":
          break;
        case "BT":
        case "ET":
          // text objects
          break;
        case "Tc":
        case "Tw":
        case "Tz":
        case "TL":
        case "Tf":
        case "Tr":
        case "Ts":
          // text state
          break;
        case "Td":
        case "TD":
        case "Tm":
        case "T*":
          // text positioning
          break;
        case "Tj":
        case "TJ":
        case "'":
        case "\"":
          // text showing
          break;
        case "d0":
        case "d1":
          // type 3 fonts
          break;
        case "CS":
        case "cs":
        case "SC":
        case "SCN":
        case "sc":
        case "scn":
        case "G":
        case "g":
        case "RG":
        case "rg":
        case "K":
        case "k":
          // color
          break;
        case "sh":
          // shading patterns
          break;
        case "BI":
        case "ID":
        case "EI":
          // inline images
          break;
        case "Do":
          // XObject
          break;
        case "MP":
        case "DP":
        case "BMC":
        case "BDC":
        case "EMC":
          // Marked content
          break;
        case "BX":
        case "EX":
          // Compatibility
          break;
        default:
          break;
      }
    }

    private string ReadNextString()
    {
      SkipWhiteSpace();
      int startPos = _pos;
      while (!IsCurrentCharPDFWhitespaceOrNewLine())
        ReadChar();

      return Encoding.UTF8.GetString(_buffer.Slice(startPos, _pos - startPos));
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
}
