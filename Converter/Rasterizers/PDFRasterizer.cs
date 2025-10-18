using Converter.FileStructures.PDF;
using System.Security.Cryptography.X509Certificates;

namespace Converter.Rasterizers
{
  /// <summary>
  /// PDF Specific rasterizer, it builds on top of STBTrueType to address PDF specific things, while keeping STBTrueType.cs funtionality same
  /// as stb_truetype.h
  /// </summary>
  public class PDFRasterizer
  {
    private IFontHelper _fontHelper;
    public PDFRasterizer(PDF_ResourceDict pdfResourceDict)
    {

    }
  }
}
