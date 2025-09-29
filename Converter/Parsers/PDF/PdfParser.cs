using Converter.FileStructures;
using Converter.FIleStructures;
using Converter.Parsers.Fonts;
using Converter.Writers.TIFF;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
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
    public PDFFile Parse(string filepath)
    {
      PDFFile file = new PDFFile();
      file.Stream = File.OpenRead(filepath);
      
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


      SpanParseHelper parseHelper = new SpanParseHelper(ref footerBuffer);
      ParseTrailer(file, ref parseHelper);
      // TODO use parsehelper dont seek and copy again
      ParseLastCrossRefByteOffset(file);
      ParseCrossReferenceTable(file);
      ParseCatalogDictionary(file);
      ParseRootPageTree(file);
      ParsePagesData(file);
      ConvertPageDataToImage(file);
    }

    private void ConvertPageDataToImage(PDFFile file)
    {
      byte[] rawContent = file.PageInformation[0].ContentDict.RawStreamData;
      ITIFFWriter writer = new TIFFBilevelWriter("convertTest.pdf");
      TIFFWriterOptions tiffOptions = new TIFFWriterOptions()
      {
        Width = (int)file.PageInformation[0].MediaBox.urX,
        Height = (int)file.PageInformation[0].MediaBox.urY,
      };
      writer.WriteEmptyImage(ref tiffOptions);

      PDFGOInterpreter pdfGo = new PDFGOInterpreter(rawContent.AsSpan(), file.PageInformation[0].ResourceDict.Font, writer);
      pdfGo.ParseAll();
    }

    // TODO: process resource and content in parallel?
    private void ParsePagesData(PDFFile file)
    {
      PageInfo pInfo;
      for (int i = 0; i < file.PageInformation.Count; i++)
      {
        // Process Resources
        ResourceDict resourceDict = new ResourceDict();
        ParseResourceDictionary(file, file.PageInformation[i].ResourcesIR, ref resourceDict);
        pInfo = file.PageInformation[i];
        pInfo.ResourceDict = resourceDict;

        // Process Contents
        CommonStreamDict contentDict = new CommonStreamDict();
        ParsePageContents(file, file.PageInformation[i].ContentsIR, ref contentDict);
        pInfo.ContentDict = contentDict;
        file.PageInformation[i] = pInfo;
        // don't do anything else for now, untill i learn about graphics and image formats
        // i can parse data further but i hav eno clue what to do with it or what i really need.
      }
    }

    // TODO: for later, see we can decode withoutloading it all
    private void ParsePageContents(PDFFile file, (int objectIndex, int) objectPosition, ref CommonStreamDict contentDict)
    {
      int objectIndex = objectPosition.objectIndex;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      // allocate bigger so we can reuse it for content stream later
      // but maybe allocate smaller just to get lenght and then see if it can fit everything in stack
      // you can do some entire obj size - length to get stream dictionary size, but I am not sure if this would work\
      // if streams are interrupted
      // for now just expect that stream dict at least will fit in 8kb, later chang if needed when testing with big files
      Span<byte> buffer = stackalloc byte[KB * 8];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);
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
      Span<byte> encodedSpan = buffer.Slice(helper._position, (int)encodedStreamLen);
      contentDict.RawStreamData = DecodeFilter(ref encodedSpan, contentDict.Filters);
    }

    private void ParseFontFileDictAndStream(PDFFile file, (int objectIndex, int) objectPosition, ref FontFileInfo fontFileInfo, ref CommonStreamDict commonStreamDict)
    {
      int objectIndex = objectPosition.objectIndex;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      // allocate bigger so we can reuse it for content stream later
      // but maybe allocate smaller just to get lenght and then see if it can fit everything in stack
      // you can do some entire obj size - length to get stream dictionary size, but I am not sure if this would work\
      // if streams are interrupted
      // for now just expect that stream dict at least will fit in 8kb, later chang if needed when testing with big files
      Span<byte> buffer = stackalloc byte[KB * 8];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);
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
            fontFileInfo.Subtype = helper.GetNextName<FontFileSubtype>();
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

      // Parse stream
      // do some checking to know entire stream is already loaded in buffer
      long encodedStreamLen = commonStreamDict.Length;
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
      Span<byte> encodedSpan = buffer.Slice(helper._position, (int)encodedStreamLen);
      commonStreamDict.RawStreamData = DecodeFilter(ref encodedSpan, commonStreamDict.Filters);
      fontFileInfo.CommonStreamInfo = commonStreamDict;
    }

    private void ParseCommonStreamDictAsExtension(PDFFile file, ref SpanParseHelper helper, string tokenString, ref CommonStreamDict dict)
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
            SpanParseHelper irHelper = new SpanParseHelper(ref irBuffer);
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
          dict.Filters = helper.GetListOfNames<Filter>();
          break;
        default:
          break;
      }
    }
    private byte[] DecodeFilter(ref Span<byte> inputSpan, List<Filter> filters)
    {
      // first just do single filter
      Filter f = filters[0];
      if (f == Filter.Null)
        return new byte[1];

      byte[] decoded;
      switch (f)
      {
        case Filter.Null:
          decoded = Array.Empty<byte>();
          break;
        case Filter.ASCIIHexDecode:
          decoded = Array.Empty<byte>();
          break;
        case Filter.ASCII85Decode:
          decoded = Array.Empty<byte>();
          break;
        case Filter.LZWDecode:
          decoded = Array.Empty<byte>();
          break;
        case Filter.FlateDecode:
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
        case Filter.RunLengthDecode:
          decoded = Array.Empty<byte>();
          break;
        case Filter.CCITTFaxDecode:
          decoded = Array.Empty<byte>();
          break;
        case Filter.JBIG2Decode:
          decoded = Array.Empty<byte>();
          break;
        case Filter.DCTDecode:
          decoded = Array.Empty<byte>();
          break;
        case Filter.JPXDecode:
          decoded = Array.Empty<byte>();
          break;
        case Filter.Crypt:
          decoded = Array.Empty<byte>();
          break;
        default:
          decoded = new byte[1];
          break;
      }

      return decoded;
    }
    private void ParseResourceDictionary(PDFFile file, (int objIndex, int) objectPosition, ref ResourceDict resourceDict)
    {
      int objectIndex = objectPosition.objIndex;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      long objectLength = GetDistanceToNextObject(objectIndex, objectByteOffset, file);

      // do it like this, operations should be same only difference is where underleying memory will be stored
      Span<byte> buffer = objectLength <= KB * 8 ? stackalloc byte[(int)objectLength] : new byte[objectLength];
      file.Stream.Position = objectByteOffset;
      SpanParseHelper helper = new SpanParseHelper(ref buffer);
      int readBytes = file.Stream.Read(buffer);
      if (buffer.Length != readBytes)
        throw new InvalidDataException("Invalid Data");

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
          case "ExtGState":
            resourceDict.ExtGState = helper.GetNextDict();
            break;
          case "ColorSpace":
            resourceDict.ColorSpace = helper.GetNextDict();
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
            // not specified in documentation, but i've seen files where Font is just IR and not dict of IRs
            helper.SkipWhiteSpace();
            FontInfo fontInfo = new FontInfo();
            int bytesRead = 0;
            List<FontData> fontData = new List<FontData>();
            if (helper._char == '<' && helper.IsCurrentCharacterSameAsNext())
            {
              helper.ReadChar();
              // use this because we dont know when dict ends so we can just skip those and not read again on main buffer
              ParseFontIRDictionary(file, helper._buffer.Slice(helper._position), true, ref fontData);
            }
            else
            {
              (int objectIndex, int b) IR = helper.GetNextIndirectReference();
              objectByteOffset = file.CrossReferenceEntries[IR.objectIndex].TenDigitValue;
              objectLength = GetDistanceToNextObject(IR.objectIndex, objectByteOffset, file);

              // use array pool
              Span<byte> irBuffer = new byte[objectLength];
              
              long prevPos = file.Stream.Position;
              file.Stream.Position = objectByteOffset;
              readBytes = file.Stream.Read(irBuffer);
              // do i need this...?
              if (irBuffer.Length != readBytes)
                throw new InvalidDataException("Invalid data");
              ParseFontIRDictionary(file, irBuffer, false, ref fontData);
              file.Stream.Position = prevPos;
            }


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
    }

    // TODO: fix this as well, after you add array pooling of some kind
    private void ParseFontIRDictionary(PDFFile file, ReadOnlySpan<byte> buffer, bool dictOpen, ref List<FontData> fontData)
    {
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      bool dictStartFound = dictOpen;
      while (!dictStartFound)
      {
        helper.ReadUntilNonWhiteSpaceDelimiter();
        if (helper._char == '<')
          dictStartFound = helper.IsCurrentCharacterSameAsNext();
      }

      string key;
      FontInfo fontInfo;
      long objectByteOffset = 0;
      long objectLength = 0;
      FontData fd;
      TTFParser ttfParser;

      // make this larger in size so it can be reused, otherwise make new one
      // or maybe new one could be come main buffer since its bigger..??
      // can also calcualte lenght or largest dict and then alloc but its a bit more complicated
      // but i think these should always be under 8k
      Span<byte> mainBuffer = new byte[KB * 8];
      long origPos = file.Stream.Position;
      while (helper._char != '>' && !helper.IsCurrentCharacterSameAsNext())
      {
        key = helper.GetNextToken();
        if (key == "")
          break;
        fontInfo = new FontInfo();
        fd = new FontData();
        ttfParser = new TTFParser();
        (int objectIndex, int _) IR = helper.GetNextIndirectReference();
        objectByteOffset = file.CrossReferenceEntries[IR.objectIndex].TenDigitValue;
        objectLength = GetDistanceToNextObject(IR.objectIndex, objectByteOffset, file);
        Span<byte> irBuffer = objectLength <= mainBuffer.Length ? mainBuffer.Slice(0, (int)objectLength) : new byte[objectLength];
        file.Stream.Position = objectByteOffset;
        int bytesRead = file.Stream.Read(irBuffer);
        if (bytesRead != irBuffer.Length)
          throw new InvalidDataException("Invalid font dict lenght!");
        ParseFontDictionary(file, irBuffer, ref fontInfo);
        fd.Key = key;
        fd.FontInfo = fontInfo;

        ttfParser.Init(ref fontInfo.FontDescriptor.FontFile.CommonStreamInfo.RawStreamData);
        ttfParser.InitFont();
        fd.Parser = ttfParser;
        fontData.Add(fd);
        helper.ReadUntilNonWhiteSpaceDelimiter();
      }
      file.Stream.Position = origPos;
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
    private void ParseFontDictionary(PDFFile file, ReadOnlySpan<byte> buffer, ref FontInfo fontInfo)
    {
      SpanParseHelper helper = new SpanParseHelper(ref buffer);
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
          case "Subtype":
            fontInfo.SubType = helper.GetNextName<FontType>();
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
            int[] widthsArr = new int[fontInfo.LastChar - fontInfo.FirstChar + 1];
            if (helper._char == '[')
            {
              helper.ReadChar();
              for (int i = 0; i < widthsArr.Length; i++)
              {
                widthsArr[i] = helper.GetNextInt32();
              }
              helper.ReadUntilNonWhiteSpaceDelimiter();
              if (helper._char != ']')
                throw new InvalidDataException("Invalid end of widths array!");
              helper.ReadChar();
            } else if (char.IsDigit((char)helper._char))
            {
              int objectIndex = helper._char;
              long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
              long objectLength = GetDistanceToNextObject(objectIndex, objectByteOffset, file);
              Span<byte> irBuffer = new byte[objectLength];

              long currPos = file.Stream.Position;
              file.Stream.Position = objectByteOffset;
              int bytesRead = file.Stream.Read(irBuffer);
              if (bytesRead != objectLength)
                throw new InvalidDataException("Invalid array for Widths field!");
              file.Stream.Position = currPos;
              SpanParseHelper irHelper = new SpanParseHelper(ref irBuffer);

              irHelper.SkipNextToken(); // object id
              irHelper.SkipNextToken(); // seocnd number
              irHelper.SkipNextToken(); // 'obj'
              irHelper.ReadUntilNonWhiteSpaceDelimiter();
              while (helper._char != '[')
              {
                irHelper.ReadChar();
                irHelper.ReadUntilNonWhiteSpaceDelimiter();
              }

              for (int i = 0; i < widthsArr.Length; i++)
              {
                widthsArr[i] = irHelper.GetNextInt32();
              }

              irHelper.ReadUntilNonWhiteSpaceDelimiter();
              if (helper._char != ']')
                throw new InvalidDataException("Invalid end of widths array!");
            }
            else
            {
              throw new InvalidDataException("Invalid Widths value in Font Dictionary!");
            }

            fontInfo.Widths = widthsArr;
            break;
          case "FontDescriptor":
            (int objectIndex, int _) ir = helper.GetNextIndirectReference();
            FontDescriptor fontDescriptor = new FontDescriptor();
            // prob should rename
            ParseFontDescriptor(file, ir, ref fontDescriptor);
            fontInfo.FontDescriptor = fontDescriptor;
            break;
          case "Encoding":
            //TODO: Fix later to support dictionaries as welll
            fontInfo.Encoding = helper.GetNextName<EncodingInf>();
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

      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");
    }

    private void ParseFontDescriptor(PDFFile file, (int objectIndex, int _) objectPosition, ref FontDescriptor fontDescriptor)
    {
      int objectIndex = objectPosition.objectIndex;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      long objectLength = GetDistanceToNextObject(objectIndex, objectByteOffset, file);
      Span<byte> buffer = new byte[objectLength];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);
      int readBytes = file.Stream.Read(buffer);
      if (readBytes != objectLength)
        throw new InvalidDataException("Invalid font descriptor dictionary.");

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
            fontDescriptor.FontStretch = helper.GetNextName<FontStretch>();
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
            FontFlags flags = new FontFlags();
            if ((i & 1) == 1)
              flags |= FontFlags.FixedPitch;
            if ((i & 2) == 2)
              flags |= FontFlags.Serif;
            if ((i & 4) == 4)
              flags |= FontFlags.Symbolic;
            if ((i & 8) == 8)
              flags |= FontFlags.Script;
            if ((i & 32) == 32)
              flags |= FontFlags.Nonsymbolic;
            if ((i & 64) == 64)
              flags |= FontFlags.Italic;
            if ((i & 65536) == 65536) // 17th bit (indexed from 1)
              flags |= FontFlags.AllCap;
            if ((i & 131072) == 131072) // 18th bit (indexed from 1)
              flags |= FontFlags.SmallCap;
            if ((i & 262144) == 262144) // 19th bit (indexed from 1)
              flags |= FontFlags.ForceBold;
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
            (int objectIndex, int _) fontFileIR = helper.GetNextIndirectReference();
            FontFileInfo fontFileInfo = new FontFileInfo();
            CommonStreamDict commonStreamDict = new CommonStreamDict();
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
    }

    private void ParseRootPageTree(PDFFile file)
    {
      int objectIndex = file.Catalog.PagesIR.Item1;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      long objectLength = GetDistanceToNextObject(objectIndex, objectByteOffset, file);
      PageTree rootPageTree = new PageTree();

      Span<byte> buffer = objectLength <= KB * 8 ? stackalloc byte[(int)objectLength] : new byte[objectLength];

      // make linked list or just flatten references????
      // ok for now just load all and store it, later be smarter
      file.Stream.Position = objectByteOffset;
      Span<byte> pageBuffer = stackalloc byte[(int)objectLength];
      SpanParseHelper helper = new SpanParseHelper(ref pageBuffer);
      int readBytes = file.Stream.Read(pageBuffer);
      // do i need this...?
      if (pageBuffer.Length != readBytes)
        throw new InvalidDataException("Invalid data");
      FillRootPageTreeFrom(file, ref helper, ref rootPageTree);
    }

    private void FillRootPageTreeFrom(PDFFile file, ref SpanParseHelper helper, ref PageTree root)
    {
      //
      FillRootPageTreeInfo(ref helper, ref root);
      List<PageTree> pageTrees = new List<PageTree>();
      List<PageInfo> pages = new List<PageInfo>();
      pageTrees.Add(root);
      for (int i = 0; i < root.KidsIRs.Count; i++)
      {
        FillAllPageTreeAndInformation(root.KidsIRs[i], pageTrees, pages, file);
      }

      // go over kids and call FillAllPageTreeAndInformation
      file.PageTrees = pageTrees;
      file.PageInformation = pages;
    }

    private void FillRootPageTreeInfo(ref SpanParseHelper helper, ref PageTree pageTree)
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
    private void FillAllPageTreeAndInformation((int, int) kidPositionIR, List<PageTree> pageTrees, List<PageInfo> pages, PDFFile file)
    {
      long objectByteOffset = file.CrossReferenceEntries[kidPositionIR.Item1].TenDigitValue;
      long objectLength = GetDistanceToNextObject(kidPositionIR.Item1, objectByteOffset, file);

      file.Stream.Position = objectByteOffset;
      Span<byte> buffer = objectLength <= KB * 8 ? stackalloc byte[(int)objectLength] : new byte[objectLength];
     
      SpanParseHelper helper = new SpanParseHelper(ref buffer);
      int readBytes = file.Stream.Read(buffer);
      // do i need this...?
      if (buffer.Length != readBytes)
        throw new InvalidDataException("Invalid data");

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
        PageTree pageTree = new PageTree();
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
        PageInfo pageInfo = new PageInfo();
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
              pageInfo.Group = helper.GetNextDict();
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
              pageInfo.Tabs = helper.GetNextName<Tabs>();
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
    }

    private void ParseCatalogDictionary(PDFFile file)
    {
      // Instead of reading one by one to see where end is, get next object position and have that as length to load
      // Experimental!
      int objectIndex = file.Trailer.RootIR.Item1;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      long objectLength = GetDistanceToNextObject(objectIndex, objectByteOffset, file);
      // if this is bigger 4096 or double that then do see to do some kind of different processing
      // I think that reading in bulk should be faster than reading 1 char by 1 from stream
      Catalog catalog = new Catalog();
      file.Stream.Position = objectByteOffset;
      Span<byte> buffer = objectLength <= KB * 8 ? stackalloc byte[(int)objectLength] : new byte[objectLength];

      SpanParseHelper helper = new SpanParseHelper(ref buffer);
      int readBytes = file.Stream.Read(buffer);
      if (buffer.Length != readBytes)
        throw new InvalidDataException("Invalid data");
      FillCatalog(ref helper, ref catalog);

      // we maybe read a bit more sicne we read last object diff so make sure we are in correct position
      file.Stream.Position = helper._position + 1;

      // Starting from PDF 1.4 version can be in catalog and it has advantage over header one if its bigger
      if (catalog.Version > PDFVersion.Null)
        file.PdfVersion = catalog.Version;
      file.Catalog = catalog;
    }

    private void FillCatalog(ref SpanParseHelper helper, ref Catalog catalog)
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
            catalog.PageLayout = helper.GetNextName<PageLayout>();
            break;
          case "PageMode":
            catalog.PageMode = helper.GetNextName<PageMode>();
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
            catalog.StructTreeRoot = helper.GetNextDict();
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
      SpanParseHelper parseHelper = new SpanParseHelper(ref nextLineBuffer);
      int startObj = parseHelper.GetNextInt32();
      int endObj = parseHelper.GetNextInt32();
      int sectionLen = endObj - startObj;

      // TODO: Think if you want list or array and fix this later
      CRefEntry[] entryArr = new CRefEntry[sectionLen];
      Array.Fill(entryArr, new CRefEntry());
      List<CRefEntry> cRefEntries = entryArr.ToList();
      Span<byte> cRefEntryBuffer = stackalloc byte[20];
      readBytes = 0;
      CRefEntry entry;
      for (int i = startObj; i < endObj; i++)
      {
        readBytes = file.Stream.Read(cRefEntryBuffer);
        if (readBytes != cRefEntryBuffer.Length)
          throw new InvalidDataException("Invalid data");

        entry = cRefEntries[i];
        entry.TenDigitValue = (long)ConvertBytesToUnsignedInt64(cRefEntryBuffer.Slice(0, 10));
        entry.GenerationNumber = ConvertBytesToUnsignedInt16(cRefEntryBuffer.Slice(11, 5));
        byte entryType = cRefEntryBuffer[17];
        if (entryType != (byte)'n' && entryType != (byte)'f')
          throw new InvalidDataException("Invalid data");
        entry.EntryType = entryType;
        cRefEntries[i] = entry;
      }

      file.CrossReferenceEntries = cRefEntries;
    }
    // TODO: test case when trailer is not complete ">>" can't be found after opening brackets
    private void ParseTrailer(PDFFile file, ref SpanParseHelper helper)
    {
      Trailer trailer = new Trailer();

      string tokenString = "-";
      while (tokenString != string.Empty)
      {
        if (tokenString == "trailer")
        {
          // is this needed, why dont I just get next token
          bool startOfDictFound = false;
          while (!startOfDictFound)
          {
            helper.ReadUntilNonWhiteSpaceDelimiter();
            if (helper._char == '<')
              startOfDictFound = helper.IsCurrentCharacterSameAsNext();
          }
          tokenString = helper.GetNextToken();
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
        tokenString = helper.GetNextToken();
      }
      file.Trailer = trailer;
    }
    // TODO: account for this later
    // Comment from spec:
    // Beginning with PDF 1.4, the VErsion entry in the document's catalog dictionary (lcoated via the Root entry in the file's trailer
    // as described in 7.5.5, if present, shall be used instead of version specified in the Header
    // TODO: I really should have just compared and parsed the string instead of trying to act smart
    // because i will have to parse entire file and heapallock anyways
    // maybe later move this to be ref paramter
    private PDFVersion ParsePdfVersionFromHeader(Stream stream)
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
            return PDFVersion.V1_0;
          case 0x31:
            return PDFVersion.V1_1;
          case 0x32:
            return PDFVersion.V1_2;
          case 0x33:
            return PDFVersion.V1_3;
          case 0x34:
            return PDFVersion.V1_4;
          case 0x35:
            return PDFVersion.V1_5;
          case 0x36:
            return PDFVersion.V1_6;
          case 0x37:
            return PDFVersion.V1_7;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
      else
      {
        switch (minorVersion)
        {
          case 0x30:
            return PDFVersion.V2_0;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
    }

    private PDFVersion ParsePdfVersionFromCatalog(ref SpanParseHelper helper)
    {
      ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>();
      helper.GetNextStringAsReadOnlySpan(ref buffer);
      // Expect /M.m where M is major and m minor version
      if (buffer.Length != 4)
        throw new InvalidDataException("Invalid data!");
      byte majorVersion = buffer[1];
      if (majorVersion != 0x31 && majorVersion != 0x32)
        throw new InvalidDataException("Invalid data");
      if (buffer[2] != 0x2e)
        throw new InvalidDataException("Invalid data");
      byte minorVersion = buffer[3];
      if (majorVersion == 0x31)
      {
        switch (minorVersion)
        {
          case 0x30:
            return PDFVersion.V1_0;
          case 0x31:
            return PDFVersion.V1_1;
          case 0x32:
            return PDFVersion.V1_2;
          case 0x33:
            return PDFVersion.V1_3;
          case 0x34:
            return PDFVersion.V1_4;
          case 0x35:
            return PDFVersion.V1_5;
          case 0x36:
            return PDFVersion.V1_6;
          case 0x37:
            return PDFVersion.V1_7;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
      else
      {
        switch (minorVersion)
        {
          case 0x30:
            return PDFVersion.V2_0;
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
    private long GetDistanceToNextObject(int objectIndex, long rootByteOffset, PDFFile file)
    {
      long minPositiveDiff = long.MaxValue;
      int index = objectIndex;
      long diff;
      for (int i = 0; i < file.CrossReferenceEntries.Count; i++)
      {
        diff = file.CrossReferenceEntries[i].TenDigitValue - rootByteOffset;
        if (diff > 0 && diff < minPositiveDiff)
        {
          minPositiveDiff = diff;
          index = i;
        }
      }
      // means that this is last object so next object is assumed to be cross reference table
      if (index == objectIndex)
        return file.LastCrossReferenceOffset - rootByteOffset;
      return minPositiveDiff;
    }
  }
}