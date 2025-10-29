using Converter.FileStructures.General;

namespace Converter.FileStructures.TTF
{
  public class TTF_Table_POST
  {
    public short[] GlyphNameIndexes;
    public string[] GlyphNames;
    public int Format = -1;
  }

  public class TTF_Table_CMAP
  {
    public int Index30SubtableOffset = 0;
    public int Index31SubtableOffset = 0;
    public int Index10SubtableOffset = 0;
    public int Format30 = -1;
    public int Format31 = -1;
    public int Format10 = -1;
  }
}
