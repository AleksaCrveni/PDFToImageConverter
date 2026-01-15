namespace Converter.FileStructures.Type1
{
  public class TYPE1_Point2D
  {
    public float X;
    public float Y;
    public bool PartOfPath;
    public TYPE1_Point2D() { }

    public TYPE1_Point2D(float x, float y)
    {
      X = x;
      Y = y;
      PartOfPath = false;
    }

    public TYPE1_Point2D(int x, int y, bool partOfPath)
    {
      X = x;
      Y = y;
      PartOfPath = partOfPath;
    }
  }
}
