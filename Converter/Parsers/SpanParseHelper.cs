using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.Intrinsics.Arm;
using System.Text;


namespace Converter.Parsers
{
  public ref struct SpanParseHelper
  {
    private ReadOnlySpan<byte> _buffer;
    public int _position = 0; // current posion
    private int _readPosition = 0; // next position
    private byte _char; // current char
    private const int MAX_STRING_SPAN_ALLOC_SIZE = 4096;


    public SpanParseHelper(ref Span<byte> buffer)
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

    // NOTE: this works only for small values
    public void GetNextStringAsReadOnlySpan(ref ReadOnlySpan<byte> span)
    {
      SkipWhiteSpace();
      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace
      while (!IsCurrentCharPdfWhiteSpace())
      {
        ReadChar();
      }
      span = _buffer.Slice(starter, _position - starter);
    }

    // This will only check if next string is Enum, i dont want to loop over.
    // This should avoid allocations unless Enum.Parse uses it and if _buffer is allocated on stack
    public T GetNextName<T>(T? defaultValue = null) where T : struct, Enum
    {
      ReadOnlySpan<byte> span = new ReadOnlySpan<byte>();
      GetNextStringAsReadOnlySpan(ref span);

      if (span.Length > 256)
        throw new InvalidDataException("Invalid data");

      if (span.Length == 0)
        if (defaultValue != null)
          return (T)defaultValue;
        else
          throw new InvalidDataException("Invalid data");
      // because name starts with '/'
      Span<char> spanOfChars = stackalloc char[span.Length-1];
      for (int i = 0; i < spanOfChars.Length; i++)
        spanOfChars[i] = (char)span[i+1];

      if (!Enum.TryParse<T>((ReadOnlySpan<char>)spanOfChars, out T result))
        if (defaultValue != null)
          return (T)defaultValue;
        else
          throw new InvalidDataException("Invalid data");

      return result;  
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
      SkipWhiteSpace();
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

    // Non completely loose, only disregard last provided byte decided byte
    public int GetNextInt32WithExpectedNonDigitEnd(byte nonDigit)
    {
      SkipWhiteSpace();
      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace
      while (!IsCurrentCharPdfWhiteSpace())
      {
        if (IsCurrentByteDigit())
          ReadChar();
        else
          break;
      }

      // TODO: maybe dont need new span just read from buffer directly
      int len = _position - starter;
      if (_char == nonDigit)
        ReadChar();
      ReadOnlySpan<byte> numberInBytes = _buffer.Slice(starter, len);
      int result = 0;
      for (int i = 0; i < numberInBytes.Length; i++)
      {
        // these should be no negative ints so this is okay i believe?
        result = result * 10 + (int)CharUnicodeInfo.GetDecimalDigitValue((char)numberInBytes[i]);
      }
      return result;
    }

    public List<string> GetNextArrayStrict()
    {
      List<string> array = new List<string>();
      SkipWhiteSpace();
      if (_char != '[')
        throw new InvalidDataException("Invalid array data. Expected Array");
      ReadChar();
      string nextElement = ReadArrayElement();
      while (nextElement != "]" && nextElement != "")
      {
        array.Add(nextElement);
        nextElement = ReadArrayElement();
      }

      if (_char != ']')
        throw new InvalidDataException("Invalid array data. Expected Array");
      ReadChar();
      return array;
    }

    public string[] GetNextArrayKnownLengthStrict(int len)
    {
      string[] res = new string[len];
      SkipWhiteSpace();
      if (_char != '[')
        throw new InvalidDataException("Invalid array data. Expected Array");
      ReadChar();
      for (int i = 0; i < len; i++)
        res[i] = ReadArrayElement();

      SkipWhiteSpace();
      if (_char != ']')
        throw new InvalidDataException("Invalid array data. Expected Array");
      ReadChar();
      return res;
    }
    public string ReadArrayElement()
    {
      SkipWhiteSpace();
      if (_char != '<')
        throw new InvalidDataException("Invalid array data. Expected Array");
      // Move to actual array start
      ReadChar();
      int starter = _position;
      while (_char != '>' && _char != 0x00)
        ReadChar();
      if (_char == 0x00)
        throw new InvalidDataException("Invalid array data. Expected Array");
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
    public Rect  GetNextRectangle()
    {
      // .... reference aloc
      Rect rect = new Rect();
      SkipWhiteSpace();
      if (_char != '[')
        throw new InvalidDataException("Invalid Rectangle data. Expected [");
      // reach char because if there is no space between [ and next number skipspace wont move char
      // and if we move and next char is also whitespace it will be moved regardless
      ReadChar();
      int a = GetNextInt32Strict();
      if (a < 0)
        throw new InvalidDataException("Invalid Rectangle data. Expected number");
      int b = GetNextInt32Strict();
      if (b < 0)
        throw new InvalidDataException("Invalid Rectangle data. Expected number");
      int c = GetNextInt32Strict();
      if (c < 0)
        throw new InvalidDataException("Invalid Rectangle data. Expected number");
      // used this function because string number is sending with ], there is space in between from what i've seen
      int d = GetNextInt32WithExpectedNonDigitEnd((byte)']');
      if (d < 0)
        throw new InvalidDataException("Invalid Rectangle data. Expected number");
      // keep this in case there is rectangle with space after last digit
      rect.FillRect(a, b, c, d);
      return rect;
    }

    public List<(int, int)> GetNextIndirectReferenceList()
    {
      // its ok since its list atm..
      List<(int, int)> list = new();
      SkipWhiteSpace();
      if (_char != '[')
        throw new InvalidDataException("Invalid Rectangle data. Expected [");
      ReadChar();
      SkipWhiteSpace();
      while(_char != ']')
      {
        list.Add(GetNextIndirectReference());
        SkipWhiteSpace();
      }
      return list;
    }

    // TODO: This should return span not actual byte[]

    public double GetNextDouble()
    {
      SkipWhiteSpace();
      int start = _position;
      while (!IsCurrentCharPdfWhiteSpace())
      {
        ReadChar();
      }

      // TODO: if this will work for 32 bit do some better check and fix later
      int diff = _position - start;
      if (diff > 64)
        throw new InvalidDataException("Expected 64 bit double");
      Span<byte> bytes = stackalloc byte[diff];
      // its ok since bytes are on stack
      _buffer.Slice(start, _position - start).CopyTo(bytes);
      if (BitConverter.IsLittleEndian)
        bytes.Reverse();
      return BitConverter.ToDouble(bytes);
    }
    public byte[] GetNextStream()
    {
      SkipWhiteSpace();
      int start = _position;
      while (!IsCurrentCharPdfWhiteSpace())
      {
        ReadChar();
      }
      return _buffer.Slice(start, _position - start).ToArray();
    }

    // NOTE: max value lenght to match is MAX_STRING_SPAN_ALLOC_SIZE bytes
    // NOTE: order is to skip next x words, i.e Expect Next 3rd to be etc
    // NOTE: do not use this for variables expectation because we dont know if variable is UTF8 or 16
    // TODO: Refactor this later
      
    public bool ExpectNextUTF8(string valToMatch, int order = 0)
    {
      for (int i = 0; i < order; i++)
        SkipNextString();
      // use span not to allocate another string when getting new string
      // count sizeof char to be 1 because we use this internally for expecting UTF8s
      int strSize = valToMatch.Length; //* sizeof(Char);
      if (strSize > MAX_STRING_SPAN_ALLOC_SIZE)
        throw new InvalidDataException("String to match too big!");
       
      Span<byte> bytesToMatch = stackalloc byte[strSize];
      // only works for little endian, add check perhaps
      Encoding.UTF8.GetBytes(valToMatch.AsSpan(), bytesToMatch);
      ReadOnlySpan<byte> nextStringInBytes = new ReadOnlySpan<byte>();
      GetNextStringAsReadOnlySpan(ref nextStringInBytes);
      int smallerLength = bytesToMatch.Length > nextStringInBytes.Length ? nextStringInBytes.Length : bytesToMatch.Length;
      // limit search because i want to  use this only on small strings not longs stream arrays that may come out as a string?
      smallerLength = smallerLength < MAX_STRING_SPAN_ALLOC_SIZE ? smallerLength : MAX_STRING_SPAN_ALLOC_SIZE;
      for (int i = 0; i < smallerLength; i++)
      {
        // does this even work with dfifferent encodings........
        if (bytesToMatch[i] != nextStringInBytes[i])
        {
          return false;
        }
      }
      return true;
    }

    public bool GoToNextStringMatch(string valToMatch)
    {
      // use span not to allocate another string when getting new string
      int strSize = valToMatch.Length * sizeof(Char);
      if (strSize > MAX_STRING_SPAN_ALLOC_SIZE)
        throw new InvalidDataException("String to match too big!");

      Span<byte> bytesToMatch = stackalloc byte[strSize];
      // only works for little endian, add check perhaps
      Encoding.Unicode.GetBytes(valToMatch.AsSpan(), bytesToMatch);
      ReadOnlySpan<byte> nextStringInBytes = new ReadOnlySpan<byte>();
      GetNextStringAsReadOnlySpan(ref nextStringInBytes);
      int smallerLength = 0;
      bool isMatched = false;
      // this loop looks so cursed
      do
      {

        smallerLength = bytesToMatch.Length > nextStringInBytes.Length ? nextStringInBytes.Length : bytesToMatch.Length;
        // limit search because i want to  use this only on small strings not longs stream arrays that may come out as a string?
        smallerLength = smallerLength < MAX_STRING_SPAN_ALLOC_SIZE ? smallerLength : MAX_STRING_SPAN_ALLOC_SIZE;
        int i = 0;
        for (i = 0; i < smallerLength; i++)
        {
          // does this even work with dfifferent encodings........
          if (bytesToMatch[i] != nextStringInBytes[i])
          {
            GetNextStringAsReadOnlySpan(ref nextStringInBytes);
            i = smallerLength + 2;
          }
        }
        // refactor this later........
        if (i != smallerLength + 2)
          isMatched = true;
      } while (!isMatched && nextStringInBytes.Length != 0);

      return isMatched;     
    }

    public void SkipNextString()
    {
      SkipWhiteSpace();
      while (!IsCurrentCharPdfWhiteSpace())
      {
        ReadChar();
      }
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
    // maybe first i should readchar
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
