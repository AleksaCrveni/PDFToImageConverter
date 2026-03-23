
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using System.Text;

namespace Converter.Rasterizers
{
  /// <summary>
  /// THIS IS PDF Version of composite font ITS DIFFERENT THAN POSTSCRIPT VERSION due to some limitations
  /// </summary>
  public class CompositeFontRasterizer : STBRasterizer, IRasterizer
  {
    private IRasterizer _actualRasterizer;
    private PDF_FontInfo _fontInfo;
    private CompositeFontInfo _cFontData;
    public CompositeFontRasterizer(byte[] rawFontBuffer, PDF_FontInfo fontInfo) : base(rawFontBuffer, null)
    {
      _cFontData = fontInfo.DescendantFontsInfo[0]; // PDF Only supports one so always take first 
      _fontInfo = fontInfo;
      _actualRasterizer = _cFontData.DescendantDict.FontDescriptor.FontFile.Type switch
      {
        PDF_FontFileType.NULL => throw new InvalidDataException("FontFile not found!"),
        PDF_FontFileType.One => throw new NotImplementedException(),
        PDF_FontFileType.Two => new TTFRasterizer(rawFontBuffer, ref _fontInfo, true),
        PDF_FontFileType.Three => throw new NotImplementedException(),
      };
    }
    protected override void InitFont() { }

    public override void GetGlyphBoundingBox(ref GlyphInfo glyphInfo, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
      _actualRasterizer.GetGlyphBoundingBox(ref glyphInfo, scaleX, scaleY, ref ix0, ref iy0, ref ix1, ref iy1);
    }

    public void GetGlyphInfo(int codepoint, ref GlyphInfo glyphInfo)
    {
      _actualRasterizer.GetGlyphInfo(codepoint, ref glyphInfo);
    }

    public (float scaleX, float scaleY) GetScale(int glyphName, double[,] textRenderingMatrix, float width)
    {
      return _actualRasterizer.GetScale(glyphName, textRenderingMatrix, width);
    }

    public override void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo)
    {
      _actualRasterizer.RasterizeGlyph(bitmapArr, byteOffset, glyphWidth, glyphHeight, glyphStride, scaleX, scaleY, shiftX, shiftY, ref glyphInfo);
    }

    public override char? FindCharFromCID(char CID)
    {
      if (_cFontData.Cmap.Cmap.TryGetValue(CID, out char res))
        return res;

      return null;
    }
    public override List<char> FindLigatureFromCID(char CID)
    {
      return _cFontData.Cmap.LigatureCmap.GetValueOrDefault(CID, Array.Empty<char>().ToList());
    }
  }
}
