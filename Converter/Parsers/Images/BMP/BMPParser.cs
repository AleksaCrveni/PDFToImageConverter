using Converter.FileStructures.BMP;
using System.Buffers.Binary;
using System.Text;
using Converter.Utils;

namespace Converter.Parsers.Images.BMP
{
  public class BMPParser
  {
    public BMPParser()
    {
      
    }

    public void Parse(string filepath)
    {
      Stream stream = File.OpenRead(filepath);
      BMPFile file = new BMPFile();
      file.Stream = stream;
      ParseHeaderAndDIBSize(file);
      ParseDIBHeader(file);
    }

    public void ParseHeaderAndDIBSize(BMPFile file)
    {
      BMP_Header header = new BMP_Header();
      // 14 for header + 4 for DIBSize
      Span<byte> buff = new byte[18];
      int bytesRead = file.Stream.Read(buff);
      if (bytesRead != buff.Length)
        throw new InvalidDataException("Header too short!");

      header.Identifier = Encoding.ASCII.GetString(buff.Slice(file.Pos, 2));
      header.FileSize = BufferReader.ReadUInt32LE(ref buff, ref file.Pos);
      header.Res1 = BufferReader.ReadUInt16LE(ref buff, ref file.Pos);
      header.Res2 = BufferReader.ReadUInt16LE(ref buff, ref file.Pos);
      header.ImageDataOffset = BufferReader.ReadUInt32LE(ref buff, ref file.Pos);
      file.Header = header;

      // Parse DIB Type 
      uint DIBSize = BufferReader.ReadUInt32LE(ref buff, ref file.Pos);

      file.DIBHeaderSize = DIBSize;
      file.DIBHeaderType = (BMP_DIB_HEADER_TYPE)DIBSize;
      if (file.DIBHeaderType == BMP_DIB_HEADER_TYPE.NULL)
        throw new InvalidDataException($"Invalid DIB_HEADER_TYPE! Got {DIBSize}");
    }

    public void ParseDIBHeader(BMPFile file)
    {
      BMP_DIBHeader DIBHeader = new BMP_DIBHeader();
      file.DIBHeader = DIBHeader;
      if (file.DIBHeaderSize > 124)
        throw new InvalidDataException("DIBHeader size too BIG!");
      Span<byte> buffer = new byte[file.DIBHeaderSize];
      int readBytes = file.Stream.Read(buffer);
      if (readBytes != file.DIBHeaderSize)
        throw new InvalidDataException("Invalid DIBHeader size!");

      file.Pos = 0;
      _ = file.DIBHeaderType switch
      {
        BMP_DIB_HEADER_TYPE.BITMAPCOREHEADER => ParseDIBCoreHeader(file),
        BMP_DIB_HEADER_TYPE.OS22XBITMAPHEADER16B => ParseDIBOS22Header16B(file),
        BMP_DIB_HEADER_TYPE.BITMAPINFOHEADER => ParseDIBInfoHeader(file, ref buffer),
        BMP_DIB_HEADER_TYPE.BITMAPV2INFOHEADER => ParseDIBV2Header(file),
        BMP_DIB_HEADER_TYPE.BITMAPV3INFOHEADER => ParseDIBV3Header(file),
        BMP_DIB_HEADER_TYPE.OS22XBITMAPHEADER64B => ParseDIBOS22Header64B(file),
        BMP_DIB_HEADER_TYPE.BITMAPV4INFOHEADER => ParseDIBV4Header(file),
        BMP_DIB_HEADER_TYPE.BITMAPV5INFOHEADER => ParseDIBV5Header(file),
      };
    }

    public void ParseDIBCoreHeader(BMPFile file)
    {
      throw new NotImplementedException();
    }
    public void ParseDIBOS22Header16B(BMPFile file)
    {
      throw new NotImplementedException();
    }
    public void ParseDIBInfoHeader(BMPFile file, ref Span<byte> buffer)
    {

    }
    public void ParseDIBV2Header(BMPFile file)
    {

    }
    public void ParseDIBV3Header(BMPFile file)
    {

    }
    public void ParseDIBOS22Header64B(BMPFile file)
    {
      throw new NotImplementedException();
    }
    public void ParseDIBV4Header(BMPFile file)
    {

    }

    public void ParseDIBV5Header(BMPFile file)
    {

    }

  }
}
