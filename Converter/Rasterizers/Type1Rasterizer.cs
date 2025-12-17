using Converter.FileStructures.PDF;
using Converter.Parsers.Fonts;
using System;

namespace Converter.Rasterizers
{
  internal class Type1Rasterizer : STBRasterizer, IRasterizer
  {
    private PDF_FontInfo _fontInfo;
    public Type1Rasterizer(byte[] rawFontBuffer, ref PDF_FontInfo fontInfo) : base(rawFontBuffer)
    {
      _fontInfo = fontInfo;
      InitFont();
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
      Type1Interpreter interpreter = new Type1Interpreter(_buffer, _fontInfo.FontDescriptor.FontFile);
      interpreter.LoadFont();
    }
  }
}
