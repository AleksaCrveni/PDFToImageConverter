using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace Converter.Parsers
{
  public ref struct SpanParseHelper
  {
    private ReadOnlySpan<byte> _buffer;
    public int _position = 0; // current posion
    private int _readPosition = 0; // next position
    public byte _char; // current char
    private const int MAX_STRING_SPAN_ALLOC_SIZE = 4096;
    //                                              (,    ),    <,    >,    [,    ],    {,    },    /,    %
    private static readonly byte[] delimiters = [0x28, 0x29, 0x3C, 0x3E, 0x5B, 0x5D, 0x7B, 0x7D, 0x2F, 0x25];

    // TODO: do 0x00 check whenever looping ReadByte()
    public SpanParseHelper(ref Span<byte> buffer)
    {
      _buffer = (ReadOnlySpan<byte>)buffer;
    }
    // it should stop at next special char, but it should exclude them!
    // if special char is first token its ok!
    // this is mostly used to get 'keys' in dictionaries
    // for values there will be specific functions called based on key
    public string GetNextToken()

    {
      SkipWhiteSpaceAndDelimiters();
      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace
      while (!IsCurrentCharPdfWhiteSpace() && !delimiters.Contains(_char))
      {
        ReadChar();
      }

      return Encoding.Default.GetString(_buffer.Slice(starter, _position - starter));
    }

    // this gives positions of start and end of next token
    public void GetNextTokenPositionInt32(ref int start, ref int end)
    {
      SkipWhiteSpaceAndDelimiters();
      start = _position;
      while (!IsCurrentCharPdfWhiteSpace() && !delimiters.Contains(_char))
      {
        ReadChar();
      }
      end = _position;
    }
    // this will go to delimiter or regular character
    public void GoToNextNonPdfWhiteSpace()
    {
      while (IsCurrentCharPdfWhiteSpace())
        ReadChar();
    }

    // this goes just to delimiter or if we are on delimiter do nothing
    // TODO: THINK IF IT SHOULD JUST MOVE TO NEXT DELIMITER
    public void ReadUntilNonWhiteSpaceDelimiter()
    {
      while (!delimiters.Contains(_char) && _readPosition < _buffer.Length)
        ReadChar();
    }

    // this will be only helper function of this kind because
    // dict is only structure that uses repeated delimiter for start
    // this may not work in the future if we do moving buffer but now its OK!
    // TODO: this really should move at leats one char...
    public bool IsCurrentCharacterSameAsNext()
    {
      // if next char is in buffer and if _char is same as next char
      if (_readPosition < _buffer.Length && _char == _buffer[_readPosition])
        return true;
      return false;
    }
    // NOTE: this works only for small values
    // NOTE: this may not work as I think it does (stack alloacted)
    public void GetNextStringAsReadOnlySpan(ref ReadOnlySpan<byte> span)
    {
      SkipWhiteSpaceAndDelimiters();
      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace
      while (!IsCurrentCharPdfWhiteSpace())
      {
        ReadChar();
      }
      span = _buffer.Slice(starter, _position - starter);
    }

    // This can be used even for array or name
    // since its impossible to know wether defautl value is expcted or not
    // when calling this method you should check if your value is (T)0 because 
    // I added default Null value where there isn't default value
    public List<T> GetListOfNames<T>() where T : struct, Enum
    {
      // fix this next, make GetNextDelimiter and check if its ] to know its end of array
      // can do that for dictionaries as well if needed
      List<T> result = new List<T>();
      GoToNextNonPdfWhiteSpace();
      // single name support
      if (_char != '[')
      {
        result.Add(GetNextName<T>());
        return result;
      }

      while (_char != ']')
      {
        result.Add(GetNextName<T>());
        GoToNextNonPdfWhiteSpace();
      }

      ReadChar();
      return result;
    }

    // This will only check if next string is Enum, i dont want to loop over.
    // This should avoid allocations unless Enum.Parse uses it and if _buffer is allocated on stack
    public T GetNextName<T>() where T : struct, Enum
    {
      int start = 0;
      int end = 0;
      GetNextTokenPositionInt32(ref start, ref end);
      int len = end - start;
      if (len > 256)
        throw new InvalidDataException("Name too long!");
      if (len == 0)
          return default(T);

      // this needs to be done because Enum.TryParse accepts only <char>
      Span<char> spanOfChars = stackalloc char[len];
      for (int i = 0; i < spanOfChars.Length; i++)
        spanOfChars[i] = (char)_buffer[start + i]; // no need to make another slice, just use main buffer and offset

      if (!Enum.TryParse<T>((ReadOnlySpan<char>)spanOfChars, out T result))
        return default(T);

      return result;
    }

    // skipp dict just for now
    // what I don't need
    // TODO: Think if i should be searching for endobj instead of >>
    public Dictionary<object, object> GetNextDict()
    {
      bool endOfOutsideDict = false;
      ReadUntilNonWhiteSpaceDelimiter();
      if (_char == '<' && IsCurrentCharacterSameAsNext())
        ReadChar();
      ReadChar();

      ReadUntilNonWhiteSpaceDelimiter();
      int innerDictCount = 0;
      
      while (!endOfOutsideDict)
      {
        if (_char == 0x00)
          throw new InvalidDataException("Unexpected EOS");
        else if (_char == '<' && IsCurrentCharacterSameAsNext())
        {
          innerDictCount++;
          ReadChar(); // read extra char to move off double deliter
        } else if (_char == '>' && IsCurrentCharacterSameAsNext())
        {
          innerDictCount--;
          ReadChar();
          if (innerDictCount < 0)
            break;
        }

        ReadChar();
        ReadUntilNonWhiteSpaceDelimiter();
      }

      ReadChar();
      return new Dictionary<object, object>();
    }

    public object GetNextNumberTree()
    {
      throw new NotImplementedException();
    }
    // TODO: Add more limiters, first digit must be > 0, but second can be 0 higher
    public (int, int) GetNextIndirectReference()
    {
      SkipWhiteSpaceAndDelimiters();
      (int a, int b) res;
      
      res.a = GetNextInt32();
      res.b = GetNextInt32();
      SkipWhiteSpaceAndDelimiters();
      if (_char != 'R')
        throw new InvalidDataException("Invalid trailer data. Expected R");
      ReadChar();
      return res;
    }

    public int GetNextInt32()
    {
      SkipWhiteSpaceAndDelimiters();
      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace
      while (IsCurrentByteDigit())
      {
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

    // do 64 for just in case, not sure if its specified max precision
    public double GetNextDouble()
    {
      SkipWhiteSpaceAndDelimiters();
      int starter = _position;
      // don't have to check if _char is 0 if we reach end of the buffer becaseu its cheked in IsCurrentCharPdfWhiteSpace;
      // long is ok, there won't be as big numbers in pdf files
      long result = 0;
      int baseIndex = 0;
      // significant
      while (IsCurrentByteDigit() || _char == '.')
      {
        if (_char == '.')
        {
          baseIndex = _position - starter;
          ReadChar();
          continue;
        }
        result = result * 10 + CharUnicodeInfo.GetDecimalDigitValue((char)_char);
        ReadChar();
      }
      // == 1 means for some reason last char was 
      if (baseIndex == 0 || _position - baseIndex - starter == 1)
        return result;
      return result / (Math.Pow(10, (_position - baseIndex - starter - 1)));
    }
    // Non completely loose, only disregard last provided byte decided byte
    public int GetNextInt32WithExpectedNonDigitEnd(byte nonDigit)
    {
      SkipWhiteSpaceAndDelimiters();
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
      ReadUntilNonWhiteSpaceDelimiter();
      if (_char != '[')
        throw new InvalidDataException("Invalid array data. Expected Array");
      ReadChar();

      string nextElement = GetNextToken();
      while (nextElement != "")
      {
        array.Add(nextElement);

        SkipWhiteSpace();
        if (_char == ']')
          break;

        nextElement = GetNextToken();       
      }

      if (nextElement == "")
        throw new InvalidDataException("Invalid array data. Expected Array");
      ReadChar();
      return array;
    }

    // bytestring true means that array elements will be byte array so exclude < and >
    public string[] GetNextArrayKnownLengthStrict(int len)
    {
      string[] res = new string[len];
      ReadUntilNonWhiteSpaceDelimiter();
      if (_char != '[')
        throw new InvalidDataException("Invalid array data. Expected Array");

      for (int i = 0; i < len; i++)
      {
        SkipWhiteSpaceAndDelimiters();
        res[i] = GetNextToken();
      }
      // move to next, so current isn't skipped
      ReadChar();
      ReadUntilNonWhiteSpaceDelimiter();
      if (_char != ']')
        throw new InvalidDataException("Invalid array data. Expected Array");
      ReadChar();
      return res;
    }
     
    // byte string is starting with < and ending with >
    public string ReadByteString()
    {
      SkipWhiteSpaceAndDelimiters();
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
      SkipWhiteSpaceAndDelimiters();
      // throw because strict
      if (_char == 0x00 || !IsCurrentByteDigit())
        throw new InvalidDataException("Invalid trailer data. Expected digit");
      return _char;
    }
    public Rect GetNextRectangle()
    {
      // .... reference aloc
      Rect rect = new Rect();
      ReadUntilNonWhiteSpaceDelimiter();
      if (_char != '[')
        throw new InvalidDataException("Invalid Rectangle data. Expected [");
      // reach char because if there is no space between [ and next number skipspace wont move char
      // and if we move and next char is also whitespace it will be moved regardless
      double a = GetNextDouble();
      if (a < 0)
        throw new InvalidDataException("Invalid Rectangle data. Expected number");
      double b = GetNextDouble();
      if (b < 0)
        throw new InvalidDataException("Invalid Rectangle data. Expected number");
      double c = GetNextDouble();
      if (c < 0)
        throw new InvalidDataException("Invalid Rectangle data. Expected number");
      // used this function because string number is sending with ], there is space in between from what i've seen
      double d = GetNextDouble();
      if (d < 0)
        throw new InvalidDataException("Invalid Rectangle data. Expected number");

      ReadUntilNonWhiteSpaceDelimiter();
      if (_char != ']')
        throw new InvalidDataException("Invalid Rectangle data. Expected ]");
      ReadChar();
      // keep this in case there is rectangle with space after last digit
      rect.FillRect(a, b, c, d);
      
      return rect;
    }

    public List<(int, int)> GetNextIndirectReferenceList()
    {
      // its ok since its list atm..
      List<(int, int)> list = new();
      ReadUntilNonWhiteSpaceDelimiter();
      if (_char != '[')
        throw new InvalidDataException("Invalid Rectangle data. Expected [");
      ReadChar();
      SkipWhiteSpaceAndDelimiters();
      while(_char != ']')
      {
        list.Add(GetNextIndirectReference());
        SkipWhiteSpace();
      }
      ReadChar();
      return list;
    }

    public byte[] GetNextStream()
    {
      SkipWhiteSpaceAndDelimiters();
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
    // TODO: Keep this, but maybe there will be used, for now it feels weird to use because string may contain
    // delimiters
    [Obsolete("Do not use for now because it will skip over non white space delimiters, use  UntilNextDelimiter() and ReadChar()")]
    public bool ExpectNextUTF8(string valToMatch, int order = 0)
    {
      // this will skip delimiters so may not work as expected
      for (int i = 0; i < order; i++)
        SkipNextToken();
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

    // String literal
    // Fix in respect to Table 3 - Escape sequences in literal string
    public string GetNextTextString()
    {
      ReadUntilNonWhiteSpaceDelimiter();
      if (_char != '(')
        throw new InvalidDataException("String literal error. Expected (");

      string result = GetNextToken();

      if (_char != ')')
        throw new InvalidDataException("String literal error. Expected (");
      ReadChar();
      return result;
    }
    public void SkipNextToken()
    {
      SkipWhiteSpaceAndDelimiters();
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

    public void SkipWhiteSpaceAndDelimiters()
    {
      while (IsCurrentCharPdfWhiteSpace() || delimiters.Contains<byte>(_char))
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

    public void ReadChar()
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
