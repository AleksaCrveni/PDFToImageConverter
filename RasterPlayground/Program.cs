using Converter;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Rasterizers;
using Converter.Utils;
using Converter.Writers.TIFF;
using RasterPlayground;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
int WIDTH =  800;
int HEIGHT = 800;
float objspaceFlatnessSquared = 0.0000000035f;
float scale = 0.1f;
string equal = "equal";
string bracket = "bracketleft";
string leftParen = "parenleft";
string rightParen = "parenright";
string plus = "plus";
//byte[] bitmap = Utils.DrawLine(WIDTH, HEIGHT, 0, 0, 10, 13);
byte[] bitmap = new byte[HEIGHT * WIDTH];
// tests
//688 230
//702 230
//707 230
//688 250
//bitmap[23 * WIDTH + 68] = 255;
//bitmap[23 * WIDTH + 72] = 200;
//bitmap[23 * WIDTH + 77] = 128;
//bitmap[25 * WIDTH + 68] = 64;
//byte[] buff1 = Utils.ConvertToWin32RGBBuffer(bitmap, HEIGHT, WIDTH);
//File.WriteAllBytes("Single\\data.txt", buff1);
//return;






byte[] res = Utils.DrawLine(20, 20, 0, 0, 8, 0);


res = new byte[20 * 20];
PathRasterizer sRaster = new PathRasterizer(Array.Empty<byte>(), "");
sRaster.MY_DrawLine(res, 0, 20, 0, 0, 8, 0);
for (int i = 0; i < res.Length; i++)
{
  if (res[i] > 0)
    throw new Exception("GAS");
}
//string fontName = "FYLNZH+MSAM10";
string fontName = "GTGWSY+CMR10";
//string fontName = "ZDDNRG+NimbusRomNo9L-Regu";
//string fontName = "MSXGKX+CMEX10";
PDF_FontInfo fontInfo = new PDF_FontInfo();
fontInfo.FontDescriptor = new PDF_FontDescriptor();
fontInfo.FontDescriptor.FontName = fontName;

byte[] input = File.ReadAllBytes(Files.RootFolder + @$"\{fontName}-fontFile.txt");

Type1Rasterizer r = new Type1Rasterizer(input, ref fontInfo);
//r.GetGlyphInfo();
string name = "bracketleft";
TYPE1_Point2D width = new TYPE1_Point2D();
PSShape? shape = r.InterpretByName(name);
Debug.Assert(shape != null);

// already scaled
List<TTFVertex> vertices = RasterHelper.ConvertToTTFVertexFormat(shape);
StringBuilder sb = new StringBuilder();
int j = 0;
for (int i = 0; i < shape._moves.Count; i++)
{
  PS_COMMAND v = shape._moves[i];
  if (v == PS_COMMAND.MOVE_TO)
  {
    sb.Append($"{vertices[i].x} {vertices[i].y} ");
    sb.Append("MOVE_TO ");
  } else if (v == PS_COMMAND.LINE_TO)
  {
    sb.Append($"{vertices[i].x} {vertices[i].y} ");
    sb.Append("LINE_TO ");
  }
  else if (v == PS_COMMAND.CURVE_TO)
  {
    sb.Append($"{vertices[i].cx} {vertices[i].cy} {vertices[i].cx1} {vertices[i].cy1} {vertices[i].x} {vertices[i].y} ");
    sb.Append("CURVE_TO ");
  } else
  {
    throw new InvalidDataException("Invalid PS_COMMAND!");
  }
  
}

File.WriteAllText($"TTF_VERTEX_FROM_TYPE1__{name}__{scale.ToString()}.txt", sb.ToString());

List<int> windingLengths = new List<int>();
int windingCount = 0;

List<PointF> windings = r.STB_FlattenCurves(ref vertices, vertices.Count, 0.35f / scale, ref windingLengths, ref windingCount);
int ix0 = 0;
int iy0 = 0;
int ix1 = 0;
int iy1 = 0;
int height = 0;
RasterHelper.GetFakeBoundingBoxFromPoints(windings, ref ix0, ref iy0, ref ix1, ref iy1, scale);

height = iy1 - iy0;
if (width.Y > 0)
  height = (int)(width.Y * scale);
BmpS result = new BmpS();
result.H = height;
result.W = (int)(shape._width.X * scale);
result.Offset = 20 * HEIGHT + 20; // draw at 20,20
result.Pixels = bitmap;
result.Stride = WIDTH;
r.STB_InternalRasterize(ref result, ref windings, ref windingLengths, windingCount, scale, scale, 0, 0, ix0, iy0, true);

byte[] buff = Utils.ConvertToWin32RGBBuffer(bitmap, HEIGHT, WIDTH);
File.WriteAllBytes("Single\\data.txt", buff);
