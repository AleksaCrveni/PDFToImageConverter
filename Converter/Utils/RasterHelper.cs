using Converter.FileStructures.PDF;
using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.Rasterizers;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Converter.Utils
{
  /// <summary>
  /// At some point figure out what to do with these functions. Currently they are here only because I am not sure where i want to put them
  /// </summary>
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

    public static Rune ReadRune(string str)
    {
      if (str[5] == '>')
      {
        return new Rune(UInt16.Parse(str.AsSpan().Slice(1, 4), NumberStyles.HexNumber));
      }
      else
      {
        // surogate
        ushort high = UInt16.Parse(str.AsSpan().Slice(1, 4), NumberStyles.HexNumber);
        ushort low = UInt16.Parse(str.AsSpan().Slice(5, 4), NumberStyles.HexNumber);

        uint code = 65_536 + (uint)((high - 55_296) * 1024) + (uint)(low - 56_320);
        return new Rune(code);
      }
    }

    public static float GetCompositeWidth(char CID, CIDFontDictionary dict)
    {
      return dict.W[CID] / 1000f;
    }
    public static uint ReadUintFromHex(string str)
    {
      Debug.Assert(str[0] == '<');
      Debug.Assert(str[str.Length - 1] == '>');
      Debug.Assert(str.Length == 6 || str.Length == 10);
      if (str.Length > 6)
        return UInt32.Parse(str.AsSpan().Slice(1, 8), NumberStyles.HexNumber);
      else
        return UInt16.Parse(str.AsSpan().Slice(1, 4), NumberStyles.HexNumber);
    }
  }
}
