
namespace Converter.Rasterizers
{
  public class CompositeFontRasterizer : STBRasterizer, IRasterizer
  {
    public CompositeFontRasterizer(byte[] rawFontBuffer) : base(rawFontBuffer)
    {
    }

    public (int glyphIndex, string glyphName) GetGlyphInfo(char c)
    {
      throw new NotImplementedException();
    }

    public (float scaleX, float scaleY) GetScale(int glyphName, double[,] textRenderingMatrix, float width)
    {
      throw new NotImplementedException();
    }

    protected override void InitFont()
    {
      throw new NotImplementedException();
    }
  }
}
