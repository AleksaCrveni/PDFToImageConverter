using Converter.Parsers.Fonts;
using Converter.Writers.TIFF;

namespace Tester
{
  /// <summary>
  /// I am not really sure how to ensure that text was rasterized correctly so for now I will just check that it has no Exceptions and verify result manually
  /// </summary>
  [TestClass]
  public sealed class TTFTester
  {
    public readonly bool WriteImageOutput = true;
    public readonly bool WriteBitmapIndex = false;
    public readonly string WritePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\TestOutput";

    public TTFTester()
    {
      // just simple detection if im running in pipeline or in cloud, so we don't write stuff
      // for now i run tests only locally
      bool isLocal = Directory.Exists("C:\\FakeFolder");

      WriteImageOutput = isLocal ? false : WriteImageOutput;
      WriteBitmapIndex = isLocal ? false : WriteBitmapIndex;
    }
    public void Write(byte[] arr, int bitmapWidth, int bitmapHeight, int lineHeight, string textToTranslate, string imageName)
    {
      TTFParser parser = new TTFParser();
      parser.Init(ref arr);
      parser.InitFont();
      byte[] bitmap = new byte[bitmapHeight * bitmapWidth];
      float scaleFactor = parser.ScaleForPixelHeight(lineHeight);
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

        //int kern;
        //kern = parser.GetCodepointKernAdvance(textToTranslate[i], textToTranslate[i + 1]);
        //x += (int)Math.Round(kern * scaleFactor);
      }

      List<string> indexes = new List<string>();
      
      bool hasOne = false;
      for (int i = 0; i < bitmap.Length; i++)
        if (bitmap[i] > 0)
        {
          indexes.Add($"{i.ToString()}");
          hasOne = true;
          break;
        }
    
      if (!hasOne)
        throw new Exception("Something went wrong, bitmap empty!");

      if (WriteBitmapIndex)
        File.WriteAllLines($"{WritePath}/{imageName}_ind.txt", indexes);

      if (WriteImageOutput)
      {
        TIFFGrayscaleWriter writer = new TIFFGrayscaleWriter($"{WritePath}/{imageName}_img.tiff");
        var options = new TIFFWriterOptions()
        {
          Width = bitmapWidth,
          Height = bitmapHeight
        };

        writer.WriteImageWithBuffer(ref options, bitmap);
      }
    }

    [TestMethod]
    public void Arial_SingleLine()
    {
      byte[] arr = File.ReadAllBytes("C:/Windows/Fonts/arial.ttf");
      Write(arr, 1024, 256, 64, "ThIs iS 123!.?", "arial_singleline");
    }

    [TestMethod]
    public void Arial_i_SingleLine()
    {
      byte[] arr = File.ReadAllBytes("C:/Windows/Fonts/ariali.ttf");
      Write(arr, 1024, 256, 64, "ThIs iS 123!.?", "arial_i_singleline");
    }

    [TestMethod]
    public void Arial_bi_SingleLine()
    {
      byte[] arr = File.ReadAllBytes("C:/Windows/Fonts/arialbi.ttf");
      Write(arr, 1024, 256, 64, "ThIs iS 123!.?", "arial_bi_singleline");
    }


    [TestMethod]
    public void Calibri_SingleLine()
    {
      byte[] arr = File.ReadAllBytes("C:/Windows/Fonts/calibri.ttf");
      Write(arr, 1024, 256, 64, "ThIs iS 123!.?", "calibri_singleline");
    }

    [TestMethod]
    public void Calibri_i_SingleLine()
    {
      byte[] arr = File.ReadAllBytes("C:/Windows/Fonts/calibrii.ttf");
      Write(arr, 1024, 256, 64, "ThIs iS 123!.?", "calibri_i_singleline");
    }
    [TestMethod]
    public void Calibri_b_SingleLine()
    {
      byte[] arr = File.ReadAllBytes("C:/Windows/Fonts/calibrib.ttf");
      Write(arr, 1024, 256, 64, "ThIs iS 123!.?", "calibri_b_singleline");
    }
  }
}
