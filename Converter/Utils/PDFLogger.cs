
using System.Text;

namespace Converter.Utils
{
  public class PDFLogger
  {
    StringBuilder _buffer;
    public PDFLogger()
    {
      _buffer = new StringBuilder();
    }

    public void Log(string l) => _buffer.Append(l);
    public void Log(char c) => _buffer.Append(c);
    
    public void LineToLog(double x, double y)
    {
      Log(" LINE ");
      Log(x.ToString());
      Log(' ');
      Log(y.ToString());
    }
    
    public void MoveToLog(double x, double y)
    {
      Log(" MOVE ");
      Log(x.ToString());
      Log(' ');
      Log(y.ToString());
    }

    public void CloseShape(double x, double y)
    {
      MoveToLog(x, y);
      Log(" PATH_CLOSED ");
    }

    public void Dump(string path)
    {
      File.WriteAllText(path, _buffer.ToString());
      Clear();
    }
    public void Clear() => _buffer.Clear();
    public override string ToString() => _buffer.ToString();
  }
}
