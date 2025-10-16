using Converter;
using Converter.Rasterizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
  [TestClass]
  public sealed class TIFFParserTests
  {
    [TestMethod]
    public void ParseTest()
    {
      STBTrueType parser = new STBTrueType();
      parser.Parse(Files.BilevelTiff);
    }

    [TestMethod]
    public void ParseMyTiff()
    {
      STBTrueType parser = new STBTrueType();
      parser.Parse(Files.CreateTestTiff);
    }
  }
}
