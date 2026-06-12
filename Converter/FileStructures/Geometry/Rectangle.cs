
namespace Converter.FileStructures.Geometry
{
  /// <summary>
  /// Difference between PDF_Rect and this is that we will use this for geometry ops and rasterizing
  /// and PDF_Rect just stores data from PDFFile
  /// Origin is TOP-LEFT, so height and Y coordinates must be passed convertered correctly if needed, by the called
  /// </summary>
  public struct Rectangle
  {
    public PointF UpperLeft;
    public PointF UpperRight;
    public PointF LowerLeft;
    public PointF LowerRight;

    public Rectangle(float llX, float llY, float height, float width)
    {
      LowerLeft = new PointF(llX, llY);
      LowerRight = new PointF(llX + width, llY);
      UpperRight = new PointF(llX + width, llY - height);
      UpperLeft = new PointF(llX, llY - height);
    }
    
  }
}
