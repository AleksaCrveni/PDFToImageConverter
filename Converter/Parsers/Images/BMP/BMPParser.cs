using Converter.FileStructures.BMP;
using Converter.Utils;
using System.Text;

namespace Converter.Parsers.Images.BMP
{
  public class BMPParser
  {
    // TODO: idk if we can just read all in one bufferwhat if image is too big?, do we even care for that scenario?
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
      ParseColorTable(file);
      ParseRasterData(file);
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
      switch (file.DIBHeaderType)
      {
        case BMP_DIB_HEADER_TYPE.BITMAPCOREHEADER : 
          ParseDIBCoreHeader(file);
          break;
        case BMP_DIB_HEADER_TYPE.OS22XBITMAPHEADER16B : 
          ParseDIBOS22Header16B(file);
          break;
        case BMP_DIB_HEADER_TYPE.BITMAPINFOHEADER : 
          ParseDIBInfoHeader(file, ref buffer);
          break;
        case BMP_DIB_HEADER_TYPE.BITMAPV2INFOHEADER : 
          ParseDIBV2Header(file);
          break;
        case BMP_DIB_HEADER_TYPE.BITMAPV3INFOHEADER : 
          ParseDIBV3Header(file);
          break;
        case BMP_DIB_HEADER_TYPE.OS22XBITMAPHEADER64B : 
          ParseDIBOS22Header64B(file);
          break;
        case BMP_DIB_HEADER_TYPE.BITMAPV4INFOHEADER : 
          ParseDIBV4Header(file);
          break;
        case BMP_DIB_HEADER_TYPE.BITMAPV5INFOHEADER:
          ParseDIBV5Header(file);
          break;
        default:
          break;
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
      BMP_DIBHeader h = file.DIBHeader;
      h.Width = BufferReader.ReadInt32LE(ref buffer, ref file.Pos);
      h.Height = BufferReader.ReadInt32LE(ref buffer, ref file.Pos);
      h.NumOfColorPlanes = BufferReader.ReadUInt16LE(ref buffer, ref file.Pos);
      h.BitsPerPixel = BufferReader.ReadUInt16LE(ref buffer, ref file.Pos);
      uint compVal = BufferReader.ReadUInt32LE(ref buffer, ref file.Pos);
      // support 0, 1, 2
      BMP_COMPRESSION compression = (BMP_COMPRESSION)compVal;
      if (compVal != 0 && compression == BMP_COMPRESSION.BI_RGB
        && (compVal == 0 || compVal == 1 || compVal == 2))
        throw new InvalidDataException("Invalid Compression Method");
      h.Compression = compression;
      h.ImageSize = BufferReader.ReadUInt32LE(ref buffer, ref file.Pos);
      h.XRes = BufferReader.ReadInt32LE(ref buffer, ref file.Pos);
      h.YRes = BufferReader.ReadInt32LE(ref buffer, ref file.Pos);
      h.NumOfColors = BufferReader.ReadUInt32LE(ref buffer, ref file.Pos);
      h.NumOfImportantColors = BufferReader.ReadUInt32LE(ref buffer, ref file.Pos);
      // no support for extra masks

    }
    public void ParseDIBV2Header(BMPFile file)
    {
      throw new NotImplementedException();
    }
    public void ParseDIBV3Header(BMPFile file)
    {
      throw new NotImplementedException();
    }
    public void ParseDIBOS22Header64B(BMPFile file)
    {
      throw new NotImplementedException();
    }
    public void ParseDIBV4Header(BMPFile file)
    {
      throw new NotImplementedException();
    }

    public void ParseDIBV5Header(BMPFile file)
    {
      throw new NotImplementedException();
    }

    public void ParseColorTable(BMPFile file)
    {
      if (file.DIBHeader.NumOfColors == 0)
        return;

      byte[] buffer = new byte[file.DIBHeader.NumOfColors * 4];
      int bytesRead = file.Stream.Read(buffer, 0, buffer.Length);
      file.Pos = 0;

      if (bytesRead != buffer.Length)
        throw new InvalidDataException("Invalid ColorTable data!");
      file.DIBHeader.ColorTable = buffer;
    }

    public void ParseRasterData(BMPFile file)
    {
      byte[] buffer = new byte[file.DIBHeader.ImageSize];
      file.Stream.Position = file.Header.ImageDataOffset;
      int bytesRead = file.Stream.Read(buffer, 0, buffer.Length);
      file.Pos = 0;

      if (bytesRead != buffer.Length)
        throw new InvalidDataException("Invalid Raster Data!");
      file.RasterData = buffer;
    }
  }
}
