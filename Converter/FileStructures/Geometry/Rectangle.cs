
namespace Converter.FileStructures.Geometry
{
  /// <summary>
  /// Difference between PDF_Rect and this is that we will use this for geometry ops and rasterizing
  /// and PDF_Rect just stores data from PDFFile
  /// Origin is TOP-LEFT, so height and Y coordinates must be passed convertered correctly if needed, by the called
  /// </summary>
  public struct Rectangle
  {
    public PointF LowerLeft;
    public PointF UpperRight;
    // these 2 fields might be redundant
    public float Height;
    public float Width;

    public Rectangle(float llX, float llY, float width, float height)
    {
      LowerLeft = new PointF(llX, llY);
      UpperRight = new PointF(llX + width, llY - height);
      Height = height;
      Width = width;
    }
    
  }
}
