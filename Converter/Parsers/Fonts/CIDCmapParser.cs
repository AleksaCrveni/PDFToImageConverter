using Converter.FileStructures.PDF;
using Converter.StaticData;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Converter.Parsers.Fonts
{
  public ref struct CIDCmapParserHelper
  {
    // THESE SHOULDNT BE RUNES I GUESS
    public ReadOnlySpan<byte> _buffer;
    private string _lastWord;
    private int _position;
    private int _readPosition;
    private byte _char = 0x1b;
    private int _capacity = 4;
    public CIDCmapParserHelper(ref ReadOnlySpan<byte> buffer, string CMAPEncoding)
    {
      _buffer = buffer;
      SetCapacity(CMAPEncoding);
    }

    // TODO: Optimize this
    public void Parse(PDF_CID_CMAP cmap)
    {
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
    }

    private void ParseBfCharsRange(PDF_CID_CMAP cmap)
    {
      int n = Convert.ToInt32(_lastWord);
      SkipWhiteSpaceAndNewline();
      char CIDStart = '0';
      char CIDEnd = '0';
      char code = '0';
      Rune r;
      for (int i = 0; i < n; i++)
      {
        CIDStart = ReadUShortBE();
        CIDEnd = ReadUShortBE();
        SkipWhiteSpaceOnly();
        if (_char == '<')
        {
          code = GetNextCodePoint();
          for (char j = CIDStart; j <= CIDEnd; j++)
          {
            cmap.Cmap.Add(j, code);
          }
          
        } else {
          // _char == '['
          ReadChar();
          uint m = (uint)CIDEnd - CIDStart + 1;
          for (uint j = 0; j < m; j++)
          {
            SkipWhiteSpaceAndNewline();

            // if true it means that we are expecting a ligature
            if (_buffer[_position + 5] != '>')
            {
              ReadChar();
              List<char> list = new List<char>();
              while (_char != '>' && _char != PDFConstants.NULL)
              {
                list.Add((char)UInt16.Parse(_buffer.Slice(_position, 4), NumberStyles.HexNumber));
                SetChar(_position + 4);
                SkipWhiteSpaceOnly();
              }
              cmap.LigatureCmap.Add(CIDStart++, list);
              ReadChar();
            }
            else
            {
              cmap.Cmap.Add(CIDStart++, ReadUShortBE());
            }
          }
          ReadChar(); // ]
        }

        SkipWhiteSpaceAndNewline();
      }
    }
    private List<Rune> GetLigatureRunes()
    {
      SkipWhiteSpaceAndNewline();
      ReadChar(); // '<'
      List<Rune> list = new List<Rune>(_capacity);
      while (_char != '>')
      {
        list.Add(new Rune(UInt16.Parse(_buffer.Slice(_position, 4), NumberStyles.HexNumber)));
        SetChar(_position + 4);
        SkipWhiteSpaceOnly();
      }
      ReadChar(); // '>'
      return list;
    }

    private void ParseBfChars(PDF_CID_CMAP cmap)
    {
      int n = Convert.ToInt32(_lastWord);
      SkipWhiteSpaceAndNewline();
      char CID = '0';
      char c = '0';
      for (int i =0; i < n; i++)
      {
        CID = GetNextCodePoint();
        c = ReadUShortBE();
        cmap.Cmap.Add(CID, c);
        SkipWhiteSpaceAndNewline();
      }
    }

    /// <summary>
    /// Added this becuase I am really not certain what char (CID) limit is so we will be safe and use uint always
    /// </summary>
    /// <returns></returns>
    public uint GetNextUIntFromHex()
    {
      if (_buffer[_position + 5] == '>')
        return UInt16.Parse(_buffer.Slice(_position + 1, 4), NumberStyles.HexNumber);
      else
        return UInt32.Parse(_buffer.Slice(_position + 1, 8), NumberStyles.HexNumber);
    }

    public char ReadUShortBE()
    {
      SkipWhiteSpaceOnly();
      ushort u = UInt16.Parse(_buffer.Slice(_position + 1, 4), NumberStyles.HexNumber);
      // 1 (<) + 4  + 1 (>)
      SetChar(_position + 6);
      return (char)u;
    }

    public char GetNextCodePoint()
    {
      SkipWhiteSpaceOnly();
      if (_buffer[_position + 5] == '>')
      {
        return (char)ReadUShortBE();
      }
      else
      {
        uint val = UInt32.Parse(_buffer.Slice(_position + 1, 8), NumberStyles.HexNumber);
        return (char)val;
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

    private void SetCapacity(string encoding)
    {
      // can't use switch since 'consts' are actually readonly
      if (encoding == PDFConstants.Identity_H)
        _capacity = 2;
      else
        _capacity = 4;
    }
  }
}
