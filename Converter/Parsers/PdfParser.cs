﻿
using System.Globalization;
using System.IO;
using System.Text;

namespace Converter.Parsers
{
  // Note to myself - when dealing with variables that are indirect references add 'IR' on the end of the name
  // TODO: Am I stupid or i can just compare characters directly instead of bytes........................
  public class PdfParser
  {
    StringBuilder _sb;
    private readonly byte _newLineByte = 10;
    private readonly string _trailerConst = "trailer";

    public PdfParser()
    {
      _sb = new StringBuilder();
    }

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
    }
    private void ParseRootPageTree(PDFFile file)
    {
      int objectIndex = file.Catalog.PagesIR.Item1;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      long objectLength = GetDistanceToNextObject(objectIndex, objectByteOffset, file);
      // make linked list or just flatten references????
      // ok for now just load all and store it, later be smarter
      PageTree rootPageTree = new PageTree();
      if (objectLength <= 8192*2)
      {
        file.Stream.Position = objectByteOffset;
        Span<byte> pageBuffer = stackalloc byte[(int)objectLength];
        SpanParseHelper helper = new SpanParseHelper(ref pageBuffer);
        int readBytes = file.Stream.Read(pageBuffer);
        // do i need this...?
        if (pageBuffer.Length != readBytes)
          throw new InvalidDataException("Invalid data");
        FillRootPageTreeFromSpan(file, ref helper, ref rootPageTree);
      }
      else
      {
        FillRootPageTreeFromStream(file.Stream, objectByteOffset, objectLength, ref rootPageTree);
      }
      
    }
    private void FillRootPageTreeFromSpan(PDFFile file, ref SpanParseHelper helper, ref PageTree root)
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
      // testing, idk if this is good idea, i maybe able to do something more with it later
      // When you jump to object it starts at 3 so you have to skip first and second int and 'obj'
      // This is strict if i want to make it less strict use MatchNextString which will skip all until string is matched
      if (!helper.ExpectNextUTF8("<<", 3))
        throw new InvalidDataException("Invalid Root Page Tree Info, expected start of dict");
      string tokenString = helper.GetNextString();
      while (tokenString != "" && tokenString != ">>")
      {
        object _ = tokenString switch
        {
          // Have some generic enum parser
          "/Parent" => pageTree.ParentIR = helper.GetNextIndirectReference(),
          "/Count" => pageTree.Count = helper.GetNextInt32Strict(),
          "/MediaBox" => pageTree.MediaBox = helper.GetNextRectangle(),
          "/Kids" => pageTree.KidsIRs = helper.GetNextIndirectReferenceList(),
          _ => ""
        };
        tokenString = helper.GetNextString();
      }

      if (tokenString == "")
        throw new InvalidDataException("Invalid dictionary");
    }
    // THIS FILLS UP BOTH PAGEINFO AND PAGE TREE INFO
    private void FillAllPageTreeAndInformation((int, int) kidPositionIR, List<PageTree> pageTrees, List<PageInfo> pages, PDFFile file)
    {
      long objectByteOffset = file.CrossReferenceEntries[kidPositionIR.Item1].TenDigitValue;
      long objectLength = GetDistanceToNextObject(kidPositionIR.Item1, objectByteOffset, file);
      
      if (objectLength > 8192 * 2)
      {
        // TODO: Do better span or heap alloct diff
        throw new NotImplementedException(); 
      }

      file.Stream.Position = objectByteOffset;
      Span<byte> pageBuffer = stackalloc byte[(int)objectLength];
      SpanParseHelper helper = new SpanParseHelper(ref pageBuffer);
      int readBytes = file.Stream.Read(pageBuffer);
      // do i need this...?
      if (pageBuffer.Length != readBytes)
        throw new InvalidDataException("Invalid data");
      if (!helper.ExpectNextUTF8("<<", 3))
        throw new InvalidDataException("Invalid Root Page Tree Info, expected start of dict");

      if (!helper.ExpectNextUTF8("/Type"))
        throw new InvalidDataException("Invalid Root Page Tree Info, expected start of dict");

      string tokenString = helper.GetNextString();
      if (tokenString == "/Pages")
      {
        PageTree pageTree = new PageTree();
        while (tokenString != "" && tokenString != ">>")
        {
          object _ = tokenString switch
          {
            // Have some generic enum parser
            "/Parent" => pageTree.ParentIR = helper.GetNextIndirectReference(),
            "/Count" => pageTree.Count = helper.GetNextInt32Strict(),
            "/MediaBox" => pageTree.MediaBox = helper.GetNextRectangle(),
            "/Kids" => pageTree.KidsIRs = helper.GetNextIndirectReferenceList(),
            _ => ""
          };
          tokenString = helper.GetNextString();
        }
        pageTrees.Add(pageTree);
        for (int i = 0; i < pageTree.KidsIRs.Count; i++)
        {
          FillAllPageTreeAndInformation(pageTree.KidsIRs[i], pageTrees, pages, file);
        }
      }
      else if (tokenString == "/Page")
      {
        PageInfo pageInfo = new PageInfo();
        while (tokenString != "" && tokenString != ">>")
        {
          object _ = tokenString switch
          {
            // Have some generic enum parser
            "/Parent" => pageInfo.ParentIR = helper.GetNextIndirectReference(),
            "/LastModified" => pageInfo.LastModified = DateTime.UtcNow, // TODO: Fix this when you know how date format looks like!
            "/Resources" => pageInfo.ResourcesIR = helper.GetNextIndirectReference(),
            "/MediaBox" => pageInfo.MediaBox = helper.GetNextRectangle(),
            "/CropBox" => pageInfo.CropBox = helper.GetNextRectangle(),
            "/BleedBox" => pageInfo.BleedBox = helper.GetNextRectangle(),
            "/TrimBox" => pageInfo.TrimBox = helper.GetNextRectangle(),
            "/ArtBox" => pageInfo.ArtBox = helper.GetNextRectangle(),
            "/BoxColorInfo" => pageInfo.BoxColorInfo = helper.GetNextDict(),
            "/Contents" => pageInfo.ContentsIR = helper.GetNextIndirectReference(),
            "/Rotate" => pageInfo.Rotate = helper.GetNextInt32Strict(),
            "/Group" => pageInfo.Group = helper.GetNextDict(),
            "/Thumb" => pageInfo.Thumb = helper.GetNextStream(),
            "/B" => pageInfo.B = helper.GetNextIndirectReferenceList(),
            "/Dur" => pageInfo.Dur = helper.GetNextDouble(),
            "/Trans" => pageInfo.Trans = helper.GetNextDict(),
            "/Annots" => pageInfo.Annots = helper.GetNextArrayStrict(),
            "/AA" => pageInfo.AA = helper.GetNextDict(),
            "/Metadata" => pageInfo.Metadata = helper.GetNextStream(),
            "/PieceInfo" => pageInfo.PieceInfo = helper.GetNextDict(),
            "/StructParents" => pageInfo.StructParents = helper.GetNextInt32Strict(),
            "/ID" => pageInfo.ID = helper.GetNextArrayStrict(),
            "/PZ" => pageInfo.PZ = helper.GetNextInt32Strict(),
            "/SeparationInfo" => pageInfo.SeparationInfo = helper.GetNextDict(),
            "/Tabs" => pageInfo.Tabs = helper.GetNextName<Tabs>(),
            "/TemplateInstantiated" => pageInfo.TemplateInstantiated = helper.GetNextString(),
            "/PresSteps" => pageInfo.PresSteps = helper.GetNextDict(),
            "/UserUnit" => pageInfo.UserUnit = helper.GetNextDouble(),
            "/VP" => pageInfo.VP = helper.GetNextDict(),
            _ => ""
          };
          tokenString = helper.GetNextString();
        }
        pages.Add(pageInfo);
      }
    }

    private void FillRootPageTreeFromStream(Stream stream, long startPosition, long length, ref PageTree root)
    {
      throw new NotImplementedException();
    }
    private void ParseCatalogDictionary(PDFFile file)
    {
      // Instead of reading one by one to see where end is, get next object position and have that as length to load
      // Experimental!
      int objectIndex = file.Trailer.RootIR.Item1;
      long objectByteOffset = file.CrossReferenceEntries[objectIndex].TenDigitValue;
      long objectLength = GetDistanceToNextObject(objectIndex, objectByteOffset, file);
      // if this is bigger 8192 or double that then do see to do some kind of different processing
      // I think that reading in bulk should be faster than reading 1 char by 1 from stream
      Catalog catalog = new Catalog();
      if (objectLength <= 8192)
      {
        file.Stream.Position = objectByteOffset;
        Span<byte> catalogBuffer = stackalloc byte[(int)objectLength];
        SpanParseHelper helper = new SpanParseHelper(ref catalogBuffer);
        int readBytes = file.Stream.Read(catalogBuffer);
        if (catalogBuffer.Length != readBytes)
          throw new InvalidDataException("Invalid data");
        FillCatalogFromSpan(ref helper, ref catalog);
        // we maybe read a bit more sicne we read last object diff so make sure we are in correct position
        
        file.Stream.Position = helper._position + 1;
      } 
      else
      {
        // heap alloc
        // NOT TESTED!!!!
        FillCatalogFromStream(file.Stream, objectByteOffset, objectLength, ref catalog);
      }

      // Starting from PDF 1.4 version can be in catalog and it has advantage over header one if its bigger
      if (catalog.Version > PDFVersion.INVALID)
        file.PdfVersion = catalog.Version;
      file.Catalog = catalog;
    }

    private void FillCatalogFromSpan(ref SpanParseHelper helper, ref Catalog catalog)
    {
 
      string tokenString = helper.GetNextString();
      int tokenInt = 0;
      while (tokenString != string.Empty)
      {
        // Maybe move this to dictionary parsing
        if (tokenString == "<<")
        {
          tokenString = helper.GetNextString();
          while (tokenString != "" && tokenString != ">>")
          {
            // TODO: Probably move this to normal switch or if statement because of string alloc in case key is not found
            object _ = tokenString switch
            {
              // Have some generic enum parser
              "/Version" => catalog.Version = ParsePdfVersionFromCatalog(ref helper),
              "/Extensions" => catalog.Extensions = helper.GetNextDict(),
              "/Pages" => catalog.PagesIR = helper.GetNextIndirectReference(),
              "/PageLabels" => catalog.PageLabels = helper.GetNextNumberTree(),
              "/Names" => catalog.Names = helper.GetNextDict(),
              "/Dests" => catalog.DestsIR = helper.GetNextIndirectReference(),
              "/ViewerPreferences" => catalog.ViewerPreferences = helper.GetNextDict(),
              "/PageLayout" => catalog.PageLayout = helper.GetNextName<PageLayout>(PageLayout.SinglePage),
              "/PageMode" => catalog.PageMode = helper.GetNextName<PageMode>(PageMode.UserNone),
              "/Outlines" => catalog.OutlinesIR = helper.GetNextIndirectReference(),
              "/Threads" => catalog.ThreadsIR = helper.GetNextIndirectReference(),
              // not correct, leave for now
              "/OpenAction" => catalog.OpenAction = helper.GetNextString(),
              "/AA" => catalog.AA = helper.GetNextDict(),
              "/URI" => catalog.URI = helper.GetNextDict(),
              "/AcroForm" => catalog.AcroForm = helper.GetNextDict(),
              "/MetaData" => catalog.MetadataIR = helper.GetNextIndirectReference(),
              "/StructTreeRoot" => catalog.StructTreeRoot = helper.GetNextDict(),
              "/MarkInfo" => catalog.MarkInfo = helper.GetNextDict(),
              "/Lang" => catalog.Lang = helper.GetNextString(),
              "/SpiderInfo" => catalog.SpiderInfo = helper.GetNextDict(),
              "/OutputIntents" => catalog.OutputIntents = helper.GetNextArrayStrict(),
              "/PieceInfo" => catalog.PieceInfo = helper.GetNextDict(),
              "/OCProperties" => catalog.OCProperties = helper.GetNextDict(),
              "/Perms" => catalog.Perms = helper.GetNextDict(),
              "/Legal" => catalog.Legal = helper.GetNextDict(),
              "/Requirements" => catalog.Requirements = helper.GetNextArrayStrict(),
              "/Collection" => catalog.Collection = helper.GetNextDict(),
              "/NeedsRendering" => catalog.NeedsRendering = helper.GetNextString() == "true",
              _ => ""
            };
            tokenString = helper.GetNextString();
          }
          // Means that dict is corrupt
          if (tokenString == "")
            throw new InvalidDataException("Invalid dictionary");
          break;
        }
        else
        {
          tokenString = helper.GetNextString();
        }
      }
    }
    private void FillCatalogFromStream(Stream stream, long startPosition, long length, ref Catalog catalog)
    {
      throw new NotImplementedException();
    }

    // TODO: This will work only for one section, not subsections.Fix it later!
    private void ParseCrossReferenceTable(PDFFile file)
    {
      // TODO: I guess byteoffset should be long
      // i really feel i should be loading in more bytes in chunk
      file.Stream.Position = (long)file.LastCrossReferenceOffset;

      // make sure talbe is starting with xref keyword
      Span<byte> xrefBufferChar = stackalloc byte[4] { (byte)'x', (byte)'r', (byte)'e', (byte)'f' };
      Span<byte> xrefBuffer = stackalloc byte[4];
      int readBytes = file.Stream.Read(xrefBuffer);
      if (xrefBuffer.Length != readBytes)
        throw new InvalidDataException("Invalid data");
      if (!AreSpansEqual(xrefBuffer, xrefBufferChar, 4))
        throw new InvalidDataException("Invalid data");

      Span<byte> nextLineBuffer = StreamParseHelper.GetNextLine(file.Stream).AsSpan();
      SpanParseHelper parseHelper = new SpanParseHelper(ref nextLineBuffer);
      int startObj = parseHelper.GetNextInt32Strict();
      int endObj = parseHelper.GetNextInt32Strict();
      int sectionLen = endObj - startObj;

      // TODO: Think if you want list or array and fix this later
      CRefEntry[] entryArr = new CRefEntry[sectionLen];
      Array.Fill<CRefEntry>(entryArr, new CRefEntry());
      List<CRefEntry> cRefEntries = entryArr.ToList();
      Span<byte> cRefEntryBuffer = stackalloc byte[20];
      readBytes = 0;
      CRefEntry entry;
      for (int i = (int)startObj; i < endObj; i++)
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
      string tokenString = helper.GetNextString();
      int tokenInt = 0;
      while (tokenString != string.Empty)
      {
        if (tokenString == _trailerConst)
        {
          tokenString = helper.GetNextString();

          // Maybe move this to dictionary parsing
          if (tokenString == "<<")
          {
            tokenString = helper.GetNextString();
            while (tokenString != "" && tokenString != ">>")
            {
              // TODO: Probably move this to normal switch or if statement because of string alloc in case key is not found
              object _ = tokenString switch
              {
                "/Size" => trailer.Size = helper.GetNextInt32Strict(),
                "/Root" => trailer.RootIR = helper.GetNextIndirectReference(),
                "/Info" => trailer.InfoIR = helper.GetNextIndirectReference(),
                "/Encrypt" => trailer.EncryptIR = helper.GetNextIndirectReference(),
                "/Prev" => trailer.Prev = helper.GetNextInt32Strict(),
                "/ID" => trailer.ID = helper.GetNextArrayKnownLengthStrict(2),
                _ => ""
              };
              tokenString = helper.GetNextString();
            }
            // Means that dict is corrupt
            if (tokenString == "")
              throw new InvalidDataException("Invalid dictionary");
            break;
          }
        }
        else
        {
          tokenString = helper.GetNextString();
        }
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
      if (buffer[0] != (byte)0x25)
        throw new InvalidDataException("Invalid data");
      if (buffer[1] != (byte)0x50)
        throw new InvalidDataException("Invalid data");
      if (buffer[2] != (byte)0x44)
        throw new InvalidDataException("Invalid data");
      if (buffer[3] != (byte)0x46)
        throw new InvalidDataException("Invalid data");
      if (buffer[4] != (byte)0x2d)
        throw new InvalidDataException("Invalid data");
      // checking version
      byte majorVersion = buffer[5];
      if (majorVersion != (byte)0x31 && majorVersion != (byte)0x32)
        throw new InvalidDataException("Invalid data");
      if (buffer[6] != (byte)0x2e)
        throw new InvalidDataException("Invalid data");
      byte minorVersion = buffer[7];
      if (majorVersion == (byte)0x31)
      {
        switch (minorVersion)
        {
          case (byte)0x30:
            return PDFVersion.V1_0;
            break;
          case (byte)0x31:
            return PDFVersion.V1_1;
            break;
          case (byte)0x32:
            return PDFVersion.V1_2;
            break;
          case (byte)0x33:
            return PDFVersion.V1_3;
            break;
          case (byte)0x34:
            return PDFVersion.V1_4;
            break;
          case (byte)0x35:
            return PDFVersion.V1_5;
            break;
          case (byte)0x36:
            return PDFVersion.V1_6;
            break;
          case (byte)0x37:
            return PDFVersion.V1_7;
            break;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
      else
      {
        switch (minorVersion)
        {
          case (byte)0x30:
            return PDFVersion.V2_0;
            break;
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
      if (majorVersion != (byte)0x31 && majorVersion != (byte)0x32)
        throw new InvalidDataException("Invalid data");
      if (buffer[2] != (byte)0x2e)
        throw new InvalidDataException("Invalid data");
      byte minorVersion = buffer[3];
      if (majorVersion == (byte)0x31)
      {
        switch (minorVersion)
        {
          case (byte)0x30:
            return PDFVersion.V1_0;
          case (byte)0x31:
            return PDFVersion.V1_1;
          case (byte)0x32:
            return PDFVersion.V1_2;
          case (byte)0x33:
            return PDFVersion.V1_3;
          case (byte)0x34:
            return PDFVersion.V1_4;
          case (byte)0x35:
            return PDFVersion.V1_5;
          case (byte)0x36:
            return PDFVersion.V1_6;
          case (byte)0x37:
            return PDFVersion.V1_7;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
      else
      {
        switch (minorVersion)
        {
          case (byte)0x30:
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
        value = value * 10 + (long)CharUnicodeInfo.GetDecimalDigitValue((char)buffer[index]);
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
    private UInt16 ConvertBytesToUnsignedInt16(Span<byte> buffer)
    {
      uint res = 0;
      for (int i = 0; i < buffer.Length; i++)
      {
        // these should be no negative ints so this is okay i believe?
        res = res * 10 + (uint)CharUnicodeInfo.GetDecimalDigitValue((char)buffer[i]);
      }
      // this should wrap, idk if i should throw exception
      return (UInt16)res;
    }

    private UInt64 ConvertBytesToUnsignedInt64(Span<byte> buffer)
    {
      uint res = 0;
      for (int i = 0; i < buffer.Length; i++)
      {
        // these should be no negative ints so this is okay i believe?
        res = res * 10 + (uint)CharUnicodeInfo.GetDecimalDigitValue((char)buffer[i]);
      }
      // this should wrap, idk if i should throw exception
      return (UInt64)res;
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