namespace Converter.FileStructures.PNG
{
  public class PNGFile
  {
    public int Width;
    public int Height;
    public byte BitDepth;
    public byte SampleDepth;
    public PNG_COLOR_TYPE ColorType;
    public PNG_COMPRESSION Compression;
    public PNG_FILTER Filter;
    public PNG_INTERLANCE Interlance;
  }

  public class Chunk
  {
    public uint Length;
    public PNG_CHUNK_TYPE Type;
    public byte[] Data;
    public uint CRC;
  }

  

}
