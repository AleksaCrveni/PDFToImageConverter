using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.Rasterizers;

namespace Converter.Utils
{
  // TODO: think if this should actually be part oF STBRasterizer
  public static class RasterHelper
  {
    public static List<TTFVertex> ConvertToTTFVertexFormat(PSShape s)
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
            v.x = (short)(s._shapePoints[i++]);
            v.y = (short)(s._shapePoints[i++]);
            break;
          case PS_COMMAND.LINE_TO:
            v = new TTFVertex();
            v.type = (byte)TTF_VMove.VLINE;
            v.x = (short)(s._shapePoints[i++]);
            v.y = (short)(s._shapePoints[i++]);
            break;
          // cubic Bezier
          case PS_COMMAND.CURVE_TO:
            v = new TTFVertex();
            v.type = (byte)TTF_VMove.VCUBIC;

            // This is correct order for converting from PS CurveTo arguments to Vertex format that will be passed to TesselateCubic
            v.cx = (short)(s._shapePoints[i++]);
            v.cy = (short)(s._shapePoints[i++]);
            v.cx1 = (short)(s._shapePoints[i++]);
            v.cy1 = (short)(s._shapePoints[i++]);
            v.x = (short)(s._shapePoints[i++]);
            v.y = (short)(s._shapePoints[i++]);

            break;
          default:
            throw new InvalidDataException($"Unexpected command: {cmd}");
        }
        vertices.Add(v);
      }
      return vertices;
    }

    // TODO
    public static void GetFakeBoundingBoxFromPoints(List<PointF> points, ref int ix0, ref int iy0, ref int ix1, ref int iy1, float scale)
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
      ix0 = (int)MathF.Floor(fx0 * scale);
      iy0 = (int)Math.Floor(-fy1 * scale);
      ix1 = (int)Math.Floor(fx1 * scale);
      iy1 = (int)Math.Ceiling(-fy0 * scale);
    }
  }
}
