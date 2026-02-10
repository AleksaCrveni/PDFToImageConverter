using Converter;
using Converter.FileStructures.PDF;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Rasterizers;
using Converter.Writers.TIFF;
using RasterPlayground;
using System.Diagnostics;
int WIDTH =  1500;
int HEIGHT = 1500;
float objspaceFlatnessSquared = 0.0000000035f;
float scale = 1f;
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
List<TTFVertex> vertices = r.ConvertToTTFVertexFormat(shape, scale);
List<int> windingLengths = new List<int>();
int windingCount = 0;

List<PointF> windings = r.STB_FlattenCurves(ref vertices, vertices.Count, 0.35f / scale, ref windingLengths, ref windingCount);
int ix0 = 0;
int iy0 = 0;
int ix1 = 0;
int iy1 = 0;
int height = 0;
r.GetFakeBoundingBoxFromPoints(windings, ref ix0, ref iy0, ref ix1, ref iy1);

height = iy1 - iy0;
if (width.Y > 0)
  height = (int)(width.Y * scale);
BmpS result = new BmpS();
result.H = height;
result.W = (int)(shape._width.X * scale);
result.Offset = 20 * HEIGHT + 20; // draw at 20,20
result.Pixels = bitmap;
result.Stride = WIDTH;
r.STB_InternalRasterize(ref result, ref windings, ref windingLengths, windingCount, 1, 1, 0, 0, ix0, iy0, true);

byte[] buff = Utils.ConvertToWin32RGBBuffer(bitmap, HEIGHT, WIDTH);
File.WriteAllBytes("Single\\data.txt", buff);
byte[] b = new byte[buff.Length + 4]; // icnlude size

return;
buff.CopyTo(b, 4);
b[0] = (byte)(WIDTH >> 8);
b[1] = (byte)WIDTH;
b[2] = (byte)(HEIGHT >> 8);
b[3] = (byte)(HEIGHT);
File.WriteAllBytes("Single\\data.txt", b);
