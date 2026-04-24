using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Utils;
using System.Buffers.Binary;

namespace Converter.Rasterizers
{
  // Currently implemented for Type1 Adobe font
  // TODO: Way later see if List is appropriate DS due to double sizing when growing 
  // Cubic bezier
  public class PSShape
  {
    public List<PS_COMMAND> _moves;
    public List<double> _shapePoints;
    public TYPE1_Point2D _width;
    
    // Temp
    public List<PointF> _windings;
    public List<int> _windingLengths;
    public int _windingCount;
    public int _xMin;
    public int _yMin;

    // this may differ than last in _moves because we may turn one PS_COMMAND into multiple other ones
    // For now we do this for Type1Interpreter so that we can detect if last move is charend becuse we turn it into LineTo & MoveTo
    public PS_COMMAND _actualLast;
    public PSShape()
    {
      _moves = new List<PS_COMMAND>(32);
      _shapePoints = new List<double>(32 * 4);
      _width = new TYPE1_Point2D();
    }

    public void LineTo(double dx, double dy)
    {
      _shapePoints.Add(dx);
      _shapePoints.Add(dy);
      _moves.Add(PS_COMMAND.LINE_TO);
      _actualLast = PS_COMMAND.LINE_TO;
    }

    public void MoveTo(double dx, double dy)
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

    public void CloseShape()
    {
      if (_moves.Count > 0 && _actualLast == PS_COMMAND.CLOSEPATH)
        return;

      int pos = _shapePoints.Count - 1;
      double targetX = 0;
      double targetY = 0;
      // get last move to
      for (int j = _moves.Count - 1; j >= 0; j--)
      {
        PS_COMMAND move = _moves[j];
        if (move == PS_COMMAND.CUBIC_CURVE_TO)
        {
          pos -= 6;
        }
        else if (move == PS_COMMAND.QUAD_CURVE_TO)
        {
          pos -= 4;
        }
        else if (move == PS_COMMAND.LINE_TO)
        {
          pos -= 2;
        }
        else if (move == PS_COMMAND.MOVE_TO)
        {
          targetY = _shapePoints[pos--];
          targetX = _shapePoints[pos];
          break;
        }
      }

      LineTo(targetX, targetY); // draw line
      _actualLast = PS_COMMAND.CLOSEPATH; // assign close path so that we can check if last one was closepath
    }
    public void CurveTo(double dx1, double dy1, double dx2, double dy2, double dx3, double dy3)
    {
      _shapePoints.Add(dx1);
      _shapePoints.Add(dy1);
      _shapePoints.Add(dx2);
      _shapePoints.Add(dy2);
      _shapePoints.Add(dx3);
      _shapePoints.Add(dy3);
      _moves.Add(PS_COMMAND.CUBIC_CURVE_TO);
      _actualLast = PS_COMMAND.CUBIC_CURVE_TO;
    }

    /// <summary>
    /// Do this for compute it, or maybe we scale down each point when we add them and not all affter
    /// </summary>
    /// <param name="scale"></param>
    public void ScaleAll(double scale)
    {
      for (int i = 0; i < _shapePoints.Count; i++)
        _shapePoints[i] = _shapePoints[i] * scale;
    }

    // Positions are absolute so reader should anticipate that
    public void SaveAbsolute(string name)
    {
#if RELEASE
          return;
#endif
      FileStream fs = File.Create(Path.Join(Files.RootFolder, $"PS_SHAPE_{name}.shape"));
      byte[] mem = new byte[8096]; // limit for now for 8k shapes
      Span<byte> buffer = mem.AsSpan();
      int pos = 0;
      PositionIncrBufferWriter writer = new PositionIncrBufferWriter(ref buffer, true);
      bool absolute = true;
      buffer[pos++] = (byte)(absolute ? 1 : 0);
      // width data
      writer.WriteDouble(ref pos, _width.X);
      writer.WriteDouble(ref pos, _width.Y);
      buffer[pos++] = (byte)(_width.PartOfPath ? 1 : 0);
      writer.WriteSigned32ToBuffer(ref pos, _moves.Count); // move count
      for (int i = 0; i < _moves.Count; i++)
      {
        buffer[pos++] = (byte)_moves[i];
      }
      writer.WriteSigned32ToBuffer(ref pos, _shapePoints.Count);
      fs.Write(buffer.Slice(0, pos));
      pos = 0;

      for (int i = 0; i < _shapePoints.Count; i++)
      {
        writer.WriteDouble(ref pos, _shapePoints[i]);
        if (pos > buffer.Length - 24) // 24 is arbitary
        {
          fs.Write(buffer.Slice(0, pos));
          pos = 0;
        }
      }
      // leftover
      if (pos != 0)
      {
        fs.Write(buffer.Slice(0, pos));
      }

      fs.Flush();
      fs.Close();
      fs.Dispose();
    }

    /// <summary>
    ///  not efficient and we trusdt input too much but its ok for now just get it to work
    /// </summary>
    /// <param name="buffer"></param>
    public void LoadData(byte[] data)
    {
      int pos = 0;
      bool absolute = data[pos++] == 1 ? true : false;
      Span<byte> buffer = data.AsSpan();
      _width = new TYPE1_Point2D();
      _width.X = BufferReader.ReadDoubleLE(ref buffer, ref pos);
      _width.Y = BufferReader.ReadDoubleLE(ref buffer, ref pos);
      _width.PartOfPath = buffer[pos++] == 1 ? true : false;

      int moveCount = BufferReader.ReadInt32LE(ref buffer, ref pos);
      for (int i = 0; i < moveCount; i++)
      {
        _moves.Add((PS_COMMAND)buffer[pos++]);
      }
      int shapePointsCount = BufferReader.ReadInt32LE(ref buffer, ref pos);
      for (int i = 0; i < shapePointsCount; i++)
      {
        _shapePoints.Add(BufferReader.ReadDoubleLE(ref buffer, ref pos));
      };
    }
  }
}
