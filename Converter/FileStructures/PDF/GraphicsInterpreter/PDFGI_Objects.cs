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
}
