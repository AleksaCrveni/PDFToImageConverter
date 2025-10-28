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
    protected byte[] _outputBuffer;
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
       
    
    public abstract void PDF_DrawText(string font, string textToWrite, PDFGI_DrawState state, int positionAdjustment = 0);

    public virtual PDF_FontData GetFontDataFromKey(string searchKey)
    {
      foreach (PDF_FontData fd in _fontDataRecords)
        if (fd.Key == searchKey)
          return fd;

      return new PDF_FontData();
    }

    // TODO: optimize
    public void ComputeTextRenderingMatrix(PDFGI_TextObject currentTextObject, double[,] CTM, ref double[,] textRenderingMatrix)
    {
      // Set initial value to first matrix
      double[,] identity = new double[3, 3];
      identity[0, 0] = currentTextObject.FontScaleFactor * currentTextObject.Th;
      identity[0, 1] = 0;
      identity[0, 2] = 0;
      identity[1, 0] = 0;
      identity[1, 1] = currentTextObject.FontScaleFactor;
      identity[1, 2] = 0;
      identity[2, 0] = 0;
      identity[2, 1] = currentTextObject.TRise;
      identity[2, 2] = 1;

      double[,] mid = new double[3, 3];
      MyMath.MultiplyMatrixes3x3(identity, currentTextObject.TextMatrix, ref mid);
      MyMath.MultiplyMatrixes3x3(mid, CTM, ref textRenderingMatrix);
    }

    public abstract void Save();
  }
}
