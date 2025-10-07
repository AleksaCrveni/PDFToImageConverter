using Converter.FileStructures.PDF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Converter.FileStructures
{
  public class ContentStreamObject
  {
  }

  // this data specified in page descriptions
  // Table 52 + 53 
  public struct GraphicsState
  {
    public GraphicsState()
    {

    }
    // Device Independent
    public CTM CTM;
    public object ClippingPath;
    public PDF_ColorSpaceInfo ColorSpaceInfo;
    public object Color;
    public object TextState;
    public double LineWidth;
    public int LineCap;
    public int LineJoin;
    public double MiterLimit;
    public DashPattern DashPattern;
    public RenderingIntent RenderingIntent;
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

  public struct DashPattern
  {
    public int[] DashArray;
    public int Phase;
  }
  public struct CTM
  {
    public CTM (double _xLocation, double _yLocation, double _xOrientation, double _yOrientation, double _xLen, double _yLen)
    {
      XLocation = _xLocation;
      YLocation = _yLocation;
      XOrientation = _xOrientation;
      YOrientation = _yOrientation;
      XxLen = _xLen;
      YLen = _yLen;
    }
    // origin location
    public double XLocation;
    public double YLocation;

    // axis orientation
    public double XOrientation;
    public double YOrientation;

    // lengths of the units along each axis
    public double XxLen;
    public double YLen;
  }

  public enum RenderingIntent
  {
    Null = 0,
    AbsoluteColorimetric,
    RelativeColorimetric,
    Saturation,
    Perceptual
  }

  public class TextObject
  {
    public TextObject()
    {
      InitMatrixes();
      Active = false;
    }
    public List<string> Literal;
    public double[,] TextMatrix;
    // at new line, TLM will capture value of TM
    public double[,] TextLineMatrix;
    public string FontRef;
    public double FontScaleFactor;
    // to know if we are inside or not
    public bool Active;
    public void InitMatrixes()
    {
      TextMatrix = MyMath.RealIdentityMatrix3x3();
      TextLineMatrix = MyMath.RealIdentityMatrix3x3();
    }

  }

  public struct MyPoint()
  {
    public int X1;
    public int Y1;
    public int X2;
    public int Y2;
    public int X3;
    public int Y3;

  }
  // not sure if any othe rparamters will be needed wrap it in now
  public struct PathConstruction
  {
    // instead of list this could be queue type FIFO
    public List<(PathConstructOperator, MyPoint)> PathConstructs;
    public PathConstruction()
    {
      PathConstructs = new List<(PathConstructOperator, MyPoint)>();
    }
    public bool NonZeroClippingPath = false;
    public bool EvenOddClippingPath = false;
  }
  public class PathObject
  { }
  public class ShadingObject
  { }
  public class ExternalObject
  { }
  public class InlineImageObject
  {
    
  }
  
  public class ClippingPathObject
  {

  }

  public enum PathConstructOperator : uint
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
