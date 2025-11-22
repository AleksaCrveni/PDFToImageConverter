
using Converter.FileStructures.PDF;

namespace Converter.Rasterizers
{
  public class CompositeFontRasterizer : STBRasterizer, IRasterizer
  {
    private IRasterizer _actualRasterizer;
    public CompositeFontRasterizer(byte[] rawFontBuffer, ref PDF_FontInfo fontInfo) : base(rawFontBuffer)
    {
      //fontInfo.CompositeFontInfo.DescendantDict.BaseFont
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
