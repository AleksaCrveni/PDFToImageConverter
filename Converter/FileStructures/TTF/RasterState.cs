namespace Converter.FileStructures.TTF
{
  public class RasterState
  {
    public int X = 0;
    public int Baseline = 0;
    public int BitmapWidth;
    public int BitmapHeight;
    public RasterState(int x, int baseline, int bitmapWidth, int bitmapHeight)
    {
      X = x;
      Baseline = baseline;
      BitmapWidth = bitmapWidth;
      BitmapHeight = bitmapHeight;
    }
  }
}
