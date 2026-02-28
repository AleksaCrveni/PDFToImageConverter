using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.Writers.TIFF;
namespace Converter.Converters.Image.TIFF
{
  public class TIFFGrayscaleConverter : AConverter
  {
    private ITIFFWriter _writer;
    public TIFFGrayscaleConverter(List<PDF_FontData> fontDataRecords, PDF_ResourceDict rDict, PDF_PageInfo pInfo, SourceConversion source, TIFFWriterOptions options)
      : base(fontDataRecords, rDict, pInfo, source, options) { }

    public override void SetupConverter()
    {
      int width = (int)__pInfo.MediaBox.urX;
      int height = (int)__pInfo.MediaBox.urY;
      // make sure that buffer is big enough;
      if (__options.Width == 0 || __options.Width < width)
        __options.Width = width;

      if (__options.Height == 0 || __options.Height < height)
        __options.Height = height;

      // temp workaround
      long rnd = Random.Shared.NextInt64();
      if (Directory.Exists("TestOutput"))
        _writer = new TIFFGrayscaleWriter($"TestOutput/{rnd}_convertTest.tiff");
      else
        _writer = new TIFFGrayscaleWriter($"convertTest.tiff");
      TIFFWriterOptions tiffOptions = new TIFFWriterOptions()
        {
          Width = __options.Width,
          Height = __options.Height
        };
    }

    public override void Save(byte[] buffer)
    {

      TIFFWriterOptions tiffOptions = new TIFFWriterOptions()
      {
        Width = __options.Width,
        Height = __options.Height
      };
      _writer.WriteImageWithBuffer(ref tiffOptions, buffer);
    }
  }
}

