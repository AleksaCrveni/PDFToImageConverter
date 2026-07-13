using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.Writers.TIFF;

namespace Converter.Converters.Image.TIFF
{
  public class TIFFRGBConverter : AConverter
  {
    private ITIFFWriter _writer;
    public TIFFRGBConverter(List<PDF_FontData> fontDataRecords, PDF_ResourceDict rDict, PDF_PageInfo pInfo, SourceConversion source, TIFFWriterOptions options, Stream outStream)
     : base(fontDataRecords, rDict, pInfo, source, options, outStream) { }
    public override byte[] CreateBuffer()
    {
      byte[] buff = new byte[__options.Width * __options.Height * 3];
      Array.Fill<byte>(buff, 255);
      return buff;
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

    public override void SetupConverter()
    {
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
          _writer = new TIFFRGBWriter($"TestOutput/{rnd}_convertTest.tiff");
        else
          _writer = new TIFFRGBWriter(__outputStream);
        TIFFWriterOptions tiffOptions = new TIFFWriterOptions()
        {
          Width = __options.Width,
          Height = __options.Height
        };
      }
    }
  }
}
