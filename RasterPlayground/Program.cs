using Converter;
using Converter.FileStructures.PDF;
using RasterPlayground;
using System.Diagnostics;
int WIDTH = 15;
int HEIGHT = 15;

byte[] bitmap = Utils.DrawLine(WIDTH, HEIGHT, 0, 0, 10, 13);
byte[] buff = Utils.ConvertToWin32RGBBuffer(bitmap, HEIGHT, WIDTH);

string fontName = "GTGWSY+CMR10";
PDF_FontInfo fontInfo = new PDF_FontInfo();
fontInfo.FontDescriptor = new PDF_FontDescriptor();
fontInfo.FontDescriptor.FontName = fontName;

byte[] input = File.ReadAllBytes(Files.RootFolder + @$"\{fontName}-fontFile.txt");
//Type1Rasterizer r = new Type1Rasterizer(input, ref fontInfo);
//r.InterpretByName("equal");

File.WriteAllBytes("Single\\data.txt", buff);
