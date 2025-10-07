namespace Converter.FileStructures.TTF
{
  public enum TTF_ScalarType
  {
    Null = 0,
    True, // recognized by OS X and iOS as referring to TrueType fonts
    Typ1, // recognized as referring to the old style of PostScript font housed in a sfnt wrapper
    Otto, // indicates an OpenType font with PostScript outlines (that is, a 'CFF ' table instead of a 'glyf' table)
  }

  public enum TTF_PlatformID : ushort
  {
    Unicode = 0,
    Macintosh = 1,
    DO_NOT_USE = 2,
    Microsoft = 3
  }

  public enum TTF_MSPlatformSpecificID : ushort
  {
    MS_Symbol = 0,
    MS_UnicodeBMP = 1,
    MS_ShiftJIS = 2,
    MS_UnicodeFULL = 10
  }

  // Glyph shapes
  public enum TTF_VMove : byte
  {
    VMOVE = 1,
    VLINE,
    VCURVE,
    VCUBIC
  }

  public enum TTF_RASTERIZER_VERSION
  {
    V1,
    V2
  }
}
