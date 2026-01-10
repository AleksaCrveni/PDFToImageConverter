using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.Rasterizers;
using Converter.Writers.TIFF;
namespace Converter.Converters
{
  public abstract class AConverter : IConverter
  {
    protected TargetConversion _target;
    protected SourceConversion _source;
    protected List<PDF_FontData> _fontDataRecords;
    protected PDF_ResourceDict _rDict;
    protected PDF_PageInfo _pInfo;
    protected TIFFWriterOptions _options;
    protected Stream _outputStream;

    public AConverter(List<PDF_FontData> fontDataRecords, PDF_ResourceDict rDict, PDF_PageInfo pInfo, SourceConversion source, TIFFWriterOptions options)
    {
      _fontDataRecords = fontDataRecords;
      _rDict = rDict;
      _pInfo = pInfo;
      _source = source;
      _options = options;
      SetupConverter();
    }
    public abstract void SetupConverter();

    public abstract void Save(byte[] buffer);

    public virtual int GetHeight() => _options.Height;
    public virtual int GetWidth() => _options.Width;
  }
}
