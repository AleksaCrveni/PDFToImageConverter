using System.Globalization;
using System.Text;

namespace Converter.Parsers
{
  public class PdfParser
  {
    StringBuilder _sb;
    private readonly byte _newLineByte = 10;
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
    public ulong Parse(string filepath)
    {
      var stream = (Stream)File.OpenRead(filepath);
      // go to end to find byte offset to cross refernce table
      PDFVersion pdfVersion = PDFVersion.V1_0;
      ParsePdfVersionFromHeader(stream, ref pdfVersion);
      return GetCrossRefByteOffset(stream);
    }
    
    private void ParsePdfVersionFromHeader(Stream stream, ref PDFVersion version)
    {

      stream.Seek(0, SeekOrigin.Begin);

      Span<byte> buffer = stackalloc byte[8];
      int bytesRead = stream.Read(buffer);
      if (bytesRead != buffer.Length)
        throw new InvalidDataException("Invalid data");
      // checking for %PDF- in bytes
      if (buffer[0] != (byte)25)
        throw new InvalidDataException("Invalid data");
      if (buffer[1] != (byte)50)
        throw new InvalidDataException("Invalid data");
      if (buffer[2] != (byte)44)
        throw new InvalidDataException("Invalid data");
      if (buffer[3] != (byte)46)
        throw new InvalidDataException("Invalid data");
      if (buffer[4] != (byte)2d)
        throw new InvalidDataException("Invalid data");
      // checking version
      byte majorVersion = buffer[5]; 
      if (majorVersion != (byte)31 && majorVersion != (byte)32)
        throw new InvalidDataException("Invalid data");
      if (buffer[6] != CharUnicodeInfo.GetDecimalDigitValue('.'))
        throw new InvalidDataException("Invalid data");
      byte minorVersion = buffer[7];
      if (majorVersion == (byte)31)
      {
        switch (minorVersion)
        {
          case (byte)30:
            version = PDFVersion.V1_0;
            break;
          case (byte)31:
            version = PDFVersion.V1_1;
            break;
          case (byte)32:
            version = PDFVersion.V1_2;
            break;
          case (byte)33:
            version = PDFVersion.V1_3;
            break;
          case (byte)34:
            version = PDFVersion.V1_4;
            break;
          case (byte)35:
            version = PDFVersion.V1_5;
            break;
          case (byte)36:
            version = PDFVersion.V1_6;
            break;
          case (byte)37:
            version = PDFVersion.V1_7;
            break;
          default:
            throw new InvalidDataException("Invalid data");
        }
      }
      else
      {
        switch (minorVersion)
        {
          case (byte)30:
            version = PDFVersion.V2_0;
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
    private ulong GetCrossRefByteOffset(Stream stream)
    {
      bool found = false;
      sbyte index = 0;

      // probably irellevant but in case of big files , long value is 18446744073709551615 which is 20char +2 for \n on end and start
      Span<byte> buffer = stackalloc byte[20 + 1];

      // not sure if this is needed, but validate if %%EOF exists
      Span<byte> _eofBytes = stackalloc byte[6] { 37, 37, 69, 79, 70, 10 };
      Span<byte> _eofByteBuffer = stackalloc byte[6];
      stream.Seek(-6, SeekOrigin.End);
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
  enum PDFVersion
  {
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
}
