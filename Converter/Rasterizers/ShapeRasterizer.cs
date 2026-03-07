
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Utils;
using System.Diagnostics;

namespace Converter.Rasterizers
{
  /// <summary>
  /// Primarily used for rasterazing PDF paths
  /// Its kind of stateless workaround
  /// </summary>
  public class ShapeRasterizer : STBRasterizer, IRasterizer
  {
    public ShapeRasterizer(byte[] rawFontBuffer, string? encodingType) : base(rawFontBuffer, encodingType)
    {
    }

    public override void GetGlyphBoundingBox(ref GlyphInfo glyphInfo, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
    }

    public void GetGlyphInfo(int codepoint, ref GlyphInfo glyphInfo)
    {
      throw new NotImplementedException();
    }

    public (float scaleX, float scaleY) GetScale(int glyphIndex, double[,] textRenderingMatrix, float width)
    {
      throw new NotImplementedException();
    }

    public override void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo)
    {
     
    }

    protected override void InitFont()
    {
      throw new NotImplementedException();
    }
  }
}
