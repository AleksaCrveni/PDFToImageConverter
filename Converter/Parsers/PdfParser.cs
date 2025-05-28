using System.Globalization;
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
      var stream = (Stream)File.OpenRead(filepath);
      // go to end to find byte offset to cross refernce table
      PDFFile file = new PDFFile();
      ReadInitialData(stream, file);
      return file;
    }

    // Read PDFVersion, Byte offset for last cross reference table, file trailer

    void ReadInitialData(Stream stream, PDFFile file)
    {

      file.PdfVersion = ParsePdfVersionFromHeader(stream);
      // Read last 1024 bytes and trailer, startxref, (last)cross reference tables bytes and %%EOF
      stream.Seek(-1024, SeekOrigin.End);
      Span<byte> footerBuffer = stackalloc byte[1024];
      int readBytes = stream.Read(footerBuffer);
      if (readBytes != footerBuffer.Length)
        throw new InvalidDataException("Invalid data");


      ParseHelper parseHelper = new ParseHelper(footerBuffer);
      file.Trailer = ParseTrailer(ref parseHelper);
      // TODO use parsehelper dont seek and copy again
      file.LastCrossReferenceOffset = ParseLastCrossRefByteOffset(stream);
    }
    // TODO: test case when trailer is not complete ">>" can't be found after opening brackets
    private Trailer ParseTrailer(ref ParseHelper helper)
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
      return trailer;
    }
    // TODO: account for this later
    // Comment from spec:
    // Beginning with PDF 1.4, the VErsion entry in the document's catalog dictionary (lcoated via the Root entry in the file's trailer
    // as described in 7.5.5, if present, shall be used instead of version specified in the Header
    // TODO: I really should have just compared and parsed the string instead of trying to act smart
    // because i will have to parse entire file and heapallock anyways
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
    // TODO: Fix EOF finding
    // Adobe 1.3 PDF spec
    // Acrobat viewers require only that the %%EOF marker appear somewhere within the last 1024 bytes of the file.
    // Adobe 1.7 PDF spec
    // The trailer of a PDF file enables a conforming reader to quickly find the cross-reference table and certain special objects.
    // Conforming readers should read a PDF file from its end. The last line of the file shall contain only the end-of-file marker, %%EOF
    // https://stackoverflow.com/questions/11896858/does-the-eof-in-a-pdf-have-to-appear-within-the-last-1024-bytes-of-the-file
    private ulong ParseLastCrossRefByteOffset(Stream stream)
    {
      stream.Seek(-6, SeekOrigin.End);
      bool found = false;
      sbyte index = 0;

      // probably irellevant but in case of big files , long value is 18446744073709551615 which is 20char +2 for \n on end and start
      Span<byte> buffer = stackalloc byte[20 + 1];

      // not sure if this is needed, but validate if %%EOF exists
      Span<byte> _eofBytes = stackalloc byte[6] { 37, 37, 69, 79, 70, 10 };
      Span<byte> _eofByteBuffer = stackalloc byte[6];
      int bytesRead = stream.Read(_eofByteBuffer);
      if (bytesRead != _eofByteBuffer.Length)
        throw new InvalidDataException("Invalid data");

      for (index = 0; index < bytesRead; index++)
      {
        if (_eofBytes[index] != _eofByteBuffer[index])
          throw new InvalidDataException("Invalid data");
      }

      // go to max possible position where cross reference offset byte count might start
      stream.Seek(-_eofByteBuffer.Length - buffer.Length, SeekOrigin.End);

      // try to read byte count offset for cross reference table
      bytesRead = stream.Read(buffer);
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
      ulong value = 0;
      // move newlineindex for one to get past \n to actual first digit
      for (index = ++newLineIndex; index < buffer.Length - 1; index++)
      {
        if (!char.IsDigit((char)buffer[index]))
          throw new InvalidDataException("Invalid data");
        value = value * 10 + (ulong)CharUnicodeInfo.GetDecimalDigitValue((char)buffer[index]);
      }
      return value;
    }


  }

  // extensions should be directly supported instead of normal versions for 1_7 and 2_0
  // so i think i dont need these enums since header in files is 2_0 OR 1_7
  public enum PDFVersion
  {
    INVALID,
    V1_0,
    V1_1,
    V1_2,
    V1_3,
    V1_4,
    V1_5,
    V1_6,
    V1_7,
    V1_7_2008,
    V2_0,
    V2_0_2020
  }
  
  // Spec reference on page 51
  // Table 15
  public struct Trailer
  {
    
    public int Size;
    public int Prev;
    public (int, int) RootIR;
    // not sure what it is, fix later
    public (int, int) EncryptIR;
    public (int, int) InfoIR;
    public string[] ID;
    // Only in hybrid-reference file
    // The byte offset in the decoded stream from the bgegging of the file of a cross reference stream
    public int XrefStm;
  }

}

