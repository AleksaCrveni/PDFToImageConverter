using System.Globalization;
using System.Text;


namespace Converter.Parsers
{
  public ref struct SpanParseHelper
  {
    private ReadOnlySpan<byte> _buffer;
    public int _position = 0; // current posion
    private int _readPosition = 0; // next position
    private byte _char; // current char

    public SpanParseHelper(Span<byte> buffer)
    {
      _buffer = (ReadOnlySpan<byte>)buffer;
    }
    public string GetNextString()
    {
      SkipWhiteSpace();
      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace
      while (!IsCurrentCharPdfWhiteSpace())
      {
        ReadChar();
      }

      return Encoding.Default.GetString(_buffer.Slice(starter, _position - starter));
    }
    
    public T GetNextName<T>() where T : Enum
    {
      throw new NotImplementedException();
    }
    public Dictionary<object, object> GetNextDict()
    {
      throw new NotImplementedException();
    }

    public object GetNextNumberTree()
    {
      throw new NotImplementedException();
    }
    // TODO: Add more limiters, first digit must be > 0, but second can be 0 higher
    public (int, int) GetNextIndirectReference()
    {
      (int a, int b) res;
      
      res.a = GetNextInt32Strict();
      res.b = GetNextInt32Strict();
      SkipWhiteSpace();
      if (_char != 'R')
        throw new InvalidDataException("Invalid trailer data. Expected R");
      ReadChar();
      return res;
    }

    public int GetNextInt32Strict()
    {
      SkipWhiteSpace();
      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace
      while (!IsCurrentCharPdfWhiteSpace())
      {
        if (!IsCurrentByteDigit())
          throw new InvalidDataException("Invalid trailer data. Expected digit");
        ReadChar();
      }

      // TODO: maybe dont need new span just read from buffer directly
      ReadOnlySpan<byte> numberInBytes = _buffer.Slice(starter, _position - starter);
      int result = 0;
      for (int i = 0; i < numberInBytes.Length; i++)
      {
        // these should be no negative ints so this is okay i believe?
        result = result * 10 + (int)CharUnicodeInfo.GetDecimalDigitValue((char)numberInBytes[i]);
      }
      return result;
    }

    public string[] GetNextArrayStrict()
    {
      throw new NotImplementedException();
    }
    public string[] GetNextArrayKnownLengthStrict(int len)
    {
      string[] res = new string[len];
      SkipWhiteSpace();
      if (_char != '[')
        throw new InvalidDataException("Invalid trailer data. Expected Array");
      ReadChar();
      for (int i = 0; i < len; i++)
        res[i] = ReadArrayElement();

      SkipWhiteSpace();
      if (_char != ']')
        throw new InvalidDataException("Invalid trailer data. Expected Array");
      ReadChar();
      return res;
    }
    public string ReadArrayElement()
    {
      SkipWhiteSpace();
      if (_char != '<')
        throw new InvalidDataException("Invalid trailer data. Expected Array");
      // Move to actual array start
      ReadChar();
      int starter = _position;
      while (_char != '>' && _char != 0x00)
        ReadChar();
      if (_char == 0x00)
        throw new InvalidDataException("Invalid trailer data. Expected Array");
      string res = Encoding.Default.GetString(_buffer.Slice(starter, _position - starter));

      // Move to next from '>'
      ReadChar();
      return res;
    }

    public byte GetNextDigitStrict()
    {
      SkipWhiteSpace();
      // throw because strict
      if (_char == 0x00 || !IsCurrentByteDigit())
        throw new InvalidDataException("Invalid trailer data. Expected digit");
      return _char;
    }
    // can this be inlined?
    private bool IsByteDigit(byte b)
    {
      if (b < 48 || b > 57)
        return false;
      return true;
    }
    private bool IsCurrentByteDigit()
    {
      if (_char < 48 || _char > 57)
        return false;
      return true;
    }
    public void SkipWhiteSpace()
    {
      while (IsCurrentCharPdfWhiteSpace())
        ReadChar();
    }

    // PDF Whitespaces are defined in specification
    // Page 20
    // Table 1
    public bool IsCurrentCharPdfWhiteSpace()
    {
      // NUL || HORIZONTAL TAB (HT) || LINE FEED (LF) || FORM FEED (FF) || CARRIAGE RETURN (CR) || SPACE (SP)
      if (_char == (byte)0x00 || _char == (byte)0x09 || _char == (byte)0x0a || _char == (byte)0x0c || _char == (byte)0x0d || _char == (byte)0x20)
        return true;
      return false;
    }


    private void ReadChar()
    {
      if (_readPosition >= _buffer.Length)
        _char = (byte)0x00;
      else
        _char = _buffer[_readPosition];

      // set curr and go next
      _position = _readPosition++;
    }

    // I don't think if this is good idea
    // Replace internal buffer and reset positions
    public void ReplaceInternalSpan(Span<byte> buffer)
    {
      _buffer = buffer;
      _position = 0;
      _readPosition = 0;
    }


  }
}
