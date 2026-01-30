using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Parsers.Fonts;
using Converter.StaticData;
using System.Diagnostics;
using static System.Formats.Asn1.AsnWriter;

namespace Converter.Rasterizers
{
  public class Type1Rasterizer : STBRasterizer, IRasterizer
  {
    private PDF_FontInfo _fontInfo;
    private Type1Interpreter _interpreter;
    private double[,] _fontMatrix;
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
      return _interpreter.InterpretCharString(charName,lsb, currPoint);
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

    // this is just temp to get it to work with STBrasterizer
    public void GetFakeBoundingBoxFromPoints(List<PointF> points, ref int ix0, ref int iy0, ref int height, float scale)
    {
      float fx0 = int.MaxValue; // min X
      float fy0 = int.MinValue; // max Y
      float fyMin = int.MaxValue; // min Y
      foreach (PointF p in points)
      {
        if (p.X < fx0)
          fx0 = p.X;
        if (p.Y < fyMin)
          fyMin = p.Y;
        else if (p.Y > fy0)
          fy0 = p.Y;
      }

      height = (int)(fy0 - fyMin);
      ix0 = (int)MathF.Floor(fx0);
      iy0 = (int)Math.Floor(-fy0);
    }

    public override void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo)
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
      int ix0 = 0;
      int iy0 = 0;
      int height = 0;
      GetFakeBoundingBoxFromPoints(windings, ref ix0, ref iy0, ref height, scale);
      // ??
      if (width.Y > 0)
        height = (int)(width.Y * scale);
      BmpS result = new BmpS();
      result.H = height;
      result.W = (int)(shape._width.X * scale);
      result.Offset = byteOffset; // draw at 20,20
      result.Pixels = bitmapArr;
      result.Stride = glyphStride;
      STB_InternalRasterize(ref result, ref windings, ref windingLengths, windingCount, 1, 1, 0, 0, ix0, iy0, true);
    }
  }
}
