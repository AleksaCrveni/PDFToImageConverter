
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;

namespace Converter.Rasterizers
{
  public class CompositeFontRasterizer : STBRasterizer, IRasterizer
  {
    private IRasterizer _actualRasterizer;
    public CompositeFontRasterizer(byte[] rawFontBuffer, ref PDF_FontInfo fontInfo) : base(rawFontBuffer, fontInfo.EncodingData.BaseEncoding)
    {
      //fontInfo.CompositeFontInfo.DescendantDict.BaseFont
    }

    public (int glyphIndex, string glyphName) GetGlyphInfo(int codepoint)
    {
      throw new NotImplementedException();
    }

    public (float scaleX, float scaleY) GetScale(int glyphName, double[,] textRenderingMatrix, float width)
    {
      throw new NotImplementedException();
    }

    public void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo)
    {
      throw new NotImplementedException();
    }

    protected override void InitFont()
    {
      throw new NotImplementedException();
    }
  }
}
