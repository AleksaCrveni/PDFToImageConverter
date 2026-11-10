using Converter;
using Converter.Parsers.Images.TIFF;
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
      TIFFParser.Parse(Files.BilevelTiff);
    }

    [TestMethod]
    public void ParseMyTiff()
    {
      TIFFParser.Parse(Files.CreateTestTiff);
    }
  }
}
