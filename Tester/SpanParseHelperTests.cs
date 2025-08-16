using Converter;
using Converter.Parsers;
using System.Diagnostics;

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
      Debug.Assert(nextToken == "Type");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "Annot");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "Subtype");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "Link");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "Border");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "0");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "0");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "0");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "Rect");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "92");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "355.7");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "189.9");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "371.4");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "A");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "Type");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "Action");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "S");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "URI");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "URI");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "https:");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "products.office.com");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "en-us");

      nextToken = helper.GetNextToken();
      Debug.Assert(nextToken == "word");
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
      Debug.Assert(f == GenericEnum.Enum3);
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
      Debug.Assert(f == GenericEnum.Enum1);
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
      Debug.Assert(f[0] == GenericEnum.Enum1);
      Debug.Assert(f[1] == GenericEnum.Enum2);
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
      Debug.Assert(f == Filter.LZWDecode);
    }
  }
}
