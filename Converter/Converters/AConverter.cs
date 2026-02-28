using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.Rasterizers;
using Converter.Writers.TIFF;
namespace Converter.Converters
{
  public abstract class AConverter : IConverter
  {
    protected TargetConversion __target;
    protected SourceConversion __source;
    protected List<PDF_FontData> __fontDataRecords;
    protected PDF_ResourceDict __rDict;
    protected PDF_PageInfo __pInfo;
    protected TIFFWriterOptions __options;
    protected Stream __outputStream;

    public AConverter(List<PDF_FontData> fontDataRecords, PDF_ResourceDict rDict, PDF_PageInfo pInfo, SourceConversion source, TIFFWriterOptions options)
    {
      __fontDataRecords = fontDataRecords;
      __rDict = rDict;
      __pInfo = pInfo;
      __source = source;
      __options = options;
      SetupConverter();
    }
    public abstract void SetupConverter();

    public abstract void Save(byte[] buffer);

    public virtual int GetHeight() => __options.Height;
    public virtual int GetWidth() => __options.Width;
  }
}
