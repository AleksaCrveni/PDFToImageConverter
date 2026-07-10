using Converter.FileStructures.PDF;

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



  }
}
