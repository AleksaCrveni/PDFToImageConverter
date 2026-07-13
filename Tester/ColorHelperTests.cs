using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.Utils;

namespace Tester
{
  [TestClass]
  public class ColorHelperTests
  {
    [TestMethod]
    public void TestCMYKtoRGBIntensityConversionBasic()
    {
      MyColor c = new MyColor();
      ColorHelper.ConvertCMYKtoRGBbyIntensity(0, 0, 1, 0, c);
      Assert.AreEqual(1, c.R);
      Assert.AreEqual(1, c.G);
      Assert.AreEqual(0, c.B);
      Assert.AreEqual(1, c.A);
    }

    [TestMethod]
    public void TestCMYKtoRGBIntensityConversionComplex()
    {
      MyColor c = new MyColor();
      ColorHelper.ConvertCMYKtoRGBbyIntensity(0, 81/100d, 100/100d, 52/100d, c);
      Assert.AreEqual(Math.Round(122/255d, 2), Math.Round(c.R, 2));
      Assert.AreEqual(Math.Round(23/255d, 2), Math.Round(c.G,2));
      Assert.AreEqual(0, c.B);
      Assert.AreEqual(1, c.A);
    }
  }
}
