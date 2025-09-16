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
  public class GraphicsState
  {
    public GraphicsState()
    {

    }
    // Device Independent
    public object[] CTM;
    public object ClippingPath;
    public List<ColorSpace> ColorSpace;
    public object Color;
    public object TextState;
    public double LineWidth;
    public int LineCap;
    public int LineJoin;
    public int MiterLimit;
    public object DashPattern;
    public object RenderingIntent;
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

  public enum ColorSpace
  {
    DeviceGray
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
