using Converter;
using Converter.FileStructures.PDF;
using Converter.Parsers.PDF;

namespace Tester
{
  [TestClass]
  public sealed class PdfParserTests
  {
    /// <summary>
    /// Here we wil just try to parse some files and visually check them if they pass
    /// I tried writing unit tests for all pdf properties but amount of time its needed to collect right information and increasing difficulting to do so with bigger and compressed files is not worth it
    /// TODO: It still has some issues with concurency when savinga  file but w/e it will be fixed soon
    /// </summary>
    public PdfParserTests()
    {
      string dirName = "TestOutput";
      if (Directory.Exists(dirName))
        Directory.Delete(dirName, true);

      Directory.CreateDirectory(dirName);
    }

    [TestMethod]
    public void BaseDocTest()
    {
      PdfParser pdfParser = new PdfParser();
      PDF_Options options = new PDF_Options();
      PDFFile pdfFile = pdfParser.Parse(Files.BaseDocFilePath, ref options);
    }

    [TestMethod]
    public void SampleDocTest()
    {
      PdfParser pdfParser = new PdfParser();
      PDF_Options options = new PDF_Options();
      PDFFile pdfFile = pdfParser.Parse(Files.Sample, ref options);
    }

  }
}
