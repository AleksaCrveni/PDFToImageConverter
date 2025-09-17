using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public List<ColorSpace> ColorSpace;
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
      xLocation = _xLocation;
      yLocation = _yLocation;
      xOrientation = _xOrientation;
      yOrientation = _yOrientation;
      xLen = _xLen;
      yLen = _yLen;
    }
    // origin location
    public double xLocation;
    public double yLocation;

    // axis orientation
    public double xOrientation;
    public double yOrientation;

    // lengths of the units along each axis
    public double xLen;
    public double yLen;
  }

  public enum ColorSpace
  {
    DeviceGray
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
}
