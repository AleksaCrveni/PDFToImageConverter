using Converter.FileStructures.TIFF;

namespace Converter.FileStructures
{
  /// <summary>
  /// R -> required, O -> optional, SD -> needed for some OSes, Offsets -> OF
  /// </summary>
  public struct TrueTypeFont
  {
    public TrueTypeFont() { }

    public FontDirectory FontDirectory;
    public TableOffsets Offsets;
    public ushort NumOfGlyphs;
    public int Svg;
    public uint IndexMapOffset;
    public ushort IndexToLocFormat;
  }

  public struct TableOffsets
  {

    public TableOffsets() { }
    public FakeSpan cmap = new FakeSpan(); // R
    public FakeSpan glyf = new FakeSpan(); // R
    public FakeSpan head = new FakeSpan(); // R
    public FakeSpan hhea = new FakeSpan(); // R
    public FakeSpan hmtx = new FakeSpan(); // R
    public FakeSpan loca = new FakeSpan(); // R
    public FakeSpan maxp = new FakeSpan(); // R
    public FakeSpan name = new FakeSpan(); // R
    public FakeSpan post = new FakeSpan(); // R

    public FakeSpan cvt  = new FakeSpan(); // SD
    public FakeSpan fpgm = new FakeSpan(); // SD
    public FakeSpan hdmx = new FakeSpan(); // SD
    public FakeSpan kern = new FakeSpan(); // SD
    public FakeSpan OS_2 = new FakeSpan(); // SD
    public FakeSpan prep = new FakeSpan(); // SD
  }


  public struct FontDirectory
  {
    // offset subtable
    public ScalarType ScalarType;
    public ushort NumTables;       // number of tables in the font
    public ushort SearchRange;     // (maximum power of 2 <= numTables)*16
    public ushort EntrySelector;   // log2(maximum power of 2 <= numTables)
    public ushort RangeShift;      // number of tables in the font

    // table directory  

  }

  public enum ScalarType
  {
    Null = 0,
    True, // recognized by OS X and iOS as referring to TrueType fonts
    Typ1, // recognized as referring to the old style of PostScript font housed in a sfnt wrapper
    Otto, // indicates an OpenType font with PostScript outlines (that is, a 'CFF ' table instead of a 'glyf' table)
  }

  public enum PlatformID : ushort
  {
    Unicode = 0,
    Macintosh = 1,
    DO_NOT_USE = 2,
    Microsoft = 3
  }

  public enum MSPlatformSpecificID : ushort
  {
    MS_Symbol = 0,
    MS_UnicodeBMP = 1,
    MS_ShiftJIS = 2,
    MS_UnicodeFULL = 10
  }


}
