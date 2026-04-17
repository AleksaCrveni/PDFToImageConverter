using Converter.Converters;
using Converter.Converters.Image.TIFF;
using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.Parsers.Fonts;
using Converter.Parsers.ICC;
using Converter.Rasterizers;
using Converter.StaticData;
using Converter.Utils;
using Converter.Writers.TIFF;
using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
namespace Converter.Parsers.PDF
{
  // Note to myself - when dealing with variables that are indirect references add 'IR' on the end of the name
  // TODO: Am I stupid or i can just compare characters directly instead of bytes........................
  // TODO: Decide constats for magic numbers for stackallock
  public class PdfParser
  {
    private readonly byte _newLineByte = 10;
    private readonly int KB = 1024;

    delegate void ParseAnnotSubTypeData(PDFFile file, IPDF_AnnotData annot, ref PDFSpanParseHelper helper, string key);
    delegate void ParseAnnotActionSubTypeData(PDFFile file, IPDF_AnnotActionData action, ref PDFSpanParseHelper helper, string key);
    public void SaveStingRepresentationToDisk(string filepath)
    {
      byte[] arr = File.ReadAllBytes(filepath);
      char[] cArr = new char[arr.Length];
      StringBuilder sb = new StringBuilder();
      List<string> lines = new();
      char c = '-';
      for (int i = 0; i < arr.Length; i++)
      {
        cArr[i] = (char)arr[i];
        c = (char)arr[i];
        // TODO: Improve EOL check - (SP CR, SP LF, CR LF) are possible Eend of lines
        if (c == '\n')
        {
          lines.Add(sb.ToString());
          sb.Clear();
        }
        else
        {
          sb.Append(c);
        }
      }

      File.WriteAllLines(filepath.Replace("pdf", "txt"), lines);
    }

    // Maybe i shouldn't tie input stream to PDFFIle and just pass it separately
    /// <summary>
    /// File.Stream will be closed and disposed eventually, do not write to it
    /// </summary>
    /// <param name="filepath"></param>
    /// <param name="options"></param>
    /// <returns>
    /// 
    /// </returns>
    public PDFFile Parse(string filepath, ref PDF_Options options, bool DEBUG = false)
    {
      PDFFile file = new PDFFile();
      Stream inStream = File.OpenRead(filepath);
      // have this better just hardcode for now
      Stream outStream = File.Create("convertTest.tiff");
      // go to end to find byte offset to cross refernce table
      Parse(file, inStream, outStream, ref options, DEBUG);
      inStream.Close();
      outStream.Flush();
      outStream.Close();
      return file;

    }

    /// <summary>
    /// Whoever passes the streams is responsible for disposing them
    /// </summary>
    /// <param name="file"></param>
    /// <param name="inputStream"></param>
    /// <param name="outputStream"></param>
    /// <param name="options"></param>
    public void Parse(PDFFile file, Stream inputStream, Stream outputStream, ref PDF_Options options, bool DEBUG = false)
    {
      file.Stream = inputStream;
      file.Options = options;
      ReadInitialData(file, outputStream, DEBUG);
    }


    // Read PDFVersion, Byte offset for last cross reference table, file trailer

    void ReadInitialData(PDFFile file, Stream outStream, bool DEBUG)
    {
      file.PdfVersion = ParsePdfVersionFromHeader(file.Stream);
      ParseTrailersAndCrossReferenceData(file);
      ParseCatalogDictionary(file, (file.Trailer.RootIR.Item1, file.Trailer.RootIR.Item2));
      ParseRootPageTree(file, (file.Catalog.PagesIR.Item1, file.Catalog.PagesIR.Item2));
      ParsePagesData(file);
      if (!DEBUG)
        ConvertPageDataToImage(file, outStream);
    }

    private void ConvertPageDataToImage(PDFFile file, Stream outStream)
    {
      byte[] rawContent = file.PageInformation[0].ContentDict.RawStreamData;
      PDF_ResourceDict rDict = file.PageInformation[0].ResourceDict;

      // TODO: make this later based on some mode, to be to convert to other file formats as well
      // TODO: save in conveter later
      IConverter converter = file.Target switch
      {
        TargetConversion.TIFF_BILEVEL => throw new NotImplementedException(),
        TargetConversion.TIFF_GRAYSCALE => new TIFFGrayscaleConverter(rDict.Font, rDict, file.PageInformation[0], SourceConversion.PDF, new TIFFWriterOptions(), outStream),
        TargetConversion.TIFF_PALLETE => throw new NotImplementedException(),
        TargetConversion.TIFF_RGB => throw new NotImplementedException(),
      };
    
      PDFGOInterpreter pdfGo = new PDFGOInterpreter(rawContent, rDict, converter);
      pdfGo.ConvertToPixelData();
    }

    // TODO: process resource and content in parallel?
    private void ParsePagesData(PDFFile file)
    {
      PDF_PageInfo pInfo;
      for (int i = 0; i < file.PageInformation.Count; i++)
      {
        pInfo = file.PageInformation[i];

        // Do content first since its easier to log it if unsupported font type is used
        // Process Contents
        PDF_CommonStreamDict contentDict = new PDF_CommonStreamDict();
        ParsePageContents(file, file.PageInformation[i].ContentsIR, ref contentDict);
        pInfo.ContentDict = contentDict;

        //File.WriteAllBytes(Path.Join(Files.RootFolder, "Prijemni-1_content.txt"), contentDict.RawStreamData);
        // Process Resources
        PDF_ResourceDict resourceDict = new PDF_ResourceDict();
        ParseResourceDictionary(file, file.PageInformation[i].ResourcesIR, resourceDict);
        pInfo.ResourceDict = resourceDict;

        file.PageInformation[i] = pInfo;
      }
    }

    // Wrap for now if i have to do extra work later
    private void ParsePageContents(PDFFile file, List<(int objIndex, int generation)> objPositions, ref PDF_CommonStreamDict contentDict)
    {
      PDF_CommonStreamDict intermDict = new PDF_CommonStreamDict();
      // Use list for now since it will double internal buffer when it fills it
      // TODO: see if we can just story array of conntents and process them like that in PDFGOInterpreter,
      // but I image that would add unneccesary complexity object will be using only RawStreamData Field
      // NOTE: this does mem copy for now and its not ideal solution it also might brick with large content pages
      List<byte> buff = new List<byte>();
      foreach ((int objIndex, int generation) objPosition in objPositions)
      {
        ParseCommonStream(file, objPosition, ref intermDict);
        buff.AddRange(intermDict.RawStreamData);
      }
      contentDict.RawStreamData = buff.ToArray();
    }

    private void ParseCommonStream(PDFFile file, (int objIndex, int generation) objPosition, ref PDF_CommonStreamDict dict)
    {
      SharedAllocator allocator = GetObjBuffer(file, objPosition);
      ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);
        PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);

      helper.GoToStartOfDict();
      string tokenString = helper.GetNextToken();
      while (tokenString != string.Empty)
      {
        ParseCommonStreamDictAsExtension(file, ref helper, tokenString, ref dict);
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper.IsEndOfDict())
          break;
        tokenString = helper.GetNextToken();
      }
      helper.SkipNextToken(); // stream
      helper.SkipWhiteSpace();
      ReadOnlySpan<byte> encodedSpan = buffer.Slice(helper._position, (int)dict.Length);
      dict.RawStreamData = DecompressionHelper.DecodeFilters(ref encodedSpan, dict.Filters);
      FreeAllocator(allocator);
    }

    private void ParseFontFileDictAndStream(PDFFile file, (int objectIndex, int) objPosition, ref PDF_FontFileInfo fontFileInfo, ref PDF_CommonStreamDict commonStreamDict)
    {
      SharedAllocator allocator = GetObjBuffer(file, objPosition);
      ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);
      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);

      bool startDictFound = false;
      while (!startDictFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          startDictFound = helper.IsCurrentCharacterSameAsNext();
      }

      string tokenString = helper.GetNextToken();
      // TODO: do better validation here
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "Length1":
            fontFileInfo.Length1 = helper.GetNextInt32();
            break;
          case "Length2":
            fontFileInfo.Length2 = helper.GetNextInt32();
            break;
          case "Length3":
            fontFileInfo.Length3 = helper.GetNextInt32();
            break;
          case "Subtype":
            fontFileInfo.Subtype = helper.GetNextName<PDF_FontFileSubtype>();
            break;
          case "Metadata":
            // IR, has XMP syntax, 
            break;
          default:
            ParseCommonStreamDictAsExtension(file, ref helper, tokenString, ref commonStreamDict);
            break;
        }
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");

      tokenString = helper.GetNextToken();
      if (tokenString != "stream")
        throw new InvalidDataException("Expected stream!");

      long encodedStreamLen = commonStreamDict.Length;

      // go to next line
      helper.SkipWhiteSpace();
      ReadOnlySpan<byte> encodedSpan = buffer.Slice(helper._position, (int)encodedStreamLen);
      commonStreamDict.RawStreamData = DecompressionHelper.DecodeFilters(ref encodedSpan, commonStreamDict.Filters);
      fontFileInfo.CommonStreamInfo = commonStreamDict;
      FreeAllocator(allocator);
    }

    private void ParseCommonStreamDictAsExtension(PDFFile file, ref PDFSpanParseHelper helper, string tokenString, ref PDF_CommonStreamDict dict)
    {
      switch (tokenString)
      {
        // docs say that this is direct value, but i've seen it being IR in some files so account for that?
        case "Length":
          // TODO: Can this be long? test later with huge files
          int firstNumber = helper.GetNextInt32();
          helper.SkipWhiteSpace();
          if (char.IsDigit((char)helper._char))
          {
            // its IR value so read it all and temporary jump to read value
            long IRByteOffset = file.CrossReferenceEntries[firstNumber].TenDigitValue;
            long currPosition = file.Stream.Position;
            file.Stream.Position = IRByteOffset;

            Span<byte> irBuffer = stackalloc byte[KB / 4]; // 256
            PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);
            file.Stream.Read(irBuffer);
            irHelper.SkipNextToken(); // object id
            irHelper.SkipNextToken(); // seocnd number
            irHelper.SkipNextToken(); // 'obj'
            firstNumber = irHelper.GetNextInt32();

            helper.SkipNextToken();
            file.Stream.Position = currPosition;
          }
          dict.Length = firstNumber;
          // continue because we alreayd loaded next string
          break;
        case "Filter":
          // this will work even if there is one filter and its not date
          dict.Filters = helper.GetListOfNames<ENCODING_FILTER>();
          break;
        default:
          break;
      }
    }

    private void ParseResourceDictionary(PDFFile file, (int objIndex, int generation) objPosition, PDF_ResourceDict resourceDict)
    {
      SharedAllocator allocator = GetObjBuffer(file, objPosition);
      ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);

      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      ParseResourceDictionary(file, ref helper, true, resourceDict);
      FreeAllocator(allocator);
     }

    private void ParseResourceDictionary(PDFFile file, ref PDFSpanParseHelper helper, bool isIndirect, PDF_ResourceDict resourceDict)
    {
      bool dictStartFound = !isIndirect;
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }

      string tokenString = helper.GetNextToken();
      SharedAllocator? irAllocator = null;
      ReadOnlySpan<byte> irSpan;
      
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "ExtGState":
            resourceDict.ExtGState = helper.GetNextDict();
            break;
          case "ColorSpace":
            // NOTE: skip[ for now beacuase i have no idea how to parse
            List<PDF_ColorSpace> csData = new List<PDF_ColorSpace>();
            helper.SkipWhiteSpace();
            
            if (helper._char == '<')
            {
              ParseColorSpaceDict(file, ref helper, csData);
            }
            else
            {
              #region memAllocAndHelper
              (int objIndex, int generation) objPosition = helper.GetNextIndirectReference();
              irAllocator = GetObjBuffer(file, objPosition);
              irSpan = irAllocator.Buffer.AsSpan(irAllocator.Range);
              PDFSpanParseHelper csIrAllocator = new PDFSpanParseHelper(ref irSpan);
              PDFSpanParseHelper csIrHelper = new PDFSpanParseHelper(ref irSpan);
              #endregion memAllocAndHelper
              ParseColorSpaceDict(file, ref csIrHelper, csData);
              #region freeMem
              FreeAllocator(irAllocator);
              #endregion freeMem
            }
            resourceDict.ColorSpace = csData;
            break;
          case "Pattern":
            resourceDict.Pattern = helper.GetNextDict();
            break;
          case "Shading":
            resourceDict.Shading = helper.GetNextDict();
            break;
          case "XObject":
            resourceDict.XObject = helper.GetNextDict();
            break;
          case "Font":
            List<PDF_FontData> fontData = new List<PDF_FontData>();

            (bool isDirect, SharedAllocator? allocator) info = ReadIntoDirectOrIndirectDict(file, ref helper);
            if (info.isDirect)
            {
              ParseFontIRDictionary(file, ref helper, true, fontData);
            }
            else
            {
              ReadOnlySpan<byte> irBuffer = info.allocator!.Buffer.AsSpan(info.allocator.Range);
              PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);
              ParseFontIRDictionary(file, ref irHelper, false, fontData);
            }
            FreeAllocator(info.allocator);
            resourceDict.Font = fontData;
            break;
          case "ProcSet":
            resourceDict.ProcSets = helper.GetListOfNames<PDF_ProcedureSet>();
            break;
          case "Properties":
            resourceDict.Properties = helper.GetNextDict();
            break;
          default:
            break;
        }

        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");
    }

    private void ParseColorSpaceDict(PDFFile file, ref PDFSpanParseHelper helper, List<PDF_ColorSpace> list)
    {
      // this should work in both cases where we are at < and when we literally load object with header and stuff
      helper.GoToStartOfDict();
      while (helper._char != PDFConstants.NULL)
      {
        PDF_ColorSpace cs = new PDF_ColorSpace();
        cs.Key = helper.GetNextToken();
        helper.SkipWhiteSpace();
        if (helper.IsCurrentByteDigit())
        {
          #region memAllocAndHelper
          (int objIndex, int generation) objPosition = helper.GetNextIndirectReference();
          SharedAllocator allocator = GetObjBuffer(file, objPosition);
          ReadOnlySpan<byte> irSpan = allocator.Buffer.AsSpan(allocator.Range);
          PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irSpan);
          #endregion memAllocAndHelper

          ParseColorSpaceData(file, ref irHelper, cs);
          #region freeMem
          FreeAllocator(allocator);
          #endregion freeMem
        }
        else if (helper._char == '[')
        {
          ParseColorSpaceData(file, ref helper, cs);
        }
        else
        {
          cs.Family = helper.GetNextName<PDF_ColorSpaceFamily>();
        }

        list.Add(cs);
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if ((helper._char == '>' && helper.IsCurrentCharacterSameAsNext()) )
          break;
      }
      helper.ReadChar();
    }

    // th is is pretty much just a dispatch function
    private void ParseColorSpaceData(PDFFile file, ref PDFSpanParseHelper helper, PDF_ColorSpace cs)
    {
      // we dont have to wroy about getting into array or skipping obj header
      cs.Family = helper.GetNextName<PDF_ColorSpaceFamily>();
      cs.HasExtraData = true;
      IPDF_ExtraColorSpaceData extraData = null;
      switch (cs.Family)
      {
        case PDF_ColorSpaceFamily.DeviceGray:
          throw new NotImplementedException();
          break;
        case PDF_ColorSpaceFamily.DeviceRGB:
          // has no extra data
          cs.HasExtraData = false;
          break;
        case PDF_ColorSpaceFamily.DeviceCMYK:
          throw new NotImplementedException();
          break;
        case PDF_ColorSpaceFamily.CalGray:
          throw new NotImplementedException();
          break;
        case PDF_ColorSpaceFamily.CalRGB:
          throw new NotImplementedException();
          break;
        case PDF_ColorSpaceFamily.Lab:
          throw new NotImplementedException();
          break;
        case PDF_ColorSpaceFamily.ICCBased:
          extraData = new PDF_ICCExtraData();
          helper.SkipWhiteSpace();
          if (helper.IsCurrentByteDigit())
          {
            #region memAllocAndHelper
            (int objIndex, int generation) objPosition = helper.GetNextIndirectReference();
            SharedAllocator allocator = GetObjBuffer(file, objPosition);
            ReadOnlySpan<byte> irSpan = allocator.Buffer.AsSpan(allocator.Range);
            PDFSpanParseHelper ICCHelper = new PDFSpanParseHelper(ref irSpan);
            #endregion memAllocAndHelper
            ParseICCBasedCS(file, ref ICCHelper, extraData);
            #region freeMem
            FreeAllocator(allocator);
            #endregion freeMem
          }
          else
          {
            ParseICCBasedCS(file, ref helper, extraData);
          }
          break;
        case PDF_ColorSpaceFamily.Indexed:
          throw new NotImplementedException();
          break;
        case PDF_ColorSpaceFamily.Pattern:
          extraData = new PDF_PatternExtraData();
          ParsePatternCS(file, ref helper, extraData);
          break;
        case PDF_ColorSpaceFamily.Separation:
          throw new NotImplementedException();
          break;
        case PDF_ColorSpaceFamily.DeviceN:
          throw new NotImplementedException();
          break;
        case PDF_ColorSpaceFamily.NULL:
        default:
          throw new InvalidDataException("Invalid ColorSpace data Family!");
          break;
      }

      cs.ExtraCSData = extraData;
    }

    // array of IRs with keys 
    // i.e [ /ICCBased{we are here} 7 0 R]
    private void ParseICCBasedCS(PDFFile file, ref PDFSpanParseHelper helper, IPDF_ExtraColorSpaceData extra)
    {
      PDF_ICCExtraData data = (PDF_ICCExtraData)extra;

      helper.GoToStartOfDict();
      string tokenString = helper.GetNextToken();
      // TODO: do better validation here
      PDF_CommonStreamDict commonStreamDict = new PDF_CommonStreamDict();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "N":
            data.N = helper.GetNextInt32();
            break;
          case "Alternate":
            PDF_ColorSpaceFamily cs = helper.GetNextName<PDF_ColorSpaceFamily>();
            if (cs == PDF_ColorSpaceFamily.NULL)
              throw new InvalidDataException("Alternate color space invalid!");

            data.Alternate = cs;
            break;
          case "Range":
            throw new NotImplementedException();
            break;
          case "Metadata":
            throw new NotImplementedException();
            break;
          default:
            ParseCommonStreamDictAsExtension(file, ref helper, tokenString, ref commonStreamDict);
            break;
        }

        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;

        tokenString = helper.GetNextToken();
      }

      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");

      helper.SkipNextToken(); // skip stream
      helper.SkipWhiteSpace(); // skip LF
      ReadOnlySpan<byte> encodedSpan = helper._buffer.Slice(helper._position, (int)commonStreamDict.Length);
      commonStreamDict.RawStreamData = DecompressionHelper.DecodeFilters(ref encodedSpan, commonStreamDict.Filters);

      //#if DEBUG
      //  File.WriteAllBytes(Path.Join(Files.RootFolder, "Sample-ICCSample.txt"), commonStreamDict.RawStreamData);
      //#endif
      data.CommonStreamDict = commonStreamDict;

      ICCParser iCCParser = new ICCParser(commonStreamDict.RawStreamData);
      iCCParser.Parse();
    }

    private void ParsePatternCS(PDFFile file, ref PDFSpanParseHelper helper, IPDF_ExtraColorSpaceData extra)
    {
      PDF_PatternExtraData data = (PDF_PatternExtraData)extra;
      PDF_ColorSpace cs = new PDF_ColorSpace();
      ParseColorSpaceData(file, ref helper, cs);
      data.ColorSpace = cs;
    }

    private void ParseRGBCS(PDFFile file, ref PDFSpanParseHelper helper, IPDF_ExtraColorSpaceData extra)
    {

    }

    private void ParseFontIRDictionary(PDFFile file, ref PDFSpanParseHelper helper, bool dictOpen, List<PDF_FontData> fontData)
    {
      bool dictStartFound = dictOpen;
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }

      string key;
      PDF_FontInfo fontInfo;
      long objectByteOffset = 0;
      long objectLength = 0;
      PDF_FontData fd;
      IRasterizer ttfParser;

      List<(string key, (int objIndex, int generation) objPosition)> objPositions = new List<(string key, (int objIndex, int generation) objPosition)>();

      while (helper._char != '>' && !helper.IsCurrentCharacterSameAsNext())
      {
        key = helper.GetNextToken();
        if (key == "")
          break;
        
        (int objIndex, int generation) IR = helper.GetNextIndirectReference();

        objPositions.Add((key, IR));
        helper.ReadUntilNonWhiteSpaceDelimiter();
      }
      // skip end of dict
      helper.ReadChar();
      helper.ReadChar();
      SharedAllocator allocator = null;
      ReadOnlySpan<byte> irBuffer;
      int largestObjectSize = GetBiggestObjectSizeFromList(file, objPositions);
      if (largestObjectSize > 0)
        ForceCreateArrayInsharedPool(largestObjectSize);
      // name -> key
      foreach ((string name, (int objIndex, int generation) objPosition) in objPositions)
      {
        fontInfo = new PDF_FontInfo();
        allocator = GetObjBuffer(file, objPosition);
        irBuffer = allocator.Buffer.AsSpan(allocator.Range);

        fd = new PDF_FontData();
        ParseFontDictionary(file, irBuffer, ref fontInfo);
        fd.Key = name;
        fd.FontInfo = fontInfo;
        // TODO: assume that data is filled ? 
        // TODO: create right rasterized based on subtype and fontfile // Do I still eneed to do this?
#if DEBUG
        if (fontInfo.FontDescriptor != null)
          File.WriteAllBytes(Files.RootFolder + @$"\{fontInfo.FontDescriptor.FontName}" + @"-fontFile.txt", fontInfo.FontDescriptor.FontFile.CommonStreamInfo.RawStreamData);
        else
        {
          foreach(var entry in fontInfo.DescendantFontsInfo)
          {
            File.WriteAllBytes(Files.RootFolder + @$"\Composite_{entry.DescendantDict.BaseFont}" + @"-fontFile.txt", entry.DescendantDict.FontDescriptor.FontFile.CommonStreamInfo.RawStreamData);
          }
        }
#endif

        IRasterizer rasterizer = fontInfo.SubType switch
        {
          PDF_FontType.Null => throw new NotImplementedException(),
          PDF_FontType.Type0 => new CompositeFontRasterizer(fontInfo.DescendantFontsInfo[0].DescendantDict.FontDescriptor.FontFile.CommonStreamInfo.RawStreamData, fontInfo),
          PDF_FontType.Type1 => new Type1Rasterizer(fontInfo.FontDescriptor.FontFile.CommonStreamInfo.RawStreamData, ref fontInfo),
          PDF_FontType.MMType1 => throw new NotImplementedException(),
          PDF_FontType.Type3 => throw new NotImplementedException(),
          PDF_FontType.TrueType => new TTFRasterizer(fontInfo.FontDescriptor.FontFile.CommonStreamInfo.RawStreamData, ref fontInfo),
          PDF_FontType.CIDFontType0 => throw new NotImplementedException(),
          PDF_FontType.CIDFontType2 => throw new NotImplementedException(),
          PDF_FontType.OpenType => throw new NotImplementedException(),
        };
        fd.Rasterizer = rasterizer;
        fontData.Add(fd);
        FreeAllocator(allocator);
      }
    }


    /// <summary>
    /// Parse FontDictioanry data for ResourceDict that is referenced in from ParseFontIRDictionary.
    /// This is different thatn FileFont!!
    /// </summary>
    /// <param name="file"></param>
    /// <param name="buffer">Buffer where entire data is contained, unless some data is referenced in IR</param>
    /// <param name="dictOpen">True if we read into dict already because we aren't sure if its IR or dict in first place</param>
    /// <param name="fontInfo"></param>
    /// <returns>Number of bytes moved inside small buffer</returns>
    private void ParseFontDictionary(PDFFile file, ReadOnlySpan<byte> buffer, ref PDF_FontInfo fontInfo)
    {
      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      bool dictStartFound = false;
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }
      // TODO: I really don't think i should do nesting IR parsing
      // I should parse IRs and then after function si done check if there are any IRs to be parsed
      // because it seems that data at IR can depend on that that comes after indirect refernce in parent dictionary
      // i.e first and last char can come after Widths arr
      (int wIndex, int generation) widthIR = (-1, 0);
      double[] widthsArr;
      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "Subtype":
            fontInfo.SubType = helper.GetNextName<PDF_FontType>();
            break;
          case "Name":
            fontInfo.Name = helper.GetNextToken();
            break;
          case "BaseFont":
            fontInfo.BaseFont = helper.GetNextToken();
            break;
          case "FirstChar":
            fontInfo.FirstChar= helper.GetNextInt32();
            break;
          case "LastChar":
            fontInfo.LastChar = helper.GetNextInt32();
            break;
          case "Widths":
            helper.SkipWhiteSpace();
            // can be direct array or IR
            
            if (helper._char == '[')
            {
              widthsArr = new double[fontInfo.LastChar - fontInfo.FirstChar + 1];
              helper.ReadChar();
              for (int i = 0; i < widthsArr.Length; i++)
              {
                widthsArr[i] = helper.GetNextDouble();
              }
              helper.ReadUntilNonWhiteSpaceDelimiter();
              if (helper._char != ']')
                throw new InvalidDataException("Invalid end of widths array!");
              helper.ReadChar();
              fontInfo.Widths = widthsArr;
            } else if (char.IsDigit((char)helper._char))
            {
              widthIR = helper.GetNextIndirectReference();
            }
            else
            {
              throw new InvalidDataException("Invalid Widths value in Font Dictionary!");
            }

            
            break;
          case "FontDescriptor":
            (int objectIndex, int _) ir = helper.GetNextIndirectReference();
            PDF_FontDescriptor fontDescriptor = new PDF_FontDescriptor();
            // prob should rename
            ParseFontDescriptor(file, ir, ref fontDescriptor);
            fontInfo.FontDescriptor = fontDescriptor;
            break;
          case "Encoding":
            // can be either name of IR to dict
            helper.SkipWhiteSpace();
            int bytesRead = 0;
            if (helper._char == '/')
            {
              fontInfo.EncodingData.BaseEncoding = helper.GetNextToken();
            }
            else
            {
              (int objIndex, int generation) IR = helper.GetNextIndirectReference();
              SharedAllocator irAllocator = GetObjBuffer(file, IR);
              ReadOnlySpan<byte> irBuffer = irAllocator.Buffer.AsSpan(irAllocator.Range);

              ParseFontEncodingDictionary(file, irBuffer, ref fontInfo.EncodingData);
              FreeAllocator(irAllocator);
            }
            break;
          case "DescendantFonts":
            // Spec says its array, but i've seen examples where writers just slap IR without array
            // but that will be handled helper method
            fontInfo.DescendantFontsIR = helper.GetNextIndirectReferenceList();
            break;
          case "ToUnicode":
            fontInfo.ToUnicodeIR = helper.GetNextIndirectReference();
            break;
          default:
            break;
        }

        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

      if (fontInfo.SubType == PDF_FontType.TrueType && fontInfo.EncodingData.BaseEncoding == string.Empty)
      {
        if (!((fontInfo.FontDescriptor.Flags & PDF_FontFlags.Symbolic) == PDF_FontFlags.Symbolic))
          fontInfo.EncodingData.BaseEncoding = "StandardEncoding";
      }

      //parse width if its IR
      if (widthIR.wIndex > -1)
      {
        SharedAllocator irAllocator = GetObjBuffer(file, widthIR);
        ReadOnlySpan<byte> irBuffer = irAllocator.Buffer.AsSpan(irAllocator.Range);

        widthsArr = new double[fontInfo.LastChar - fontInfo.FirstChar + 1];
        PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);
        // NOTE: sometime obj can be without obj num and other stuff, maybe when its compressed it doesnt have that?
        irHelper.ReadChar();
        if (irHelper._char != '[')
        {
          irHelper.SkipNextToken(); // object id
          irHelper.SkipNextToken(); // seocnd number
          irHelper.SkipNextToken(); // 'obj'
          irHelper.ReadUntilNonWhiteSpaceDelimiter();
        }

        while (irHelper._char != '[')
        {
          irHelper.ReadChar();
          irHelper.ReadUntilNonWhiteSpaceDelimiter();
        }

        for (int i = 0; i < widthsArr.Length; i++)
        {
          widthsArr[i] = irHelper.GetNextDouble();
        }

        irHelper.ReadUntilNonWhiteSpaceDelimiter();
        if (irHelper._char != ']')
          throw new InvalidDataException("Invalid end of widths array!");

        fontInfo.Widths = widthsArr;
        FreeAllocator(irAllocator);
      }

      // check if its composite Font
      if (fontInfo.SubType == PDF_FontType.Type0)
      {
        fontInfo.DescendantFontsInfo = new List<CompositeFontInfo>();
        foreach ((int objIndex, int generation) objPosition in fontInfo.DescendantFontsIR)
        {
          CompositeFontInfo cInfo = new CompositeFontInfo();
          CIDFontDictionary CIDFontDictionary = new CIDFontDictionary();

          ParseCIDFontDictionary(file, objPosition, CIDFontDictionary);
          cInfo.DescendantDict = CIDFontDictionary;

          PDF_CID_CMAP cmap = new PDF_CID_CMAP();
          ParseToUnicodeCMAP(file, fontInfo.ToUnicodeIR, cmap, fontInfo.EncodingData.BaseEncoding);
          cInfo.Cmap = cmap;
          fontInfo.DescendantFontsInfo.Add(cInfo);
        }
      }

      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");
    }

    private void ParseToUnicodeCMAP(PDFFile file, (int objIndex, int generation) objPosition, PDF_CID_CMAP cmap, string cmapEncoding)
    {
      PDF_CommonStreamDict dict = new PDF_CommonStreamDict();
      ParseCommonStream(file, objPosition, ref dict);

      ReadOnlySpan<byte> buffer = dict.RawStreamData.AsSpan();
      CIDCmapParserHelper helper = new CIDCmapParserHelper(ref buffer, cmapEncoding);
      helper.Parse(cmap);
    }

    /// <summary>
    /// Can be single element array [ IR ] or actual object?
    /// </summary>
    /// <param name="file"></param>
    /// <param name="objPosition"></param>
    private void ParseCIDFontDictionary(PDFFile file, (int objIndex, int generation) objPosition, CIDFontDictionary dict)
    {
      SharedAllocator allocator = GetObjBuffer(file, objPosition);
      ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);
      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      helper.SkipWhiteSpace();
      // idk what this is for
      if (helper._char == '[')
      {
        helper.ReadChar();
        objPosition = helper.GetNextIndirectReference();
        helper.SkipWhiteSpace();
        helper.ReadChar(); // ]
        FreeAllocator(allocator);
        allocator = GetObjBuffer(file, objPosition);
        buffer = allocator.Buffer.AsSpan(allocator.Range);
        helper = new PDFSpanParseHelper(ref buffer);
      }
      ParseCIDFontDictionary(file, ref helper, dict); 
      FreeAllocator(allocator);
    }

    private void ParseCIDFontDictionary(PDFFile file, ref PDFSpanParseHelper helper, CIDFontDictionary dict)
    {
      helper.GoToStartOfDict();
      string tokenString = helper.GetNextToken();
      while (tokenString != string.Empty)
      {
        switch (tokenString)
        {
          case "Subtype":
            dict.Subtype = helper.GetNextName<PDF_FontType>();
            break;
          case "BaseFont":
            dict.BaseFont = helper.GetNextToken();
            break;
          case "CIDSystemInfo":
            CIDSystemInfo CIDSystemInfo = new CIDSystemInfo();
            (bool isDirect, SharedAllocator? allocator) info = ReadIntoDirectOrIndirectDict(file, ref helper);
            if (info.isDirect)
            {
              ParseCIDSystemInfo(file, ref helper, CIDSystemInfo, true);
            }
            else
            {
              ReadOnlySpan<byte> irBuffer = info.allocator.Buffer.AsSpan(info.allocator.Range);
              PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);
              ParseCIDSystemInfo(file, ref irHelper, CIDSystemInfo, false);
            }
            FreeAllocator(info.allocator);
            dict.CIDSystemInfo = CIDSystemInfo;
            break;
          case "FontDescriptor":
            (int objIndex, int generation) objPosition = helper.GetNextIndirectReference();
            PDF_FontDescriptor fd = new PDF_FontDescriptor();
            ParseFontDescriptor(file, objPosition, ref fd);
            dict.FontDescriptor = fd;
            break;
          case "DW":
            dict.DW = helper.GetNextInt32();
            break;
          case "DW2":
            helper.SkipWhiteSpace();
            helper.ReadChar(); // skip '['
            dict.DW2[0] = helper.GetNextInt32();
            dict.DW2[1] = helper.GetNextInt32();
            helper.SkipWhiteSpace();
            helper.ReadChar(); // skip ']'
            break;
          case "W":
            // can this be IR?
            Dictionary<int, int> widths = new  Dictionary<int, int>();
            info = ReadIntoDirectOrIndirectArray(file, ref helper);
            if (info.isDirect)
            {
              ParseCIDDictWArray(file, ref helper, widths);
            }
            else
            {
              ReadOnlySpan<byte> irBuffer = info.allocator.Buffer.AsSpan(info.allocator.Range);
              PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);
              ParseCIDDictWArray(file, ref irHelper, widths);
            }
            dict.W = widths;
            break;
          case "W2":
            // vertical metrics
            Dictionary<int, (Vector2 vDisplacement, Vector2 position)> widths2 = new Dictionary<int, (Vector2 vDisplacement, Vector2 position)>();
            info = ReadIntoDirectOrIndirectArray(file, ref helper);
            if (info.isDirect)
            {
              ParseCIDDictW2Array(file, ref helper, widths2);
            }
            else
            {
              ReadOnlySpan<byte> irBuffer = info.allocator.Buffer.AsSpan(info.allocator.Range);
              PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);
              ParseCIDDictW2Array(file, ref irHelper, widths2);
            }
            dict.W2 = widths2;
            break;
          case "CIDToGIDMap":
            helper.SkipWhiteSpace();
            if (helper._char == '/')
            {
              helper.SkipNextToken();
              dict.CIDToGIDMapName = FileStructures.CompositeFonts.CIDToGIDMap.IDENTITY;
            } else
            {
              PDF_CommonStreamDict CIDToGIDMapData = new PDF_CommonStreamDict();
              objPosition = helper.GetNextIndirectReference();
              ParseCommonStream(file, objPosition, ref CIDToGIDMapData);
              dict.CIDToGIDMap = CIDToGIDMapData;
            }
            break;
          default:
            break;
        }
        if (helper.IsEndOfDict())
          break;
        tokenString = helper.GetNextToken();
      }

    }

    public void ParseCIDDictW2Array(PDFFile file, ref PDFSpanParseHelper helper, Dictionary<int, (Vector2 vDisplacement, Vector2 position)> widths)
    {
      int CIDStart = 0;
      double w1 = 0;
      double posX = 0;
      double posY = 0;
      int CIDEnd = 0;
      Vector2 vDisplacement;
      Vector2 position;
      while (helper._char != ']' && helper._readPosition < helper._buffer.Length)
      {
        CIDStart = helper.GetNextInt32();
        helper.SkipWhiteSpace();
        if (helper._char == '[')
        {
          helper.ReadChar(); // skip '['
          while (helper._char != ']' && helper._readPosition < helper._buffer.Length)
          {
            w1 = helper.GetNextDouble();
            posX = helper.GetNextDouble();
            posY = helper.GetNextDouble();
            vDisplacement = new Vector2(0, (float)w1);
            position = new Vector2((float)posX, (float)posY);
            widths.Add(CIDStart++, (vDisplacement, position));  
            helper.SkipWhiteSpace();
          }
          helper.ReadChar(); // skip ']'
        } else
        {
          CIDEnd = helper.GetNextInt32();
          w1 = helper.GetNextDouble();
          posX = helper.GetNextDouble();
          posY = helper.GetNextDouble();
          vDisplacement = new Vector2(0, (float)w1);
          position = new Vector2((float)posX, (float)posY);

          for (int i = CIDStart; i <= CIDEnd; i++)
          {
            widths.Add(i, (vDisplacement, position));
          }
        }
      }

      helper.ReadChar(); // ']'
    }

    public void ParseCIDDictWArray(PDFFile file, ref PDFSpanParseHelper helper, Dictionary<int, int> widths)
    {
      int CIDStart = 0;
      int CIDEnd = 0;
      int width = 0;
      int value = 0;
      while (helper._char != ']' && helper._readPosition < helper._buffer.Length)
      {
        CIDStart = helper.GetNextInt32();
        helper.SkipWhiteSpace();
        if (helper._char == '[')
        {
          helper.ReadChar(); // skip '['
          while(helper._char != ']' && helper._readPosition < helper._buffer.Length)
          {
            value = helper.GetNextInt32();
            widths.Add(CIDStart++, value);
            helper.SkipWhiteSpace();
          }
          helper.ReadChar(); // skip ']'
          
        } else
        {
          CIDEnd = helper.GetNextInt32();
          value = helper.GetNextInt32();

          /// i cant remebmer is this good
          for (int i = CIDStart; i <= CIDEnd; i++)
          {
            widths.Add(i, value);
          }
          
        }
        helper.SkipWhiteSpace();

      }
      helper.ReadChar(); // ']'
    }

    public void ParseCIDSystemInfo(PDFFile file, ref PDFSpanParseHelper helper, CIDSystemInfo info, bool inDict)
    {
      if (inDict)
        helper.ReadChar();
      else
        helper.GoToStartOfDict();
      string tokenString = helper.GetNextToken();
      while (tokenString != string.Empty)
      {
        switch (tokenString)
        {
          case "Registry":
            info.Registry = helper.GetNextStringLiteral();
            break;
          case "Ordering":
            info.Ordering = helper.GetNextStringLiteral();
            break;
          case "Supplement":
            info.Supplement = helper.GetNextInt32();
            break;
          default:
            break;
        }
        if (helper.IsEndOfDict())
          break;
        tokenString = helper.GetNextToken();
      }
      helper.ReadChar(); // move off first > so outside check doesnt think its end of that dict
    }

    private void ParseFontEncodingDictionary(PDFFile file, ReadOnlySpan<byte> buffer, ref PDF_FontEncodingData data)
    {
      // default value is StandardFont for nonsymbolic and for symbolic fonts its fon's encoding
      // so set null if it ssymbolic and not defined and later checked to skip it
      // we will set correction in parent function because encoding might come before font dictionary
      data.BaseEncoding = string.Empty;
      bool dictStartFound = false;
      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }

      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "BaseEncoding":
            data.BaseEncoding = helper.GetNextToken();
            break;
          case "Differences":
            helper.ReadUntilNonWhiteSpaceDelimiter();
            if (helper._char != '[')
              throw new InvalidDataException("Invalid start of Differences Array!");
            helper.ReadChar();
            int lastIndex = 0;
            helper.SkipWhiteSpace();
            while (helper._char != ']' || helper._char == PDFConstants.NULL)
            {

              if (helper.IsCurrentByteDigit())
                lastIndex = helper.GetNextInt32();
              else if (helper._char == '/')
              {

                data.Differences.Add((lastIndex, helper.GetNextToken()));
                lastIndex++;
              }

              helper.SkipWhiteSpace();
            }
            break;
          default:
            break;
        }
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

    }

    private void ParseAnnots(PDFFile file, List<(int objIndex, int generation)> objPositions, List<PDF_Annot> list)
    {
      PDFSpanParseHelper helper = new PDFSpanParseHelper();
      foreach ((int objIndex, int generation) objPosition in objPositions)
      {
        SharedAllocator allocator = GetObjBuffer(file, objPosition);
        PDF_Annot annot = new PDF_Annot();
        helper.SetBuffer(allocator.Buffer, ref allocator.Range);
        helper.SkipObjHeader();
        ParseAnnot(file, ref helper, annot);
        FreeAllocator(allocator);
      }
    }

    private void ParseAnnot(PDFFile file, ref PDFSpanParseHelper helper, PDF_Annot annot)
    {
      if (!helper.GoToStartOfDict())
        throw new InvalidDataException("Expected Dict got EOF!");

      int readPos = helper._readPosition;
      int pos = helper._position;
      helper.GoToNextStringMatch("/Subtype");
      helper.SkipNextToken();
      // have to do some manual stuff because we can't convert /3D because we cant start enum entry with number
      helper.ReadUntilNonWhiteSpaceDelimiter();
      // next char
      if (helper._buffer[readPos] == 3)
      {
        helper.ReadChar();
        if (helper._char == 'D')
          annot.SubType = PDF_AnnotSubtype._3D;
        else
          throw new InvalidDataException("Unknown Annot type!");
      } else
      {
        annot.SubType = helper.GetNextName<PDF_AnnotSubtype>();
      }

      if (annot.SubType == PDF_AnnotSubtype.NULL)
        throw new InvalidDataException("Unknown Annot type!");
      helper._readPosition = readPos;
      helper._position = pos;

      ParseAnnotSubTypeData subTypeFunction = ParseLinkAnnotAsExtension;
      switch (annot.SubType)
      {
        case PDF_AnnotSubtype.NULL:
          break;
        case PDF_AnnotSubtype.Text:
          break;
        case PDF_AnnotSubtype.Link:
          annot.AnnotData = new PDF_LinkAnnot();
          subTypeFunction = ParseLinkAnnotAsExtension;
          break;
        case PDF_AnnotSubtype.FreeText:
          break;
        case PDF_AnnotSubtype.Line:
          break;
        case PDF_AnnotSubtype.Square:
          break;
        case PDF_AnnotSubtype.Circle:
          break;
        case PDF_AnnotSubtype.Polygon:
          break;
        case PDF_AnnotSubtype.PolyLine:
          break;
        case PDF_AnnotSubtype.Highlight:
          break;
        case PDF_AnnotSubtype.Underline:
          break;
        case PDF_AnnotSubtype.Squiggly:
          break;
        case PDF_AnnotSubtype.StrikeOut:
          break;
        case PDF_AnnotSubtype.Stamp:
          break;
        case PDF_AnnotSubtype.Caret:
          break;
        case PDF_AnnotSubtype.Ink:
          break;
        case PDF_AnnotSubtype.Popup:
          break;
        case PDF_AnnotSubtype.FileAttachment:
          break;
        case PDF_AnnotSubtype.Sound:
          break;
        case PDF_AnnotSubtype.Movie:
          break;
        case PDF_AnnotSubtype.Widget:
          break;
        case PDF_AnnotSubtype.Screen:
          break;
        case PDF_AnnotSubtype.PrinterMark:
          break;
        case PDF_AnnotSubtype.TrapNet:
          break;
        case PDF_AnnotSubtype.Watermark:
          break;
        case PDF_AnnotSubtype._3D:
          break;
        case PDF_AnnotSubtype.Redact:
          break;
      }

      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "Rect":
            annot.Rect = helper.GetNextRectangle();
            break;
          case "Contents":
            annot.Contents = helper.GetNextTextString();
            break;
          case "P":
            annot.P_IR = helper.GetNextIndirectReference();
            break;
          case "NM":
            annot.NM = helper.GetNextTextString();
            break;
          case "M":
            // TODO: support Date
            annot.M = helper.GetNextTextString();
            break;
          case "F":
            annot.F = helper.GetNextInt32();
            break;
          case "AP":
            annot.AP = helper.GetNextDict();
            break;
          case "AS":
            annot.AS = helper.GetNextName<PDF_AnnotAppearanceState>();
            if (annot.AS == null)
              throw new InvalidDataException("Invalid Annotation Appearance stream!");
            break;
          case "Border":
            if (helper.IsCurrentByteDigit())
            {
              #region memAllocAndHelper
              (int objIndex, int generation) objPosition = helper.GetNextIndirectReference();
              SharedAllocator allocator = GetObjBuffer(file, objPosition);
              ReadOnlySpan<byte> irSpan = allocator.Buffer.AsSpan(allocator.Range);
              PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irSpan);
              #endregion memAllocAndHelper
              irHelper.SkipObjHeader();
              ParseAnnotBorder(ref irHelper, annot);
              #region freeMem
              FreeAllocator(allocator);
              #endregion freeMem
            }
            else
            {
              ParseAnnotBorder(ref helper, annot);
            }
            break;
          case "C":
            annot.C = helper.GetNextDoubleArray();
            break;
          case "StructParent":
            annot.StructParent = helper.GetNextInt32();
            break;
          case "OC":
            annot.OC = helper.GetNextDict();
            break;
          default:
            subTypeFunction(file, annot.AnnotData, ref helper, tokenString);
            break;
        }

        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

    }


    private void ParseAnnotBorder(ref PDFSpanParseHelper helper, PDF_Annot annot)
    {
      helper.ReadUntilNonWhiteSpaceDelimiter();
      if (helper._char != '[')
        throw new InvalidDataException("Invalid Annot Border data, Array expected!");

      for (int i = 0; i < 3; i++)
      {
        annot.Border[i] = helper.GetNextDouble();
      }

      helper.ReadUntilNonWhiteSpaceDelimiter();
      if (helper._char == ']')
      {
        helper.ReadChar();
        return;
      }

      if (helper._char != '[')
        throw new InvalidDataException("Invalid Annot Border DashLine data, Array expected!");

      int c = 0;
      while (helper._char != ']' && c < 2)
      {
        annot.BorderDashLine.Add(helper.GetNextDouble());
        helper.SkipWhiteSpace();
        c++;
      }
      if (helper._char != ']')
        throw new InvalidDataException("Invalid Annot Border DashLine data, Array expected!");
      helper.ReadChar();
    }

    private void ParseLinkAnnotAsExtension(PDFFile file, IPDF_AnnotData iAnnot, ref PDFSpanParseHelper helper, string key)
    {
      PDF_LinkAnnot annot = (PDF_LinkAnnot)iAnnot;
      switch (key)
      {
        case "A":
          helper.SkipWhiteSpace();
          PDF_AnnotAction action = new PDF_AnnotAction();
          if (helper.IsCurrentByteDigit())
          {
            #region memAllocAndHelper
				    (int objIndex, int generation) objPosition = helper.GetNextIndirectReference();
            SharedAllocator allocator = GetObjBuffer(file, objPosition);
            ReadOnlySpan<byte> irSpan = allocator.Buffer.AsSpan(allocator.Range);
            PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irSpan);
            #endregion memAllocAndHelper
            irHelper.SkipObjHeader();
            
            ParseAnnotAction(file, action, ref irHelper);
            #region freeMem
            FreeAllocator(allocator);
            #endregion freeMem
          }
          else
          {
            ParseAnnotAction(file, action , ref helper);
          }
          annot.Actions = action;
          break;
        default:
          break;
      }
    }

    private void ParseAnnotAction(PDFFile file, PDF_AnnotAction action, ref PDFSpanParseHelper helper)
    {
      if (!helper.GoToStartOfDict())
        throw new InvalidDataException("Expected Dict got EOF!");
      int readPos = helper._readPosition;
      int pos = helper._position;
      helper.GoToNextStringMatch("/S");
      helper.SkipNextToken();
      // have to do some manual stuff because we can't convert /3D because we cant start enum entry with number
      action.SubType = helper.GetNextName<PDF_AnnotActionType>();

      if (action.SubType == PDF_AnnotActionType.NULL)
        throw new InvalidDataException("Unknown Annot type!");
      helper._readPosition = readPos;
      helper._position = pos;

      ParseAnnotActionSubTypeData subTypeFunction = ParseAnnotURIActionAsExtension;
      switch (action.SubType)
      {
        case PDF_AnnotActionType.NULL:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.Launch:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.Thread:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.URI:
          action.ActionData = new PDF_AnnotURIAction();
          subTypeFunction = ParseAnnotURIActionAsExtension;
          break;
        case PDF_AnnotActionType.Sound:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.Movie:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.Hide:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.Named:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.SubmitForm:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.ResetForm:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.ImportData:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.JavaScript:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.SetOCGState:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.Rendition:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.Trans:
          throw new NotImplementedException();
          break;
        case PDF_AnnotActionType.GoTo3DView:
          throw new NotImplementedException();
          break;
        default:
          break;
      }

      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "S":
            helper.SkipNextToken();
            break;
          case "Next":
            throw new NotImplementedException();
            break;
          default:
            subTypeFunction(file, action.ActionData, ref helper, tokenString);
            break;
        }
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

    }
    private void ParseAnnotURIActionAsExtension(PDFFile file, IPDF_AnnotActionData iAction, ref PDFSpanParseHelper helper, string key)
    {
      PDF_AnnotURIAction action = (PDF_AnnotURIAction)iAction;
      switch (key)
      {
        case "URI":
          action.URI = helper.GetNextStringLiteral();
          break;
        case "isMap":
          action.IsMap = helper.GetNextBool();
          break;
        default:
          break;
      }
    }

    private void ParseFontDescriptor(PDFFile file, (int objIndex, int generation) objPosition, ref PDF_FontDescriptor fontDescriptor)
    {
      SharedAllocator allocator = GetObjBuffer(file, objPosition);
      ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);

      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      bool dictStartFound = false;
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }

      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "FontName":
            fontDescriptor.FontName = helper.GetNextToken();
            break;
          // NOTE: Spec says that this is byte string, but i've seen it be string literal in examples as well, so support both
          case "FontFamily":
            helper.SkipWhiteSpace();
            if (helper._char == '<')
              fontDescriptor.FontFamily = helper.GetNextByteString();
            else
              fontDescriptor.FontFamily = helper.GetNextStringLiteral();
            break;
          case "FontStretch":
            fontDescriptor.FontStretch = helper.GetNextName<PDF_FontStretch>();
            break;
          case "FontWeight":
            int fw = helper.GetNextInt32();
            if (fw != 100 && fw != 200 && fw != 300 && fw != 400 && fw != 500 &&
                fw != 600 && fw != 700 && fw != 800 && fw != 900)
              throw new InvalidDataException("Invalid font weight in font descriptor!");
            fontDescriptor.FontWeight = fw;
            break;
          case "Flags":
            uint i = helper.GetNextUnsignedInt32();
            PDF_FontFlags flags = new PDF_FontFlags();
            if ((i & 1) == 1)
              flags |= PDF_FontFlags.FixedPitch;
            if ((i & 2) == 2)
              flags |= PDF_FontFlags.Serif;
            if ((i & 4) == 4)
              flags |= PDF_FontFlags.Symbolic;
            if ((i & 8) == 8)
              flags |= PDF_FontFlags.Script;
            if ((i & 32) == 32)
              flags |= PDF_FontFlags.Nonsymbolic;
            if ((i & 64) == 64)
              flags |= PDF_FontFlags.Italic;
            if ((i & 65536) == 65536) // 17th bit (indexed from 1)
              flags |= PDF_FontFlags.AllCap;
            if ((i & 131072) == 131072) // 18th bit (indexed from 1)
              flags |= PDF_FontFlags.SmallCap;
            if ((i & 262144) == 262144) // 19th bit (indexed from 1)
              flags |= PDF_FontFlags.ForceBold;
            fontDescriptor.Flags = flags;
            break;
          case "FontBBox":
            fontDescriptor.FontBBox = helper.GetNextRectangle();
            break;
          case "ItalicAngle":
            fontDescriptor.ItalicAngle = helper.GetNextInt32();
            break;
          case "Ascent":
            fontDescriptor.Ascent = helper.GetNextInt32();
            break;
          case "Descent":
            fontDescriptor.Descent = helper.GetNextInt32();
            break;
          case "Leading":
            fontDescriptor.Leading = helper.GetNextInt32();
            break;
          case "CapHeight":
            fontDescriptor.CapHeight = helper.GetNextInt32();
            break;
          case "StemV":
            fontDescriptor.StemV = helper.GetNextInt32();
            break;
          case "XHeight":
            fontDescriptor.XHeight = helper.GetNextInt32();
            break;
          case "StemH":
            fontDescriptor.StemH = helper.GetNextInt32();
            break;
          case "AvgWidth":
            fontDescriptor.AvgWidth = helper.GetNextInt32();
            break;
          case "MaxWidth":
            fontDescriptor.MaxWidth = helper.GetNextInt32();
            break;
          case "MissingWidth":
            fontDescriptor.MissingWidth = helper.GetNextInt32();
            break;
          case "FontFile":
          case "FontFile2":
          case "FontFile3":
            PDF_FontFileInfo fontFileInfo = new PDF_FontFileInfo();
            fontFileInfo.Type = tokenString switch
            {
              "FontFile"  => PDF_FontFileType.One,
              "FontFile2" => PDF_FontFileType.Two,
              "FontFile3" => PDF_FontFileType.Three,
              _           => PDF_FontFileType.NULL
            };
            (int objectIndex, int generation) fontFileIR = helper.GetNextIndirectReference();

            PDF_CommonStreamDict commonStreamDict = new PDF_CommonStreamDict();
            ParseFontFileDictAndStream(file, fontFileIR, ref fontFileInfo, ref commonStreamDict);
            fontFileInfo.CommonStreamInfo = commonStreamDict;
            fontDescriptor.FontFile = fontFileInfo;
            break;
          case "CharSet":
            helper.SkipWhiteSpace();
            if (helper._char == '<')
              fontDescriptor.CharSet = helper.GetNextByteString();
            else
            {
              fontDescriptor.CharSet = helper.GetNextStringLiteral();
            }
            break;
          case "Style": // CID Only
            throw new NotImplementedException();
            break;
          case "Lang":
            throw new NotImplementedException();
            break;
          case "FD":
            throw new NotImplementedException();
            break;
          case "CIDSet":
            objPosition = helper.GetNextIndirectReference();
            PDF_CommonStreamDict CIDSetDict = new PDF_CommonStreamDict();
            ParseCommonStream(file, objPosition, ref CIDSetDict);
            fontDescriptor.CIDSet = CIDSetDict;
            break;
          default:
            break;
        }
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

      FreeAllocator(allocator);
    }

    private void ParseRootPageTree(PDFFile file, (int objIndex, int generation) objPosition)
    {
      SharedAllocator allocator = GetObjBuffer(file, objPosition);
      ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);

      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      // make linked list or just flatten references????
      // ok for now just load all and store it, later be smarter
      PDF_PageTree rootPageTree = new PDF_PageTree();
      FillRootPageTreeFrom(file, ref helper, rootPageTree);
      FreeAllocator(allocator);
    }

    private void FillRootPageTreeFrom(PDFFile file, ref PDFSpanParseHelper helper, PDF_PageTree root)
    {
      //
      FillRootPageTreeInfo(ref helper, root);
      List<PDF_PageTree> pageTrees = new List<PDF_PageTree>();
      List<PDF_PageInfo> pages = new List<PDF_PageInfo>();
      pageTrees.Add(root);
      for (int i = 0; i < root.KidsIRs.Count; i++)
      {
        FillAllPageTreeAndInformation(root.KidsIRs[i], pageTrees, pages, file);
      }

      // go over kids and call FillAllPageTreeAndInformation
      file.PageTrees = pageTrees;
      file.PageInformation = pages;
    }

    private void FillRootPageTreeInfo(ref PDFSpanParseHelper helper, PDF_PageTree pageTree)
    {
      bool dictStartFound = false;
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }

      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "Parent":
            pageTree.ParentIR = helper.GetNextIndirectReference();
            break;
          case "Count":
            pageTree.Count = helper.GetNextInt32();
            break;
          case "MediaBox":
            pageTree.MediaBox = helper.GetNextRectangle();
            break;
          case "Kids":
            pageTree.KidsIRs = helper.GetNextIndirectReferenceList();
            break;
          case "Resources":
            pageTree.ResourcesIR = helper.GetNextIndirectReference();
            break;
          case "ProcSet":
            pageTree.ProcSet = helper.GetListOfNames<PDF_ProcedureSet>();
            break;
          default:
            break;
        }

        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;

        tokenString = helper.GetNextToken();
      }

      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");
    }
    // THIS FILLS UP BOTH PAGEINFO AND PAGE TREE INFO
    private void FillAllPageTreeAndInformation((int objIndex, int generation) objPosition, List<PDF_PageTree> pageTrees, List<PDF_PageInfo> pages, PDFFile file)
    {
      SharedAllocator allocator = GetObjBuffer(file, objPosition);
      ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);

      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      // start of dict
      bool dictStartFound = false;
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }

      // need to know if this is first key, maybe dont need to check and just skip and expect data
      string tokenString = helper.GetNextToken();
      if (tokenString != "Type")
        throw new InvalidDataException("Expected Page info type key!");

      tokenString = helper.GetNextToken();
      if (tokenString == "Pages")
      {
        PDF_PageTree pageTree = new PDF_PageTree();
        while (tokenString != "")
        {
          switch (tokenString)
          {
            case "Parent":
              pageTree.ParentIR = helper.GetNextIndirectReference();
              break;
            case "Count":
              pageTree.Count = helper.GetNextInt32();
              break;
            case "MediaBox":
              pageTree.MediaBox = helper.GetNextRectangle();
              break;
            case "Kids":
              pageTree.KidsIRs = helper.GetNextIndirectReferenceList();
              break;
            default:
              break;
          }

          helper.ReadUntilNonWhiteSpaceDelimiter();
          if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
            break;
          tokenString = helper.GetNextToken();
        }
        pageTrees.Add(pageTree);
        for (int i = 0; i < pageTree.KidsIRs.Count; i++)
        {
          FillAllPageTreeAndInformation(pageTree.KidsIRs[i], pageTrees, pages, file);
        }
      }
      else if (tokenString == "Page")
      {
        PDF_PageInfo pageInfo = new PDF_PageInfo();
        while (tokenString != "")
        {
          switch (tokenString)
          {
            case "Parent":
              pageInfo.ParentIR = helper.GetNextIndirectReference();
              break;
            case "LastModified" :
              pageInfo.LastModified = DateTime.UtcNow; // TODO: Fix this hen you know how dat e asecformat looks like
             break;
            case "Resources" :
              // this can be both
              // if its IR it will be parsed later
              (bool isDirect, SharedAllocator? allocator) info = ReadIntoDirectOrIndirectDict(file, ref helper, false);
              if (info.isDirect)
              {
                PDF_ResourceDict rDict = new PDF_ResourceDict();
                ParseResourceDictionary(file, ref helper, false, rDict);
                pageInfo.ResourceDict = rDict;
              }
              else 
                pageInfo.ResourcesIR = helper.GetNextIndirectReference();
              FreeAllocator(info.allocator);
              break;
            case "MediaBox" :
              pageInfo.MediaBox = helper.GetNextRectangle();
              break;
            case "CropBox" :
              pageInfo.CropBox = helper.GetNextRectangle();
              break;
            case "BleedBox" :
              pageInfo.BleedBox = helper.GetNextRectangle();
              break;
            case "TrimBox" :
              pageInfo.TrimBox = helper.GetNextRectangle();
              break;
            case "ArtBox" :
              pageInfo.ArtBox = helper.GetNextRectangle();
              break;
            case "BoxColorInfo" :
              pageInfo.BoxColorInfo = helper.GetNextDict();
              break;
            case "Contents" :
              helper.SkipWhiteSpace();
              if (helper._char == '[')
                pageInfo.ContentsIR = helper.GetNextIndirectReferenceList();
              else
                pageInfo.ContentsIR = new List<(int objIndex, int generation)>() { helper.GetNextIndirectReference() };
                break;
            case "Rotate" :
              pageInfo.Rotate = helper.GetNextInt32();
              break;
            case "Group" :
              pageInfo.Group = helper.SkipNextDictOrIR();
              break;
            case "Thumb" :
              pageInfo.Thumb = helper.GetNextStream();
              break;
            case "B" :
              pageInfo.B = helper.GetNextIndirectReferenceList();
              break;
            case "Dur" :
              pageInfo.Dur = helper.GetNextDouble();
              break;
            case "Trans" :
              pageInfo.Trans = helper.GetNextDict();
              break;
            case "Annots" :
              List<(int objIndex, int generation)> objPositions = ParsePDFListOfIndirectReferences(file, ref helper);
              List<PDF_Annot> annots = new List<PDF_Annot>();
              ParseAnnots(file, objPositions, annots);
              pageInfo.Annots = annots;
              break;
            case "AA" :
              pageInfo.AA = helper.GetNextDict();
              break;
            case "Metadata" :
              pageInfo.Metadata = helper.GetNextStream();
              break;
            case "PieceInfo" :
              pageInfo.PieceInfo = helper.GetNextDict();
              break;
            case "StructParents" :
              pageInfo.StructParents = helper.GetNextInt32();
              break;
            case "ID" :
              pageInfo.ID = helper.GetNextArrayStrict();
              break;
            case "PZ" :
              pageInfo.PZ = helper.GetNextInt32();
              break;
            case "SeparationInfo" :
              pageInfo.SeparationInfo = helper.GetNextDict();
              break;
            case "Tabs" :
              pageInfo.Tabs = helper.GetNextName<PDF_Tabs>();
              break;
            case "TemplateInstantiated" :
              pageInfo.TemplateInstantiated = helper.GetNextToken();
              break;
            case "PresSteps" :
              pageInfo.PresSteps = helper.GetNextDict();
              break;
            case "UserUnit" :
              pageInfo.UserUnit = helper.GetNextDouble();
              break;
            case "VP" :
              pageInfo.VP = helper.GetNextDict();
              break;
            default:
              break;
          }

          helper.ReadUntilNonWhiteSpaceDelimiter();
          if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
            break;
          tokenString = helper.GetNextToken();
        }
        pages.Add(pageInfo);
      }

      FreeAllocator(allocator);
    }

    private void ParseCatalogDictionary(PDFFile file, (int objIndex, int generation) objPosition)
    {
      SharedAllocator allocator = GetObjBuffer(file, objPosition);
      ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);

      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);

      // if this is bigger 4096 or double that then do see to do some kind of different processing
      // I think that reading in bulk should be faster than reading 1 char by 1 from stream
      PDF_Catalog catalog = new PDF_Catalog();

      FillCatalog(ref helper, catalog);

      // we maybe read a bit more sicne we read last object diff so make sure we are in correct position
      // TODO: verify if this stupid line below can be deleted
      file.Stream.Position = helper._position + 1;

      // Starting from PDF 1.4 version can be in catalog and it has advantage over header one if its bigger
      if (catalog.Version > PDF_Version.Null)
        file.PdfVersion = catalog.Version;
      file.Catalog = catalog;
      FreeAllocator(allocator);
    }

    private void FillCatalog(ref PDFSpanParseHelper helper, PDF_Catalog catalog)
    {
      bool startOfDictFound = false;
      while (!startOfDictFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          startOfDictFound = helper.IsCurrentCharacterSameAsNext();
      }
      string tokenString = helper.GetNextToken();
      // Maybe move this to dictionary parsing
      while (tokenString != "")
      {
        switch (tokenString) 
        {
          case "Version":
            catalog.Version = ParsePdfVersionFromCatalog(ref helper);
            break;
          case "Extensions":
            catalog.Extensions = helper.GetNextDict();
            break;
          case "Pages":
            catalog.PagesIR = helper.GetNextIndirectReference();
            break;
          case "PageLabels":
            catalog.PageLabels = helper.GetNextNumberTree();
            break;
          case "Names":
            catalog.Names = helper.GetNextDict();
            break;
          case "Dests":
            catalog.DestsIR = helper.GetNextIndirectReference();
            break;
          case "ViewerPreferences":
            catalog.ViewerPreferences = helper.GetNextDict();
            break;
          case "PageLayout":
            catalog.PageLayout = helper.GetNextName<PDF_PageLayout>();
            break;
          case "PageMode":
            catalog.PageMode = helper.GetNextName<PDF_PageMode>();
            break;
          case "Outlines":
            catalog.OutlinesIR = helper.GetNextIndirectReference();
            break;
          case "Threads":
            catalog.ThreadsIR = helper.GetNextIndirectReference();
            break;
          case "OpenAction":
            // skip for now, this is needed only for renderer
            catalog.OpenAction = new object();
            break;
          case "AA":
            catalog.AA = helper.GetNextDict();
            break;
          case "URI":
            catalog.URI = helper.GetNextDict();
            break;
          case "AcroForm":
            catalog.AcroForm = helper.GetNextDict();
            break;
          case "MetaData":
            catalog.MetadataIR = helper.GetNextIndirectReference();
            break;
          case "StructTreeRoot":
            catalog.StructTreeRoot = helper.SkipNextDictOrIR();
            break;
          case "MarkInfo":
            catalog.MarkInfo = helper.GetNextDict();
            break;
          case "Lang":
            catalog.Lang = helper.GetNextStringLiteral();
            break;
          case "SpiderInfo":
            catalog.SpiderInfo = helper.GetNextDict();
            break;
          case "OutputIntents":
            catalog.OutputIntents = helper.GetNextArrayStrict();
            break;
          case "PieceInfo":
            catalog.PieceInfo = helper.GetNextDict();
            break;
          case "OCProperties":
            catalog.OCProperties = helper.GetNextDict();
            break;
          case "Perms":
            catalog.Perms = helper.GetNextDict();
            break;
          case "Legal":
            catalog.Legal = helper.GetNextDict();
            break;
          case "Requirements":
            catalog.Requirements = helper.GetNextArrayStrict();
            break;
          case "Collection":
            catalog.Collection = helper.GetNextDict();
            break;
          case "NeedsRendering":
            catalog.NeedsRendering = helper.GetNextToken() == "true";
            break;
          default:
            break;
        }

        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }
      // Means that dict is corrupt
      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");
    }

    /// <summary>
    /// Here we need to check if we can find trailer, if we cant find trailer, we KNOW its fully cross-reference stream (compressed) file
    ///   -> In which case we check xref offset for the dictionary
    /// If we find trailer
    ///   -> we will parse normal trailer dict with XRefStm check as well because in this case file may be hybrid file
    ///   -> and then we will first parse xref and create main table and then parse xrefstm offset and then Prev recusevly
    ///   -> in hybrid files /Prev may be either compressed or normal afaik
    /// </summary>
    /// <param name="file"></param>
    /// <exception cref="InvalidDataException"></exception>
    private void ParseTrailersAndCrossReferenceData(PDFFile file)
    {
      // 1. Find last trailer
      // 2. Create xref list with correct size in PDFFile
      // 3. Process xrefoffsets and /Prevs in last trailer
      int chunkSize = KB;
      file.Stream.Seek(-chunkSize, SeekOrigin.End);
      byte[] arr = ArrayPool<byte>.Shared.Rent(chunkSize);
      int bytesRead = file.Stream.Read(arr);
      // not sure if i can do this 
      if (bytesRead != chunkSize)
        throw new InvalidDataException("Invalid PDF Data!");

      file.Trailer = new PDF_Trailer();
      PDFSpanParseHelper helper = new PDFSpanParseHelper(arr, 0, bytesRead);
      
      // 1.
      ReadOnlySpan<byte> tokenSpan = new ReadOnlySpan<byte>();
      ReadOnlySpan<char> trailerSpan = "trailer".AsSpan();
      ReadOnlySpan<char> xrefSpan = "startxref".AsSpan();
      helper.GetNextStringAsReadOnlySpan(ref tokenSpan);
      int lastTrailerStartPos = -1;
      int lastXRefStartPos = -1;
      while (tokenSpan.Length != 0)
      {
        if (AreCharsEqualBytes(ref trailerSpan, ref tokenSpan))
        {
          lastTrailerStartPos = helper._readPosition;
        }
        else if (AreCharsEqualBytes(ref xrefSpan, ref tokenSpan))
        {
          lastXRefStartPos = helper._readPosition;
        }
        helper.GetNextStringAsReadOnlySpan(ref tokenSpan);
      }

      // 2.
      helper._readPosition = lastXRefStartPos;
      uint xrefOffset = helper.GetNextUnsignedInt32(); // make this long
      // No trailer keyword found, means its cross-reference stream type
      if (lastTrailerStartPos == -1)
      {
        ParseTrailer(file, xrefOffset, true);
      }
      else
      {
        helper._readPosition = lastTrailerStartPos;
        // we will load this once again, it may be redundant if there are no incremental updates
        // but when there are its easier for each trailer to allocate space themselves because we will be jumping in the file
        ParseTrailer(file, file.Stream.Length - (chunkSize - helper._readPosition), true);
      }

      ArrayPool<byte>.Shared.Return(arr);
    }

    /// <summary>
    /// We will call this recursevly because in hybrid files Prev may be normal trailer or for cross-reference streams
    /// </summary>
    /// <param name="file"></param>
    /// <param name="offset"></param>
    /// <param name="update"></param>
    private void ParseTrailer(PDFFile file, long offset, bool update)
    {
      int chunkSize = KB * 2; // can maybe be 1kb 
      file.Stream.Seek(offset, SeekOrigin.Begin);
      byte[] arr = ArrayPool<byte>.Shared.Rent(chunkSize);
      int bytesRead = file.Stream.Read(arr);
      PDFSpanParseHelper helper = new PDFSpanParseHelper(arr, 0, bytesRead);

      helper.SkipWhiteSpace();
      if (helper.IsCurrentByteDigit())
      {
        int prev = ParseCrossReferenceStreamAndDict(file, ref helper, update, offset);
        ArrayPool<byte>.Shared.Return(arr);
        if (prev != -1 && file.Trailer.Hybrid == false)
          // this should will always be stream, we just call this since it will alloc and jump to correct place
          ParseTrailer(file, prev, false); 
      }
      else if (helper._char == 'x') // xref
      {
        ParseCrossReferenceTable(file, offset);
      }
      else
      {
        // this means its normal dict
        int prev = ParseNormalTrailer(file, ref helper, update);
        ArrayPool<byte>.Shared.Return(arr);
        if (prev != -1)
        {
          ParseTrailer(file, prev, false);
        } 
      }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <param name="helper"></param>
    /// <param name="update"></param>
    /// <returns>Prev value. We return it so parent function can release trailer array if necessary, so that we can utilize pool better
    /// in case of many possible prevs</returns>
    /// <exception cref="InvalidDataException"></exception>
    private int ParseNormalTrailer(PDFFile file, ref PDFSpanParseHelper helper, bool update)
    {
      string tokenString = helper.GetNextToken();
      int xRefStm = -1;
      int prev = -1;
      while (tokenString != string.Empty)
      {
        // put in separate function like parse TrailerDict or something similar  
        switch (tokenString)
        {
          case "Size":
            int size = helper.GetNextInt32();
            if (update)
            {
              file.Trailer.Size = size;
              file.CrossReferenceEntries = new List<PDF_XrefEntry>(file.Trailer.Size);
              // TODO: not sure how efficient this can be if there are thousands of objects
              // Make it faster. Check how Span2D does it, I just need fill
              for (int i = 0; i < file.Trailer.Size; i++)
              {
                PDF_XrefEntry e = new PDF_XrefEntry();
                file.CrossReferenceEntries.Add(e);
              }
            }
            break;
          case "Root":
            (int objIndex, int generation) ir = helper.GetNextIndirectReference();
            if (update)
            {
              file.Trailer.RootIR = ir;
            }
            break;
          case "Info":
            ir = helper.GetNextIndirectReference();
            if (update)
            {
              file.Trailer.InfoIR = ir;
            }
            break;
          case "Encrypt":
            ir = helper.GetNextIndirectReference();
            if (update)
            {
              file.Trailer.EncryptIR = ir;
            }
            break;
          case "Prev":
            prev = helper.GetNextInt32();
            if (update)
            {
              file.Trailer.Prev = prev;
            }
            break;
          case "ID":
            string[] ID = helper.GetNextArrayKnownLengthStrict(2);
            if (update)
            {
              file.Trailer.ID = ID;
            }
            break;
          case "XRefStm":
            file.Trailer.Hybrid = true;
            xRefStm = helper.GetNextInt32();
            break;
          default:
            break;
        }

        // end of dict
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

      if (tokenString == string.Empty)
        throw new InvalidDataException("Invalid trailer!");

      // first we search standard then xRefStm and then Prev
      tokenString = helper.GetNextToken();
      if (tokenString != "startxref")
        throw new InvalidDataException($"Expected startxref, got {tokenString}");

      int xrefPos = helper.GetNextInt32();
      ParseCrossReferenceTable(file, xrefPos);
      // parse this

      if (xRefStm != -1)
      {
        ParseTrailer(file, xRefStm, false);
      }

      return prev;
    }

    // TODO: This will work only for one section, not subsections.Fix it later!
    private void ParseCrossReferenceTable(PDFFile file, long byteOffset)
    {
      // TODO: I guess byteoffset should be long
      // i really feel i should be loading in more bytes in chunk
      file.Stream.Position = byteOffset;
      Span<byte> header = stackalloc byte[64]; // small header to read "xref LF xrefsize"
      int bytesRead = file.Stream.Read(header);

      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref header);
      string tokenString = helper.GetNextToken();
      if (tokenString != "xref")
        throw new InvalidDataException($"Expected xref. Got {tokenString}");

      int startObj = helper.GetNextInt32();
      int endObj = helper.GetNextInt32();
      int sectionLen = endObj - startObj;

      // empty 
      if (sectionLen == 0)
        return;
      // 17 bytes for def + bit extra, 2 for CRLF 
      int arrLen = (20 + 2) * sectionLen;
      byte[] arr = ArrayPool<byte>.Shared.Rent(arrLen);

      file.Stream.Position = byteOffset + helper._position;
      bytesRead = file.Stream.Read(arr, 0, arrLen);
      helper = new PDFSpanParseHelper(arr, 0, bytesRead);
      PDF_XrefEntry entry;
      for (int i = startObj; i < endObj; i++)
      {
        // we shouldn't update if entry exists because it means that it was set correctly
        // by newer cross-reference table since we parse them from newest to oldest
        entry = file.CrossReferenceEntries[i];
        if (entry.Index == -1)
        {
          entry.TenDigitValue = helper.GetNextInt64();
          entry.GenerationNumber = helper.GetNextUnsignedInt16();
          entry.Index = i;
          helper.SkipWhiteSpace();
          byte entryType = helper._char;
          helper.ReadChar();// move off entry Type
          if (entryType != (byte)'n' && entryType != (byte)'f')
            throw new InvalidDataException("Invalid data");
          if (entryType == 'n')
            entry.EntryType = PDF_XrefEntryType.NORMAL;
          else
            entry.EntryType = PDF_XrefEntryType.FREE;
        }
      }

      ArrayPool<byte>.Shared.Return(arr);
    }

    public (bool isDirectObject, SharedAllocator? allocator) ReadIntoDirectOrIndirectArray(PDFFile file, ref PDFSpanParseHelper helper, bool returnIRBuffer = true)
    {
      helper.SkipWhiteSpace();
      if (helper._char == '[')
      {
        return (true, null);
      }
      // shortcut
      if (!returnIRBuffer)
        return (false, new SharedAllocator());

      (int objIndex, int generation) IR = helper.GetNextIndirectReference();
      SharedAllocator allocator = GetObjBuffer(file, IR);
      return (false, allocator);
    }

    [Obsolete]
    /// <summary>
    /// Checks wether next object is indirect reference to dictionary or its direct dictionary in the current object
    /// We do it this way so we dont have to deal with delegeate issues where we can't always pass same arguments (i.e object that needs to be 
    /// filled or something else)
    /// Call <see cref="FreeAllocator(SharedAllocator?)"/> on info.allocator after!
    /// </summary>
    /// <param name="file">PR</param>
    /// <param name="helper">Current helper </param>
    /// <returns>IsDirectObject is set to true if its direct object and allocator is null. Reversed if its indirect reference and allocator is used</returns>
    /// <postnotes>I dont think this API is really good, because it might read into dict so we need to pass another param in the that we will call with allocator data</postnotes>
    /// <postnotes>So either change code to not read into dict "helper.ReadChar()" and update usages or do not use it at all!</postnotes>
    public (bool isDirectObject, SharedAllocator? allocator) ReadIntoDirectOrIndirectDict(PDFFile file, ref PDFSpanParseHelper helper, bool returnIRBuffer = true)
    {
      helper.SkipWhiteSpace();
      if (helper._char == '<' && helper.IsCurrentCharacterSameAsNext())
      {
        helper.ReadChar();
        return (true, null);
      }
      // shortcut
      if (!returnIRBuffer)
        return (false, new SharedAllocator());

      (int objIndex, int generation) IR = helper.GetNextIndirectReference();
      SharedAllocator allocator = GetObjBuffer(file, IR);
      return (false, allocator);
    }

    public int ParseCrossReferenceStreamAndDict(PDFFile file, ref PDFSpanParseHelper helper, bool update, long xrefPos) 
    {
      
      // This is data needed for parsing, no need to save it anywhere later
      List<(int, int)> indexes = new List<(int, int)>();
      int size = 0;
      int prev = -1;
      // NOTE: this can be byte since values are low or simply 3 int variables
      Span<int> W = stackalloc int[3]; // its always 3
      PDF_CommonStreamDict commonStreamDict = new PDF_CommonStreamDict();
      helper.SkipObjHeader();
      if (!helper.GoToStartOfDict())
        throw new InvalidDataException("Expected Dict got EOF!");

      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "Size":
            size = helper.GetNextInt32();
            if (update)
            {
              file.Trailer.Size = size;
              file.CrossReferenceEntries = new List<PDF_XrefEntry>(file.Trailer.Size);
              // TODO: not sure how efficient this can be if there are thousands of objects
              // Make it faster. Check how Span2D does it, I just need fill
              for (int i = 0; i < file.Trailer.Size; i++)
              {
                PDF_XrefEntry e = new PDF_XrefEntry();
                file.CrossReferenceEntries.Add(e);
              }
            }
            break;
          case "Root":
            (int objIndex, int generation) ir = helper.GetNextIndirectReference();
            if (update)
            {
              file.Trailer.RootIR = ir;
            }
            break;
          case "Info":
            ir = helper.GetNextIndirectReference();
            if (update)
            {
              file.Trailer.InfoIR = ir;
            }
            break;
          case "Encrypt":
            ir = helper.GetNextIndirectReference();
            if (update)
            {
              file.Trailer.EncryptIR = ir;
            }
            break;
          case "Prev":
            prev = helper.GetNextInt32();
            break;
          case "ID":
            string[] ID = helper.GetNextArrayKnownLengthStrict(2);
            if (update)
            {
              file.Trailer.ID = ID;
            }
            break;
          case "Index":
            // these are not IR but pair of ints which is what this function parses efficiently
            indexes = helper.GetNextListOfPairIntegers();
            break;
          case "W":
            helper.SkipWhiteSpace();
            // can be direct array or IR

            if (helper._char == '[')
            {
              helper.ReadChar();
              for (int i = 0; i < W.Length; i++)
              {
                W[i] = helper.GetNextInt32();
              }
              helper.ReadUntilNonWhiteSpaceDelimiter();
              if (helper._char != ']')
                throw new InvalidDataException("Invalid end of widths array!");
              helper.ReadChar();
            }
            else
              throw new InvalidDataException("Invalid W array!");
            break;
          default:
            ParseCommonStreamDictAsExtension(file, ref helper, tokenString, ref commonStreamDict);
            break;
        }

        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

      helper.SkipNextToken(); // skip 'stream'
      helper.SkipWhiteSpace(); // skip new line

      // set pos at start of the stream
      long streamStartPos = xrefPos + helper._position;

      // TODO: i think we can avoid this catsing and creating new array by passing Stream directly to Decode method!
      byte[] arr = ArrayPool<byte>.Shared.Rent((int)commonStreamDict.Length);
      file.Stream.Position = streamStartPos;
      int readBytes = file.Stream.Read(arr, 0, (int)commonStreamDict.Length);
      if (readBytes != commonStreamDict.Length)
        throw new InvalidDataException("Invalid cross reference stream!");
      ReadOnlySpan<byte> buffer = arr.AsSpan(0, (int)commonStreamDict.Length);
      byte[] decoded = DecompressionHelper.DecodeFilters(ref buffer, commonStreamDict.Filters);

      if (indexes.Count == 0)
        indexes.Add((0, size));

      buffer = decoded.AsSpan();
      helper = new PDFSpanParseHelper(decoded, 0, decoded.Length);

      int type;
      int secondField;
      int thirdField;
      PDF_XrefEntry entry;
      foreach((int start, int end) subSection in indexes)
      {
        for (int i = subSection.start; i < subSection.end; i++)
        {
          entry = file.CrossReferenceEntries[i];
          if (entry.Index == -1)
          {
            type = helper.ReadSpecificSizeInt32(W[0]);
            secondField = helper.ReadSpecificSizeInt32(W[1]);
            thirdField = helper.ReadSpecificSizeInt32(W[2]);
            
            if (type == 0)
            {
              entry.EntryType = PDF_XrefEntryType.FREE;
              entry.GenerationNumber = (ushort)thirdField;
            }
            else if (type == 1)
            {
              entry.EntryType = PDF_XrefEntryType.NORMAL;
              entry.TenDigitValue = secondField;
              entry.GenerationNumber = (ushort)thirdField;

            }
            else if (type == 2)
            {
              entry.EntryType = PDF_XrefEntryType.COMPRESSED;
              entry.StreamIR = secondField;
              entry.IndexInOS = thirdField;
            }
            entry.Index = i;
          }
        }
      }

      ArrayPool<byte>.Shared.Return(arr);
      return prev;
    }

    // TODO: account for this later
    // Comment from spec:
    // Beginning with PDF 1.4, the VErsion entry in the document's catalog dictionary (lcoated via the Root entry in the file's trailer
    // as described in 7.5.5, if present, shall be used instead of version specified in the Header
    // TODO: I really should have just compared and parsed the string instead of trying to act smart
    // because i will have to parse entire file and heapallock anyways
    // maybe later move this to be ref paramter
    private PDF_Version ParsePdfVersionFromHeader(Stream stream)
    {
      // We parse this first so we should already be at 0
      // stream.Seek(0, SeekOrigin.Begin);
      Span<byte> buffer = stackalloc byte[8];
      int bytesRead = stream.Read(buffer);
      if (bytesRead != buffer.Length)
        throw new InvalidDataException("Invalid data");
      // checking for %PDF-X.X in bytes
      if (buffer[0] != 0x25)
        throw new InvalidDataException("Invalid data");
      if (buffer[1] != 0x50)
        throw new InvalidDataException("Invalid data");
      if (buffer[2] != 0x44)
        throw new InvalidDataException("Invalid data");
      if (buffer[3] != 0x46)
        throw new InvalidDataException("Invalid data");
      if (buffer[4] != 0x2d)
        throw new InvalidDataException("Invalid data");
      // checking version
      byte majorVersion = buffer[5];
      if (majorVersion != 0x31 && majorVersion != 0x32)
        throw new InvalidDataException("Invalid data");
      if (buffer[6] != 0x2e)
        throw new InvalidDataException("Invalid data");
      byte minorVersion = buffer[7];
      if (majorVersion == 0x31)
      {
        switch (minorVersion)
        {
          case 0x30:
            return PDF_Version.V1_0;
          case 0x31:
            return PDF_Version.V1_1;
          case 0x32:
            return PDF_Version.V1_2;
          case 0x33:
            return PDF_Version.V1_3;
          case 0x34:
            return PDF_Version.V1_4;
          case 0x35:
            return PDF_Version.V1_5;
          case 0x36:
            return PDF_Version.V1_6;
          case 0x37:
            return PDF_Version.V1_7;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
      else
      {
        switch (minorVersion)
        {
          case 0x30:
            return PDF_Version.V2_0;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
    }

    private PDF_Version ParsePdfVersionFromCatalog(ref PDFSpanParseHelper helper)
    {
      ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>();
      helper.GetNextStringAsReadOnlySpan(ref buffer);
      // Expect /M.m where M is major and m minor version
      if (buffer.Length != 3)
        throw new InvalidDataException("Invalid data!");
      byte majorVersion = buffer[0];
      if (majorVersion != 0x31 && majorVersion != 0x32)
        throw new InvalidDataException("Invalid data");
      if (buffer[1] != 0x2e)
        throw new InvalidDataException("Invalid data");
      byte minorVersion = buffer[2];
      if (majorVersion == 0x31)
      {
        switch (minorVersion)
        {
          case 0x30:
            return PDF_Version.V1_0;
          case 0x31:
            return PDF_Version.V1_1;
          case 0x32:
            return PDF_Version.V1_2;
          case 0x33:
            return PDF_Version.V1_3;
          case 0x34:
            return PDF_Version.V1_4;
          case 0x35:
            return PDF_Version.V1_5;
          case 0x36:
            return PDF_Version.V1_6;
          case 0x37:
            return PDF_Version.V1_7;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
      else
      {
        switch (minorVersion)
        {
          case 0x30:
            return PDF_Version.V2_0;
            break;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
    }

    // this is probably naive solution wihtout charset taken in account
    private bool AreSpansEqual(Span<byte> a, Span<byte> b, int len, bool strict = true)
    {
      if (a.Length != b.Length && strict)
        throw new InvalidDataException("Spans different size");
      // support only int32 size for now??
      for (int i = 0; i < len; i++)
        if (a[i] != b[i]) return false;

      return true;
    }

    /// <summary>
    /// Use this only when we know target encoding i.e that char < 256
    /// </summary>
    /// <param name="a">string as char span</param>
    /// <param name="b">string as byte span</param>
    /// <returns></returns>
    public bool AreCharsEqualBytes(ref ReadOnlySpan<char> a, ref ReadOnlySpan<byte> b)
    {
      if (a.Length != b.Length)
        return false;

      for (int i = 0; i < a.Length; i++)
        if (a[i] != b[i]) return false;

      return true;
    }


    // generation is always 0 for object streams
    // Fix this to save correct size of objctss not always 8k
    private void ParseObjectStream(PDFFile file, int objId, long offset, PDF_ObjectStream objStreamInfo)
    {
      file.Stream.Position = offset;
      // preferably use stack here beacuse stream that we may read will be disposed becuase its decoded
      // so we don't need another array to fit it in 
      // NOTE: it should be optional and maybe allowed stack value to be configurable because we may not know what code 
      // that calls this function does
      byte[] arr = new byte[KB * 8];
      int readBytes =file.Stream.Read(arr);
      ReadOnlySpan<byte> buffer = arr.AsSpan();
      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      bool res = helper.GoToStartOfDict();
      if (!res)
        throw new InvalidDataException("Can't find object stream dictionary!");

      string tokenString = helper.GetNextToken();
      PDF_CommonStreamDict commonStreamDict = new PDF_CommonStreamDict();
      while (tokenString != string.Empty)
      {
        switch (tokenString)
        {
          case "N":
            objStreamInfo.N = helper.GetNextInt32();
            break;
          case "First":
            objStreamInfo.First = helper.GetNextInt32();
            break;
          case "Extends":
            objStreamInfo.ExtendsIR = helper.GetNextIndirectReference();
            break;
          default:
            ParseCommonStreamDictAsExtension(file, ref helper, tokenString, ref commonStreamDict);
            break;
        }

        if (helper.IsEndOfDict())
          break;

        tokenString = helper.GetNextToken();
      }
      helper.SkipWhiteSpaceAndDelimiters(); // >>
      helper.SkipNextToken(); // stream

      // Parse stream
      // check if we loaded stream in our 8k buffer
      bool streamLoaded = commonStreamDict.Length < buffer.Length - helper._readPosition;
      if (streamLoaded)
        buffer = buffer.Slice(helper._readPosition, (int)commonStreamDict.Length);
      else
      {
        // set correct position and read entire stream
        buffer = new byte[commonStreamDict.Length];
        helper = new PDFSpanParseHelper(ref buffer);
        // TODO: this isnt used
      }
      commonStreamDict.RawStreamData = DecompressionHelper.DecodeFilters(ref buffer, commonStreamDict.Filters);
      objStreamInfo.CommonStreamDict = commonStreamDict;

      // Parse offsets
      buffer = objStreamInfo.CommonStreamDict.RawStreamData.AsSpan();
      helper = new PDFSpanParseHelper(ref buffer);
      objStreamInfo.Offsets = new List<(int, int)>();
      for (int i = 0; i < objStreamInfo.N; i++)
      {
        int objIdRef = helper.GetNextInt32();
        int objOffset = helper.GetNextInt32();
        // First is first byte of first object in the stream, because 'header' is filled with N * pairs of ints (offsets)
        objStreamInfo.Offsets.Add((objIdRef, objStreamInfo.First + objOffset));
      }
      file.ObjectStreams.Add((objId, objStreamInfo));
    }

    private ulong ConvertBytesToUnsignedInt64(Span<byte> buffer)
    {
      uint res = 0;
      for (int i = 0; i < buffer.Length; i++)
      {
        // these should be no negative ints so this is okay i believe?
        res = res * 10 + (uint)CharUnicodeInfo.GetDecimalDigitValue((char)buffer[i]);
      }
      // this should wrap, idk if i should throw exception
      return res;
    }
    // I think if i count object number it would be a bit faster but this is fine too
   
    // NOTE: THIS WILL count until next object start so it will count endobj and (i.e) 8 0 obj or longer byte, it should be ok for now
    // TODO: count until object end remove endobj 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectIndex"></param>
    /// <param name="rootByteOffset"></param>
    /// <param name="file"></param>
    /// <returns>If -1 is returned it means that object is compressed and that it GetObjBuffer should be called that will contain that decompressed buffer</returns>
    private long GetDistanceToNextNormalObject(PDFFile file, ref PDF_XrefEntry entry)
    {
      if (entry.EntryType == PDF_XrefEntryType.COMPRESSED)
        return -1;
      if (entry.EntryType == PDF_XrefEntryType.FREE)
        return 0;
      long minPositiveDiff = long.MaxValue;
      int index = entry.Index;
      long diff;
      for (int i = 0; i < file.CrossReferenceEntries.Count; i++)
      {
        if (file.CrossReferenceEntries[i].EntryType != PDF_XrefEntryType.NORMAL)
          continue;

        diff = file.CrossReferenceEntries[i].TenDigitValue - entry.TenDigitValue;
        if (diff > 0 && diff < minPositiveDiff)
        {
          minPositiveDiff = diff;
          index = i;
        }
      }
      // TODO: Not sure if this is best approach,
      // but this makes it treaky when there are incremental updates, so we can't be sure i guess
      // for now just load till the end of the stream
      // means that this is last object so next object is assumed to be cross reference table
      if (index == entry.Index)
        return file.Stream.Length - entry.TenDigitValue;
      return minPositiveDiff;
    }

    /// <summary>
    /// Returns largest object so that we can create one object instead of many objects
    /// when doing iterations over some array of keys (indirect references)
    /// TODO: this algorithm may be slow depending on size of the objPositions and totalNumber of normal objects in PDF
    /// Current big O for time is numOfNormalObjectsInObjPositions * numOfNormalObjectsInPDFFile + numOfNormalObjectsInObjPositions
    /// </summary>
    /// <param name="file">PDF Root file</param>
    /// <param name="objPositions">Positions of objects that need to be processed</param>
    /// <returns>Size of largest object</returns>
    public int GetBiggestObjectSizeFromList(PDFFile file, List<(int objIndex, int generation)> objPositions)
    {
      int[] distancesArr = ArrayPool<int>.Shared.Rent(objPositions.Count);
      int normalCount = 0;
      PDF_XrefEntry entry;
      for (int i = 0; i < objPositions.Count; i++)
      {
        entry = GetXrefEntry(file, objPositions[i]);
        if (entry.EntryType == PDF_XrefEntryType.NORMAL)
          distancesArr[normalCount++] = (int)GetDistanceToNextNormalObject(file, ref entry);
      }
      if (normalCount == 0)
        return 0;

      Span<int> distances = distancesArr.AsSpan(0, normalCount);
      int maxSize = distances[0];
      for (int i = 1; i < distances.Length; i++)
      {
        if (distances[i] > maxSize)
          maxSize = distances[i];
      }
      ArrayPool<int>.Shared.Return(distancesArr);

      return maxSize;
    }

    /// <summary>
    /// Returns largest object so that we can create one object instead of many objects
    /// when doing iterations over some array of keys (indirect references)
    /// TODO: this algorithm may be slow depending on size of the objPositions and totalNumber of normal objects in PDF
    /// Current big O for time is numOfNormalObjectsInObjPositions * numOfNormalObjectsInPDFFile + numOfNormalObjectsInObjPositions
    /// TODO: see if I can reduce some of the copied code compared to non key version without allocating more memory
    /// </summary>
    /// <param name="file">PDF Root file</param>
    /// <param name="objPositions">Positions of objects that need to be processed</param>
    /// <returns>Size of largest object</returns>
    public int GetBiggestObjectSizeFromList(PDFFile file, List<(string key, (int objIndex, int generation) objPosition)> objPositions)
    {
      int[] distancesArr = ArrayPool<int>.Shared.Rent(objPositions.Count);
      int normalCount = 0;
      PDF_XrefEntry entry;
      for (int i = 0; i < objPositions.Count; i++)
      {
        entry = GetXrefEntry(file, objPositions[i].objPosition);
        if (entry.EntryType == PDF_XrefEntryType.NORMAL)
          distancesArr[normalCount++] = (int)GetDistanceToNextNormalObject(file, ref entry);
      }
      if (normalCount == 0)
        return 0;

      Span<int> distances = distancesArr.AsSpan(0, normalCount);
      int maxSize = distances[0];
      for (int i = 1; i < distances.Length; i++)
      {
        if (distances[i] > maxSize)
          maxSize = distances[i];
      }
      ArrayPool<int>.Shared.Return(distancesArr);

      return maxSize;
    }




    // Wrap this and use otuside, because I'm not sure if this will change later, so its easier to refactor it if I wrap it
    private PDF_XrefEntry GetXrefEntry(PDFFile file, (int objIndex, int generation) objPosition)
    {
      return file.CrossReferenceEntries[objPosition.objIndex];
    }

    /// <summary>
    /// Returns SharedAllocator instance that contains buffer with data. This buffer may be rented from array pool
    /// so FreeAlloactor() must be always called after work with SharedAllocator object is finihsed!
    /// </summary>
    /// <param name="file">PDF root object</param>
    /// <param name="objPosition">Object Position</param>
    /// <param name="multiplier">
    /// Multipler used to say to return bigger array or to create new one if it odesnt exist.
    /// Usually used when we are loading some objects in a loop and they may differ in size by little amount
    /// This way we would reuse one or two arrays than to create new array each time we need little more memory than last time
    /// </param>
    /// <returns>Shared alloactor</returns>
    private SharedAllocator GetObjBuffer(PDFFile file, (int objIndex, int generation) objPosition)
    {
      PDF_XrefEntry xRefEntry = GetXrefEntry(file, objPosition);
      SharedAllocator allocator = new SharedAllocator();
      int offset = 0;
      int len = 0;

      if (xRefEntry.EntryType == PDF_XrefEntryType.COMPRESSED)
      {
        byte[] b = GetCompressedObjBuffer(file, ref xRefEntry, out offset, out len);
        allocator.Buffer = b;
        allocator.Range = new Range(offset, offset + len);
        allocator.IsSharedArray = false;
      }
      else if (xRefEntry.EntryType == PDF_XrefEntryType.NORMAL)
      {
        byte[] b = GetNormalObjBuffer(file, ref xRefEntry, out offset, out len);
        allocator.Buffer = b;
        allocator.Range = new Range(offset, offset + len);
        allocator.IsSharedArray = true;
      }
      else
      {
        allocator.Buffer = Array.Empty<byte>();
        allocator.Range = new Range(0, 0);
        allocator.IsSharedArray = false;
      }

      return allocator;
    }

    /// <summary>
    /// Use only for Compressed obj buffer
    /// Generation number of compressed objects is always 0, as well as the object strreams
    /// </summary>
    /// <param name="file"></param>
    /// <param name="objInfo"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    private byte[] GetCompressedObjBuffer(PDFFile file, ref PDF_XrefEntry entry, out int offset, out int len)
    {
      // TODO: Is this good idea?
      if (entry.EntryType != PDF_XrefEntryType.COMPRESSED)
        throw new InvalidDataException("Object type is not compressed!");
      
      // check if stream IR for this entry exists in object streams list
      // if it exists
      // return byte[] buffer and position of the object with the offset and length (next offset - curr offset)
      // if doesnt parse it
      PDF_ObjectStream info = null;
      foreach ((int key, PDF_ObjectStream stream) in file.ObjectStreams)
        if (key == entry.Index)
          info = stream;

      if (info is null)
      {
        // TODO: check if generation 0 is of objects in the stream or the stream itself
        PDF_XrefEntry streamEntry = file.CrossReferenceEntries[entry.StreamIR];
        info = new PDF_ObjectStream();
        ParseObjectStream(file, entry.StreamIR, streamEntry.TenDigitValue, info);
      }

      for (int i = 0; i < info.Offsets.Count; i++)
      {
        if (info.Offsets[i].objId == entry.Index)
        {
          offset = info.Offsets[i].offset;
          len = info.CommonStreamDict.RawStreamData.Length - offset;
          // offsets are in increasing order, so to get object size we can just do next objcet offset - current offset
          // unless we are at last index
          if (i + 1 < info.Offsets.Count)
            len = info.Offsets[i + 1].offset - offset;
          return info.CommonStreamDict.RawStreamData;
          break;
        }
      }

      // TODO: Handle not found
      offset = 0;
      len = 0;
      return Array.Empty<byte>();
    }

    private byte[] GetNormalObjBuffer(PDFFile file, ref PDF_XrefEntry entry, out int offset, out int len)
    {
      // should ths point to null obj
      if (entry.EntryType != PDF_XrefEntryType.NORMAL)
        throw new InvalidDataException("Trying to access  not normal object");

      int objectLength = (int)(GetDistanceToNextNormalObject(file, ref entry));
      
      byte[] buffer = ArrayPool<byte>.Shared.Rent(objectLength);

      long origPos = file.Stream.Position;
      file.Stream.Position = entry.TenDigitValue;
      len = file.Stream.Read(buffer, 0, objectLength);
      Debug.Assert(len == objectLength);
      offset = 0;
      file.Stream.Position = origPos;

      return buffer;
    }

    /// <summary>
    /// Returns allocator buffer to array pool if it was rented
    /// Should always be used when work is finished with SharedAllocator object
    /// </summary>
    /// <param name="allocator"></param>
    private void FreeAllocator(SharedAllocator? allocator)
    {
      if (allocator == null)
        return;

      if (allocator.IsSharedArray)
      {
        ArrayPool<byte>.Shared.Return(allocator.Buffer!);
        // not sure what happens if I return array to buffer but keep reference to it
        allocator.Buffer = null;
      }
    }

    /// <summary>
    /// Force creates array with specified size in shared array pool
    /// Usually done whne itering over list of keys or indirect references to create one array that can fit any obj from the list
    /// </summary>
    /// <param name="size">Size of array that will be created</param>
    private void ForceCreateArrayInsharedPool(int size)
    {
      byte[] arr = ArrayPool<byte>.Shared.Rent(size);
      ArrayPool<byte>.Shared.Return(arr);
    }

    /// <summary>
    /// This method is introduced and should be used in most cases because it support checks for IR
    /// Decided to add this because in documentations it says that some field may be array but some writers might just use IR
    /// and since we don't want to allocate new memory in helper we have always had to handle this case manually in the parser
    /// TODO: replace old way of doing it with this method
    /// </summary>
    /// <returns>Result</returns>
    public List<string> ParsePDFArray(PDFFile file, ref PDFSpanParseHelper helper)
    {
      helper.SkipWhiteSpace();
      // its IR
      if (helper.IsCurrentByteDigit())
      {
        (int objIndex, int generation) objPosition = helper.GetNextIndirectReference();
        SharedAllocator allocator = GetObjBuffer(file, objPosition);
        ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);
        PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref buffer);
        irHelper.SkipObjHeader();
        List<string> res = irHelper.GetNextArrayStrict();
        FreeAllocator(allocator);
        return res;
      }
      else if (helper._char == '[')
      {
        return helper.GetNextArrayStrict();
      }
      else
      {
        throw new InvalidDataException("Invalid array data!");
      }
    }

    public List<(int objIndex, int generation)> ParsePDFListOfIndirectReferences(PDFFile file, ref PDFSpanParseHelper helper)
    {
      helper.SkipWhiteSpace();
      // its IR
      if (helper.IsCurrentByteDigit())
      {
        (int objIndex, int generation) objPosition = helper.GetNextIndirectReference();
        SharedAllocator allocator = GetObjBuffer(file, objPosition);
        ReadOnlySpan<byte> buffer = allocator.Buffer.AsSpan(allocator.Range);
        PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref buffer);
        irHelper.SkipObjHeader();
        List<(int objIndex, int generation)> res = irHelper.GetNextIndirectReferenceList();
        FreeAllocator(allocator);
        return res;
      }
      else if (helper._char == '[')
      {
        return helper.GetNextIndirectReferenceList();
      }
      else
      {
        throw new InvalidDataException("Invalid array data!");
      }
    }
  }
}