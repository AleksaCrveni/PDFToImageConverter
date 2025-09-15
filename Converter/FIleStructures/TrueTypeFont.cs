namespace Converter.FileStructures
{
  /// <summary>
  /// R -> required, O -> optional, SD -> needed for some OSes, Offsets -> OF
  /// </summary>
  public ref struct TrueTypeFont
  {
    public TrueTypeFont() { }

    public FontDirectory FontDirectory;
    public TableOffsets Offsets;
    public ushort NumOfGlyphs;
    public int Svg;
    public uint IndexMapOffset;
    public ushort IndexToLocFormat;
  }

  public ref struct TableOffsets
  {

    public TableOffsets() { }
    public ReadOnlySpan<byte> cmap = new ReadOnlySpan<byte>(); // R
    public ReadOnlySpan<byte> glyf = new ReadOnlySpan<byte>(); // R
    public ReadOnlySpan<byte> head = new ReadOnlySpan<byte>(); // R
    public ReadOnlySpan<byte> hhea = new ReadOnlySpan<byte>(); // R
    public ReadOnlySpan<byte> hmtx = new ReadOnlySpan<byte>(); // R
    public ReadOnlySpan<byte> loca = new ReadOnlySpan<byte>(); // R
    public ReadOnlySpan<byte> maxp = new ReadOnlySpan<byte>(); // R
    public ReadOnlySpan<byte> name = new ReadOnlySpan<byte>(); // R
    public ReadOnlySpan<byte> post = new ReadOnlySpan<byte>(); // R

    public ReadOnlySpan<byte> cvt  = new ReadOnlySpan<byte>(); // SD
    public ReadOnlySpan<byte> fpgm = new ReadOnlySpan<byte>(); // SD
    public ReadOnlySpan<byte> hdmx = new ReadOnlySpan<byte>(); // SD
    public ReadOnlySpan<byte> kern = new ReadOnlySpan<byte>(); // SD
    public ReadOnlySpan<byte> OS_2 = new ReadOnlySpan<byte>(); // SD
    public ReadOnlySpan<byte> prep = new ReadOnlySpan<byte>(); // SD
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
