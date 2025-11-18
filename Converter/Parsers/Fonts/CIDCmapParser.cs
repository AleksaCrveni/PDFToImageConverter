using Converter.FileStructures.PDF;
using System.Globalization;
using System.Text;

namespace Converter.Parsers.Fonts
{
  public ref struct CIDCmapParserHelper
  {
    public ReadOnlySpan<byte> _buffer;
    private string _lastWord;
    private int _position;
    private int _readPosition;
    private byte _char = 0x1b;
    public CIDCmapParserHelper(ref ReadOnlySpan<byte> buffer)
    {
      _buffer = buffer;
    }

    // TODO: Optimize this
    public PDF_CIDCMAP Parse()
    {
      PDF_CIDCMAP cmap = new PDF_CIDCMAP();
      string token = GetNextToken();
      while (token != "endcmap" && token != string.Empty)
      {
        switch (token)
        {
          case "begincodespacerange":
            //ParseCodeSpaceRange(cmap); I dont care about this right now I think
            // From spec it just says that it should ALWAYS be <0000> <FFFF> but it has no use (unless im mistaken)
            break;
          case "beginbfchar":
            ParseBfChars(cmap);
            break;
          case "beginbfrange":
            ParseBfCharsRange(cmap);
            break;
          default:
            break;
        }
        _lastWord = token;
        token = GetNextToken();
        
      }
      return cmap;
    }

    private void ParseBfCharsRange(PDF_CIDCMAP cmap)
    {
      int n = Convert.ToInt32(_lastWord);
      SkipWhiteSpaceAndNewline();
      ushort CIDStart = 0;
      ushort CIDEnd = 0;
      uint code = 0;
      Rune r;
      for (int i = 0; i < n; i++)
      {
        CIDStart = ReadUShortBE();
        CIDEnd = ReadUShortBE();
        SkipWhiteSpaceOnly();
        if (_char == '<')
        {
          code = GetNextCodePoint();
          for (ushort j = CIDStart; j <= CIDEnd; j++)
          {
            cmap.Cmap.Add(j, new Rune(code++));
          }
          
        } else {
          // _char == '['
          ReadChar();
          int m = CIDEnd - CIDStart + 1;
          for (int j = 0; j < m; j++)
          {
            cmap.LigatureCmap.Add(CIDStart++, GetLigatureRunes());
          }
          ReadChar();
        }

        SkipWhiteSpaceAndNewline();
      }
    }
    private List<Rune> GetLigatureRunes()
    {
      SkipWhiteSpaceAndNewline();
      ReadChar(); // '<'
      List<Rune> list = new List<Rune>();
      while (_char != '>')
      {
        list.Add(new Rune(UInt16.Parse(_buffer.Slice(_position, 4), NumberStyles.HexNumber)));
        SetChar(_position + 4);
        SkipWhiteSpaceOnly();
      }
      ReadChar(); // '>'
      return list;
    }

    private void ParseBfChars(PDF_CIDCMAP cmap)
    {
      int n = Convert.ToInt32(_lastWord);
      SkipWhiteSpaceAndNewline();
      ushort CID = 0;
      Rune r;
      for (int i =0; i < n; i++)
      {
        CID = ReadUShortBE();
        r = ReadRune();
        cmap.Cmap.Add(CID, r);
        SkipWhiteSpaceAndNewline();
      }
    }

    public ushort ReadUShortBE()
    {
      SkipWhiteSpaceOnly();
      ushort u = UInt16.Parse(_buffer.Slice(_position + 1, 4), NumberStyles.HexNumber);
      // 1 (<) + 4  + 1 (>)
      SetChar(_position + 6);
      return u;
    }

    public uint GetNextCodePoint()
    {
      SkipWhiteSpaceOnly();
      if (_buffer[_position + 5] == '>')
      {
        return ReadUShortBE();
      }
      else
      {
        // surogate
        ushort high = UInt16.Parse(_buffer.Slice(_position + 1, 4), NumberStyles.HexNumber);
        ushort low = UInt16.Parse(_buffer.Slice(_position + 5, 4), NumberStyles.HexNumber);
        // 1 (<) + 8  + 1 (>)
        SetChar(_position + 10);
        uint code = 65_536 + (uint)((high - 55_296) * 1024) + (uint)(low - 56_320);
        return code;
      }
    }
    public Rune ReadRune()
    {
      SkipWhiteSpaceOnly();
      // ushort
      if (_buffer[_position + 5] == '>')
      {
        return new Rune(ReadUShortBE());
      } else
      {
        // surogate
        ushort high = UInt16.Parse(_buffer.Slice(_position + 1, 4), NumberStyles.HexNumber);
        ushort low = UInt16.Parse(_buffer.Slice(_position + 5, 4), NumberStyles.HexNumber);
        // 1 (<) + 8  + 1 (>)
        SetChar(_position + 10);
        uint code = 65_536 + (uint)((high - 55_296) * 1024) + (uint)(low - 56_320);
        return new Rune(code);
      }
    }

    private void SkipWhiteSpaceOnly()
    {
      while (_char != PDFConstants.NULL && _char == ' ')
        ReadChar();
    }

    private void SkipWhiteSpaceAndNewline()
    {
      while (_char != PDFConstants.NULL && (_char == ' ' || _char == '\n' || _char == '\r'))
        ReadChar();
    }

    public string GetNextToken()
    {
      SkipWhiteSpaceAndNewline();

      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace
      while (_char != PDFConstants.NULL && _char != ' ' && _char != '\n' && _char != '\r')
        ReadChar();
      return Encoding.Default.GetString(_buffer.Slice(starter, _position - starter));
    }

    private void ReadChar()
    {
      if (_readPosition >= _buffer.Length)
        _char = PDFConstants.NULL;
      else
        _char = _buffer[_readPosition];

      // set curr and go next
      _position = _readPosition++;
    }

    private void SetChar(int pos)
    {
      _readPosition = pos;
      if (_readPosition >= _buffer.Length)
        _char = PDFConstants.NULL;
      else
        _char = _buffer[_readPosition];

      // set curr and go next
      _position = _readPosition++;
    }
  }
}
