
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

    public override void GetGlyphBoundingBox(ref GlyphInfo glyphInfo, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
      throw new NotImplementedException();
    }

    public void GetGlyphInfo(int codepoint, ref GlyphInfo glyphInfo)
    {
      throw new NotImplementedException();
    }

    public (float scaleX, float scaleY) GetScale(int glyphName, double[,] textRenderingMatrix, float width)
    {
      throw new NotImplementedException();
    }

    public override void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo)
    {
      throw new NotImplementedException();
    }

    protected override void InitFont()
    {
      throw new NotImplementedException();
    }
  }
}
