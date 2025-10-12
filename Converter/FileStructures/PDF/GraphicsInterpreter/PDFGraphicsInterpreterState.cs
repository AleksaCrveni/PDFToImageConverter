using System.Security.Cryptography.X509Certificates;

namespace Converter.FileStructures.PDF.GraphicsInterpreter
{
  // this data specified in page descriptions
  // Table 52 + 53 
  public struct GraphicsState
  {
    public GraphicsState()
    {
      // double assignedmnet, fix later
      CTM = new double[3,3];
      BlendMode = new object[1];
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

    public GraphicsState DeepCopy()
    {
      GraphicsState newGS = new GraphicsState();
      newGS.CTM[0, 0] = this.CTM[0, 0];
      newGS.CTM[0, 1] = this.CTM[0, 1];
      newGS.CTM[0, 2] = this.CTM[0, 2];
      newGS.CTM[1, 0] = this.CTM[1, 0];
      newGS.CTM[1, 1] = this.CTM[1, 1];
      newGS.CTM[1, 2] = this.CTM[1, 2];
      newGS.CTM[2, 0] = this.CTM[2, 0];
      newGS.CTM[2, 1] = this.CTM[2, 1];
      newGS.CTM[2, 2] = this.CTM[2, 2];

      // TODO: make sure these are coppied propely
      newGS.ClippingPath = this.ClippingPath;
      newGS.ColorSpaceInfo = this.ColorSpaceInfo;
      newGS.Color = this.Color;
      newGS.TextState = this.TextState;
      newGS.LineWidth = this.LineWidth;
      newGS.LineCap = this.LineCap;
      newGS.LineJoin = this.LineJoin;
      newGS.MiterLimit = this.MiterLimit;
      newGS.DashPattern = this.DashPattern;
      newGS.RenderingIntent = this.RenderingIntent;
      newGS.StrokeAdjustment = this.StrokeAdjustment;
      newGS.BlendMode = this.BlendMode;
      newGS.SoftMask = this.SoftMask;
      newGS.AlphaConstant = this.AlphaConstant;
      newGS.AlphaSource = this.AlphaSource;

      return newGS;
    }
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
 