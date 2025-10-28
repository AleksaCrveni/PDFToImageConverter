using Converter.FileStructures.PDF.GraphicsInterpreter;

namespace Converter.Converters
{
  // Support draw text from any file type
  public interface IConverter
  {
    public void PDF_DrawText(string font, string textToWrite, PDFGI_DrawState state, int positionAdjustment = 0);
    public void SetupConverter();
    public void ComputeTextRenderingMatrix(PDFGI_TextObject currentTextObject, double[,] CTM, ref double[,] textRenderingMatrix);
    public void Save();
  }
}
