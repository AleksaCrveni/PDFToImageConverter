namespace Converter.FileStructures.TTF
{
  public class RasterState
  {
    public int X = 0;
    public int Y = 0;
    public int Baseline = 0;
    public int BitmapWidth;
    public int BitmapHeight;
    public RasterState(int x, int y, int baseline, int bitmapWidth, int bitmapHeight)
    {
      X = x;
      Y = y;
      Baseline = baseline;
      BitmapWidth = bitmapWidth;
      BitmapHeight = bitmapHeight;
    }
  }
}
