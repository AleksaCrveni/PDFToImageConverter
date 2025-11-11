using Converter.Converters;
using Converter.Converters.Image.TIFF;
using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.Rasterizers;
using Converter.Writers.TIFF;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
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

    // return some kind of struct
    public PDFFile Parse(string filepath, ref PDF_Options options)
    {
      PDFFile file = new PDFFile();
      file.Stream = File.OpenRead(filepath);
      file.Options = options;
      
      // go to end to find byte offset to cross refernce table
      
      ReadInitialData(file);
      return file;

    }
    // Read PDFVersion, Byte offset for last cross reference table, file trailer

    void ReadInitialData(PDFFile file)
    {
      file.PdfVersion = ParsePdfVersionFromHeader(file.Stream);
      // Read last 1024 bytes and trailer, startxref, (last)cross reference tables bytes and %%EOF
      file.Stream.Seek(-1024, SeekOrigin.End);
      Span<byte> footerBuffer = stackalloc byte[1024];
      int readBytes = file.Stream.Read(footerBuffer);
      if (readBytes != footerBuffer.Length)
        throw new InvalidDataException("Invalid data");


      PDFSpanParseHelper parseHelper = new PDFSpanParseHelper(ref footerBuffer);
      ParseTrailer(file, ref parseHelper);

      // NOTE: It may already be set in Parse Trailer if xref is cross-reference stream
      // because in that case trailer and xref dict are in one dictionary
      if (file.LastCrossReferenceOffset == 0)
      {
        ParseLastCrossRefByteOffset(file);
        ParseCrossReferenceTable(file);
      }

      ParseCatalogDictionary(file, (file.Trailer.RootIR.Item1, file.Trailer.RootIR.Item2));
      ParseRootPageTree(file, (file.Catalog.PagesIR.Item1, file.Catalog.PagesIR.Item2));
      ParsePagesData(file);
      ConvertPageDataToImage(file);
    }

    private void ConvertPageDataToImage(PDFFile file)
    {
      byte[] rawContent = file.PageInformation[0].ContentDict.RawStreamData;
      Span<byte> fourByteSlice = stackalloc byte[4];
      PDF_ResourceDict rDict = file.PageInformation[0].ResourceDict;

      // TODO: make this later based on some mode, to be to convert to other file formats as well
      // TODO: save in conveter later
      IConverter converter = file.Target switch
      {
        TargetConversion.TIFF_BILEVEL => throw new NotImplementedException(),
        TargetConversion.TIFF_GRAYSCALE => new TIFFGrayscaleConverter(rDict.Font, rDict, file.PageInformation[0], SourceConversion.PDF, new TIFFWriterOptions()),
        TargetConversion.TIFF_PALLETE => throw new NotImplementedException(),
        TargetConversion.TIFF_RGB => throw new NotImplementedException(),
      };
    
      PDFGOInterpreter pdfGo = new PDFGOInterpreter(rawContent.AsSpan(), ref rDict, ref fourByteSlice, converter);

      pdfGo.ConvertToPixelData();
    }

    // TODO: process resource and content in parallel?
    private void ParsePagesData(PDFFile file)
    {
      PDF_PageInfo pInfo;
      for (int i = 0; i < file.PageInformation.Count; i++)
      {
        // Process Resources
        PDF_ResourceDict resourceDict = new PDF_ResourceDict();
        ParseResourceDictionary(file, file.PageInformation[i].ResourcesIR, ref resourceDict);
        pInfo = file.PageInformation[i];
        pInfo.ResourceDict = resourceDict;

        // Process Contents
        PDF_CommonStreamDict contentDict = new PDF_CommonStreamDict();
        ParsePageContents(file, file.PageInformation[i].ContentsIR, ref contentDict);
        pInfo.ContentDict = contentDict;
        file.PageInformation[i] = pInfo;
        // don't do anything else for now, untill i learn about graphics and image formats
        // i can parse data further but i hav eno clue what to do with it or what i really need.
      }
    }

    // TODO: for later, see we can decode withoutloading it all
    private void ParsePageContents(PDFFile file, (int objectIndex, int) objectPosition, ref PDF_CommonStreamDict contentDict)
    {
      int objectIndex = objectPosition.objectIndex;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      // allocate bigger so we can reuse it for content stream later
      // but maybe allocate smaller just to get lenght and then see if it can fit everything in stack
      // you can do some entire obj size - length to get stream dictionary size, but I am not sure if this would work\
      // if streams are interrupted
      // for now just expect that stream dict at least will fit in 8kb, later chang if needed when testing with big files
      Span<byte> buffer = stackalloc byte[KB * 8];
      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      file.Stream.Position = objectByteOffset;
      int readBytes = file.Stream.Read(buffer);
      if (readBytes == 0)
        throw new InvalidDataException("Unexpected EOS!");

      bool startDictFound = false;
      while (!startDictFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          startDictFound = helper.IsCurrentCharacterSameAsNext();
      }

      // TODO: expand this
      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        ParseCommonStreamDictAsExtension(file, ref helper, tokenString, ref contentDict);
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

      // Parse stream
      long encodedStreamLen = contentDict.Length;
      // do some checking to know entire stream is already loaded in buffer
      if (encodedStreamLen + helper._position > buffer.Length)
      {
        // full stream isn't loaded in the buffer so we have to load it
        if (encodedStreamLen > buffer.Length)
        {
          // heap alloc if we can't reuse existing buffer
          buffer = new byte[encodedStreamLen];
        }
        // set position after stream dict
        file.Stream.Position = objectByteOffset + helper._position;
        readBytes = file.Stream.Read(buffer);
        if (readBytes == 0)
          throw new InvalidDataException("Unexpected EOS!");
        helper._position = 0;
      }
      // go to next line
      helper.SkipWhiteSpace();
      ReadOnlySpan<byte> encodedSpan = buffer.Slice(helper._position, (int)encodedStreamLen);
      contentDict.RawStreamData = DecodeFilter(ref encodedSpan, contentDict.Filters);
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
      commonStreamDict.RawStreamData = DecodeFilter(ref encodedSpan, commonStreamDict.Filters);
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
          dict.Filters = helper.GetListOfNames<PDF_Filter>();
          break;
        default:
          break;
      }
    }

    private byte[] DecodeFilter(ref ReadOnlySpan<byte> inputSpan, List<PDF_Filter> filters)
    {
      // first just do single filter
      PDF_Filter f = filters[0];
      if (f == PDF_Filter.Null)
        return new byte[1];

      byte[] decoded;
      switch (f)
      {
        case PDF_Filter.Null:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.ASCIIHexDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.ASCII85Decode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.LZWDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.FlateDecode:
          // figure out if its gzip, base deflate or zlib decompression
          Stream decompressor;

          var arr = inputSpan.ToArray();
          var compressStream = new MemoryStream(arr);
          byte b0 = inputSpan[0];
          byte b1 = inputSpan[1];
          // account for big/lttiel end
          // not sure if in deflate stream this can be first byte
          if ((b0 & 15) == 8 && (b0 >> 4 & 15)  == 7)
          {
            // ZLIB check (MSB is left)
            // CM 0-3 bits need to be 8
            // CMINFO 4-7 bits need to be 7
            decompressor = new ZLibStream(compressStream, CompressionMode.Decompress);
          }
          else if (b0 == 31 && b1 == 139)
          {
            // GZIP check (MSB is right)
            decompressor = new GZipStream(compressStream, CompressionMode.Decompress);
          }
          else
            decompressor = new DeflateStream(compressStream, CompressionMode.Decompress);

          MemoryStream stream = new MemoryStream();
          decompressor.CopyTo(stream);
          // dispose streams
          compressStream.Dispose();
          decompressor.Dispose();
          // write custom stream because this will copy, so we copy from decompressor
          // and then we have to copy again, i would preferably have one copy
          decoded = stream.ToArray();
          stream.Dispose();
          break;
        case PDF_Filter.RunLengthDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.CCITTFaxDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.JBIG2Decode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.DCTDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.JPXDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.Crypt:
          decoded = Array.Empty<byte>();
          break;
        default:
          decoded = new byte[1];
          break;
      }

      return decoded;
    }

  

    private void ParseResourceDictionary(PDFFile file, (int objIndex, int generation) objPosition, ref PDF_ResourceDict resourceDict)
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
      SharedAllocator? irAllocator = null;
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "ExtGState":
            resourceDict.ExtGState = helper.GetNextDict();
            break;
          case "ColorSpace":
            List<PDF_ColorSpaceData> csData = new List<PDF_ColorSpaceData>();

            (bool isDirect, SharedAllocator? allocator) info = ReadIntoDirectOrIndirectDict(file, ref helper);
            if (info.isDirect)
            {
              ParseColorSpaceIRDictionary(file, ref helper, true, ref csData);
            }
            else
            {
              ReadOnlySpan<byte> irBuffer = info.allocator.Buffer.AsSpan(info.allocator.Range);
              PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);
              ParseColorSpaceIRDictionary(file, ref irHelper, false, ref csData);
            }
            FreeAllocator(info.allocator);
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

            info = ReadIntoDirectOrIndirectDict(file, ref helper);
            if (info.isDirect)
            {
              ParseFontIRDictionary(file, ref helper, true, ref fontData);
            }
            else
            {
              ReadOnlySpan<byte> irBuffer = info.allocator!.Buffer.AsSpan(info.allocator.Range);
              PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);
              ParseFontIRDictionary(file, ref irHelper, false, ref fontData);
            }
            FreeAllocator(info.allocator);
            resourceDict.Font = fontData;
            break;
          case "ProcSet":
            resourceDict.ProcSet = helper.GetNextArrayStrict();
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

      FreeAllocator(allocator);
    }

    private void ParseColorSpaceStreamAndDictionary(PDFFile file, (int objIndex, int generation) objPosition, ref PDF_ColorSpaceDictionary dict)
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
      PDF_CommonStreamDict commonStreamDict = new PDF_CommonStreamDict();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "N":
            dict.N = helper.GetNextInt32();
            break;
          case "Alternate":
            PDF_ColorSpace cs = helper.GetNextName<PDF_ColorSpace>();
            if (cs == PDF_ColorSpace.NULL)
              throw new InvalidDataException("Alternate color space invalid!");

            dict.Alternate = cs;
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
      ReadOnlySpan<byte> encodedSpan = buffer.Slice(helper._position, (int)commonStreamDict.Length);
      commonStreamDict.RawStreamData = DecodeFilter(ref encodedSpan, commonStreamDict.Filters);
#if DEBUG
      File.WriteAllBytes(Path.Join(Files.RootFolder, "ColorSpaceDecodedSample.txt"), commonStreamDict.RawStreamData);
#endif
      dict.CommonStreamDict = commonStreamDict;
      FreeAllocator(allocator);
    }

    // array of IRs with keys 
    // i.e [ /ICCBased 7 0 R]
    private void ParseColorSpaceIRArray(PDFFile file, ReadOnlySpan<byte> buffer, ref List<PDF_ColorSpaceInfo> info)
    {
      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      helper.ReadUntilNonWhiteSpaceDelimiter();
      if (helper._char != '[')
        throw new InvalidDataException("Invalid ColorSpace Family array!");
      PDF_ColorSpaceInfo csi;
      while (helper._char != ']' && helper._char != PDFConstants.NULL)
      {
        csi = new PDF_ColorSpaceInfo();
        PDF_ColorSpace csf = helper.GetNextName<PDF_ColorSpace>();
        if (csf == PDF_ColorSpace.NULL)
          throw new InvalidDataException("Invalid Color Space Family!");
        
        (int objectIndex, int _) objPosition = helper.GetNextIndirectReference();
        PDF_ColorSpaceDictionary csd = new PDF_ColorSpaceDictionary();
        ParseColorSpaceStreamAndDictionary(file, objPosition, ref csd);

        csi.ColorSpaceFamily = csf;
        csi.Dict = csd;
        info.Add(csi);
        helper.ReadUntilNonWhiteSpaceDelimiter();
      }

    }

    private int ParseColorSpaceIRDictionary(PDFFile file, ref PDFSpanParseHelper helper, bool dictOpen, ref List<PDF_ColorSpaceData> csData)
    {
      bool dictStartFound = dictOpen;
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }

      string key;
      long objectByteOffset = 0;
      long objectLength = 0;

      // can also calcualte lenght or largest dict and then alloc but its a bit more complicated because objects can be compressed 
      // but??? We don't care about compressed objects since that memory will be loaded regardless and we will just slice into it
      // so just get largest 'normal' size
      // but i think these should always be under 8k

      long origPos = file.Stream.Position;
      List<(string key, (int objIndex, int generation) objPosition)> objPositions = new List<(string key, (int objIndex, int generation) objPosition)>();
      while (helper._char != '>' && !helper.IsCurrentCharacterSameAsNext())
      {
        // /Cs1 i.e  its not color space family or similar
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
        PDF_ColorSpaceData cs = new PDF_ColorSpaceData();
        List<PDF_ColorSpaceInfo> csInfo = new List<PDF_ColorSpaceInfo>();

        allocator = GetObjBuffer(file, objPosition);
        irBuffer = allocator.Buffer.AsSpan(allocator.Range);
        ParseColorSpaceIRArray(file, irBuffer, ref csInfo);
        cs.Key = name;
        cs.ColorSpaceInfo = csInfo;
        csData.Add(cs);
        FreeAllocator(allocator);
      }
      return helper._position;
    }

    
    private void ParseFontIRDictionary(PDFFile file, ref PDFSpanParseHelper helper, bool dictOpen, ref List<PDF_FontData> fontData)
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

        IRasterizer rasterizer = new TTFRasterizer(fontInfo.FontDescriptor.FontFile.CommonStreamInfo.RawStreamData, ref fontInfo);
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
      int[] widthsArr;
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
              widthsArr = new int[fontInfo.LastChar - fontInfo.FirstChar + 1];
              helper.ReadChar();
              for (int i = 0; i < widthsArr.Length; i++)
              {
                widthsArr[i] = helper.GetNextInt32();
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
            PDF_FontEncodingData encodingData = new PDF_FontEncodingData();
            if (helper._char == '/')
            {
              PDF_FontEncodingType encType = helper.GetNextName<PDF_FontEncodingType>();
              if (encType == PDF_FontEncodingType.Null)
                throw new InvalidDataException("Invalid encoding name!");
              encodingData.BaseEncoding = encType;
            }
            else
            {
              (int objIndex, int generation) IR = helper.GetNextIndirectReference();
              SharedAllocator irAllocator = GetObjBuffer(file, IR);
              ReadOnlySpan<byte> irBuffer = irAllocator.Buffer.AsSpan(irAllocator.Range);

              ParseFontEncodingDictionary(file, irBuffer, ref encodingData);
              FreeAllocator(irAllocator);
            }

            fontInfo.EncodingData = encodingData;
            break;
          case "ToUnicode":
            //TODO: fix, this is actually IR to stream
            //placeholder
            (var _, var r) = helper.GetNextIndirectReference();
            fontInfo.ToUnicode = new byte[1];
            break;
          default:
            break;
        }

        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '>' && helper.IsCurrentCharacterSameAsNext())
          break;
        tokenString = helper.GetNextToken();
      }

      if (fontInfo.EncodingData.BaseEncoding == PDF_FontEncodingType.Null)
      {
        if (!((fontInfo.FontDescriptor.Flags & PDF_FontFlags.Symbolic) == PDF_FontFlags.Symbolic))
          fontInfo.EncodingData.BaseEncoding = PDF_FontEncodingType.StandardEncoding;
      }

      //parse width if its IR
      if (widthIR.wIndex > -1)
      {
        SharedAllocator irAllocator = GetObjBuffer(file, widthIR);
        ReadOnlySpan<byte> irBuffer = irAllocator.Buffer.AsSpan(irAllocator.Range);

        widthsArr = new int[fontInfo.LastChar - fontInfo.FirstChar + 1];
        PDFSpanParseHelper irHelper = new PDFSpanParseHelper(ref irBuffer);

        irHelper.SkipNextToken(); // object id
        irHelper.SkipNextToken(); // seocnd number
        irHelper.SkipNextToken(); // 'obj'
        irHelper.ReadUntilNonWhiteSpaceDelimiter();
        while (irHelper._char != '[')
        {
          irHelper.ReadChar();
          irHelper.ReadUntilNonWhiteSpaceDelimiter();
        }

        for (int i = 0; i < widthsArr.Length; i++)
        {
          widthsArr[i] = irHelper.GetNextInt32();
        }

        irHelper.ReadUntilNonWhiteSpaceDelimiter();
        if (irHelper._char != ']')
          throw new InvalidDataException("Invalid end of widths array!");

        fontInfo.Widths = widthsArr;
        FreeAllocator(irAllocator);
      }


      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");
    }

    private void ParseFontEncodingDictionary(PDFFile file, ReadOnlySpan<byte> buffer, ref PDF_FontEncodingData data)
    {
      // default value is StandardFont for nonsymbolic and for symbolic fonts its fon's encoding
      // so set null if it ssymbolic and not defined and later checked to skip it
      // we will set correction in parent function because encoding might come before font dictionary
      data.BaseEncoding = PDF_FontEncodingType.Null;
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
            data.BaseEncoding = helper.GetNextName<PDF_FontEncodingType>();
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
          case "FontFamily":
            fontDescriptor.FontFamily = helper.GetNextByteString();
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
            (int objectIndex, int generation) fontFileIR = helper.GetNextIndirectReference();
            PDF_FontFileInfo fontFileInfo = new PDF_FontFileInfo();
            PDF_CommonStreamDict commonStreamDict = new PDF_CommonStreamDict();
            ParseFontFileDictAndStream(file, fontFileIR, ref fontFileInfo, ref commonStreamDict);
            fontFileInfo.CommonStreamInfo = commonStreamDict;
            fontDescriptor.FontFile = fontFileInfo;
            break;
          case "CharSet":
            fontDescriptor.CharSet = helper.GetNextToken();
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
      FillRootPageTreeFrom(file, ref helper, ref rootPageTree);
      FreeAllocator(allocator);
    }

    private void FillRootPageTreeFrom(PDFFile file, ref PDFSpanParseHelper helper, ref PDF_PageTree root)
    {
      //
      FillRootPageTreeInfo(ref helper, ref root);
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

    private void FillRootPageTreeInfo(ref PDFSpanParseHelper helper, ref PDF_PageTree pageTree)
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
              pageInfo.ResourcesIR = helper.GetNextIndirectReference();
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
              pageInfo.ContentsIR = helper.GetNextIndirectReference();
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
              pageInfo.Annots = helper.GetNextArrayStrict();
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

      FillCatalog(ref helper, ref catalog);

      // we maybe read a bit more sicne we read last object diff so make sure we are in correct position
      // TODO: verify if this stupid line below can be deleted
      file.Stream.Position = helper._position + 1;

      // Starting from PDF 1.4 version can be in catalog and it has advantage over header one if its bigger
      if (catalog.Version > PDF_Version.Null)
        file.PdfVersion = catalog.Version;
      file.Catalog = catalog;
      FreeAllocator(allocator);
    }

    private void FillCatalog(ref PDFSpanParseHelper helper, ref PDF_Catalog catalog)
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
            catalog.Lang = helper.GetNextTextString();
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

    // TODO: This will work only for one section, not subsections.Fix it later!
    private void ParseCrossReferenceTable(PDFFile file)
    {
      // TODO: I guess byteoffset should be long
      // i really feel i should be loading in more bytes in chunk
      file.Stream.Position = file.LastCrossReferenceOffset;

      // make sure talbe is starting with xref keyword
      Span<byte> xrefBufferChar = stackalloc byte[4] { (byte)'x', (byte)'r', (byte)'e', (byte)'f' };
      Span<byte> xrefBuffer = stackalloc byte[4];
      int readBytes = file.Stream.Read(xrefBuffer);
      if (xrefBuffer.Length != readBytes)
        throw new InvalidDataException("Invalid data");
      if (!AreSpansEqual(xrefBuffer, xrefBufferChar, 4))
        throw new InvalidDataException("Invalid data");

      Span<byte> nextLineBuffer = StreamHelper.GetNextLineAsSpan(file.Stream);
      PDFSpanParseHelper parseHelper = new PDFSpanParseHelper(ref nextLineBuffer);
      int startObj = parseHelper.GetNextInt32();
      int endObj = parseHelper.GetNextInt32();
      int sectionLen = endObj - startObj;

      // TODO: Think if you want list or array and fix this later
      PDF_XrefEntry[] entryArr = new PDF_XrefEntry[sectionLen];
      Array.Fill(entryArr, new PDF_XrefEntry());
      List<PDF_XrefEntry> cRefEntries = entryArr.ToList();
      Span<byte> cRefEntryBuffer = stackalloc byte[20];
      readBytes = 0;
      PDF_XrefEntry entry;
      for (int i = startObj; i < endObj; i++)
      {
        readBytes = file.Stream.Read(cRefEntryBuffer);
        if (readBytes != cRefEntryBuffer.Length)
          throw new InvalidDataException("Invalid data");

        entry = cRefEntries[i];
        entry.TenDigitValue = (long)ConvertBytesToUnsignedInt64(cRefEntryBuffer.Slice(0, 10));
        entry.GenerationNumber = ConvertBytesToUnsignedInt16(cRefEntryBuffer.Slice(11, 5));
        entry.Index = i;
        byte entryType = cRefEntryBuffer[17];
        if (entryType != (byte)'n' && entryType != (byte)'f')
          throw new InvalidDataException("Invalid data");
        if (entryType == 'n')
          entry.EntryType = PDF_XrefEntryType.NORMAL;
        else
          entry.EntryType = PDF_XrefEntryType.FREE;
        cRefEntries[i] = entry;
      }

      file.CrossReferenceEntries = cRefEntries;
    }
    // TODO:   test case when trailer is not complete ">>" can't be found after opening brackets
    private void ParseTrailer(PDFFile file, ref PDFSpanParseHelper helper)
    {
      // 1. Check if trailer exists
      // 2. if its not found check at xref position if its cross reference stream
      bool trailerFound = false;
      List<PDF_XrefEntry> cRefEntry;
      long xrefPos = 0;
      PDF_Trailer trailer = new PDF_Trailer();

      string tokenString = "-";
      while (tokenString != string.Empty)
      {
        if (tokenString == "trailer")
        {
          trailerFound = true;
          // is this needed, why dont I just get next token
          bool startOfDictFound = false;
          while (!startOfDictFound)
          {
            helper.ReadUntilNonWhiteSpaceDelimiter();
            if (helper._char == '<')
              startOfDictFound = helper.IsCurrentCharacterSameAsNext();
          }
          tokenString = helper.GetNextToken();
          // put in separate function like parse TrailerDict or something similar
          while (tokenString != "")
          {
            switch (tokenString)
            {
              case "Size":
                trailer.Size = helper.GetNextInt32();
                break;
              case "Root":
                trailer.RootIR = helper.GetNextIndirectReference();
                break;
              case "Info":
                trailer.InfoIR = helper.GetNextIndirectReference();
                break;
              case "Encrypt":
                trailer.EncryptIR = helper.GetNextIndirectReference();
                break;
              case "Prev":
                trailer.Prev = helper.GetNextInt32();
                break;
              case "ID":
                trailer.ID = helper.GetNextArrayKnownLengthStrict(2);
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
          // Means that dict is corrupt
          if (tokenString == "")
            throw new InvalidDataException("Invalid dictionary");
          break;
        }
        else if (tokenString == "startxref")
        {
          // should this be getnextlong?
          xrefPos = helper.GetNextInt32();
          break;
        }
        else
        {
          tokenString = helper.GetNextToken();
        }
          
      }

      // trailer keyword not found, so that means its in cross-reference stream dictionary 7.5.8.1
      if (!trailerFound)
      {
        if (xrefPos == 0)
        {
          ParseLastCrossRefByteOffset(file);
          xrefPos = file.LastCrossReferenceOffset;
        }
        cRefEntry = new List<PDF_XrefEntry>();
        ParseCrossReferenceStreamAndDict(file, xrefPos, ref trailer, cRefEntry);
        file.CrossReferenceEntries = cRefEntry;
      }

      file.LastCrossReferenceOffset = xrefPos;
      file.Trailer = trailer;
    }


    /// <summary>
    /// Checks wether next object is indirect reference to dictionary or its direct dictionary in the current object
    /// We do it this way so we dont have to deal with delegeate issues where we can't always pass same arguments (i.e object that needs to be 
    /// filled or something else)
    /// Call <see cref="FreeAllocator(SharedAllocator?)"/> on info.allocator after!
    /// </summary>
    /// <param name="file">PR</param>
    /// <param name="helper">Current helper </param>
    /// <returns>IsDirectObject is set to true if its direct object and allocator is null. Reversed if its indirect reference and allocator is used</returns>
    public (bool isDirectObject, SharedAllocator? allocator) ReadIntoDirectOrIndirectDict(PDFFile file, ref PDFSpanParseHelper helper)
    {
      helper.SkipWhiteSpace();
      if (helper._char == '<' && helper.IsCurrentCharacterSameAsNext())
      {
        helper.ReadChar();
        return (true, null);
      }

      (int objIndex, int generation) IR = helper.GetNextIndirectReference();
      SharedAllocator allocator = GetObjBuffer(file, IR);
      return (false, allocator);
    }

    // TODO: Use shared pool
    public void ParseCrossReferenceStreamAndDict(PDFFile file, long xrefOffset, ref PDF_Trailer trailer, List<PDF_XrefEntry> cRefEntry) 
    {
      // Load dict in stack buffer becuase i dont know how big content stream might be
      
      int sBufferSize = KB * 4;
      byte[] arr = new byte[sBufferSize];
      
      file.Stream.Position = xrefOffset;
      int bytesRead = file.Stream.Read(arr);
      ReadOnlySpan<byte> buffer = arr.AsSpan();
      PDFSpanParseHelper helper = new PDFSpanParseHelper(ref buffer);
      bool startOfDictFound = false;
      // This is data needed for parsing, no need to save it anywhere later
      List<(int, int)> indexes = new List<(int, int)>();
      int size = 0;
      int prev;
      // NOTE: this can be byte since values are low or simply 3 int variables
      Span<int> W = stackalloc int[3]; // its always 3
      PDF_CommonStreamDict commonStreamDict = new PDF_CommonStreamDict();

      while (!startOfDictFound && helper._readPosition < buffer.Length)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          startOfDictFound = helper.IsCurrentCharacterSameAsNext();
      }
      
      string tokenString = helper.GetNextToken();
      while (tokenString != "")
      {
        switch (tokenString)
        {
          case "Size":
            trailer.Size = helper.GetNextInt32();
            size = trailer.Size;
            break;
          case "Root":
            trailer.RootIR = helper.GetNextIndirectReference();
            break;
          case "Info":
            trailer.InfoIR = helper.GetNextIndirectReference();
            break;
          case "Encrypt":
            trailer.EncryptIR = helper.GetNextIndirectReference();
            break;
          case "Prev":
            trailer.Prev = helper.GetNextInt32();
            prev = trailer.Prev;
            break;
          case "ID":
            trailer.ID = helper.GetNextArrayKnownLengthStrict(2);
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
      long streamStartPos = xrefOffset + helper._position;
      // parse stream 
      arr = new byte[commonStreamDict.Length];
      file.Stream.Position = streamStartPos;
      int readBytes = file.Stream.Read(arr);
      if (readBytes != buffer.Length)
        throw new InvalidDataException("Invalid cross reference stream!");
      buffer = arr.AsSpan();
      byte[] decoded = DecodeFilter(ref buffer, commonStreamDict.Filters);

      if (indexes.Count == 0)
        indexes.Add((0, size));

      buffer = decoded.AsSpan();
      helper = new PDFSpanParseHelper(ref buffer);

      int type;
      int secondField;
      int thirdField;
      foreach((int start, int end) subSection in indexes)
      {
        for (int i = subSection.start; i < subSection.end; i++)
        {
          type = helper.ReadSpecificSizeInt32(W[0]);
          secondField = helper.ReadSpecificSizeInt32(W[1]);
          thirdField = helper.ReadSpecificSizeInt32(W[2]);
          PDF_XrefEntry entry = new PDF_XrefEntry();
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
          if (cRefEntry.Count > i)
            cRefEntry[i] = entry;
          else
            cRefEntry.Insert(i, entry);
        }
      }
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

    // TODO: Fix EOF finding
    // Adobe 1.3 PDF spec
    // Acrobat viewers require only that the %%EOF marker appear somewhere within the last 1024 bytes of the file.
    // Adobe 1.7 PDF spec
    // The trailer of a PDF file enables a conforming reader to quickly find the cross-reference table and certain special objects.
    // Conforming readers should read a PDF file from its end. The last line of the file shall contain only the end-of-file marker, %%EOF
    // https://stackoverflow.com/questions/11896858/does-the-eof-in-a-pdf-have-to-appear-within-the-last-1024-bytes-of-the-file
    // NOTE: according to specification for NON cross reference stream PDF files, max lengh for byteoffset is 10 digits so 10^10 bytes (10gb) so ulong will be more than enough
    // NOTE: i can't seem to find max lenght for cross refernce stream PDF files
    // TODO: Fix whatever this is
    private void ParseLastCrossRefByteOffset(PDFFile file)
    {
      file.Stream.Seek(-6, SeekOrigin.End);
      bool found = false;
      sbyte index = 0;

      // probably irellevant but in case of big files , long value is 18446744073709551615 which is 20char +2 for \n on end and start
      Span<byte> buffer = stackalloc byte[20 + 1];

      // not sure if this is needed, but validate if %%EOF exists
      Span<byte> _eofBytes = stackalloc byte[6] { 37, 37, 69, 79, 70, 10 };
      Span<byte> _eofByteBuffer = stackalloc byte[6];
      int bytesRead = file.Stream.Read(_eofByteBuffer);
      if (bytesRead != _eofByteBuffer.Length)
        throw new InvalidDataException("Invalid data");

      for (index = 0; index < bytesRead; index++)
      {
        if (_eofBytes[index] != _eofByteBuffer[index])
          throw new InvalidDataException("Invalid data");
      }

      // go to max possible position where cross reference offset byte count might start
      file.Stream.Seek(-_eofByteBuffer.Length - buffer.Length, SeekOrigin.End);

      // try to read byte count offset for cross reference table
      bytesRead = file.Stream.Read(buffer);
      if (bytesRead != buffer.Length)
        throw new InvalidDataException("Invalid data");

      sbyte newLineIndex = -1;
      for (index = 1; index < bytesRead - 1; index++)
      {
        if (buffer[index] == _newLineByte)
          newLineIndex = index;
      }
      if (newLineIndex == -1 && buffer[0] != _newLineByte)
        throw new InvalidDataException("File too big");
      if (newLineIndex == buffer.Length - 2)
        throw new InvalidDataException("Invalid data");
      // get actual value
      long value = 0;
      // move newlineindex for one to get past \n to actual first digit
      for (index = ++newLineIndex; index < buffer.Length - 1; index++)
      {
        if (!char.IsDigit((char)buffer[index]))
          throw new InvalidDataException("Invalid data");
        value = value * 10 + CharUnicodeInfo.GetDecimalDigitValue((char)buffer[index]);
      }
      file.LastCrossReferenceOffset =  value;
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

    // do these better, seems odd?
    private ushort ConvertBytesToUnsignedInt16(Span<byte> buffer)
    {
      uint res = 0;
      for (int i = 0; i < buffer.Length; i++)
      {
        // these should be no negative ints so this is okay i believe?
        res = res * 10 + (uint)CharUnicodeInfo.GetDecimalDigitValue((char)buffer[i]);
      }
      // this should wrap, idk if i should throw exception
      return (ushort)res;
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
      commonStreamDict.RawStreamData = DecodeFilter(ref buffer, commonStreamDict.Filters);
      objStreamInfo.CommonStreamDict = commonStreamDict;

      // Parse offsets
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
      // means that this is last object so next object is assumed to be cross reference table
      if (index == entry.Index)
        return file.LastCrossReferenceOffset - entry.TenDigitValue;
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
        ArrayPool<byte>.Shared.Return(allocator.Buffer);
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
  }
}