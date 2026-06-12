using Converter.FileStructures.Geometry;
using Converter.Utils;
using System.Diagnostics;

namespace Tester.GeometryTests
{
  [TestClass]
  public class RectangleIntersectionTests
  {

    [TestMethod]
    public void TestDoNotTouchALeftB()
    {
      Rectangle a = new Rectangle(0, 10, 10, 10);
      Rectangle b = new Rectangle(11, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestDoNotTouchABellowB()
    {
      Rectangle a = new Rectangle(0, 21, 10, 10);
      Rectangle b = new Rectangle(0, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestDoNotTouchARightB()
    {
      Rectangle a = new Rectangle(11, 10, 10, 10);
      Rectangle b = new Rectangle(0, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestDoNotTouchAAboveB()
    {
      Rectangle a = new Rectangle(0, 10, 10, 10);
      Rectangle b = new Rectangle(11, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }

    [TestMethod]
    public void TestDoTouchALeftB()
    {
      Rectangle a = new Rectangle(0, 10, 10, 10);
      Rectangle b = new Rectangle(10, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestDoTouchABellowB()
    {
      Rectangle a = new Rectangle(0, 20, 10, 10);
      Rectangle b = new Rectangle(0, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestDoTouchARightB()
    {
      Rectangle a = new Rectangle(10, 10, 10, 10);
      Rectangle b = new Rectangle(0, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestDoTouchAAboveB()
    {
      Rectangle a = new Rectangle(0, 10, 10, 10);
      Rectangle b = new Rectangle(10, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestOverlap()
    {
      Rectangle a = new Rectangle(0, 10, 10, 10);
      Rectangle b = new Rectangle(0, 10, 10, 10);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestFullInsideAinB()
    {
      // A is inside B
      Rectangle a = new Rectangle(1, 10, 5, 5);
      Rectangle b = new Rectangle(0, 20, 20, 20);
      Rectangle original = a;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }

    [TestMethod]
    public void TestFullInsideBinA()
    {
      // B is inside A
      Rectangle a = new Rectangle(0, 20, 20, 20);
      Rectangle b = new Rectangle(1, 10, 5, 5);
      Rectangle original = b;
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestLowerLeftOnLeftSide()
    {
      // B lowerLeft is on left side of A
      Rectangle a = new Rectangle(0, 60, 20, 20);
      Rectangle b = new Rectangle(0, 50, 10, 25);
      Rectangle original = new Rectangle(0, 50, 10, 10);
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, original));
    }
    [TestMethod]
    public void TestIntersection1()
    {
      // same lowerleft but A upperRight is bigger
      Rectangle a = new Rectangle(0, 10, 10, 10);
      Rectangle b = new Rectangle(0, 10, 5, 5);
      Rectangle newRect = new Rectangle(0, 10, 5, 5);
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, newRect));
    }
    [TestMethod]
    public void TestIntersection2()
    {
      // same lowerleft but B upperRight is bigger
      Rectangle a = new Rectangle(0, 10, 10, 10);
      Rectangle b = new Rectangle(0, 10, 15, 15);
      Rectangle newRect = new Rectangle(0, 10, 10, 10);
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, newRect));
    }

    [TestMethod]
    public void TestIntersection3()
    {
      // same uperright but A lowerRight is bigger
      Rectangle a = new Rectangle(0, 20, 10, 20);
      Rectangle b = new Rectangle(0, 10, 10, 10);
      Rectangle newRect = new Rectangle(0, 10, 10, 10);
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, newRect));
    }
    [TestMethod]
    public void TestIntersection4()
    {
      // same uperright but B lowerRight is bigger
      Rectangle a = new Rectangle(0, 10, 10, 10);
      Rectangle b = new Rectangle(0, 15, 10, 15);
      Rectangle newRect = new Rectangle(0, 10, 10, 10);
      Geometry.IntersectRect(ref a, ref b);
      Assert.IsTrue(CompareRectangles(a, newRect));
    }
    public bool CompareRectangles(Rectangle a, Rectangle b)
    {
      return ComparePoints(a.LowerLeft, b.LowerLeft)
          && ComparePoints(a.UpperRight, b.UpperRight)
          && a.Width == b.Width
          && a.Height == b.Height;
    }


    public bool ComparePoints(PointF a, PointF b)
    {
      return a.X == b.X && a.Y == b.Y;
    }
  }
}
