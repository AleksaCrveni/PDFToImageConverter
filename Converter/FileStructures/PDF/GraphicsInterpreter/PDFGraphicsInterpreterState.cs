namespace Converter.FileStructures.PDF.GraphicsInterpreter
{
  // this data specified in page descriptions
  // Table 52 + 53 
  public struct GraphicsState
  {
    public GraphicsState()
    {

    }
    // Device Independent
    public double[,] CTM;
    public object ClippingPath;
    public PDF_ColorSpaceInfo ColorSpaceInfo;
    public object Color;
    public object TextState;
    public double LineWidth;
    public int LineCap;
    public int LineJoin;
    public double MiterLimit;
    public PDFGI_DashPattern DashPattern;
    public PDFGI_RenderingIntent RenderingIntent;
    public bool StrokeAdjustment;
    public object[] BlendMode;
    public object SoftMask;
    public double AlphaConstant;
    public bool AlphaSource;

    // Device Dependent
    // Used for Scan conversion
    public bool Overprint;
    public double OverprintMode;
    public object BlackGeneration;
    public object UndercolorRemoval;
    public object Transfer;
    public object Halftone;
    public double Flatness;
    public double Smoothness;
  }

  public struct PDFGI_DashPattern
  {
    public int[] DashArray;
    public int Phase;
  }

  public struct PDFGI_Point()
  {
    public int X1;
    public int Y1;
    public int X2;
    public int Y2;
    public int X3;
    public int Y3;

  }
  // not sure if any othe rparamters will be needed wrap it in now
  public struct PDFGI_PathConstruction
  {
    // instead of list this could be queue type FIFO
    public List<(PDFGI_PathConstructOperator, PDFGI_Point)> PathConstructs;
    public PDFGI_PathConstruction()
    {
      PathConstructs = new List<(PDFGI_PathConstructOperator, PDFGI_Point)>();
    }
    public bool NonZeroClippingPath = false;
    public bool EvenOddClippingPath = false;
  }
}
 