namespace Converter.FileStructures.BMP
{
  public class BMPFile
  {
    public Stream Stream { get; set; }
    public int Pos;
    public BMP_Header Header;
    public DIB_HEADER_TYPE DIBHeaderType;
    public BMP_DIBHeader DIBHeader;
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
    
  }
}
