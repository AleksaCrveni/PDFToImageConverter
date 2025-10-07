namespace Converter.FileStructures.General
{
  public struct FakeSpan
  {
    public int Position;
    public int Length;
    public FakeSpan(int position, int length)
    {
      Position = position;
      Length = length;
    }
  }
}
