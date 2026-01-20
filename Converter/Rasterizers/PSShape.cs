using Converter.FileStructures.PostScript;
using Converter.FileStructures.Type1;
using System.Reflection.Metadata.Ecma335;

namespace Converter.Rasterizers
{
  // Currently implemented for Type1 Adobe font
  // TODO: Way later see if List is appropriate DS due to double sizing when growing 
  // Cubic bezier
  public class PSShape
  {
    public List<PS_COMMAND> _moves;
    public List<float> _shapePoints;
    public TYPE1_Point2D _width;
    // this may differ than last in _moves because we may turn one PS_COMMAND into multiple other ones
    // For now we do this for Type1Interpreter so that we can detect if last move is charend becuse we turn it into LineTo & MoveTo
    public PS_COMMAND _actualLast;
    public PSShape()
    {
      _moves = new List<PS_COMMAND>();
      _shapePoints = new List<float>();
      _width = new TYPE1_Point2D();
    }

    public void LineTo(float dx, float dy)
    {
      _shapePoints.Add(dx);
      _shapePoints.Add(dy);
      _moves.Add(PS_COMMAND.LINE_TO);
      _actualLast = PS_COMMAND.LINE_TO;
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
        _actualLast = PS_COMMAND.MOVE_TO;
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
      _actualLast = PS_COMMAND.CURVE_TO;
    }
  }
}
