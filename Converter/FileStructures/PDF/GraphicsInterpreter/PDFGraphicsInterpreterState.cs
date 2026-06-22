using Converter.Rasterizers;
using Converter.Utils;

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
      TextState = new PDFGI_TextObject();
    }
    // Device Independent
    public double[,] CTM;
    public object ClippingPath;
    public PDFGI_ColorState StrokingColorSpace;
    public PDFGI_ColorState NonStrokingColorSpace;
    public PDFGI_TextObject TextState;

    // TODO(@Aleksa): I am pretty sure these are part of TextObject an not used, check if we will use them and if not remove them!
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
      Copy.Matrix3x3(newGS.CTM, this.CTM);

      // TODO: make sure these are coppied propely
      newGS.ClippingPath = this.ClippingPath;
      newGS.StrokingColorSpace = this.StrokingColorSpace;
      newGS.NonStrokingColorSpace = this.NonStrokingColorSpace;
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

      // NOTE(@Aleksa) This MAYBE doens't have to be a deep copy, think about it
      newGS.TextState = this.TextState.DeepCopy();
      return newGS;
    }
  }

  public struct PDFGI_DashPattern
  {
    public int[] DashArray;
    public int Phase;
  }

  public class PointD
  {
    public double X;
    public double Y;
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
    public PSShape Shape;
    public PDFGI_PathConstruction()
    {
      Shape = new PSShape();
    }
    public bool NonZeroClippingPath = false;
    public bool EvenOddClippingPath = false;
  }
  /// <summary>
  /// Different rasterizers may use different fields
  /// So far Name and Shape are used by Type1 Rasterizer and Index is used by TTF
  /// </summary>
  public struct GlyphInfo()
  {
    public string Name;
    public int Index;
    public PSShape? Shape;
  }
}
 