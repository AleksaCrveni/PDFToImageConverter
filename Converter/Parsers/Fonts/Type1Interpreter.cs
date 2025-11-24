using Converter.FileStructures.PDF;
using Converter.FileStructures.Type1;
using Converter.Parsers.PDF;
using Converter.StaticData;
using System.Globalization;
using System.Text;

namespace Converter.Parsers.Fonts
{
  /// <summary>
  /// This isnt compliant Type1 general purpose interpreter. Unless CFF, i think that there is only one 
  /// Font in font file. First only suppport non CFF font file data i.e 
  /// This is stack based interpreter
  /// </summary>
  public ref struct Type1Interpreter
  {
    public ReadOnlySpan<byte> _buffer;
    private PDF_FontFileInfo _ffInfo;
    public int _position = 0; // current posion
    public int _readPosition = 0; // next position
    public byte _char; // current char
    private Stack<OperandType> operandTypes;
    private Stack<double> realOperands;
    private Stack<string> stringOperands;
    private Stack<int> arrayLengths;
    private static readonly byte[] delimiters = [
      TYPE1Constants.LEFT_BRACKET,
      TYPE1Constants.RIGHT_BRACKET,
      TYPE1Constants.LEFT_PARENTHESIS,
      TYPE1Constants.RIGHT_PARENTHESIS,
      TYPE1Constants.SLASH,
      TYPE1Constants.LESS_THAN,
      TYPE1Constants.MORE_THAN
      ];

    public Type1Interpreter(ref Span<byte> buffer, ref PDF_FontFileInfo ffInfo)
    {
      _buffer = (ReadOnlySpan<byte>)buffer;
      _ffInfo = ffInfo;
    }

    public Type1Interpreter(ref ReadOnlySpan<byte> buffer, ref PDF_FontFileInfo ffInfo)
    {
      _buffer = buffer;
      _ffInfo = ffInfo;
    }

    public TYPE1_Font LoadFont()
    {
      TYPE1_Font font = new TYPE1_Font();
      ParseHeader(font);
      Interpreter(font);
      return null;
    }

    private void Interpreter(TYPE1_Font font)
    {
      string token = ProcessNextToken();
      while (token != string.Empty)
      {
        switch (token)
        {
          case "":
            break;
          default:
            stringOperands.Push(token);
            operandTypes.Push(OperandType.STRING);
            break;
        }
        token = ProcessNextToken();
      }
    }
  
    private string ProcessNextToken()
    {
      SkipWhiteSpace();
      string tok = string.Empty;
      if (IsCurrentCharDigit() || _char == '-')
      {
        GetNumber();
        return tok;
      }

      int starter = _position;

      while (!IsCurrentCharSpaceOrNull() && !delimiters.Contains(_char))
        ReadChar();
      tok =  Encoding.Default.GetString(_buffer.Slice(starter, _position - starter));
      return tok;
    }

    private void ParseHeader(TYPE1_Font info)
    {
      // For now skip header
      SkipUntilAfter("%%EndComments".AsSpan());
    }

    // NOTE: Use only when you know str encoding
    private void SkipUntilAfter(ReadOnlySpan<char> strToCmp)
    {
      ReadOnlySpan<byte> token = new ReadOnlySpan<byte>();
      GetNextStringAsSpan(ref token);
      bool found = false;
      while (token.Length != 0 || !found)
      {
        // we can do this because we know encoding of text we are searching for
        if (strToCmp.Length == token.Length)
        {
          found = true;
          for (int i = 0; i < strToCmp.Length; i++)
          {
            if (strToCmp[i] != token[i])
            {
              found = false;
              break;
            }
          }
        }

        if (!found)
          GetNextStringAsSpan(ref token);
      }
      SkipWhiteSpace();
    }

    private void GetNextStringAsSpan(ref ReadOnlySpan<byte> span)
    {
      SkipWhiteSpace();
      int start = _position;
      while (!IsCurrentCharSpaceOrNull())
        ReadChar();
      span = _buffer.Slice(start, _position - start);
    }

    private void SkipWhiteSpaceAndDelimiters()
    {
      while (IsCurrentCharSpaceOrNull() || delimiters.Contains(_char))
        ReadChar();
    }
    
    private void SkipWhiteSpace()
    {
      while (IsCurrentCharSpaceOrNull())
        ReadChar();
    }

    private bool IsCurrentCharSpaceOrNull()
    {
      if (_char == PDFConstants.SP || _char == PDFConstants.HT || _char == PDFConstants.LF || _char == PDFConstants.NULL)
        return true;
      return false;
    }

    private bool IsCurrentCharDigit()
    {
      if (_char < 48 || _char > 57)
        return false;
      return true;
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


    private void GetNumber()
    {
      int startPos = _position;
      int negativeMulti = 1;
      if (_char == '-')
      {
        negativeMulti = -1;
        ReadChar();
      }

      int integer = 0;
      int baseIndex = -1;
      while ((IsCurrentCharDigit() || _char == '.') && !IsCurrentCharSpaceOrNull())
      {
        if (_char == '.')
        {
          baseIndex = _position - startPos;
          ReadChar();
          continue;
        }

        integer = integer * 10 + CharUnicodeInfo.GetDecimalDigitValue((char)_char);
        ReadChar();
      }

      if (baseIndex == -1)
      {
        // number is integer
        realOperands.Push(integer * negativeMulti);
      }
      else
      {
        // number has no decimals but is expected to be double
        // i.e 253.0
        if (_position - baseIndex - startPos == 1)
          realOperands.Push((double)(integer * negativeMulti));
        else
          realOperands.Push((integer / Math.Pow(10, _position - baseIndex - startPos - 1)) * negativeMulti);
      }
      operandTypes.Push(OperandType.DOUBLE);
    }
  }
}
