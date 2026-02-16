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

  public enum PDF_XrefEntryType : byte
  {
    FREE, // f
    NORMAL, // n
    COMPRESSED // type 3 in cross reference streams 
  }

  // Table 122
  // FontFile -> One
  // FontFile2 -> Two
  // FontFile3 -> Three
  public enum PDF_FontFileType
  {
    NULL,
    One,
    Two,
    Three
  }

  public enum PDF_Tabs
  {
    Null = 0,
    R,
    C,
    S
  }

  public enum PDF_FontEncodingType
  {
    Null = 0,
    StandardEncoding,
    MacRomanEncoding,
    MacExpertEncoding,
    WinAnsiEncoding,
    SymbolSetEncoding
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

  public enum PDF_FontEncodingSource
  {
    ENCODING,
    CMAP
  }

  public enum PDF_ProcedureSet
  {
    NULL,
    PDF,
    TEXT,
    IMAGEB,
    IMAGEC,
    IMAGEL
  }

}
