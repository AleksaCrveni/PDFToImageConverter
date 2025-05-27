using System.IO;
using System;
using Converter;
using Converter.Parsers;
using System.Reflection;

namespace Tester
{
  [TestClass]
  public sealed class TrailerTests
  {
    public TrailerTests()
    {
      
    }
    [TestMethod]
    public void BasicTrailerSucess()
    {
      PdfParser pdfParser = new PdfParser();
      PDFFile pdfFile = pdfParser.Parse(Files.BaseDocFilePath);
      Assert.IsTrue(pdfFile.PdfVersion == PDFVersion.V1_3);
      Assert.IsTrue(pdfFile.LastCrossReferenceOffset == 10777);
      Assert.IsTrue(pdfFile.Trailer.Size == 21);
      Assert.IsTrue(pdfFile.Trailer.RootIR == (12, 0));
      Assert.IsTrue(pdfFile.Trailer.InfoIR == (1, 0));
      Assert.IsTrue(pdfFile.Trailer.ID[0] == "7d7c94e7ba72081782315eefabec9c1d");
      Assert.IsTrue(pdfFile.Trailer.ID[1] == "7d7c94e7ba72081782315eefabec9c1d");
      Assert.IsTrue(pdfFile.Trailer.EncryptIR == default((int, int)));
      Assert.IsTrue(pdfFile.Trailer.XrefStm == default(int));
    }
  }
}
