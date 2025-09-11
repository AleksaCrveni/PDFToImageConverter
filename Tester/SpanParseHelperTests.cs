using Converter.FIleStructures;
using Converter.Parsers;

namespace Tester
{
  [TestClass]
  public sealed class SpanParseHelperTests
  {
    private enum GenericEnum
    {
      Enum1,
      Enum2,
      Enum3,
      Enum4
    }
    public SpanParseHelperTests()
    {

    }

    [TestMethod]
    public void GetNextTokenTest()
    {
      // tokens only gets characters between delimiters so urls in tests will look chopped but its ok
      // they wouldn't have been loaded with get next token anyways
      string input = "<</Type/Annot/Subtype/Link/Border[0 0 0]/Rect[92 355.7 189.9 371.4]/A<</Type/Action/S/URI/URI(https://products.office.com/en-us/word)>>\r\n>>";
      Span<byte> buffer = new byte[input.Length];
      for (int i = 0; i < input.Length; i++)
        buffer[i] = (byte)input[i];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      string nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "Type");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "Annot");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "Subtype");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "Link");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "Border");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "0");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "0");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "0");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "Rect");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "92");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "355.7");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "189.9");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "371.4");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "A");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "Type");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "Action");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "S");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "URI");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "URI");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "https:");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "products.office.com");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "en-us");

      nextToken = helper.GetNextToken();
      Assert.IsTrue(nextToken == "word");
    }

    [TestMethod]
    public void GetNextGenericNameSuccess()
    {
      string input = "/Enum3//";
      Span<byte> buffer = new byte[input.Length];
      for (int i = 0; i < input.Length; i++)
        buffer[i] = (byte)input[i];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      GenericEnum f = helper.GetNextName<GenericEnum>();
      Assert.IsTrue(f == GenericEnum.Enum3);
    }

    [TestMethod]
    public void GetNextGenericNameDefault()
    {
      string input = "/Enum0//";
      Span<byte> buffer = new byte[input.Length];
      for (int i = 0; i < input.Length; i++)
        buffer[i] = (byte)input[i];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      GenericEnum f = helper.GetNextName<GenericEnum>();
      Assert.IsTrue(f == GenericEnum.Enum1);
    }

    [TestMethod]
    public void GetNextGenericNameListSuccess()
    {
      string input = "[ /Enum1 /Enum2 ]";
      Span<byte> buffer = new byte[input.Length];
      for (int i = 0; i < input.Length; i++)
        buffer[i] = (byte)input[i];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      List<GenericEnum> f = helper.GetListOfNames<GenericEnum>();
      Assert.IsTrue(f[0] == GenericEnum.Enum1);
      Assert.IsTrue(f[1] == GenericEnum.Enum2);
    }

    [TestMethod]
    public void GetNextNameFilterTest()
    {
      string input = "/LZWDecode//";
      Span<byte> buffer = new byte[input.Length];
      for (int i = 0; i < input.Length; i++)
        buffer[i] = (byte)input[i];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      Filter f = helper.GetNextName<Filter>();
      Assert.IsTrue(f == Filter.LZWDecode);
}

    [TestMethod]
    public void GetNextRectangle()
    {
      string input = "[92 355.7 189.9 371.4]";
      Span<byte> buffer = new byte[input.Length];
      for (int i = 0; i < input.Length; i++)
        buffer[i] = (byte)input[i];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      Rect r = helper.GetNextRectangle();
      Rect testR = new Rect();
      testR.FillRect(92, 355.7, 189.9, 371.4);
      Assert.IsTrue(r == testR);
    }

    [TestMethod]
    public void GetNextFloat32WholeSuccess()
    {
      string input = "127";
      Span<byte> buffer = new byte[input.Length];
      for (int i = 0; i < input.Length; i++)
        buffer[i] = (byte)input[i];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      double f = helper.GetNextDouble();
      Assert.IsTrue(f == 127);
    }

    [TestMethod]
    public void GetNextFloat32SignificantAndBaseSuccess()
    {
      string input = "127.523";
      Span<byte> buffer = new byte[input.Length];
      for (int i = 0; i < input.Length; i++)
        buffer[i] = (byte)input[i];
      SpanParseHelper helper = new SpanParseHelper(ref buffer);

      double f = helper.GetNextDouble();
      Assert.IsTrue(f == 127.523);
    }
  }
}
