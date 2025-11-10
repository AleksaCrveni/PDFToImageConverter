namespace Converter.FileStructures.General
{
  public class FakeSpan
  {
    public int Position;
    public int Length;
    public FakeSpan() { }
    public FakeSpan(int position, int length)
    {
      Position = position;
      Length = length;
    }

    public void SetData(int position, int length)
    {
      Position = position;
      Length = length;
    }
  }
}
