using Converter.FileStructures.General;

namespace Converter.FileStructures.TTF
{
  /// <summary>
  /// </summary>
  public struct TrueTypeFont
  {
    public TrueTypeFont() { }

    public FontDirectory FontDirectory;
    public TableOffsets Offsets;
    public ushort NumOfGlyphs;
    public int Svg;
    public int IndexMapOffset;
    public ushort IndexToLocFormat;
    public ushort CmapFormat;
    public bool Cff;
    public uint StartOffset;
  }

 public struct TTFVertex
  {
    public short x, y, cx, cy, cx1, cy1;
    public byte type, padding;
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

    /// R -> required, O -> optional, SD -> needed for some OSes, Offsets -> OF
    public FakeSpan cvt = new FakeSpan(); // SD
    public FakeSpan fpgm = new FakeSpan(); // SD
    public FakeSpan hdmx = new FakeSpan(); // SD
    public FakeSpan kern = new FakeSpan(); // SD
    public FakeSpan OS_2 = new FakeSpan(); // SD
    public FakeSpan prep = new FakeSpan(); // SD

    public FakeSpan gpos = new FakeSpan(); // O
  }

  public struct PointF
  {
    public float X;
    public float Y;
    public PointF(float x, float y)
    {
      X = x;
      Y = y;
    }
    // TODO: Test these
    public static bool operator ==(PointF a, PointF b)
    {
      return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(PointF a, PointF b)
    {
      return !(a.X == b.X && a.Y == b.Y);
    }
  }
  // bitmap struct
  public struct BmpS
  {
    public BmpS() { }
    public int W, H, Stride = 0;
    public byte[] Pixels;
    public int Offset;
  }

  // used for V2 rasterizer
  public struct ActiveEdgeV2
  {
    public float fx, fdx, fdy;
    public float direction;
    public float sy;
    public float ey;
  }

  public struct TTFEdge
  {
    public float x0, y0, x1, y1;
    public bool Invert;
  }
  public struct FontDirectory
  {
    // offset subtable
    public TTF_ScalarType ScalarType;
    public ushort NumTables;       // number of tables in the font
    public ushort SearchRange;     // (maximum power of 2 <= numTables)*16
    public ushort EntrySelector;   // log2(maximum power of 2 <= numTables)
    public ushort RangeShift;      // number of tables in the font
  }
}
