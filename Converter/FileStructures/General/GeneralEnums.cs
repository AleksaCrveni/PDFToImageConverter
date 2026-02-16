namespace Converter.FileStructures.General
{
  public enum TargetConversion
  {
    TIFF_BILEVEL,
    TIFF_GRAYSCALE,
    TIFF_PALLETE,
    TIFF_RGB
  }

  public enum SourceConversion
  {
    PDF
  }

  public enum ENCODING_FILTER
  {
    Null = 0,
    ASCIIHexDecode,
    ASCII85Decode,
    LZWDecode,
    FlateDecode,
    RunLengthDecode,
    CCITTFaxDecode,
    JBIG2Decode,
    DCTDecode,
    JPXDecode,
    Crypt
  }
}
