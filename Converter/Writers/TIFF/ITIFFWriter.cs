namespace Converter.Writers.TIFF
{
  public interface ITIFFWriter
  {
    void WriteEmptyImage(ref TIFFWriterOptions options);
    void WriteIntoImageData(int x, int y, int[,] bitmap);
  }
}
