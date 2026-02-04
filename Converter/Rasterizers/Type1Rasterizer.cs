using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Parsers.Fonts;
using Converter.StaticData;
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
      _interpreter = new Type1Interpreter(_buffer, _fontInfo);
      InitFont();
    }

    public void GetGlyphInfo(int codepoint, ref GlyphInfo glyphInfo)
    {
      string glyphName = _fontInfo.EncodingData.GetGlyphNameFromDifferences(codepoint);
      if (glyphName == string.Empty)
      {
        if (codepoint < _encodingArray.Length)
        {
          int glyphNameIndex = _encodingArray[codepoint];
          glyphName = PDFEncodings.GetGlyphName(glyphNameIndex);
        }
        {
          glyphName = ".notdef";
        }
      }

      // Type1 doesnt use glyphIndex
      glyphInfo.Index = 0;
      glyphInfo.Name = glyphName;
    }

    public (float scaleX, float scaleY) GetScale(int glyphName, double[,] textRenderingMatrix, float width)
    {
      // in type 1 font units are already scaled including width are already scaled
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
      List<TTFVertex> vertices = ConvertToTTFVertexFormat(shape, scale); // for now we only support aspect ratio scaling
      List<int> windingLengths = new List<int>();
      int windingCount = 0;

      List<PointF> windings = STB_FlattenCurves(ref vertices, vertices.Count, 0.35f / scale, ref windingLengths, ref windingCount);

      GetFakeBoundingBoxFromPoints(windings, ref ix0, ref iy0, ref ix1, ref iy1);
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
 
    // temp
    public List<TTFVertex> ConvertToTTFVertexFormat(PSShape s, float scale)
    {
      List<TTFVertex> vertices = new List<TTFVertex>();
      int i = 0;
      int HEIGHT = 300;
      int WIDTH = 300;
      TTFVertex v;
      foreach (PS_COMMAND cmd in s._moves)
      {
        switch (cmd)
        {
          case PS_COMMAND.MOVE_TO:
            v = new TTFVertex();
            v.type = (byte)TTF_VMove.VMOVE;
            v.x = (short)(s._shapePoints[i++] * scale);
            v.y = (short)(s._shapePoints[i++] * scale);
            break;
          case PS_COMMAND.LINE_TO:
            v = new TTFVertex();
            v.type = (byte)TTF_VMove.VLINE;
            v.x = (short)(s._shapePoints[i++] * scale);
            v.y = (short)(s._shapePoints[i++] * scale);
            break;
          // cubic Bezier
          case PS_COMMAND.CURVE_TO:
            v = new TTFVertex();
            v.type = (byte)TTF_VMove.VCUBIC;
            
            // This is correct order for converting from PS CurveTo arguments to Vertex format that will be passed to TesselateCubic
            v.cx = (short)(s._shapePoints[i++] * scale);
            v.cy = (short)(s._shapePoints[i++] * scale);
            v.cx1 = (short)(s._shapePoints[i++] * scale);
            v.cy1 = (short)(s._shapePoints[i++] * scale);
            v.x = (short)(s._shapePoints[i++] * scale);
            v.y = (short)(s._shapePoints[i++] * scale);
          
            break;
          default:
            throw new InvalidDataException($"Unexpected command: {cmd}");
        }
        vertices.Add(v);
      }
      return vertices;
    }


    // TODO
    public void GetFakeBoundingBoxFromPoints(List<PointF> points, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
      // Units are already scaled
      float fx0 = float.MaxValue; // min X
      float fy0 = float.MaxValue; // min Y
      float fx1 = float.MinValue; // max X
      float fy1 = float.MinValue; // max Y

      foreach (PointF p in points)
      {
        if (p.X < fx0)
          fx0 = p.X;
        else if (p.X > fx1)
          fx1 = p.X;

        if (p.Y < fy0)
          fy0 = p.Y;
        else if (p.Y > fy1)
          fy1 = p.Y;
      }

      // Y axis is ivnerted
      ix0 = (int)MathF.Floor(fx0);
      iy0 = (int)Math.Floor(-fy1);
      ix1 = (int)Math.Floor(fx1);
      iy1 = (int)Math.Ceiling(-fy0);
    }


    public override void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo)
    {
      BmpS result = new BmpS();
      result.H = glyphHeight;
      result.W = glyphWidth;
      result.Offset = byteOffset;
      result.Pixels = bitmapArr;
      result.Stride = glyphStride;
      STB_InternalRasterize(ref result, ref _currentShape._windings, ref _currentShape._windingLengths, _currentShape._windingCount, 1, 1, 0, 0, _currentShape._xMin, _currentShape._yMin, true);
    }
  }
}
