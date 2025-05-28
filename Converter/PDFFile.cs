using Converter.Parsers;
namespace Converter
{
  public class PDFFile
  {
    public PDFVersion PdfVersion { get; set; } = PDFVersion.INVALID;
    public ulong LastCrossReferenceOffset { get; set; }
    public Trailer Trailer { get; set; }
  }
}
