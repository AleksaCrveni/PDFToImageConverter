using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Parsers.Fonts;
using Converter.StaticData;
using Converter.Utils;
using System.Diagnostics;

namespace Converter.Rasterizers
{
  public class Type1Rasterizer : STBRasterizer, IRasterizer
  {
    private PDF_FontInfo _fontInfo;
    private Type1Interpreter _interpreter;
    private double[,] _fontMatrix;
    // Temp workaround
    // TODO: We should probably Cache all glyphs after we compute it 
    // but currently we have to scale them to get bounding box so we would have 2 caches which is not ideal
    // this could be avoided if we change how PDFGO works which is probably what I will end up doing
    private PSShape? _currentShape; 
    public Type1Rasterizer(byte[] rawFontBuffer, ref PDF_FontInfo fontInfo) : base(rawFontBuffer, fontInfo.EncodingData.BaseEncoding)
    {
      _fontInfo = fontInfo;
      _interpreter = new Type1Interpreter(__buffer, _fontInfo);
      InitFont();
    }

    public void GetGlyphInfo(int codepoint, ref GlyphInfo glyphInfo)
    {
      string glyphName = string.Empty;

      glyphName = _fontInfo.EncodingData.GetGlyphNameFromDifferences(codepoint);
      if (glyphName == string.Empty)
      {
        if (codepoint < __encodingArray.Length)
        {
          int glyphNameIndex = __encodingArray[codepoint];
          glyphName = PDFEncodings.GetGlyphName(glyphNameIndex);
        }
        else
        {
          glyphName = ".notdef";
        }
      }

      // If PDF Encoding is empty check fontfile encoding
      // TODO: this works only for bytes so we will need to make this work different and return some kind of list that will have to be processed or something
      // for now put assert
      if (glyphName == ".notdef")
      {
        Debug.Assert(codepoint < 256);
        glyphName = _interpreter.font.FontDict.Encoding[codepoint];
      }

      // Type1 doesnt use glyphIndex
      glyphInfo.Index = 0;
      glyphInfo.Name = glyphName;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="glyphName"></param>
    /// <param name="textRenderingMatrix"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public (float scaleX, float scaleY) GetScale(int glyphName, double[,] textRenderingMatrix, float width)
    {
      float scaleX = (float)_fontMatrix[0, 0]; 
      float scaleY = (float)_fontMatrix[1, 1];

      // not sure if we need to scale width here

      scaleX *= (float)textRenderingMatrix[0, 0];
      scaleY *= (float)textRenderingMatrix[1, 1];
      return (scaleX, scaleY);
    }

    public override void GetGlyphBoundingBox(ref GlyphInfo glyphInfo, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
      TYPE1_Point2D width = new TYPE1_Point2D();
      PSShape? shape = InterpretByName(glyphInfo.Name);
      Debug.Assert(shape != null);
      float scale = scaleX > scaleY ? scaleX : scaleY;
      // we scale here
      List<TTFVertex> vertices = RasterHelper.ConvertToTTFVertexFormat(shape); // for now we only support aspect ratio scaling
      List<int> windingLengths = new List<int>();
      int windingCount = 0;

      List<PointF> windings = STB_FlattenCurves(ref vertices, vertices.Count, 0.35f / scale, ref windingLengths, ref windingCount);

      RasterHelper.GetFakeBoundingBoxFromPoints(windings, ref ix0, ref iy0, ref ix1, ref iy1, scale);
      shape._windingCount = windingCount;
      shape._windingLengths = windingLengths;
      shape._windings = windings;
      shape._xMin = ix0;
      shape._yMin = iy0;

      _currentShape = shape;
    }

    protected override void InitFont()
    {
      
      _interpreter.LoadFont();
      if (_interpreter.font.FontDict.FontMatrix != null)
        _fontMatrix = _interpreter.font.FontDict.FontMatrix;
      else
        _fontMatrix = new double[3, 3] { { 0.001, 0, 0 }, { 0, 0.001, 0 }, { 0, 0, 0 } }; // default
    }

    public PSShape? InterpretByName(string charName)
    {
      TYPE1_Point2D lsb = new TYPE1_Point2D();
      TYPE1_Point2D currPoint = new TYPE1_Point2D();
      byte[]? data = _interpreter.font.FontDict.Private.CharStrings.GetValueOrDefault(charName);
      if (data == null)
        return null;
      PSShape s = new PSShape();

      // Separate operand stack independed of PS stack
      // So called Type 1 Build-Char operand stack and can hold up to 24 numeric values
      // This might be an array considering we have to clear stack often
      Stack<float> opStack = new Stack<float>(24);
      _interpreter.InterpretCharString(data, opStack, lsb, currPoint, s, charName);
      return s;
    }
       
    public override void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo)
    {
      BmpS result = new BmpS();
      result.H = glyphHeight;
      result.W = glyphWidth;
      result.Offset = byteOffset;
      result.Pixels = bitmapArr;
      result.Stride = glyphStride;
      STB_InternalRasterize(ref result, ref _currentShape._windings, ref _currentShape._windingLengths, _currentShape._windingCount, scaleX, scaleY, 0, 0, _currentShape._xMin, _currentShape._yMin, true);
    }
  }
}
