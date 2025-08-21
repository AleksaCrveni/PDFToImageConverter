namespace Converter.FIleStructures
{
  public class TIFFFile
  {

    public TIFFHeader Header = new TIFFHeader();
    public Stream Stream { get; set; }
    // this isn't global since there are more lists, but for now we just parse 1 so keep
    public Tags Tags { get; set; }
  }

  public struct TIFFHeader
  {
    // if false its Big Endian
    public bool IsLittleEndian;
    public int FirstIFDByteOffset;
  }

  // 12 B
  public struct IFD
  {
    public short Tag;
    public short Type;
    public int Count;
    public int ValueOrOffset;
  }

  // Maybe in future have one big struct
  // there are ~90 tags?
  // 32 * 90
  // just do it for now and fix if any issues
  public struct Tags
  {
    public short PhotometricInterpretation;
    public short Compression;
    public int ImageLength;
    public int ImageWidth;
    public short ResolutionUnit;
    // in actual data this is represented as 2 ints, 1 for numerator and one for denominator
    // but we will process it directly in double
    public double XResolution;
    // -||-
    public double YResolution;
    public int RowsPerStrip;
    public int StripOffsets;
    public int StripByteCounts;
    public int NewSubfileType;
    public short BitsPerSample;
  }
}
