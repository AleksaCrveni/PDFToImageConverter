using Converter.FileStructures.General;
using Converter.FileStructures.TIFF;
using Converter.Parsers.Images;
using Converter.Parsers.Images.PNG;
using Converter.Parsers.Images.TIFF;


uint marker = 0;
marker |= 255;
marker <<= 24;
marker |= 255; // a;

if (args.Length != 2)
{
  Console.WriteLine("Invalid arguments!");
  return 1;
}

string p1 = args[0];
string p2 = args[1];
if (!p1.EndsWith(".tiff") || !p2.EndsWith(".tiff"))
{
  Console.WriteLine("Only TIFF files supported!");
  return 1;
}

if (!File.Exists(p1))
{
  Console.WriteLine($"File {p1} can't be found!");
  return 1;
}

if (!File.Exists(p2))
{
  Console.WriteLine($"File {p2} can't be found!");
  return 1;
}

Img img1 = ImgLoader.Load(p1);
Img img2 = ImgLoader.Load(p2);


if (img1.BitsPerSample != 8)
{
  Console.WriteLine("Only 8 bit sample precision supported!");
  return 1;
}
// do some basic checks
if (img1.Height != img2.Height ||
    img1.Width  != img2.Width  ||
    img1.BitsPerSample != img2.BitsPerSample ||
    img1.BytesPerPixel != img2.BytesPerPixel)
{
  Console.WriteLine("Images must be same size, bpp, samples per pixel and fill order!");
  return 1;
}

byte[] rawOut = new byte[img1.RawPixels.Length];
Array.Copy(img1.RawPixels, rawOut, img1.RawPixels.Length);
int bpp = (int)img1.BytesPerPixel;
for (int i = 0; i < rawOut.Length; i+= bpp)
{
  byte R1 = rawOut[i];
  byte G1 = rawOut[i + 1];
  byte B1 = rawOut[i + 2];
  byte A1 = 255;
  if (bpp == 4)
    A1 = rawOut[i + 3];

  byte R2 = img2.RawPixels[i];
  byte G2 = img2.RawPixels[i + 1];
  byte B2 = img2.RawPixels[i + 2];
  byte A2 = 255;
  if (bpp == 4)
    A2 = img2.RawPixels[i + 3];
  if (R1 != R2 || G1 != G2 || B1 != B2 || A1 != A2)
  {
    rawOut[i] = (byte)(marker >> 24);
    rawOut[i + 1] = (byte)(marker >> 16);
    rawOut[i + 2] = (byte)(marker >> 8);
    if (bpp == 4)
      rawOut[i + 3] = (byte)marker;
    int pix = i == 0 ? 0 : i / bpp;
    int row = pix == 0 ? 0 : (int)(pix / img1.Height);
    int col = pix == 0 ? 0 : (int)(pix % img1.Width );
    Console.WriteLine($"Diff at pixel {pix}. Row: {row} Col: {col}. R1:{R1} G1:{G1} B1:{B1} A1:{A1} --- R2:{R2} G2:{G2} B2:{B2} A2:{A2}");
  }

}

    
return 0;