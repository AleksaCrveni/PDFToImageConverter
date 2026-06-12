using Converter.FileStructures.General;
using Converter.FileStructures.Geometry;
using System.Diagnostics;

namespace Converter.Utils
{
  public static class Geometry
  {
    /// <summary>
    /// Intersects rectangles and turns rectangle A into the result
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static void IntersectRect(ref Rectangle a, ref Rectangle b)
    {
      // 1. Check cases where rectangles obviously don't intersect or just touch opposing borders
      // i.e b is left of A or right side of B touches left side of A

      if (b.UpperRight.X <= a.LowerLeft.X) // B is left of A or B right side touches A left side
        return;
      else if (b.UpperRight.Y >= a.LowerLeft.Y) // B is bellow A or B top is touching A bottom side
        return;
      else if (b.LowerLeft.X >= a.UpperRight.X) // B is right of A or B left side is touching A right side
        return;
      else if (b.LowerLeft.Y <= a.UpperRight.Y) // B is top of A or B bottom side is touching A top side
        return;

      // 2. Check if rectangles overlap
      if (b.UpperRight.X == a.UpperRight.X
       && b.UpperRight.Y == a.UpperRight.Y
       && b.LowerLeft.X == a.LowerLeft.X
       && b.LowerLeft.Y == a.LowerLeft.Y)
        return;

      PointF newLowerLeft = new PointF();
      PointF newUpperRight = new PointF();
      // 3. Get lower left point

      if (b.LowerLeft.X > a.LowerLeft.X)
        newLowerLeft.X = b.LowerLeft.X;
      else
        newLowerLeft.X = a.LowerLeft.X;

      if (b.LowerLeft.Y < a.LowerLeft.Y)
        newLowerLeft.Y = b.LowerLeft.Y;
      else
        newLowerLeft.Y = a.LowerLeft.Y;
      // 4. Get upper right point

      if (b.UpperRight.X < a.UpperRight.X)
        newUpperRight.X = b.UpperRight.X;
      else
        newUpperRight.X = a.UpperRight.X;

      if (b.UpperRight.Y > a.UpperRight.Y)
        newUpperRight.Y = b.UpperRight.Y;
      else
        newUpperRight.Y = a.UpperRight.Y;

      // 5. Compute new height width of A
      a.LowerLeft = newLowerLeft;
      a.UpperRight = newUpperRight;
      a.Width = newUpperRight.X - newLowerLeft.X;
      a.Height = newLowerLeft.Y - newUpperRight.Y;
      Debug.Assert(a.Width > 0);
      Debug.Assert(a.Height > 0);
    }

  }
}
