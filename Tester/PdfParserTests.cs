using Converter;
using Converter.FIleStructures;
using Converter.Parsers;

namespace Tester
{
  [TestClass]
  public sealed class PdfParserTests
  {
    public PdfParserTests()
    {
      
    }

    [TestMethod]
    public void BaseDocTest()
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

      Assert.IsTrue(pdfFile.PageInformation[0].ContentDict.Length == 166);
      Assert.IsTrue(pdfFile.PageInformation[0].ContentDict.Filters[0] == Filter.FlateDecode);

      ResourceDict resourceDict = pdfFile.PageInformation[0].ResourceDict;
      FontInfo tt2FI = resourceDict.Font["TT2"];
      
      Assert.IsTrue(tt2FI.SubType == FontType.TrueType);
      Assert.IsTrue(tt2FI.BaseFont == "QVIYZW+AvenirNext-Regular");
      Assert.IsTrue(tt2FI.Encoding == EncodingInf.MacRomanEncoding);
      Assert.IsTrue(tt2FI.FirstChar == 32);
      Assert.IsTrue(tt2FI.LastChar == 117);
      int[] tt2FIWidths = new int[117 - 32 + 1]
      {
        250,0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 260, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,0, 0, 757, 0, 562, 0, 0, 0, 492, 0, 0, 0, 0, 0, 580, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 534, 0, 0, 0, 572, 0, 0, 0, 250, 0, 0, 252, 883, 0, 0, 635, 0, 0, 444, 317, 581
      };
      for (int i = 0; i < tt2FI.Widths.Length; i++)
      {
        if (tt2FI.Widths[i] != tt2FIWidths[i])
          throw new InvalidDataException("Invalid width array!");
      }
      
      FontDescriptor tt2FD = tt2FI.FontDescriptor;

      Rect tt2FontBBox = new Rect();
      tt2FontBBox.FillRect(-394, -411, 1309, 1192);

      Assert.IsTrue(tt2FD.FontName == tt2FI.BaseFont);
      Assert.IsTrue((tt2FD.Flags & FontFlags.Nonsymbolic) == FontFlags.Nonsymbolic);
      Assert.IsTrue(tt2FD.FontBBox == tt2FontBBox);
      Assert.IsTrue(tt2FD.ItalicAngle == 0);
      Assert.IsTrue(tt2FD.Ascent == 1000);
      Assert.IsTrue(tt2FD.Descent == -366);
      Assert.IsTrue(tt2FD.CapHeight == 708);
      Assert.IsTrue(tt2FD.StemV == 72);
      Assert.IsTrue(tt2FD.XHeight == 468);
      Assert.IsTrue(tt2FD.StemH == 64);
      Assert.IsTrue(tt2FD.MaxWidth == 1569);
      Assert.IsTrue(tt2FD.AvgWidth == 455);

      FontFileInfo tt2FontFileInfo = tt2FI.FontDescriptor.FontFile;
      Assert.IsTrue(tt2FontFileInfo.CommonStreamInfo.Length == 3343);
      Assert.IsTrue(tt2FontFileInfo.CommonStreamInfo.Filters[0] == Filter.FlateDecode);

      // no idea how to test actual value of decoded thing because its just random bytes and can't be easily
      // copy pasted somewhere
      Assert.IsTrue(tt2FontFileInfo.Length1 == 5628);
      Assert.IsTrue(tt2FontFileInfo.Length2 == 0);
      Assert.IsTrue(tt2FontFileInfo.Length3 == 0);

      FontInfo tt1FI = resourceDict.Font["TT1"];

      Assert.IsTrue(tt1FI.SubType == FontType.TrueType);
      Assert.IsTrue(tt1FI.BaseFont == "IJPPFY+AvenirNext-Medium");
      Assert.IsTrue(tt1FI.Encoding == EncodingInf.MacRomanEncoding);
      Assert.IsTrue(tt1FI.FirstChar == 32);
      Assert.IsTrue(tt1FI.LastChar == 118);
      int[] tt1FIWidths = new int[118 - 32 + 1]
      {
        250,0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 761, 0, 568, 0, 0, 0, 0, 0, 0, 0, 772, 0, 595, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 542, 0, 0, 0, 0,0, 0, 0, 0, 0, 527, 0, 0, 0, 610, 0, 0, 0, 444, 0, 582, 504
      };
      for (int i = 0; i < tt1FI.Widths.Length; i++)
      {
        if (tt1FI.Widths[i] != tt1FIWidths[i])
          throw new InvalidDataException("Invalid width array!");
      }

      FontDescriptor tt1FD = tt1FI.FontDescriptor;

      Rect tt1FontBBox = new Rect();
      tt2FontBBox.FillRect(-440, -436, 1369, 1199);

      Assert.IsTrue(tt1FD.FontName == tt1FI.BaseFont);
      Assert.IsTrue((tt1FD.Flags & FontFlags.Nonsymbolic) == FontFlags.Nonsymbolic);
      Assert.IsTrue(tt1FD.FontBBox == tt2FontBBox);
      Assert.IsTrue(tt1FD.ItalicAngle == 0);
      Assert.IsTrue(tt1FD.Ascent == 1000);
      Assert.IsTrue(tt1FD.Descent == -366);
      Assert.IsTrue(tt1FD.CapHeight == 708);
      Assert.IsTrue(tt1FD.StemV == 99);
      Assert.IsTrue(tt1FD.XHeight == 474);
      Assert.IsTrue(tt1FD.StemH == 88);
      Assert.IsTrue(tt1FD.MaxWidth == 1603);
      Assert.IsTrue(tt1FD.AvgWidth == 459);

      FontFileInfo tt1FontFileInfo = tt1FI.FontDescriptor.FontFile;
      Assert.IsTrue(tt1FontFileInfo.CommonStreamInfo.Length == 2368);
      Assert.IsTrue(tt1FontFileInfo.CommonStreamInfo.Filters[0] == Filter.FlateDecode);

      // no idea how to test actual value of decoded thing because its just random bytes and can't be easily
      // copy pasted somewhere
      Assert.IsTrue(tt1FontFileInfo.Length1 == 3796);
      Assert.IsTrue(tt1FontFileInfo.Length2 == 0);
      Assert.IsTrue(tt1FontFileInfo.Length3 == 0);
    }

    [TestMethod]
    public void SampleDocTest()
    {
      PdfParser pdfParser = new PdfParser();
      PDFFile pdfFile = pdfParser.Parse(Files.Sample);
      Assert.IsTrue(1 == 1);
    }

  }
}
