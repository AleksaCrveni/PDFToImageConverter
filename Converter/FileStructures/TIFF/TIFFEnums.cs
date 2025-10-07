namespace Converter.FileStructures.TIFF
{
  public enum TIFF_PhotometricInterpretation
  {
    WhiteIsZero,
    BlackIsZero,
    RGB,
    Pallete,
    TransparencyMask
  }
  public enum TIFF_Compression
  {
    NoCompression = 1,
    CCITT = 2,
    PackBits = 32773
  }

  public enum TIFF_ResolutionUnit
  {
    NoAbsoluteUnitOfMe = 1,
    Inch = 2,
    Centimeter = 3
  }

  public enum TIFF_TagType : ushort
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

  public enum TIFF_TagSize : ushort
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

  public enum TIFF_ImgDataMode
  {
    EMPTY,
    RANDOM,
    BUFFER_SUPPLIED
  }
}
