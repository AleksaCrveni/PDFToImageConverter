using BenchAndSmallTests;
using BenchmarkDotNet.Running;
using Converter;
using Converter.FileStructures.BMP;
using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PNG;
using Converter.Parsers.Images.BMP;
using Converter.Parsers.Images.PNG;
using Converter.Parsers.PDF;
using Converter.Rasterizers;
using Converter.Writers;
using Converter.Writers.BMP;
using Converter.Writers.PNG;
using Converter.Writers.TIFF;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsWPF;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Converter.FileStructures.JPEG;
using Converter.Parsers.Images.JPEG;
using Converter.Utils;
using System.Diagnostics;
using Converter.FileStructures.PDF.GraphicsInterpreter;
//int count1 = 0b_0000_0001;
//int count2 = 0b_1110_0010;

//byte a = 0b1101_0001;
//byte a1 = (byte)(a << 0b0000_0101);
//int a2 = a << count2;
//Console.WriteLine($"{a} << {count1} is {a1}; {a} << {count2} is {a2}");
//// Output:
//// 1 << 1 is 2; 1 << 225 is 2

//int b = 0b_0100;
//int b1 = b >> count1;
//int b2 = b >> count2;
//Console.WriteLine($"{b} >> {count1} is {b1}; {b} >> {count2} is {b2}");
//// Output:
//// 4 >> 1 is 2; 4 >> 225 is 2

//int count = -31;
//int c = 0b_0001;
//int c1 = c << count;
//Console.WriteLine($"{c} << {count} is {c1}");
//int i = 0;


//var str = File.ReadAllBytes(@"W:\PDFToImageConverter\Files\buc.tif");
//Span<byte> buffer = new byte[str.Length];
//str.CopyTo(buffer);

//// big or small endian
//byte b0 = buffer[0];
//byte b1 = buffer[1];
//if ((b0 == (byte)'I' || b0 == (byte)'M') && b1 == b0)
//  Console.WriteLine("First two bytes are valid.");

//if (b0 == (byte)'I')
//  Console.WriteLine("Little endian");
//else
//  Console.WriteLine("Big endian");

//// arbitary nubmer to check
//byte b2 = buffer[2];
//byte b3 = buffer[3];
//if (b0 == 'I' && b2 == 42)
//  Console.WriteLine("Valid header");
//else if (b0 == 'M' && b3 == 42)
//  Console.WriteLine("Valid header");
//else
//  Console.WriteLine("Invalid tiff header");
//byte[] arr = buffer.Slice(4, 4).ToArray();
//int firstIDFOffset = BitConverter.ToInt32(arr);


//int i = 0;


//byte[] arr = File.ReadAllBytes(Files.BilevelTiff);
//byte b = arr[23880];
//if (b == 1)
//  arr[23880] = 0;
//File.WriteAllBytes("inverted.tiff", arr);

//for (int i = 0; i < 10; i++)
//{
//  TIFFWriter.WriteRandomBilevelTIFF($"Files/Bilevel/test{i}.tif", new TIFFWriterOptions()
//  {
//    AllowStackAlloct = true,
//  });
//  TIFFWriter.WriteRandomGrayscaleTIFF($"Files/Grayscale/test{i}.tif", new TIFFWriterOptions()
//  {
//    AllowStackAlloct = true,
//  });
//  TIFFWriter.WriteRandomPaletteTiff($"Files/Palette/test{i}.tif", new TIFFWriterOptions()
//  {
//    AllowStackAlloct = true,
//  });
//  TIFFWriter.WriteRandomRGBFullColorTiff($"Files/RGBFullColor/test{i}.tif", new TIFFWriterOptions()
//  {
//    AllowStackAlloct = true,
//  });
//}


// TODO: SEE WHY ASSERTS GET TRIGGERED SOMETIMES

//STBTrueType parser = new STBTrueType();
//byte[] arr = File.ReadAllBytes(@"W:\\PDFToImageConverter\\Files\\F1.0FontInfoSample.txt");
//parser.Init(ref arr);
//int bitmapWidth = 612;
//int bitmapHeight = 792;
//int lineHeight = 36;
//parser.InitFont(); // required
//byte[] bitmap = new byte[bitmapHeight * bitmapWidth];
//float scaleFactor = parser.ScaleForPixelHeight(lineHeight);
//string textToTranslate = "Sample PDF";
//int x = 0;
//// ascent and descent are defined in font descriptor, use those I think over getting i from  the font
//int ascent = 0;
//int descent = 0;
//int lineGap = 0;
//parser.GetFontVMetrics(ref ascent, ref descent, ref lineGap);
//ascent = (int)MathF.Round(ascent * scaleFactor);
//descent = (int)MathF.Round(descent * scaleFactor);
//int baseline = 0;

//for (int i = 0; i < textToTranslate.Length; i++)
//{
//  int ax = 0; // charatcter width
//  int lsb = 0; // left side bearing

//  parser.GetCodepointHMetrics(textToTranslate[i], ref ax, ref lsb);
//  //stbtt_GetGlyphHMetrics(&info, )

//  int c_x0 = 0;
//  int c_y0 = 0;
//  int c_x1 = 0;
//  int c_y1 = 0;
//  parser.GetCodepointBitmapBox(textToTranslate[i], scaleFactor, scaleFactor, ref c_x0, ref c_y0, ref c_x1, ref c_y1);

//  // char height
//  int y = ascent + c_y0 + baseline;

//  int byteOffset = x + (int)MathF.Round(lsb * scaleFactor) + (y * bitmapWidth);
//  // BUG IS THAT I AM NOT ACCOUNTI)NG BYTE OFFSET??
//  parser.MakeCodepointBitmap(ref bitmap, byteOffset, c_x1 - c_x0, c_y1 - c_y0, bitmapWidth, scaleFactor, scaleFactor, textToTranslate[i]);
//  for (int ss = 0; ss < bitmap.Length;ss++)
//    if (bitmap[ss] > 0)
//    {
//      int sdasd = ss;
//    }
//  // advance x
//  x += (int)Math.Round(ax * scaleFactor);

//  // kerning

//  //int kern;
//  //kern = parser.GetCodepointKernAdvance(textToTranslate[i], textToTranslate[i + 1]);
//  //x += (int)Math.Round(kern * scaleFactor);
//}

//List<string> ints = new();
//for (int i = 0; i < bitmap.Length; i++)
//{
//  if (bitmap[i] > 0)
//    ints.Add($"{i.ToString()} ");
//}

//List<string> lines = new List<string>();
//File.WriteAllLines(@"W:\PDFToImageConverter\Files\program.txt", ints.ToArray());

//TIFFGrayscaleWriter writer = new TIFFGrayscaleWriter("RasterizationTest.tiff");
//var options = new TIFFWriterOptions()
//{
//  Width = bitmapWidth,
//  Height = bitmapHeight
//};
//bool hasOne = false;
//for (int i = 0; i < bitmap.Length; i++)
//  if (bitmap[i] == 1)
//    hasOne = true;

//if (!hasOne)
//  throw new Exception("Something went wrong, bitmap empty!");

//bitmap = new byte[bitmapWidth * bitmapHeight];
//Array.Fill<byte>(bitmap, 255);

//int bitmapWidth = 595;
//int bitmapHeight = 842;
//byte[] bitmap = new byte[bitmapHeight * bitmapWidth];

////127.74350017309189 u
////137.34650045633316 s
////144.67250055074692 k
////157.493000715971 P


//float x1 = 127.74350017309189f;
//float x2 = 137.34650045633316f;
//float x3 = 144.67250055074692f;
//float x4 = 157.493000715971f;


//TIFFGrayscaleWriter writer = new TIFFGrayscaleWriter($"W:\\PDFToImageConverter\\Files\\lines.tiff");
//var options = new TIFFWriterOptions()
//{
//  Width = bitmapWidth,
//  Height = bitmapHeight
//};

//for (int i = 0; i < bitmapHeight; i++)
//{
//  bitmap[(int)x1 + i * bitmapWidth] = 64;
//  bitmap[(int)x2 + i * bitmapWidth] = 64;
//  bitmap[(int)x3 + i * bitmapWidth] = 64;
//  bitmap[(int)x4 + i * bitmapWidth] = 64;

//}

//writer.WriteImageWithBuffer(ref options, bitmap);


//PdfParser pdfParser = new PdfParser();
//pdfParser.Parse(Files.BaseDocFilePath);
//var runner = BenchmarkRunner.Run<MyBenches>();

//byte[] b = File.ReadAllBytes("bytes.txt");


//string s = Encoding.Unicode.GetString(b);


//BMPParser bmp = new BMPParser();
//bmp.Parse(Files.RootFolder + "\\3b.bmp");

//byte[] arr = new byte[] { 1, 2, 0, 12 };

//uint val = BinaryPrimitives.ReadUInt32LittleEndian(arr.AsSpan());


//string s = "020";
//int codePoint = Char.ConvertToUtf32(s, 0);

//PdfParser parser = new PdfParser();
//PDF_Options options = new PDF_Options();
//parser.Parse(Files.Greek, ref options);

//BMPParser p = new BMPParser();
//p.Parse(@$"W:\PDFToImageConverter\Files\testBmpMono.bmp");

//BMPWriterOptions options = new BMPWriterOptions();
//options.Direction = BMP_DIRECTION.BOTTOM_UP;
//options.Width = 595;
//options.Height = 313;
//options.Type = BMP_TYPE.MONO;
//BMPWriter.WriteRandomBMP("myRandomBMP.bmp", ref options);

//PNGFile file = PNGParser.Parse(Files.PNGInternlancedSample);
////PNGWriter.Write("test.png", file);

//GraphicsPath gp1 = new GraphicsPath();
//RectangleF r1 = new RectangleF(0, 0, 10, 20);
//gp1.AddRectangle(r1);
//GraphicsPath gp2 = new GraphicsPath();
//RectangleF r2 = new RectangleF(5, 5, 10, 20);
//gp2.AddRectangle(r2);
//Region region1 = new Region(gp1);

//RegionData regionData = region1.GetRegionData();
//File.WriteAllBytes("regionData.bin", regionData.Data);
//Log(regionData);
//LogPath(gp1.PathData);
//LogPath(gp2.PathData);
//region1.Intersect(gp2);
//Console.WriteLine("----------------------------");
//Log(region1.GetRegionData());
//LogPath(gp1.PathData);
//LogPath(gp2.PathData);
//void Log(RegionData r)
//{
//  StringBuilder sb = new StringBuilder();
//  foreach (byte b in r.Data)
//  {
//    sb.Append((int)b);
//    sb.Append(' ');
//  }
//  Console.WriteLine(sb.ToString());
//}

//void LogPath(PathData p)
//{
//  StringBuilder sb = new StringBuilder();
//  foreach (PointF b in p.Points)
//  {
//    sb.Append(b.X);
//    sb.Append(' ');
//    sb.Append(b.Y);
//    sb.Append(' ');
//  }
//  Console.WriteLine(sb.ToString());
//}

//MyRefField f = new MyRefField();
//MyRef r = new MyRef(ref f);

//r.f = f;
//void Mutate(ref MyRef rr)
//{
//rr.f.i = 10;
//}

//Mutate(ref r);
//Console.WriteLine(r.f.i);
//ref struct MyRef
//{
//  public MyRefField f;
//  public MyRef(ref MyRefField field)
//  {
//    f = field;
//  }
//}

//ref struct MyRefField
//{
//  public int i;
//}




//JPEG_Block8x8F dct = new JPEG_Block8x8F();
//dct.Data = [8160.000000f, 0.000004f,   0.000002f,   -0.000001f,  -0.000011f,  0.000001f,   0.000001f,
//  0.000015f, -0.000088f,  132.544678f, -0.000014f,  156.347122f, -0.000000f,  233.990005f, 0.000008f,   666.347168f,
//0.000032f,   -0.000045f,  0.000002f,   -0.000013f,  0.000001f,   -0.000009f,  0.000002f,   0.000012f,
//0.000029f,   156.347122f, 0.000002f,   184.423996f, -0.000006f,  276.009979f, -0.000008f,  786.010010f,
//-0.000027f,  -0.000000f,  0.000001f,   0.000025f,   -0.000000f,  0.000025f,   0.000000f,   -0.000039f,
//0.000001f,   233.989990f, 0.000007f,   276.009949f, 0.000002f,   413.078125f, 0.000024f,   1176.347290f,
//-0.000014f,  0.000008f,   -0.000006f,  -0.000023f,  0.000000f,   -0.000040f,  0.000002f,   0.000048f,
//-0.000000f,  666.347168f, -0.000007f,  786.010010f, 0.000010f,   1176.347046f,-0.000005f,  3349.953125f];


//JPEG_Block8x8F IDCT = JPEGParser.InverseDCTBlock8x8(dct);



//void PrintBLock(JPEG_Block8x8F b)
//{
//  for (int i = 0; i < 8; i++)
//  {
//    for (int j = 0; j < 8; j++)
//    {
//      Console.Write($"{b.Data[i * 8 + j]:F6} ");
//    }
//    Console.WriteLine();
//  }
//}

//PrintBLock(dct);
//Console.WriteLine('\n');
//Console.WriteLine('\n');
//Console.WriteLine('\n');
//Console.WriteLine('\n');
//PrintBLock(IDCT);
//Console.ReadKey();
//JPEGFile file = JPEGParser.Parse(Path.Join(Files.RootFolder, "pdf-stream5-0.jpg"), true);
//File.WriteAllBytes("rawycbrMy.bin", file.Buffer);
//byte[] rgb = file.Buffer;
//Debug.Assert(file.Buffer.Length == file.Height * file.Width * 3);
//File.WriteAllBytes("rgbRawMy.bin", rgb);
//int scaledWidth = 266;
//int scaledHeight = 33;
//byte[] scaled = new byte[scaledWidth * scaledHeight * 3];
//GenericImageHelper.ScaleImage(file.Buffer, file.Height, file.Width, scaled, scaledHeight, scaledWidth, PDFGI_ColorChannel.RGB);

//TIFFRGBWriter scaledWriter = new TIFFRGBWriter("scaled.tiff");
//TIFFWriterOptions scaledOptions = new TIFFWriterOptions();
//scaledOptions.Height = scaledHeight;
//scaledOptions.Width = scaledWidth;
//scaledWriter.WriteEmptyImage(ref scaledOptions);
//scaledWriter.WriteImageWithBuffer(ref scaledOptions, scaled);

////ColorHelper.ConvertYCbCrToRGBArray(file.Buffer);
//TIFFRGBWriter writer = new TIFFRGBWriter("test.tiff");
//TIFFWriterOptions options = new TIFFWriterOptions();
//options.Height = file.Height;
//options.Width = file.Width;
//writer.WriteEmptyImage(ref options);
//writer.WriteImageWithBuffer(ref options, file.Buffer);
PdfParser p = new PdfParser();
PDF_Options o = new PDF_Options();

p.Parse(Files.Greek, ref o);
//var summary = BenchmarkRunner.Run<MyBenches>();


