namespace Converter.FileStructures.PDF.GraphicsInterpreter
{
  public enum PDFGI_RenderingIntent
  {
    Null = 0,
    AbsoluteColorimetric,
    RelativeColorimetric,
    Saturation,
    Perceptual
  }

  public enum PDFGI_PathConstructOperator : uint
  {
    m = 0x6d,
    l = 0x0c,
    c,
    v,
    y,
    h,
    re
  }
}
