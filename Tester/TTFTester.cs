using Converter.Parsers.Fonts;

namespace Tester
{
  [TestClass]
  public sealed class TTFTester
  {
    [TestMethod]
    public void TTFParserTestWindowsFont()
    {
      TTFParser parser = new TTFParser();
      byte[] arr = File.ReadAllBytes("C:/Windows/Fonts/arial.ttf");
      parser.Init(ref arr);
      int bitmapWidth = 1024;
      int bitmapHeight = 256;
      int lineHeight = 64;

      byte[] bitmap = new byte[bitmapHeight * bitmapWidth];
      float scaleFactor = parser.ScaleForPixelHeight(lineHeight);
      string textToTranslate = "this is text";
      int x = 0;
      // ascent and descent are defined in font descriptor, use those I think over getting i from  the font
      int ascent = 0;
      int descent = 0;
      int lineGap = 0;
      parser.GetFontVMetrics(ref ascent, ref descent, ref lineGap);
      ascent = (int)Math.Round(ascent * scaleFactor);
      descent = (int)Math.Round(descent * scaleFactor);
      int baseline = 0;

      for (int i = 0; i < textToTranslate.Length; i++)
      {
        int ax = 0; // charatcter width
        int lsb = 0; // left side bearing

        parser.GetCodepointHMetrics(textToTranslate[i], ref ax, ref lsb);
        //stbtt_GetGlyphHMetrics(&info, )

        int c_x0 = 0;
        int c_y0 = 0;
        int c_x1 = 0;
        int c_y1 = 0;
        parser.GetCodepointBitmapBox(textToTranslate[i], scaleFactor, scaleFactor, ref c_x0, ref c_y0, ref c_x1, ref c_y1);

        // char height
        int y = ascent + c_y0 + baseline;

        int byteOffset = x + (int)Math.Round(lsb * scaleFactor) + (y * bitmapWidth);
        parser.MakeCodepointBitmap(ref bitmap, byteOffset, c_x1 - c_x0, c_y1 - c_y0, bitmapWidth, scaleFactor, scaleFactor, textToTranslate[i]);

        // advance x
        x += (int)Math.Round(ax * scaleFactor);

        // kerning

        int kern;
        kern = parser.GetCodepointKernAdvance(textToTranslate[i], textToTranslate[i + 1]);
        x += (int)Math.Round(kern * scaleFactor);
      }
    }
  }
}
