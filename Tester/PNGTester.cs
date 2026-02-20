using Converter.FileStructures.PNG;
using Converter.Parsers.Images.PNG;
using Converter.Writers.PNG;

namespace Tester
{
  [TestClass]
  public class PNGTester
  {
    [TestMethod]
    public void TestRandomGrayscale8BitDepth()
    {
      PNGFile file = new PNGFile();
      file.Height = 720;
      file.Width = 1240;
      file.Interlance = PNG_INTERLANCE.NONE;
      file.Compression = PNG_COMPRESSION.DEFLATE;
      file.ColorType = PNG_COLOR_TYPE.GRAYSCALE;
      file.BitDepth = 8;
      PNGWriter.Write("Grayscale8Test.png", file);
    }

    [TestMethod]
    public void TestRandomPallete8BitDepth()
    {
      PNGFile file = new PNGFile();
      file.Height = 720;
      file.Width = 1240;
      file.Interlance = PNG_INTERLANCE.NONE;
      file.Compression = PNG_COMPRESSION.DEFLATE;
      file.ColorType = PNG_COLOR_TYPE.PALLETE;
      file.BitDepth = 8;
      PNGWriter.Write("Pallete8Test.png", file);
    }
    [TestMethod]
    public void TestRandomTrueColor8BitDepth()
    {
      PNGFile file = new PNGFile();
      file.Height = 720;
      file.Width = 1240;
      file.Interlance = PNG_INTERLANCE.NONE;
      file.Compression = PNG_COMPRESSION.DEFLATE;
      file.ColorType = PNG_COLOR_TYPE.TRUECOLOR;
      file.BitDepth = 8;
      PNGWriter.Write("TrueColor8Test.png", file);
    }
    [TestMethod]
    public void TestRandomGrayscaleAlpha8BitDepth()
    {
      PNGFile file = new PNGFile();
      file.Height = 720;
      file.Width = 1240;
      file.Interlance = PNG_INTERLANCE.NONE;
      file.Compression = PNG_COMPRESSION.DEFLATE;
      file.ColorType = PNG_COLOR_TYPE.GRAYSCALE_ALPHA;
      file.BitDepth = 8;
      PNGWriter.Write("GrayscaleAlpha8Test.png", file);
    }
    [TestMethod]
    public void TestRandomTrueColorAlpha8BitDepth()
    {
      PNGFile file = new PNGFile();
      file.Height = 720;
      file.Width = 1240;
      file.Interlance = PNG_INTERLANCE.NONE;
      file.Compression = PNG_COMPRESSION.DEFLATE;
      file.ColorType = PNG_COLOR_TYPE.TRUECOLOR_ALPHA;
      file.BitDepth = 8;
      PNGWriter.Write("TrueColorAlpha8Test.png", file);
    }

  }
}
