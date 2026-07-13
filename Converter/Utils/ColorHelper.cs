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

  }
}
