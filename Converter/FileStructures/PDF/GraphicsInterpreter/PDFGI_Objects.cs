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
    // to know if we are inside or not
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
}
