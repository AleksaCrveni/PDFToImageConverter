namespace Converter.FileStructures.PDF
{
  public enum PDF_Version
  {
    Null = 0,
    V1_0,
    V1_1,
    V1_2,
    V1_3,
    V1_4,
    V1_5,
    V1_6,
    V1_7,
    V1_7_2008,
    V2_0,
    V2_0_2020
  }

  public enum PDF_PageLayout
  {
    SinglePage,
    OneColumn,
    TwoColumnLeft,
    TwoColumnRight,
    TwoPageLeft,
    TwoPageRight,
  }

  public enum PDF_PageMode
  {
    UserNone,
    UseOutlines,
    FullScreen,
    UseOC,
    UseAttachments
  }

  public enum PDF_Tabs
  {
    Null = 0,
    R,
    C,
    S
  }

  public enum PDF_EncodingInf
  {
    Custom,
    MacRomanEncoding,
    MacExpertEncoding,
    WinAnsiEncoding,
  }

  // Table 122
  public enum PDF_FontFileType
  {
    FontFile,
    FontFil2,
    FontFile3
  }

  // Table 123
  [Flags]
  public enum PDF_FontFlags : ushort
  {
    FixedPitch = 1,
    Serif = 2,
    Symbolic = 4,
    Script = 8,
    Nonsymbolic = 16,
    Italic = 32,
    AllCap = 64,
    SmallCap = 128,
    ForceBold = 256
  }

  public enum PDF_FontStretch
  {
    Null = 0,
    UltraCondensed,
    ExtraCondensed,
    Condensed,
    SemiCondensed,
    Normal,
    SemiExpanded,
    Expanded,
    ExtraExpanded,
    UltraExpanded
  }

  public enum PDF_Filter
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

  public enum PDF_FontType
  {
    Null = 0,
    Type0,
    Type1,
    MMType1,
    Type3,
    TrueType,
    CIDFontType0,
    CIDFontType2,
    OpenType,
  }

  public enum PDF_FontFileSubtype
  {
    Null = 0,
    Type1C,
    CIDFontType0C,
    OpenType
  }

  public enum PDF_ColorSpace
  {
    NULL = 0,
    // device based
    DeviceGray,
    DeviceRGB,
    DeviceCMYK,

    // CIE based
    CalGray,
    CalRGB,
    Lab,
    ICCBased,

    // special
    Indexed,
    Pattern,
    Separation,
    DeviceN
  }
}
