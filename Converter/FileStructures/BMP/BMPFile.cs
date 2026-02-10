namespace Converter.FileStructures.BMP
{
  public class BMPFile
  {
    public Stream Stream { get; set; }
    public int Pos; // Relative position, not absolute
    public BMP_Header Header;
    public BMP_DIB_HEADER_TYPE DIBHeaderType;
    public uint DIBHeaderSize;
    public BMP_DIBHeader DIBHeader;
    public byte[] RasterData;
  }

  public class BMP_Header()
  {
    public string Identifier;
    public uint FileSize;
    public ushort Res1;
    public ushort Res2;
    public uint ImageDataOffset;
  }

  public class BMP_DIBHeader()
  {
    public int Width;
    public int Height;
    public ushort NumOfColorPlanes; // should be 1
    public ushort BitsPerPixel;
    public BMP_COMPRESSION Compression;
    public uint ImageSize; // can be 0 for BI_RGB 
    public int XRes; // Horizontal resolution Pixel per metre
    public int YRes; // Vertical resolution Pixel per metre
    public uint NumOfColors; // Or 0 to default 2^n
    public uint NumOfImportantColors; // 0 if all colors are important, generally ignored
    public byte[]? ColorTable;
  }
}
