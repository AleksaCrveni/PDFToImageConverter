using Converter.DEBUG;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Converter.Rasterizers
{
  public class Rasterizer : STBRasterizer, IRasterizer
  {
    public Rasterizer(byte[] rawFontBuffer, string? encodingType) : base(rawFontBuffer, encodingType)
    {
    }

    public override void GetGlyphBoundingBox(ref GlyphInfo glyphInfo, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
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

    void IRasterizer.GetGlyphInfo(int codepoint, ref GlyphInfo glyphInfo)
    {
      throw new NotImplementedException();
    }

    (float scaleX, float scaleY) IRasterizer.GetScale(int glyphIndex, double[,] textRenderingMatrix, float width) 
    {
      throw new NotImplementedException();
    }
#if DEBUG
    public override InterpreterStateData GetCurrentGlyphInterpreterState()
    {
      throw new NotImplementedException();
    }
#endif
  }
}
