namespace Converter.FileStructures.TIFF
{
  public class TIFFFile
  {

    public TIFF_Header Header = new TIFF_Header();
    public Stream Stream { get; set; }
    // this isn't global since there are more lists, but for now we just parse 1 so keep
    public List<TIFF_Data> TIFFs { get; set; } = new List<TIFF_Data>();
    public int TotalPages = -1; // -1 in case its not available
  }

  public struct TIFF_Data
  {
    public TIFF_Tag Tag;
    public byte[] FullImageData;
  };

  public struct TIFF_Header
  {
    // if false its Big Endian
    public bool IsLittleEndian;
    public int FirstIFDByteOffset;
  }

  // 12 B
  public struct TIFF_IFD
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
  public struct TIFF_Tag
  {
    public TIFF_Tag() { }
    public TIFF_PhotometricInterpretation? PhotometricInterpretation;
    public TIFF_Compression? Compression = TIFF.TIFF_Compression.NoCompression;
    public ushort? BitsPerSample;
    public uint ImageLength; // required
    public uint ImageWidth; // required
    public TIFF_ResolutionUnit ResolutionUnit = TIFF_ResolutionUnit.Inch;
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
}
