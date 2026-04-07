using System.Drawing;

namespace Converter.FileStructures.PDF.GraphicsInterpreter
{
  public class PDFGI_TextObject
  {
    public PDFGI_TextObject()
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
    // to know if we are inside or not -- i forgot why I added this..
    // rename later
    public double Th = 100 / 100; // Horizontal Scaling
    public double Tl = 0; // Leading
    public double Tw = 0; // Word Space
    public double Tc = 0; // Char space
    public double TL = 0; // Text leading
    public int TMode = 0; // Render mode
    public double TRise = 0; // Rise
    public bool Active;
    public void InitMatrixes()
    {
      TextMatrix = MyMath.RealIdentityMatrix3x3();
      TextLineMatrix = MyMath.RealIdentityMatrix3x3();
    }

  }
  public class PDFGI_PathObject
  { }
  public class PDFGI_ShadingObject
  { }
  public class PDFGI_ExternalObject
  { }
  public class PDFGI_InlineImageObject
  {

  }
  public class PDFGI_ClippingPathObject
  {

  }

  public class PDFGI_DrawState
  {
    public double[,] TextRenderingMatrix;
    public double[,] CTM;
    public PDFGI_TextObject TextObject;
  }

  public class PDFGI_ColorState
  {
    public PDF_ColorSpace Cs;
    public MyColor Color;
    public int IndexColor;
    public double Tint;
    public PDFGI_Pattern Pattern;
    // Pattern??
  }

  // is this similar to shape??
  public class PDFGI_Pattern()
  {

  }
  /// <summary>
  /// Used to hold 4 color values
  /// For RGB/A:
  ///   Val1 -> R;
  ///   Val2 -> G;
  ///   Val3 -> B;
  ///   Val4 -> A;
  /// for CMYK:
  ///   val1 -> Cyan
  ///   val2 -> Magenta
  ///   val3 -> Yellow
  ///   val4 -> Black
  /// for Gray:
  ///   val1 -> Gray
  ///   val1 = val2 = val3
  ///   val4 -> 0
  /// </summary>
  public class MyColor
  {
    public double Val1;
    public double Val2;
    public double Val3;
    public double Val4;
    public void SetColor(double v1, double v2, double v3, double v4)
    {
      Val1 = v1;
      Val2 = v2;
      Val3 = v3;
      Val4 = v4;
    }
  }

}