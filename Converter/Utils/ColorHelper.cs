using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;

namespace Converter.Utils
{
  public static class ColorHelper
  {
    // Table 90
    public static int GetPDFColorCountInSpace(PDF_ColorSpaceFamily f) => f switch
    {
      PDF_ColorSpaceFamily.NULL => 0,
      PDF_ColorSpaceFamily.DeviceGray => 1,
      PDF_ColorSpaceFamily.DeviceRGB => 3,
      PDF_ColorSpaceFamily.DeviceCMYK => 4,
      PDF_ColorSpaceFamily.CalGray => 1,
      PDF_ColorSpaceFamily.CalRGB => 3,
      PDF_ColorSpaceFamily.Lab => throw new NotImplementedException(),
      PDF_ColorSpaceFamily.ICCBased => throw new NotImplementedException(),
      PDF_ColorSpaceFamily.Indexed => throw new NotSupportedException(),
      PDF_ColorSpaceFamily.Pattern => throw new NotSupportedException(),
      PDF_ColorSpaceFamily.Separation => 1,
      PDF_ColorSpaceFamily.DeviceN => throw new NotImplementedException(),
    };

    public static void ConvertCMYKtoRGBbyIntensity(double C, double M, double Y, double K, MyColor rgb)
    {
      rgb.R = (1 - C) * (1 - K);
      rgb.G = (1 - M) * (1 - K);
      rgb.B = (1 - Y) * (1 - K);
    }

    public static void ConvertYCbCrToRGBArray(byte[] arr)
    {
      int R = 0;
      int G = 0;
      int B = 0;
      double y = 0;
      double cb = 0;
      double cr = 0;
      byte[] second = new byte[arr.Length];
      for (int i = 0; i < arr.Length; i +=3)
      {
        y = arr[i];
        cb = arr[i + 1];
        cr = arr[i + 2];

        R = (int)(y + 1.40200 * (cr - 0x80));
        G = (int)(y - 0.34414 * (cb - 0x80) - 0.71414 * (cr - 0x80));
        B = (int)(y + 1.77200 * (cb - 0x80));

        arr[i] = (byte)Math.Clamp(R, 0, 255);
        arr[i + 1] = (byte)Math.Clamp(G, 0, 255);
        arr[i + 2] = (byte)Math.Clamp(B, 0, 255);
      }
    }
  }
}
