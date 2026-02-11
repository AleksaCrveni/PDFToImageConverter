using Converter;
using Converter.Parsers.Images.BMP;

namespace Tester
{
  [TestClass]
  public class BMPParserTests
  {
    // TODO: BMP is simple so try matching data later
    [TestMethod]
    public void ParseMonochromeNoExceptions()
    {
      BMPParser p = new BMPParser();
      p.Parse(Files.BMPMonochrome);
    }

    [TestMethod]
    public void Parse16bNoExceptions()
    {
      BMPParser p = new BMPParser();
      p.Parse(Files.BMP16b);
    }

    [TestMethod]
    public void Parse24bNoExceptions()
    {
      BMPParser p = new BMPParser();
      p.Parse(Files.BMP24b);
    }

    [TestMethod]
    public void Parse256NoExceptions()
    {
      BMPParser p = new BMPParser();
      p.Parse(Files.BMP256b);
    }
  }
}
