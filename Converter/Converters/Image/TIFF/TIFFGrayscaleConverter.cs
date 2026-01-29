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
      int width = (int)_pInfo.MediaBox.urX;
      int height = (int)_pInfo.MediaBox.urY;
      // make sure that buffer is big enough;
      if (_options.Width == 0 || _options.Width < width)
        _options.Width = width;

      if (_options.Height == 0 || _options.Height < height)
        _options.Height = height;

      // temp workaround
      long rnd = Random.Shared.NextInt64();
      if (Directory.Exists("TestOutput"))
        _writer = new TIFFGrayscaleWriter($"TestOutput/{rnd}_convertTest.tiff");
      else
        _writer = new TIFFGrayscaleWriter($"convertTest.tiff");
      TIFFWriterOptions tiffOptions = new TIFFWriterOptions()
        {
          Width = _options.Width,
          Height = _options.Height
        };
    }

    public override void Save(byte[] buffer)
    {

      TIFFWriterOptions tiffOptions = new TIFFWriterOptions()
      {
        Width = _options.Width,
        Height = _options.Height
      };
      _writer.WriteImageWithBuffer(ref tiffOptions, buffer);
    }
  }
}

