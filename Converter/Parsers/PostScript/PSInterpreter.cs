using Converter.Parsers.PDF;
using Converter.StaticData;
using System.Globalization;
using System.Text;

namespace Converter.Parsers.PostScript
{
  public abstract class PSInterpreter
  {
    // use __ for abstract fields
    public byte[] __buffer;
    public int __position;
    public int __readPosition;
    public byte __char;
    public Stack<double> __numberOperands;
    public Stack<string> __stringOperands;
    public Stack<OperandType> __operandTypes;
    public Stack<int> __arrayLengths;
    public byte[] __delimiters;
    public PSInterpreter(byte[] buffer)
    {
      __buffer = buffer;
      __position = 0;
      __readPosition = 0;
      __char = PDFConstants.SP;
      __numberOperands = new Stack<double>();
      __stringOperands = new Stack<string>();
      __operandTypes = new Stack<OperandType>();
      __arrayLengths = new Stack<int>();
      // I understand the warning, in this case its fine since im changing abstract class field only once
      InitDelimiters(); 
    }

    public abstract bool IsCurrentCharPartOfOperator();
    public virtual void InitDelimiters()
    {
      __delimiters = new byte[]
      {
        StandardCharConstants.LEFT_BRACKET,
        StandardCharConstants.RIGHT_BRACKET,
        StandardCharConstants.LEFT_CURLY_BRACKET,
        StandardCharConstants.RIGHT_CURLY_BRACKET,
        StandardCharConstants.LEFT_PARENTHESIS,
        StandardCharConstants.RIGHT_PARENTHESIS,
        StandardCharConstants.LESS_THAN,
        StandardCharConstants.MORE_THAN,
        StandardCharConstants.SLASH
      };
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
    public string ProcessNextToken()
    {
      SkipWhiteSpace();
      // TODO: I thin kthat this check can be made redundand if we make ifelse chain of checks and this is last one of them
      // TODO: needs adjustments in case when we are processingNextToken as part of procedure because then we just want to stored them in a list, but now thinking about this, this is OK, just push in the list
      if (IsCurrentCharPartOfOperator() && __char != PDFConstants.NULL)
      {
        return GetNextString();
      }

      // is a number, either real or int
      if (IsCurrentCharDigit() || __char == '-')
      {
        GetNumber();
        return string.Empty;
      }

      // is a name
      if (__char == '/')
      {
        GetName();
        return string.Empty;
      }

      // string literal
      if (__char == '(')
      {
        GetStringLiteral();
        return string.Empty;
      }

      // array can start with { as well but then it would be used a procedure
      // I guess we can just support { and count it as procedure since its a list anyways
      // but prob wouldnt work if there are keywoards in there
      // think about proc processing
      if (__char == '[')
      {
        GetArray();
        return string.Empty;
      }

      return string.Empty;
    }

    public void GetNextStringAsSpan(ref ReadOnlySpan<byte> span)
    {
      SkipWhiteSpace();
      int start = __position;
      while (!IsCurrentCharWhiteSpace())
        ReadChar();
      span = __buffer.AsSpan().Slice(start, __position - start);
    }

    public string GetNextString()
    {
      return GetNextString(Encoding.Default);
    }

    public string GetNextString(Encoding enc)
    {
      SkipWhiteSpace();
      int start = __position;
      while (!IsCurrentCharWhiteSpace())
        ReadChar();
      return enc.GetString(__buffer.AsSpan().Slice(start, __position - start));
    }
    
    public virtual void GetArray()
    {
      ReadChar();
      int count = 0;
      SkipWhiteSpace();
      while (__char != ']' && __char != '}' && __char != PDFConstants.NULL)
      {
        if (IsCurrentCharDigit() || __char == '-')
          GetNumber();
        else if (__char == '/')
          GetName();
        else if (__char == '(')
          GetStringLiteral();

        count++;
        SkipWhiteSpace();
      }
      ReadChar(); // move off ']'
      __operandTypes.Push(OperandType.ARRAY);
      __arrayLengths.Push(count);
    }

    public virtual void GetNumber()
    {
      int startPos = __position;
      int negativeMulti = 1;
      if (__char == '-')
      {
        negativeMulti = -1;
        ReadChar();
      }

      int integer = 0;
      int baseIndex = -1;
      while ((IsCurrentCharDigit() || __char == '.') && !IsCurrentCharWhiteSpace())
      {
        if (__char == '.')
        {
          baseIndex = __position - startPos;
          ReadChar();
          continue;
        }

        integer = integer * 10 + CharUnicodeInfo.GetDecimalDigitValue((char)__char);
        ReadChar();
      }

      if (baseIndex == -1)
      {
        // number is integer
        __numberOperands.Push(integer * negativeMulti);
      }
      else
      {
        // number has no decimals but is expected to be double
        // i.e 253.0
        if (__position - baseIndex - startPos == 1)
          __numberOperands.Push((double)(integer * negativeMulti));
        else
          __numberOperands.Push((integer / Math.Pow(10, __position - baseIndex - startPos - 1)) * negativeMulti);
      }
      __operandTypes.Push(OperandType.DOUBLE);
    }

    public virtual void GetStringLiteral()
    {
      ReadChar();
      int startPos = __position;
      while (__char != ')' || __char == PDFConstants.NULL)
      {
        ReadChar();
      }

      __stringOperands.Push(Encoding.UTF8.GetString(__buffer.AsSpan().Slice(startPos, __position - startPos)));
      __operandTypes.Push(OperandType.STRING);
      ReadChar(); // skip ')'
    }
    public virtual void GetName()
    {
      ReadChar();
      int startPos = __position;
      while (!IsCurrentCharWhiteSpace() && __char != PDFConstants.NULL)
      {
        ReadChar();
      }

      __stringOperands.Push(Encoding.Default.GetString(__buffer.AsSpan().Slice(startPos, __position - startPos)));
      __operandTypes.Push(OperandType.STRING);
    }

    // this is to literally skip next 'word' it wont work on string literals
    public void SkipNextString()
    {
      SkipWhiteSpace();
      while (!IsCurrentCharWhiteSpace())
        ReadChar();
    }

    public void SkipWhiteSpaceAndDelimiters()
    {
      while (IsCurrentCharWhiteSpace() || __delimiters.Contains(__char))
        ReadChar();
    }

    public void SkipWhiteSpace()
    {
      while (IsCurrentCharWhiteSpace())
        ReadChar();
    }

    public virtual bool IsCurrentCharWhiteSpace()
    {
      if (__char == PDFConstants.SP ||
          __char == PDFConstants.HT ||
          __char == PDFConstants.LF ||
          __char == PDFConstants.CR ||
          __char == PDFConstants.NULL) 
        return true;
      return false;
    }
    public void ReadChar()
    {
      if (__readPosition >= __buffer.Length)
        __char = PDFConstants.NULL;
      else
        __char = __buffer[__readPosition];

      // set curr and go next
      __position = __readPosition++;
    }

    public bool IsCurrentCharDigit()
    {
      if (__char < 48 || __char > 57)
        return false;
      return true;
    }
  }
}
