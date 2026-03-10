
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.PostScript;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
namespace Converter.Rasterizers
{
  /// <summary>
  /// Primarily used for rasterazing PDF paths
  /// Its kind of stateless workaround
  /// </summary>
  public class PathRasterizer : STBRasterizer, IRasterizer
  {
    public PathRasterizer(byte[] rawFontBuffer, string? encodingType) : base(rawFontBuffer, encodingType)
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

    public void RasterizeShape(byte[] bitmapArr, int byteOffset, int stride, PSShape shape, double scale)
    {
      if (scale != 1)
        shape.ScaleAll(scale);

      double currX = 0;
      double currY = 0;
      double x = 0;
      double y = 0;
      int i = 0;
      // For path drawing points are absolute we dont have to incr
      foreach(PS_COMMAND cmd in shape._moves)
      {
        switch (cmd)
        {
          case PS_COMMAND.MOVE_TO:
            currX = shape._shapePoints[i++];
            currY = shape._shapePoints[i++];
            break;
          case PS_COMMAND.LINE_TO:
            x = shape._shapePoints[i++];
            y = shape._shapePoints[i++];
            MY_DrawLine(bitmapArr, byteOffset, stride, currX, currY, x, y);
            currX = x;
            currY = y;
            break;
          default:
            throw new NotImplementedException($"Not implemented {cmd.ToString()}");
        }
      }
    }
    protected override void InitFont()
    {
      throw new NotImplementedException();
    }
  }
}
