using Converter;
using Converter.Parsers;

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

      var cRefTableEntries = pdfFile.CrossReferenceEntries;
      Assert.IsTrue(cRefTableEntries.Count == pdfFile.Trailer.Size);
      CRefEntry c = new CRefEntry { TenDigitValue = 0, GenerationNumber = 65535, EntryType = (byte)'f' };
      Assert.IsTrue(cRefTableEntries[0] == c);
      c = new CRefEntry { TenDigitValue = 10702, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[1] == c);
      c = new CRefEntry { TenDigitValue = 281, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[2] == c);
      c = new CRefEntry { TenDigitValue = 3265, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[3] == c);
      c = new CRefEntry { TenDigitValue = 22, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[4] == c);
      c = new CRefEntry { TenDigitValue = 262, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[5] == c);
      c = new CRefEntry { TenDigitValue = 385, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[6] == c);
      c = new CRefEntry { TenDigitValue = 3229, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[7] == c);
      c = new CRefEntry { TenDigitValue = 3398, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[8] == c);
      c = new CRefEntry { TenDigitValue = 6511, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[9] == c);
      c = new CRefEntry { TenDigitValue = 493, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[10] == c);
      c = new CRefEntry { TenDigitValue = 3208, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[11] == c);
      c = new CRefEntry { TenDigitValue = 3348, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[12] == c);
      c = new CRefEntry { TenDigitValue = 3773, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[13] == c);
      c = new CRefEntry { TenDigitValue = 4032, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[14] == c);
      c = new CRefEntry { TenDigitValue = 6490, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[15] == c);
      c = new CRefEntry { TenDigitValue = 6893, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[16] == c);
      c = new CRefEntry { TenDigitValue = 7153, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[17] == c);
      c = new CRefEntry { TenDigitValue = 10586, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[18] == c);
      c = new CRefEntry { TenDigitValue = 10607, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[19] == c);
      c = new CRefEntry { TenDigitValue = 10660, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[20] == c);

      Assert.IsTrue(pdfFile.Catalog.PagesIR == (3, 0));

      Assert.IsTrue(pdfFile.PageTrees.Count == 1);
      Assert.IsTrue(pdfFile.PageTrees[0].Count == 1);
      Rect rect = new Rect();
      rect.FillRect(0, 0, 595, 842);
      Assert.IsTrue(pdfFile.PageTrees[0].MediaBox == rect);
      Assert.IsTrue(pdfFile.PageTrees[0].KidsIRs.Count == 1);
      Assert.IsTrue(pdfFile.PageTrees[0].KidsIRs[0] == (2, 0));
      Assert.IsTrue(pdfFile.PageInformation.Count == 1);
      Assert.IsTrue(pdfFile.PageInformation[0].ParentIR == (3, 0));
      Assert.IsTrue(pdfFile.PageInformation[0].ResourcesIR == (6, 0));
      Assert.IsTrue(pdfFile.PageInformation[0].ContentsIR == (4, 0));
      Assert.IsTrue(pdfFile.PageInformation[0].MediaBox == rect);
    }
  }
}
