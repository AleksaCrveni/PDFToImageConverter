namespace Converter.FIleStructures
{
  public class TIFFFile
  {

    public TIFFHeader Header = new TIFFHeader();
    public Stream Stream { get; set; }
    // this isn't global since there are more lists, but for now we just parse 1 so keep
    public List<Tag> Tags { get; set; } = new List<Tag>();
    public int TotalPages = -1; // -1 in case its not available
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
    public ushort Tag;
    public ushort Type;
    public uint Count;
    public uint ValueOrOffset;
  }

  // Maybe in future have one big struct
  // there are ~90 tags?
  // 32 * 90
  // just do it for now and fix if any issues
  // there is a problem with default values because 0 is taken
  // so I am just going to make field nullable and check for that.
  public struct Tag
  {
    public Tag() { }
    public PhotometricInterpretation? PhotometricInterpretation;
    public Compression? Compression = FIleStructures.Compression.NoCompression;
    public ushort? BitsPerSample;
    public uint? ImageLength;
    public uint? ImageWidth;
    public ResolutionUnit ResolutionUnit = ResolutionUnit.Inch;
    // in actual data this is represented as 2 uints, 1 for numerator and one for denominator
    // but we will process it directly in double
    public double? XResolution;
    // -||-
    public double? YResolution;
    // StripsPerImage = floor ((ImageLength + RowsPerStrip - 1) / RowsPerStrip - use for
    public uint? RowsPerStrip;
    public uint? StripOffsets;
    // required
    public uint? StripByteCounts;
    public uint NewSubfileType = 0;
    public ushort FillOrder = 1;
    public ushort Orientation = 1;
    public ushort SamplesPerPixel = 1;
    public ushort PlanarConfiguration = 1;
    public ushort PageNumber;
  }

  public enum PhotometricInterpretation
  {
    WhiteIsZero,
    BlackIsZero,
    RGB,
    Pallete,
    TransparencyMask
  }
  public enum Compression
  {
    NoCompression = 1,
    CCITT = 2,
    PackBits = 32773
  }

  public enum ResolutionUnit
  {
    NoAbsoluteUnitOfMe = 1,
    Inch = 2,
    Centimeter = 3
  }

}
