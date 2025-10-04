using BenchAndSmallTests;
using BenchmarkDotNet.Running;
using Converter;
using Converter.FIleStructures;
using Converter.Parsers.Fonts;
using Converter.Parsers.PDF;
using Converter.Writers;
using Converter.Writers.TIFF;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsWPF;
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

TTFParser parser = new TTFParser();
//byte[] arr = File.ReadAllBytes("C:/Windows/Fonts/arial.ttf");
byte[] arr = File.ReadAllBytes("W:/PDFToImageConverter/Files/TT1FontInfo.txt");
parser.Init(ref arr);
int bitmapWidth = 1024;
int bitmapHeight = 256;
int lineHeight = 64;
parser.InitFont(); // required
byte[] bitmap = new byte[bitmapHeight * bitmapWidth];
float scaleFactor = parser.ScaleForPixelHeight(lineHeight);
string textToTranslate = "Nova Dusk PDF";
int x = 0;
// ascent and descent are defined in font descriptor, use those I think over getting i from  the font
int ascent = 0;
int descent = 0;
int lineGap = 0;
parser.GetFontVMetrics(ref ascent, ref descent, ref lineGap);
ascent = (int)MathF.Round(ascent * scaleFactor);
descent = (int)MathF.Round(descent * scaleFactor);
int baseline = 0;

for (int i = 0; i < textToTranslate.Length; i++)
{
  int ax = 0; // charatcter width
  int lsb = 0; // left side bearing

  parser.GetCodepointHMetrics(textToTranslate[i], ref ax, ref lsb);
  //stbtt_GetGlyphHMetrics(&info, )

  int c_x0 = 0;
  int c_y0 = 0;
  int c_x1 = 0;
  int c_y1 = 0;
  parser.GetCodepointBitmapBox(textToTranslate[i], scaleFactor, scaleFactor, ref c_x0, ref c_y0, ref c_x1, ref c_y1);

  // char height
  int y = ascent + c_y0 + baseline;

  int byteOffset = x + (int)MathF.Round(lsb * scaleFactor) + (y * bitmapWidth);
  // BUG IS THAT I AM NOT ACCOUNTI)NG BYTE OFFSET??
  parser.MakeCodepointBitmap(ref bitmap, byteOffset, c_x1 - c_x0, c_y1 - c_y0, bitmapWidth, scaleFactor, scaleFactor, textToTranslate[i]);

  // advance x
  x += (int)Math.Round(ax * scaleFactor);

  // kerning

  //int kern;
  //kern = parser.GetCodepointKernAdvance(textToTranslate[i], textToTranslate[i + 1]);
  //x += (int)Math.Round(kern * scaleFactor);
}

List<string> ints = new();
for (int i = 0; i < bitmap.Length; i++)
{
  if (bitmap[i] > 0)
    ints.Add($"{i.ToString()} ");
}

File.WriteAllLines("myByteOutput_letter_T.txt", ints.ToArray());

TIFFGrayscaleWriter writer = new TIFFGrayscaleWriter("RasterizationTest.tiff");
var options = new TIFFWriterOptions()
{
  Width = bitmapWidth,
  Height = bitmapHeight
};
bool hasOne = false;
for (int i = 0; i < bitmap.Length; i++)
  if (bitmap[i] == 1)
    hasOne = true;

if (!hasOne)
  throw new Exception("Something went wrong, bitmap empty!");

//bitmap = new byte[bitmapWidth * bitmapHeight];
//Array.Fill<byte>(bitmap, 255);
writer.WriteImageWithBuffer(ref options, bitmap);

//PdfParser pdfParser = new PdfParser();
//pdfParser.Parse(Files.BaseDocFilePath);
//var runner = BenchmarkRunner.Run<MyBenches>();
