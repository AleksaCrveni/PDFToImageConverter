using Converter.FileStructures.PNG;

namespace Converter.Utils.PNG
{
  public static class PNGHelper
  {
    public static byte GetBitsPerPixel(PNG_COLOR_SCHEME colorScheme, byte bitDepth) => colorScheme switch
    {
      PNG_COLOR_SCHEME.G1 or
      PNG_COLOR_SCHEME.G2 or
      PNG_COLOR_SCHEME.G4 or
      PNG_COLOR_SCHEME.G8 or
      PNG_COLOR_SCHEME.G16 or
      PNG_COLOR_SCHEME.P1 or
      PNG_COLOR_SCHEME.P2 or
      PNG_COLOR_SCHEME.P4 or
      PNG_COLOR_SCHEME.P8 => bitDepth,
      PNG_COLOR_SCHEME.GA8 => 16, // 8 Grayscale 8 for Alpha
      PNG_COLOR_SCHEME.TC8 => 24,
      PNG_COLOR_SCHEME.GA16 => 32, // 16 grayscale 16 for Alpha
      PNG_COLOR_SCHEME.TCA8 => 32,
      PNG_COLOR_SCHEME.TC16 => 48,
      PNG_COLOR_SCHEME.TCA16 => 64,
    };
    public static int GetBytesPerPixel(byte bitsPerPixel)
    {
      return (int)Math.Ceiling(bitsPerPixel / 8f);
    }
    /// <summary>
    /// Includes prepended filter byte
    /// </summary>
    /// <param name="bitsPerPixel"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public static uint GetRowSize(byte bitsPerPixel, int width)
    {
      return (uint)Math.Ceiling((bitsPerPixel * width) / (decimal)8) + 1; // + 1 is for filter type on the start
    }
  }
}
