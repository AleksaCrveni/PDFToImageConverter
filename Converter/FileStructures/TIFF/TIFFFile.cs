namespace Converter.FileStructures.TIFF
{
  public class TIFFFile
  {

    public TIFFHeader Header = new TIFFHeader();
    public Stream Stream { get; set; }
    // this isn't global since there are more lists, but for now we just parse 1 so keep
    public List<TIFFData> TIFFs { get; set; } = new List<TIFFData>();
    public int TotalPages = -1; // -1 in case its not available
  }

  public struct TIFFData
  {
    public Tag Tag;
    public byte[] FullImageData;
  };
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
    public Compression? Compression = TIFF.Compression.NoCompression;
    public ushort? BitsPerSample;
    public uint ImageLength; // required
    public uint ImageWidth; // required
    public ResolutionUnit ResolutionUnit = ResolutionUnit.Inch;
    // in actual data this is represented as 2 uints, 1 for numerator and one for denominator
    // but we will process it directly in double
    public double? XResolution;
    // -||-
    public double? YResolution;
    // StripsPerImage = floor ((ImageLength + RowsPerStrip - 1) / RowsPerStrip - use for
    public uint RowsPerStrip;
    public uint? StripOffsetsPointer;
    // required
    public uint? StripByteCountsPointer;
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

  public enum TagType : ushort
  {
    NewSubfileType = 254,
    ImageWidth = 256,
    ImageLength = 257,
    BitsPerSample = 258,
    Compression = 259,
    PhotometricInterpretation = 262,
    FillOrder = 266,
    StripOffsetsPointer = 273,
    Orientation = 274,
    SamplesPerPixel = 277,
    RowsPerStrip = 278,
    StripByteCountsPointer = 279,
    XResolution = 282,
    YResolution = 283,
    ResolutionUnit = 296,
    PageNumber = 297,
    ColorMap = 320
  }

  public enum TagSize : ushort
  {
    BYTE = 1,
    ASCII = 2,
    SHORT = 3,
    LONG = 4,
    RATIONAL = 5,
    SBYTE = 6,
    UNDEFINED = 7,
    SSHORT = 8, 
    SLONG = 9,
    SRATIONAL = 10, 
    FLOAT = 11, 
    DOUBLE = 12
  }
}
