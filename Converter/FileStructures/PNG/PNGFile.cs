namespace Converter.FileStructures.PNG
{
  public class PNGFile
  {
    public int Width;
    public int Height;
    public byte BitDepth;
    public byte SampleDepth;
    public PNG_COLOR_TYPE ColorType;
    public PNG_COLOR_SCHEME ColorSheme;
    public PNG_COMPRESSION Compression;
    public PNG_INTERLANCE Interlance;
    public byte[] RawIDAT;
    public PNG_TRNS TransparencyData;
  }

  public class Chunk
  {
    public uint Length;
    public PNG_CHUNK_TYPE Type;
    public byte[] Data;
    public uint CRC;
  }
  
  public class PNG_TRNS
  {
    public int GreySample = -1;
    public (long R, long B, long G) RGBSamples = (-1, -1, -1);
    public byte[] PalleteSamples;
  }
  

}
