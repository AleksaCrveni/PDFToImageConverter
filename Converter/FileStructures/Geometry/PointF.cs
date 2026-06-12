namespace Converter.FileStructures.Geometry
{
  public struct PointF
  {
    public float X;
    public float Y;
    public PointF(float x, float y)
    {
      X = x;
      Y = y;
    }
    // TODO: Test these
    public static bool operator ==(PointF a, PointF b)
    {
      return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(PointF a, PointF b)
    {
      return !(a.X == b.X && a.Y == b.Y);
    }
  }
}
