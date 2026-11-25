using Converter.FileStructures.General;
using Converter.Parsers.Images;
using Converter.Writers;
using System.Text;

if (args == null || args.Length == 0)
{
  args = new string[]
  {
    "W:\\PDFToImageConverter\\Baseline\\sample.pdf.0.NoBGRANoNothing.png",
    "W:\\PDFToImageConverter\\Baseline\\sample_convertTest.tiff"
  };
}
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
StringBuilder sb = new StringBuilder(); 
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
    sb.AppendFormat("Diff at pixel {0}. Row: {1} Col: {2}. R1:{3} G1:{4} B1:{5} A1:{6} --- R2:{7} G2:{8} B2:{9} A2:{10}\n", pix, row, col, R1, G1, B1, A1, R2, G2, B2, A2);
  }

}
// this is very slow with Terminal on Windows, but its still faster than using WriteLine each time, its 'ok' for now
Console.Write(sb.ToString());

Img diff = new Img();
diff.BitsPerSample = img2.BitsPerSample;
diff.BytesPerPixel = img2.BytesPerPixel;
diff.Height        = img2.Height;
diff.Width         = img2.Width;
diff.Type          = img2.Type;
diff.RawPixels     = rawOut;

ImgWriter.Write("diff", diff);

return 0;