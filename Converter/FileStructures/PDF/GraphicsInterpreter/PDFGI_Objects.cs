using Converter.Utils;

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
    public PDF_FontData Font;
    public void InitMatrixes()
    {
      TextMatrix = MyMath.RealIdentityMatrix3x3();
      TextLineMatrix = MyMath.RealIdentityMatrix3x3();
    }

    public PDFGI_TextObject DeepCopy()
    {
      PDFGI_TextObject newTO = new PDFGI_TextObject();
      Copy.Matrix3x3(newTO.TextMatrix, this.TextMatrix);
      Copy.Matrix3x3(newTO.TextLineMatrix, this.TextLineMatrix);
      newTO.FontRef = this.FontRef;
      newTO.FontScaleFactor = this.FontScaleFactor;
      newTO.Th = this.Th;
      newTO.Tl = this.Tl;
      newTO.Tw = this.Tw;
      newTO.Tc = this.Tc;
      newTO.TL = this.TL;
      newTO.TMode = this.TMode;
      newTO.TRise = this.TRise;
      newTO.Active = this.Active;
      newTO.Font = this.Font;
      return newTO;
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
  /// It hold intesities of each color component between 0.0 and 1.0
  /// This allows us calculate correct color range based on bits per component  in whatever image format we are converting to
  /// </summary>
  public class MyColor
  {
    // here should be tag to know which color it is
    public double R;
    public double G;
    public double B;
    public double A = 1;
    public void SetColor(double r, double g, double b, double a)
    {
      R = r;
      G = g;
      B = b;
      A = a;
    }
  }

}