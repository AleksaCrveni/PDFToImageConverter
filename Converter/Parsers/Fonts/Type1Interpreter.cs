using Converter.FileStructures.PDF;
using Converter.FileStructures.PostScript;
using Converter.FileStructures.Type1;
using Converter.Parsers.PostScript;
using Converter.Rasterizers;
using Converter.StaticData;
using Converter.Utils;
using System.Buffers.Binary;
using System.ComponentModel.Design;
using System.Diagnostics;
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
    public TYPE1_Font font;
    
    // State
    private int _subroutineCallCount = 0;
    private bool _flex = false;
    private float[] _flexArr;
    private int _flexIndex = 0;

    public Type1Interpreter(byte[] buffer, PDF_FontInfo ffInfo) : base(buffer)
    {
      _ffInfo = ffInfo;
      font = new TYPE1_Font();
      _flexArr = new float[14]; // 14 is max size of flex arguments
    }

    public void LoadFont()
    {
      SkipHeader();
      Interpreter();
    } 

    private void Interpreter()
    {
      
      ParseFontDictionary();
      // we can skip to this by offseting with Length1 (length of clear  portion of the program)
      // length of it is Length2
      // TODO: Apparently this can be ascii string as well, look into this
      byte[] privateDictRaw = DecryptPrivateDictionary();
      Debug.Assert(privateDictRaw.Length > 0);
      File.WriteAllBytes(Files.RootFolder + @$"\{_ffInfo.FontDescriptor.FontName}-decryptedEEXEC.txt", privateDictRaw);
      ParsePrivateDictionary(font, privateDictRaw);
    }

    public override bool IsCurrentCharPartOfOperator()
    {
      if ((__char >= 65 && __char <= 90) || (__char >= 97 && __char <= 122) || __char == '\'' || __char == '\"')
        return true;
      return false;
    }

    // 6.2 Charstring Number Encoding Adobe Type1 Font Specification
    // TODO optimize if checks and can shift right by 8 instead of multipyling by 256
    public override void InterpretCharString(byte[] data, Stack<float> opStack, TYPE1_Point2D lsb, TYPE1_Point2D currPoint, PSShape outShape, string name)
    {
      ReadOnlySpan<byte> buffer = data.AsSpan();
      byte v = 0;
      int num = 0;
      for (int i = 0; i < buffer.Length; i++)
      {
        v = buffer[i];
        if (v >= 32 && v <= 246)
        {
          num = v - 139;
          opStack.Push(num);
          Log(num.ToString());
        }
        else if (v >= 247 && v <= 250)
        {
          num = ((v - 247) * 256) + buffer[++i] + 108;
          opStack.Push(num);
          Log(num.ToString());
        }
        else if (v >= 251 && v <= 254)
        {
          num = (-((v - 251) * 256)) - buffer[++i] - 108;
          opStack.Push(num);
          Log(num.ToString());
        }
        else if (v == 255)
        {
          num = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(++i, 4));
          i += 3;
          opStack.Push(num);
          Log(num.ToString());
        }
        else
        {
          // Char commands
          switch (v)
          {
            case 1: // hstem
              opStack.Clear();
              Log("hstem");
              break; 
            case 3: // vstem
              opStack.Clear();
              Log("vstem");
              break;
            case 4: // vmoveto
              currPoint.Y += opStack.Pop();
              if (_flex)
              {
                _flexArr[_flexIndex++] = currPoint.X;
                _flexArr[_flexIndex++] = currPoint.Y;
              }
              else
              {
                outShape.MoveTo(currPoint.X, currPoint.Y);
              }
              opStack.Clear();
              Log("vmoveto");
              break;
            case 5: // rlineto
              currPoint.Y += opStack.Pop();
              currPoint.X += opStack.Pop();
              outShape.LineTo(currPoint.X, currPoint.Y);
              opStack.Clear();
              Log("rlineto");
              break;
            case 6: // hlineto
              currPoint.X += opStack.Pop();
              outShape.LineTo(currPoint.X, currPoint.Y);
              opStack.Clear();
              Log("hlineto");
              break;
            case 7: // vlineto
              currPoint.Y += opStack.Pop();
              outShape.LineTo(currPoint.X, currPoint.Y);
              opStack.Clear();
              Log("vlineto");
              break;
            case 8: // rrcurveto
              float dy3 = opStack.Pop();
              float dx3 = opStack.Pop();

              float dy2 = opStack.Pop();
              float dx2 = opStack.Pop();

              float dy1 = opStack.Pop();
              float dx1 = opStack.Pop();

              outShape.CurveTo(currPoint.X + dx1, currPoint.Y + dy1,
                currPoint.X + dx1 + dx2, currPoint.Y + dy1 + dy2,
                currPoint.X + dx1 + dx2 + dx3, currPoint.Y + dy1 + dy2 + dy3);
              currPoint.X += dx1 + dx2 + dx3;
              currPoint.Y += dy1 + dy2 + dy3;
              opStack.Clear();
              Log("rrcurveto");
              break;
            case 9: // closepath
              // draw line to last moveto, but do not move point to there
              // its different to normal PS command
              // TODO: Maybe have all these methods to be part of PSinterpreter that can be overriden by childen interpteres
              if (outShape._moves.Count > 0 && outShape._moves.Last() == PS_COMMAND.CLOSEPATH)
                break;

              int pos = outShape._shapePoints.Count - 1;
              float targetX = 0;
              float targetY = 0;
              // get last move to
              for (int j = outShape._moves.Count - 1; j >= 0; j--)
              {
                PS_COMMAND move = outShape._moves[j];
                if (move == PS_COMMAND.CURVE_TO)
                {
                  pos -= 6;
                } 
                else if (move == PS_COMMAND.LINE_TO)
                {
                  pos -= 2;
                } else if (move == PS_COMMAND.MOVE_TO)
                {
                  targetY = outShape._shapePoints[pos--];
                  targetX = outShape._shapePoints[pos];
                  break;
                }
              }

              outShape.LineTo(targetX, targetY); // draw line
              outShape.MoveTo(currPoint.X, currPoint.Y); // move it back so that when we convert to vertexes we stay in same place
              outShape._actualLast = PS_COMMAND.CLOSEPATH; // assign close path so that we can check if last one was closepath
              Log("closepath");
              break;
            case 10: // callsubr
              int subr = (int)opStack.Pop();
              if (subr == 0)
              {
                Debug.Assert(_flexIndex == 14);
                // Flex uses 2 bezier curves to create one precise shallow curve
                outShape.CurveTo(_flexArr[2], _flexArr[3], _flexArr[4], _flexArr[5], _flexArr[6], _flexArr[7]);
                outShape.CurveTo(_flexArr[8], _flexArr[9], _flexArr[10], _flexArr[11], _flexArr[12], _flexArr[13]);
                _flex = false;
                _flexIndex = 0;
                opStack.Clear();
              }
              else if (subr == 1)
              {
                _flex = true;
                _flexIndex = 0;
                opStack.Clear();
              }
              else if (subr == 2)
              {
                Debug.Assert(_flex);
                opStack.Clear();
              }
              else if (subr > 3)
              {
                // not sure if i sohuld be checking 1 here,
                Debug.Assert(font.FontDict.Private.Subrs[subr]?.Length > 1);
                _subroutineCallCount++;
                if (_subroutineCallCount > 10)
                  throw new Exception("Limit of MAX 10 subroutine calls at once exceeded!");
                else
                {
                  InterpretCharString(font.FontDict.Private.Subrs[subr], opStack, lsb, currPoint, outShape, $"othersubr_{subr}");
                }
                _subroutineCallCount--;
                  
              }
              
              Log("callsubr");
              break;
            case 11: // return
              Log("return");
              return;
            case 12: // escape
              switch (buffer[++i])
              {
                case 0: // dotsection
                  opStack.Clear();
                  Log("dotsection");
                  break;
                case 1: // vstem3
                  opStack.Clear();
                  Log("vstem3");
                  break;
                case 2: // hstem3
                  opStack.Clear();
                  Log("hstem3");
                  break;
                case 6: // seac - standard encoding accented character
                  float x0 = opStack.Pop();
                  float y0 = opStack.Pop();
                  float x1 = opStack.Pop();
                  float y1 = opStack.Pop();
                  float x2 = opStack.Pop();

                  // do something
                  opStack.Clear();
                  Log("seac");
                  break;
                case 7: // sbw
                  outShape._width.Y = opStack.Pop();
                  outShape._width.X = opStack.Pop();
                  currPoint.Y = opStack.Pop();
                  currPoint.X = opStack.Pop();
                  lsb.X = currPoint.X;
                  lsb.Y = currPoint.Y;
                  opStack.Clear();
                  Log("sbw");
                  break;
                case 12: // div
                  float num1 = opStack.Pop();
                  float num2 = opStack.Pop();
                  opStack.Push(num1 / num2);
                  Log("div");
                  break;
                case 16: // callothersubr
                  int oSubr = (int)opStack.Pop();
                  int count = (int)opStack.Pop();

                  if (oSubr == 0)
                  {
                    // push only 2 args
                    __numberOperands.Push(opStack.Pop());
                    __operandTypes.Push(PDF.OperandType.DOUBLE);
                    __numberOperands.Push(opStack.Pop());
                    __operandTypes.Push(PDF.OperandType.DOUBLE);
                  }
                  else if (oSubr == 3)
                  {
                    __numberOperands.Push(3);
                    __operandTypes.Push(PDF.OperandType.DOUBLE);
                  }
                  else
                  {
                    for (int j = 0; j < count; j++)
                    {
                      __numberOperands.Push(opStack.Pop());
                      __operandTypes.Push(PDF.OperandType.DOUBLE);
                    }
                  }
                  Log("callothersubr");
                  break;
                case 17: // pop
                  // push number from interpreter stack to buildchar stack
                  opStack.Push((float)PopNumber());
                  Log("pop");
                  break;
                case 33: // setcurrentpoint
                  currPoint.Y = opStack.Pop();
                  currPoint.X = opStack.Pop();
                  // even tho docs say not to do this, we have to so that we know what to do when we are drawing
                  outShape.MoveTo(currPoint.X, currPoint.Y);
                  opStack.Clear();
                  Log("setcurrentpoint");
                  break;
                default:
                  Log(v.ToString());
                  SaveLog();
                  throw new InvalidDataException($"Invalid command: {v}");
              }
              break;
            case 13: // hsbw - horizontal side beararing and width
              outShape._width.X = opStack.Pop();
              currPoint.X = opStack.Pop();
              lsb.X = currPoint.X;
              opStack.Clear();
              Log("hsbw");
              break;
            case 14: // endchar
              Log("endchar");
              SaveLog(name);
              return;
            case 21: // rmoveto
              currPoint.Y += opStack.Pop();
              currPoint.X += opStack.Pop();

              if (_flex)
              {
                _flexArr[_flexIndex++] = currPoint.X;
                _flexArr[_flexIndex++] = currPoint.Y;
              }
              else
              {
                outShape.MoveTo(currPoint.X, currPoint.Y);
              }
                
              opStack.Clear();
              Log("rmoveto");
              break;
            case 22: // hmoveto
              currPoint.X += opStack.Pop();
              if (_flex)
              {
                _flexArr[_flexIndex++] = currPoint.X;
                _flexArr[_flexIndex++] = currPoint.Y;
              }
              else
              {
                outShape.MoveTo(currPoint.X, currPoint.Y);
              }
              opStack.Clear();
              Log("hmoveto");
              break;
            case 30: // vhcurveto
              dx3 = opStack.Pop();
              dy2 = opStack.Pop();
              dx2 = opStack.Pop();
              dy1 = opStack.Pop();

              outShape.CurveTo(currPoint.X, currPoint.Y + dy1,
                currPoint.X + dx2, currPoint.Y + dy1 + dy2,
                currPoint.X + dx2 + dx3, currPoint.Y + dy1 + dy2);

              currPoint.X += dx2 + dx3;
              currPoint.Y += dy1 + dy2;
              opStack.Clear();
              Log("vhcurveto");
              break;
            case 31: // hvcurveto
              dy3 = opStack.Pop();
              dy2 = opStack.Pop();
              dx2 = opStack.Pop();
              dx1 = opStack.Pop();

              outShape.CurveTo(currPoint.X + dx1, currPoint.Y,
                currPoint.X + dx1 + dx2, currPoint.Y + dy2,
                currPoint.X + dx1 + dx2, currPoint.Y + dy2 + dy3);

              currPoint.X += dx1 + dx2;
              currPoint.Y += dy2 + dy3;
              opStack.Clear();
              Log("hvcurveto");
              break;
            default:
              Log(v.ToString());
              SaveLog(name);
              throw new InvalidDataException($"Invalid command: {v}");
          }
        }
      }

      SaveLog(name);
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

    private void ParseFontDictionary()
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

    private void ParseSubrs()
    {
      ParseSubrs(font);
    }

    // TODO: Not sure if ParseSubrs and PArseCharStrings should be in PSIntrepreter class
    // this has option to be passed because we call it from helper inside helper
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
    
    private void ParseCharStrings()
    {
      ParseCharStrings(font);
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
    // We don't look for %%EndComments because some font files just don't contain them
    // Search for dict begin instead
    private void SkipHeader()
    {
      // skip header for now
      bool found = false;
      while (!found && __position <= __buffer.Length)
      {
        SkipUntilAfterString("dict".AsSpan());
        IsNextStringAsSpan("begin".AsSpan(), ref found);
      }
    }

    private void IsNextStringAsSpan(ReadOnlySpan<char> strToCmp, ref bool found)
    {
      ReadOnlySpan<byte> token = new ReadOnlySpan<byte>();
      GetNextStringAsSpan(ref token);
      if (strToCmp.Length == token.Length)
      {
        for (int i = 0; i < strToCmp.Length; i++)
        {
          if (strToCmp[i] != token[i])
          {
            found = false;
            return;
          }
        }
        found = true;
      }
      else
      {
        found = false;
      }
      
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
