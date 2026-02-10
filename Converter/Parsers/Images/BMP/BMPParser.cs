using Converter.FileStructures.BMP;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Text;

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
      file.Pos += 2;

      header.FileSize = BinaryPrimitives.ReadUInt32LittleEndian(buff.Slice(file.Pos, 4));
      file.Pos += 4;

      header.Res1 = BinaryPrimitives.ReadUInt16LittleEndian(buff.Slice(file.Pos, 2));
      file.Pos += 2;

      header.Res1 = BinaryPrimitives.ReadUInt16LittleEndian(buff.Slice(file.Pos, 2));
      file.Pos += 2;

      header.ImageDataOffset = BinaryPrimitives.ReadUInt32LittleEndian(buff.Slice(file.Pos, 4));
      file.Pos += 4;

      file.Header = header;

      // Parse DIB Type 
      uint DIBSize = BinaryPrimitives.ReadUInt32LittleEndian(buff.Slice(file.Pos, 4));
      file.Pos += 4;

      file.DIBHeaderType = (BMP_DIB_HEADER_TYPE)DIBSize;
      if (file.DIBHeaderType == BMP_DIB_HEADER_TYPE.NULL)
        throw new InvalidDataException($"Invalid DIB_HEADER_TYPE! Got {DIBSize}");
    }

    public void ParseDIBHeader(BMPFile file)
    {
    }
  }
}
