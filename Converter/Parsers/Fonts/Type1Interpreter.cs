using Converter.FileStructures.PDF;
using Converter.FileStructures.Type1;
using Converter.Parsers.PDF;
using Converter.Parsers.PostScript;
using Converter.StaticData;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Converter.Parsers.Fonts
{
  /// <summary>
  /// This isnt compliant Type1 general purpose interpreter. Unless CFF, i think that there is only one 
  /// Font in font file. First only suppport non CFF font file data i.e 
  /// This is stack based interpreter
  /// </summary>
  public class Type1Interpreter : PSInterpreter
  {
    private PDF_FontFileInfo _ffInfo;

    public Type1Interpreter(byte[] buffer, PDF_FontFileInfo ffInfo) : base(buffer)
    {
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
      // skip header for now
      SkipUntilAfterString("dict".AsSpan());
      SkipNextString(); // begin

      ParseFontDictionary(font);


    }
    public override bool IsCurrentCharPartOfOperator()
    {
      if ((__char >= 65 && __char <= 90) || (__char >= 97 && __char <= 122) || __char == '\'' || __char == '\"')
        return true;
      return false;
    }
    private void ParseFontDictionary(TYPE1_Font font)
    {
      font.FontDict = new();
      font.FontDict.FontInfo = new();
      string token = GetNextString();
      while (token != string.Empty)
      {
        switch (token)
        {
          case "/FontType":
            // get numbers like this now or create new function
            GetNumber();
            font.FontDict.FontType = (int)PopNumber();
            // for now
            Debug.Assert(font.FontDict.FontType == 1);
            break;
          case "/FontMatrix":
            SkipWhiteSpace();
            ReadChar();
            GetNumber();
            double a = PopNumber();
            GetNumber();
            double b = PopNumber();
            GetNumber();
            double c = PopNumber();
            GetNumber();
            double d = PopNumber();
            GetNumber();
            double e = PopNumber();
            GetNumber();
            double f = PopNumber();
            ReadChar();
            font.FontDict.FontMatrix = new double[3, 3] { { a, b, 0 }, { c, d, 0 }, { e, f, 0 } };

            break;
          case "/FontName":
            font.FontDict.FontName = GetNextString();
            break;
          case "/FontBBox":
            SkipWhiteSpace();
            ReadChar();
            GetNumber();
            a = PopNumber();
            GetNumber();
            b = PopNumber();
            GetNumber();
            c = PopNumber();
            GetNumber();
            d = PopNumber();
            PDF_Rect rect = new PDF_Rect();
            rect.FillRect(a, b, c, d);
            font.FontDict.FontBBox = rect;
            ReadChar();
            break;
          case "/FontFamily":
            font.FontDict.FontInfo.FamilyName= GetNextString();
            break;
          case "/Weight":
            font.FontDict.FontInfo.Weight = GetNextString();
            break;
          case "/ItalicAngle":
            GetNumber();
            font.FontDict.FontInfo.ItalicAngle = PopNumber();
            break;
          case "/isFixedPitch":
            font.FontDict.FontInfo.IsFixedPitch = GetNextString() == "true";
            break;
          case "/UnderlinePosition":
            GetNumber();
            font.FontDict.FontInfo.UnderlinePosition = PopNumber();
            break;
          case "/UnderlineThickness":
            GetNumber();
            font.FontDict.FontInfo.UnderlineThickness = PopNumber();
            break;
          case "/Encoding":
            SkipWhiteSpace();
            if (__char == 'S')
            {
              token = GetNextString();
              Debug.Assert(token == "StandardEncoding");
              SkipNextString();
              // TODO: Is this right or should this be int[] like ADobeStandardGlyphs that index into this
              // either way if its latter i can just refer to it since its static 
              font.FontDict.Encoding = PDFEncodings.StandardGlyphNames;
            } else
            {

            }
            
            break;
          default:
            break;
        }
        SkipUntilAfterString("def".AsSpan());
        token = ProcessNextToken();
      }
    }
    


    private string ProcessNextToken()
    {
      SkipWhiteSpace();
      string tok = string.Empty;
      if (IsCurrentCharDigit() || __char == '-')
      {
        GetNumber();
        return tok;
      }

      int starter = __position;

      while (!IsCurrentCharWhiteSpace() && !__delimiters.Contains(__char))
        ReadChar();
      tok =  Encoding.Default.GetString(__buffer.AsSpan().Slice(starter, __position - starter));
      return tok;
    }

    private void ParseHeader(TYPE1_Font info)
    {
      // For now skip header
      SkipUntilAfterString("%%EndComments".AsSpan());
    }

    // NOTE: Use only when you know str encoding
    // This DOES NOT match sbustrings, it merely checks if current string is same as strToCmp
    private void SkipUntilAfterString(ReadOnlySpan<char> strToCmp)
    {
      ReadOnlySpan<byte> token = new ReadOnlySpan<byte>();
      GetNextStringAsSpan(ref token);
      bool found = false;
      while (token.Length != 0 && !found)
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

    private double PopNumber()
    {
      __operandTypes.Pop();
      return __numberOperands.Pop();
    }

   
  }
}
