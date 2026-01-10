using Converter.FileStructures.PDF.GraphicsInterpreter;

namespace Converter.Converters
{
  // Support draw text from any file type
  public interface IConverter
  {
    public void SetupConverter();
    public void Save(byte[] buffer);
    public int GetWidth();
    public int GetHeight();
  }
}
