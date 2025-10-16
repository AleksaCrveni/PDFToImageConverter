using Converter;
using Converter.FileStructures.PDF;
using Converter.Parsers.PDF;

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

      Assert.IsTrue(pdfFile.PdfVersion == PDF_Version.V1_3);
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
      PDF_CRefEntry c = new PDF_CRefEntry { TenDigitValue = 0, GenerationNumber = 65535, EntryType = (byte)'f' };
      Assert.IsTrue(cRefTableEntries[0] == c);
      c = new PDF_CRefEntry { TenDigitValue = 10702, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[1] == c);
      c = new PDF_CRefEntry { TenDigitValue = 281, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[2] == c);
      c = new PDF_CRefEntry { TenDigitValue = 3265, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[3] == c);
      c = new PDF_CRefEntry { TenDigitValue = 22, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[4] == c);
      c = new PDF_CRefEntry { TenDigitValue = 262, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[5] == c);
      c = new PDF_CRefEntry { TenDigitValue = 385, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[6] == c);
      c = new PDF_CRefEntry { TenDigitValue = 3229, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[7] == c);
      c = new PDF_CRefEntry { TenDigitValue = 3398, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[8] == c);
      c = new PDF_CRefEntry { TenDigitValue = 6511, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[9] == c);
      c = new PDF_CRefEntry { TenDigitValue = 493, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[10] == c);
      c = new PDF_CRefEntry { TenDigitValue = 3208, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[11] == c);
      c = new PDF_CRefEntry { TenDigitValue = 3348, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[12] == c);
      c = new PDF_CRefEntry { TenDigitValue = 3773, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[13] == c);
      c = new PDF_CRefEntry { TenDigitValue = 4032, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[14] == c);
      c = new PDF_CRefEntry { TenDigitValue = 6490, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[15] == c);
      c = new PDF_CRefEntry { TenDigitValue = 6893, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[16] == c);
      c = new PDF_CRefEntry { TenDigitValue = 7153, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[17] == c);
      c = new PDF_CRefEntry { TenDigitValue = 10586, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[18] == c);
      c = new PDF_CRefEntry { TenDigitValue = 10607, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[19] == c);
      c = new PDF_CRefEntry { TenDigitValue = 10660, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[20] == c);

      Assert.IsTrue(pdfFile.Catalog.PagesIR == (3, 0));

      Assert.IsTrue(pdfFile.PageTrees.Count == 1);
      Assert.IsTrue(pdfFile.PageTrees[0].Count == 1);
      PDF_Rect rect = new PDF_Rect();
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
      Assert.IsTrue(pdfFile.PageInformation[0].ContentDict.Filters[0] == PDF_Filter.FlateDecode);

      PDF_ResourceDict resourceDict = pdfFile.PageInformation[0].ResourceDict;
      PDF_FontData tt2FData = resourceDict.Font[0];
      PDF_FontInfo tt2FI = tt2FData.FontInfo;
      
      Assert.IsTrue(tt2FI.SubType == PDF_FontType.TrueType);
      Assert.IsTrue(tt2FI.BaseFont == "QVIYZW+AvenirNext-Regular");
      Assert.IsTrue(tt2FI.EncodingData.BaseEncoding == PDF_FontEncodingType.MacRomanEncoding);
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
      
      PDF_FontDescriptor tt2FD = tt2FI.FontDescriptor;

      PDF_Rect tt2FontBBox = new PDF_Rect();
      tt2FontBBox.FillRect(-394, -411, 1309, 1192);

      Assert.IsTrue(tt2FD.FontName == tt2FI.BaseFont);
      Assert.IsTrue((tt2FD.Flags & PDF_FontFlags.Nonsymbolic) == PDF_FontFlags.Nonsymbolic);
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

      PDF_FontFileInfo tt2FontFileInfo = tt2FI.FontDescriptor.FontFile;
      Assert.IsTrue(tt2FontFileInfo.CommonStreamInfo.Length == 3343);
      Assert.IsTrue(tt2FontFileInfo.CommonStreamInfo.Filters[0] == PDF_Filter.FlateDecode);

      // no idea how to test actual value of decoded thing because its just random bytes and can't be easily
      // copy pasted somewhere
      Assert.IsTrue(tt2FontFileInfo.Length1 == 5628);
      Assert.IsTrue(tt2FontFileInfo.Length2 == 0);
      Assert.IsTrue(tt2FontFileInfo.Length3 == 0);
      PDF_FontData tt1FData = resourceDict.Font[1];
      PDF_FontInfo tt1FI = tt1FData.FontInfo;

      Assert.IsTrue(tt1FI.SubType == PDF_FontType.TrueType);
      Assert.IsTrue(tt1FI.BaseFont == "IJPPFY+AvenirNext-Medium");
      Assert.IsTrue(tt1FI.EncodingData.BaseEncoding == PDF_FontEncodingType.MacRomanEncoding);
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

      PDF_FontDescriptor tt1FD = tt1FI.FontDescriptor;

      PDF_Rect tt1FontBBox = new PDF_Rect();
      tt2FontBBox.FillRect(-440, -436, 1369, 1199);

      Assert.IsTrue(tt1FD.FontName == tt1FI.BaseFont);
      Assert.IsTrue((tt1FD.Flags & PDF_FontFlags.Nonsymbolic) == PDF_FontFlags.Nonsymbolic);
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

      PDF_FontFileInfo tt1FontFileInfo = tt1FI.FontDescriptor.FontFile;
      Assert.IsTrue(tt1FontFileInfo.CommonStreamInfo.Length == 2368);
      Assert.IsTrue(tt1FontFileInfo.CommonStreamInfo.Filters[0] == PDF_Filter.FlateDecode);

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

      Assert.IsTrue(pdfFile.PdfVersion == PDF_Version.V1_4);
      Assert.IsTrue(pdfFile.LastCrossReferenceOffset == 141561);
      Assert.IsTrue(pdfFile.Trailer.Size == 51);
      Assert.IsTrue(pdfFile.Trailer.RootIR == (49, 0));
      Assert.IsTrue(pdfFile.Trailer.InfoIR == (50, 0));
      Assert.IsTrue(pdfFile.Trailer.ID[0] == "F6E9CC2B383667A7FACE2422EADAAE69");
      Assert.IsTrue(pdfFile.Trailer.ID[1] == "F6E9CC2B383667A7FACE2422EADAAE69");
      Assert.IsTrue(pdfFile.Trailer.EncryptIR == default((int, int)));
      Assert.IsTrue(pdfFile.Trailer.XrefStm == default(int));


      var cRefTableEntries = pdfFile.CrossReferenceEntries;
      Assert.IsTrue(cRefTableEntries.Count == pdfFile.Trailer.Size);
      PDF_CRefEntry c = new PDF_CRefEntry { TenDigitValue = 0, GenerationNumber = 65535, EntryType = (byte)'f' };
      int cRefIndex = 0;
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 138560, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 19, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 2806, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 138722, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 2827, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 6544, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 138866, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 6565, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 10276, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 139010, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 10297, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 10502, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 10523, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 141115, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 140996, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 61445, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 73054, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 73077, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 73272, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 73769, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 74103, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 81507, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 81529, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 81725, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 82091, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 82317, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 91907, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 91929, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 92129, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 92531, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 92791, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 100402, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 100424, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 100627, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 101037, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 101308, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 136140, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 136163, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 136346, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 137411, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 138386, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 138459, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 139156, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 139212, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 139626, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 139956, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 140304, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 140690, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 141271, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);
      c = new PDF_CRefEntry { TenDigitValue = 141386, GenerationNumber = 0, EntryType = (byte)'n' };
      Assert.IsTrue(cRefTableEntries[cRefIndex++] == c);

      Assert.IsTrue(pdfFile.Catalog.PagesIR == (15, 0));
      Assert.IsTrue(pdfFile.Catalog.OutlinesIR == (43, 0));
      Assert.IsTrue(pdfFile.Catalog.Lang == "en-US");
      // dont test open action, we don't need that for converter
      // only if we were to wrote renderer
      PDF_Rect rect = new PDF_Rect();
      rect.FillRect(0, 0, 595, 842);
      Assert.IsTrue(pdfFile.PageTrees.Count == 1);
      Assert.IsTrue(pdfFile.PageTrees[0].Count == 4);
      Assert.IsTrue(pdfFile.PageTrees[0].KidsIRs.Count == 4);
      Assert.IsTrue(pdfFile.PageTrees[0].KidsIRs[0] == (1, 0));
      Assert.IsTrue(pdfFile.PageTrees[0].KidsIRs[1] == (4, 0));
      Assert.IsTrue(pdfFile.PageTrees[0].KidsIRs[2] == (7, 0));
      Assert.IsTrue(pdfFile.PageTrees[0].KidsIRs[3] == (10, 0));
      Assert.IsTrue(pdfFile.PageTrees[0].MediaBox == rect);

      Assert.IsTrue(pdfFile.PageInformation.Count == 4);
      Assert.IsTrue(pdfFile.PageInformation[0].MediaBox == rect);
      Assert.IsTrue(pdfFile.PageInformation[1].MediaBox == rect);
      Assert.IsTrue(pdfFile.PageInformation[2].MediaBox == rect);
      Assert.IsTrue(pdfFile.PageInformation[3].MediaBox == rect);

      
      Assert.IsTrue(pdfFile.PageInformation[0].ParentIR == (15, 0));
      Assert.IsTrue(pdfFile.PageInformation[1].ParentIR == (15, 0));
      Assert.IsTrue(pdfFile.PageInformation[2].ParentIR == (15, 0));
      Assert.IsTrue(pdfFile.PageInformation[3].ParentIR == (15, 0));

      Assert.IsTrue(pdfFile.PageInformation[0].ResourcesIR == (42, 0));
      Assert.IsTrue(pdfFile.PageInformation[1].ResourcesIR == (42, 0));
      Assert.IsTrue(pdfFile.PageInformation[2].ResourcesIR == (42, 0));
      Assert.IsTrue(pdfFile.PageInformation[3].ResourcesIR == (42, 0));
      // Annots?
      Assert.IsTrue(pdfFile.PageInformation[0].ContentsIR == (2, 0));
      Assert.IsTrue(pdfFile.PageInformation[1].ContentsIR == (5, 0));
      Assert.IsTrue(pdfFile.PageInformation[2].ContentsIR == (8, 0));
      Assert.IsTrue(pdfFile.PageInformation[3].ContentsIR == (11, 0));

      Assert.IsTrue(pdfFile.PageInformation[0].ContentDict.Length == 2716);
      Assert.IsTrue(pdfFile.PageInformation[1].ContentDict.Length == 3646);
      Assert.IsTrue(pdfFile.PageInformation[2].ContentDict.Length == 3640);
      Assert.IsTrue(pdfFile.PageInformation[3].ContentDict.Length == 132);

      Assert.IsTrue(pdfFile.PageInformation[0].ContentDict.Filters[0] == PDF_Filter.FlateDecode);
      Assert.IsTrue(pdfFile.PageInformation[1].ContentDict.Filters[0] == PDF_Filter.FlateDecode);
      Assert.IsTrue(pdfFile.PageInformation[2].ContentDict.Filters[0] == PDF_Filter.FlateDecode);
      Assert.IsTrue(pdfFile.PageInformation[3].ContentDict.Filters[0] == PDF_Filter.FlateDecode);

      PDF_ResourceDict resourceDict = pdfFile.PageInformation[0].ResourceDict;
      PDF_FontData tt2FData = resourceDict.Font[0];
      PDF_FontInfo tt2FI = tt2FData.FontInfo;

      Assert.IsTrue(tt2FI.SubType == PDF_FontType.TrueType);
      Assert.IsTrue(tt2FI.BaseFont == "QVIYZW+AvenirNext-Regular");
      Assert.IsTrue(tt2FI.EncodingData.BaseEncoding == PDF_FontEncodingType.MacRomanEncoding);
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

      PDF_FontDescriptor tt2FD = tt2FI.FontDescriptor;

      PDF_Rect tt2FontBBox = new PDF_Rect();
      tt2FontBBox.FillRect(-394, -411, 1309, 1192);

      Assert.IsTrue(tt2FD.FontName == tt2FI.BaseFont);
      Assert.IsTrue((tt2FD.Flags & PDF_FontFlags.Nonsymbolic) == PDF_FontFlags.Nonsymbolic);
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

      PDF_FontFileInfo tt2FontFileInfo = tt2FI.FontDescriptor.FontFile;
      Assert.IsTrue(tt2FontFileInfo.CommonStreamInfo.Length == 3343);
      Assert.IsTrue(tt2FontFileInfo.CommonStreamInfo.Filters[0] == PDF_Filter.FlateDecode);

      // no idea how to test actual value of decoded thing because its just random bytes and can't be easily
      // copy pasted somewhere
      Assert.IsTrue(tt2FontFileInfo.Length1 == 5628);
      Assert.IsTrue(tt2FontFileInfo.Length2 == 0);
      Assert.IsTrue(tt2FontFileInfo.Length3 == 0);
      PDF_FontData tt1FData = resourceDict.Font[1];
      PDF_FontInfo tt1FI = tt1FData.FontInfo;

      Assert.IsTrue(tt1FI.SubType == PDF_FontType.TrueType);
      Assert.IsTrue(tt1FI.BaseFont == "IJPPFY+AvenirNext-Medium");
      Assert.IsTrue(tt1FI.EncodingData.BaseEncoding == PDF_FontEncodingType.MacRomanEncoding);
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

      PDF_FontDescriptor tt1FD = tt1FI.FontDescriptor;

      PDF_Rect tt1FontBBox = new PDF_Rect();
      tt2FontBBox.FillRect(-440, -436, 1369, 1199);

      Assert.IsTrue(tt1FD.FontName == tt1FI.BaseFont);
      Assert.IsTrue((tt1FD.Flags & PDF_FontFlags.Nonsymbolic) == PDF_FontFlags.Nonsymbolic);
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

      PDF_FontFileInfo tt1FontFileInfo = tt1FI.FontDescriptor.FontFile;
      Assert.IsTrue(tt1FontFileInfo.CommonStreamInfo.Length == 2368);
      Assert.IsTrue(tt1FontFileInfo.CommonStreamInfo.Filters[0] == PDF_Filter.FlateDecode);

      // no idea how to test actual value of decoded thing because its just random bytes and can't be easily
      // copy pasted somewhere
      Assert.IsTrue(tt1FontFileInfo.Length1 == 3796);
      Assert.IsTrue(tt1FontFileInfo.Length2 == 0);
      Assert.IsTrue(tt1FontFileInfo.Length3 == 0);
    }

  }
}
