using Converter.FileStructures.PostScript;
using System.Reflection.Metadata.Ecma335;

namespace Converter.Rasterizers
{
  // Currently implemented for Type1 Adobe font
  // TODO: Way later see if List is appropriate DS due to double sizing when growing 
  // Cubic bezier
  public class Shape
  {
    public List<PS_COMMAND> _moves;
    public List<float> _shapePoints;
    public Shape()
    {

    }

    public void LineTo(float dx, float dy)
    {
      _shapePoints.Add(dx);
      _shapePoints.Add(dy);
      _moves.Add(PS_COMMAND.LINE_TO);
    }

    public void MoveTo(float dx, float dy)
    {
      if (_moves.Count > 0 && _moves.Last() == PS_COMMAND.MOVE_TO)
      {
        int len = _shapePoints.Count;
        _shapePoints[len - 2] = dx;
        _shapePoints[len - 1] = dy;
      } 
      else
      {
        _shapePoints.Add(dx);
        _shapePoints.Add(dy);
        _moves.Add(PS_COMMAND.MOVE_TO);
      }
    }

    public void CurveTo(float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
    {
      _shapePoints.Add(dx1);
      _shapePoints.Add(dy1);
      _shapePoints.Add(dx2);
      _shapePoints.Add(dy2);
      _shapePoints.Add(dx3);
      _shapePoints.Add(dy3);
      _moves.Add(PS_COMMAND.CURVE_TO);
    }

    public void DrawIntoBitmap(float scale)
    {

    }
  }
}
