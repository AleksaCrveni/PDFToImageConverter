using System.Reflection.Metadata.Ecma335;

namespace Converter.Rasterizers
{
  public interface IFontHelper
  {
    public (int glyphIndex, string glyphName) GetGlyphInfo(char c);
    public (float scaleX, float scaleY) GetScale(int glyphName, double[,] textRenderingMatrix, float width);
  }
}
