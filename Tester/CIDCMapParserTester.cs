using Converter.FileStructures.PDF;
using Converter.Parsers.Fonts;
using Converter.Parsers.PDF;

namespace Tester
{
  [TestClass]
  public class CIDCMapParserTester
  {
    [TestMethod]
    public void Test1()
    {
      string s = "/CIDInit /ProcSet findresource begin 12 dict begin begincmap /CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def /CMapName /Adobe−Identity−UCS def /CMapType 2 def 1 begincodespacerange <0000> <FFFF> endcodespacerange 2 beginbfrange <0000> <005E> <0020> <005F> <0061> [<00660066> <00660069> <00660066006C>] endbfrange 1 beginbfchar <3A51> <D840DC3E> endbfchar endcmap CMapName currentdict /CMap defineresource pop end end endstream endobj";
      Span<byte> b = new byte[s.Length];
      for (int i = 0; i < s.Length; i++)
        b[i] = (byte)s[i];
      ReadOnlySpan<byte> buffer = b.Slice(0);
      CIDCmapParserHelper parser = new CIDCmapParserHelper(ref buffer);
      var _ = parser.Parse();
    }

    [TestMethod]
    public void Test2()
    {
      string s = "/CIDInit /ProcSet findresource begin\r\n12 dict begin\r\nbegincmap\r\n/CIDSystemInfo\r\n<< /Registry (Adobe)\r\n/Ordering (UCS) /Supplement 0 >> def\r\n/CMapName /Adobe-Identity-UCS def\r\n/CMapType 2 def\r\n1 begincodespacerange\r\n<0000> <FFFF>\r\nendcodespacerange\r\n55 beginbfchar\r\n<0003> <0020>\r\n<000F> <002C>\r\n<001D> <003A>\r\n<0235> <040A>\r\n<023A> <0410>\r\n<023C> <0412>\r\n<023D> <0413>\r\n<023E> <0414>\r\n<023F> <0415>\r\n<0241> <0417>\r\n<0242> <0418>\r\n<0244> <041A>\r\n<0245> <041B>\r\n<0246> <041C>\r\n<0247> <041D>\r\n<0248> <041E>\r\n<0249> <041F>\r\n<024A> <0420>\r\n<024B> <0421>\r\n<024C> <0422>\r\n<024D> <0423>\r\n<024E> <0424>\r\n<024F> <0425>\r\n<0251> <0427>\r\n<0252> <0428>\r\n<025A> <0430>\r\n<025B> <0431>\r\n<025C> <0432>\r\n<025D> <0433>\r\n<025E> <0434>\r\n<025F> <0435>\r\n<0260> <0436>\r\n<0261> <0437>\r\n<0262> <0438>\r\n<0264> <043A>\r\n<0265> <043B>\r\n<0266> <043C>\r\n<0267> <043D>\r\n<0268> <043E>\r\n<0269> <043F>\r\n<026A> <0440>\r\n<026B> <0441>\r\n<026C> <0442>\r\n<026D> <0443>\r\n<026E> <0444>\r\n<026F> <0445>\r\n<0270> <0446>\r\n<0271> <0447>\r\n<0272> <0448>\r\n<027B> <0452>\r\n<0281> <0458>\r\n<0282> <0459>\r\n<0283> <045A>\r\n<0284> <045B>\r\n<0287> <045F>\r\nendbfchar\r\nendcmap CMapName currentdict /CMap defineresource pop end end\r\n";
      Span<byte> b = new byte[s.Length];
      for (int i = 0; i < s.Length; i++)
        b[i] = (byte)s[i];
      ReadOnlySpan<byte> buffer = b.Slice(0);
      CIDCmapParserHelper parser = new CIDCmapParserHelper(ref buffer);
      PDF_CID_CMAP cidMap = parser.Parse();
      Assert.IsTrue(cidMap.Cmap.Count == 55);
    }
  }
}
