namespace Converter.FileStructures.Type1
{
  public class TYPE1_Point2D
  {
    public double X;
    public double Y;
    public bool PartOfPath;
    public TYPE1_Point2D() { }

    public TYPE1_Point2D(double x, double y)
    {
      X = x;
      Y = y;
      PartOfPath = false;
    }

    public TYPE1_Point2D(double x, double y, bool partOfPath)
    {
      X = x;
      Y = y;
      PartOfPath = partOfPath;
    }
  }
}
