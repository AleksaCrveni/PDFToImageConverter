using Converter;
using Converter.Parsers;
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
      TIFFParser parser = new TIFFParser();
      parser.Parse(Files.BilevelTiff);
    }
  }
}
