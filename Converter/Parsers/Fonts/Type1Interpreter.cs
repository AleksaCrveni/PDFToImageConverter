using Converter.FileStructures.PDF;
using Converter.FileStructures.Type1;
using Converter.Parsers.PostScript;
using Converter.StaticData;
using Converter.Utils;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    private PDF_FontInfo _ffInfo;

    public Type1Interpreter(byte[] buffer, PDF_FontInfo ffInfo) : base(buffer)
    {
      _ffInfo = ffInfo;
    }

    public TYPE1_Font LoadFont()
    {
      TYPE1_Font font = new TYPE1_Font();
      ParseHeader(font);
      Interpreter(font);
      return font;
    } 

    private void Interpreter(TYPE1_Font font)
    {
      // skip header for now
      SkipUntilAfterString("dict".AsSpan());
      SkipNextString(); // begin

      ParseFontDictionary(font);
      // we can skip to this by offseting with Length1 (length of clear  portion of the program)
      // length of it is Length2
      // TODO: Apparently this can be ascii string as well, look into this
      byte[] privateDictRaw = DecryptPrivateDictionary();
      File.WriteAllBytes(Files.RootFolder + @$"\{_ffInfo.FontDescriptor.FontName}-decryptedEEXEC.txt", privateDictRaw);
      ParsePrivateDictionary(font, privateDictRaw);
    }

    public override bool IsCurrentCharPartOfOperator()
    {
      if ((__char >= 65 && __char <= 90) || (__char >= 97 && __char <= 122) || __char == '\'' || __char == '\"')
        return true;
      return false;
    }

    // this should probably be virtual as well as font dict
    public byte[] DecryptPrivateDictionary()
    {
      SkipUntilAfterString("eexec".AsSpan());
      if (__char == PDFConstants.NULL)
        return Array.Empty<byte>();
      SkipWhiteSpace();
      // TODO: len of this shouldn't go to the end of the file but can be calcualted based on
      // Lenght2 and Length1
      ReadOnlySpan<byte> encryptedPortion = __buffer.AsSpan().Slice(__position);
      return DecryptionHelper.DecryptAdobeType1EEXEC(encryptedPortion);
    }
    private void ParsePrivateDictionary(TYPE1_Font font, byte[] input)
    {
      font.FontDict.Private = new();
      if (input.Length == 0)
        return;
      // idk if this is ok workaround, this really is just just some kind of parser helper that shares common functions
      Type1Interpreter helper = new Type1Interpreter(input, _ffInfo);
      string token = helper.GetNextTokenString();
      while (token != string.Empty)
      {
        switch (token)
        {
          //case "RD":
          //  font.FontDict.Private.RDProc = helper.GetNextProcedureAsString();
          //  break;
          //case "ND":
          //  font.FontDict.Private.NDProc = helper.GetNextProcedureAsString();
          //  break;
          //case "NP":
          //  font.FontDict.Private.NPProc = helper.GetNextProcedureAsString();
          //  break;
          case "password":
            font.FontDict.Private.Password = helper.GetNextString();
            break;
          case "BlueValues":
            helper.GetArray();
            font.FontDict.Private.BlueValues = helper.PopNumberArray();
            break;
          case "OtherBlues":
            helper.GetArray();
            font.FontDict.Private.OtherBlues = helper.PopNumberArray();
            break;
          case "BlueScale":
            helper.GetNumber();
            font.FontDict.Private.BlueScale = helper.PopNumber();
            break;
          case "BlueShift":
            helper.GetNumber();
            font.FontDict.Private.BlueShift = helper.PopNumber();
            break;
          case "BlueFuzz":
            helper.GetNumber();
            font.FontDict.Private.BlueFuzz = helper.PopNumber();
            break;
          case "StdHW":
            helper.GetArray();
            font.FontDict.Private.StdHW = helper.PopNumberArray();
            break;
          case "StdVW":
            helper.GetArray();
            font.FontDict.Private.StdVW = helper.PopNumberArray();
            break;
          case "ForceBold":
            string val = helper.GetNextString();
            font.FontDict.Private.ForceBold = val == "true";
            break;
          case "StemSnapH":
            helper.GetArray();
            font.FontDict.Private.StemSnapH = helper.PopNumberArray();
            break;
          case "StemSnapV":
            helper.GetArray();
            font.FontDict.Private.StemSnapV = helper.PopNumberArray();
            break;
          case "lenIV":
            helper.GetNumber();
            font.FontDict.Private.LenIV = (ushort)helper.PopNumber();
            break;
          case "Subrs":
            helper.ParseSubrs(font);
            break;
          case "CharStrings":
            helper.ParseCharStrings(font);
            break;
          default:
            break;
        }
        token = helper.GetNextTokenString();
      }
    }

    private void ParseFontDictionary(TYPE1_Font font)
    {
      font.FontDict = new();
      font.FontDict.FontInfo = new();
      string token = GetNextTokenString();
      // this is gargabe and this file specific, fix it all later
      while (token != string.Empty && token != "currentdict")
      {
        switch (token)
        {
          case "FontType":
            // get numbers like this now or create new function
            GetNumber();
            font.FontDict.FontType = (int)PopNumber();
            // for now
            Debug.Assert(font.FontDict.FontType == 1);
            break;
          case "FontMatrix":
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
          case "FontName":
            font.FontDict.FontName = GetNextString();
            break;
          case "FontBBox":
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
          case "FontFamily":
            font.FontDict.FontInfo.FamilyName= GetNextString();
            break;
          case "Weight":
            font.FontDict.FontInfo.Weight = GetNextString();
            break;
          case "ItalicAngle":
            GetNumber();
            font.FontDict.FontInfo.ItalicAngle = PopNumber();
            break;
          case "isFixedPitch":
            font.FontDict.FontInfo.IsFixedPitch = GetNextString() == "true";
            break;
          case "UnderlinePosition":
            GetNumber();
            font.FontDict.FontInfo.UnderlinePosition = PopNumber();
            break;
          case "UnderlineThickness":
            GetNumber();
            font.FontDict.FontInfo.UnderlineThickness = PopNumber();
            break;
          case "Encoding":
            // TODO: Refactor all of this code, this all sucks
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
              GetNumber();
              int len = (int)PopNumber();
              string[] encodings = new string[len];
              for (int i = 0; i < encodings.Length; i++)
                encodings[i] = ".notdef";
              // this might not be good idea
              // TODO: i should be reading each string and search for dup and then do it based on that
              SkipUntilAfterString("for".AsSpan());
              // just put w/e so skip next string doesnt skip over dup 
              token = "placeholder";
              while (token != string.Empty && __char != 'r')
              {
                SkipNextString(); // dup
                GetNumber();
                int ind = (int)PopNumber();
                GetName();
                string charName = PopString();
                encodings[ind] = charName;
                SkipNextString(); // put
                SkipWhiteSpace();
              }

              if (token == "readonly")
                SkipNextString();
              font.FontDict.Encoding = encodings;
            }
            break;
          default:
            break;
        }
        //SkipUntilAfterString("def".AsSpan());
        token = GetNextTokenString();
      }
    }
    private void ParseSubrs(TYPE1_Font font)
    {
      GetNumber();
      int len = (int)PopNumber();
      SkipNextString(); // array
      List<string> decrypted = new List<string>(); // debug only remove later
      ReadOnlySpan<byte> buff = new ReadOnlySpan<byte>();
      if (font.FontDict.Private.Subrs == null)
        font.FontDict.Private.Subrs = new byte[len][];

      for (int i = 0; i < len; i++)
      {
        //dup 0 15 RD �1p|=-�D\�R NP
        //dup 0 15 {string currentfile exch readstring pop} �1p|=-�D\�R
        SkipNextString(); // dup
        GetNumber();
        int ind = (int)PopNumber();
        GetNumber();
        int charStringLength = (int)PopNumber();
        SkipNextString(); // RD
        ReadChar(); // skip one space
        GetNextNBytes(ref buff, charStringLength);
        SkipNextString();
        byte[] decr = DecryptionHelper.DecryptAdobeType1CharString(buff, font.FontDict.Private.LenIV);
        decrypted.Add($"dup {ind} {charStringLength} {Encoding.Default.GetString(decr)}");
        font.FontDict.Private.Subrs[ind] = decr;
      }

      File.WriteAllLines(Files.RootFolder + @$"\{_ffInfo.FontDescriptor.FontName}" + @"-decryptedSubrs.txt", decrypted);
    }
    private void ParseCharStrings(TYPE1_Font font)
    {
      GetNumber();
      int len = (int)PopNumber();
      SkipNextString(); // dict
      SkipNextString(); // dup
      SkipNextString(); // begin
      List<string> decrypted = new List<string>(); // debug only remove later
      ReadOnlySpan<byte> buff = new ReadOnlySpan<byte>();
      if (font.FontDict.Private.CharStrings == null)
        font.FontDict.Private.CharStrings = new Dictionary<string, byte[]>();

      for (int i = 0; i < len; i++)
      {
        //dup 0 15 RD �1p|=-�D\�R NP
        //dup 0 15 {string currentfile exch readstring pop} �1p|=-�D\�R
        GetName();
        string key = PopString();
        GetNumber();
        int charStringLength = (int)PopNumber();
        SkipNextString(); // RD
        ReadChar(); // skip one space
        GetNextNBytes(ref buff, charStringLength);
        SkipNextString(); // ND
        byte[] decr = DecryptionHelper.DecryptAdobeType1CharString(buff, font.FontDict.Private.LenIV);
        decrypted.Add($"dup {key} {charStringLength} {Encoding.Default.GetString(decr)}");
        font.FontDict.Private.CharStrings[key] = decr;
      }

      File.WriteAllLines(Files.RootFolder + @$"\{_ffInfo.FontDescriptor.FontName}" + @"-decryptedCharStrings.txt", decrypted);
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

      while (!IsCurrentCharWhiteSpaceOrNull() && !__delimiters.Contains(__char))
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
  }
}
