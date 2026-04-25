using Converter.FileStructures.PDF.GraphicsInterpreter;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converter.FileStructures
{
  public class PDFGO_DEBUG_STATE
  {
    public List<LiteralToDrawState> Literals = new List<LiteralToDrawState>();
    public char CurrentChar;
    public string FontRef;
    public bool isPath;
  }

  public class LiteralToDrawState
  {
    public LiteralToDrawState(string literal, int posAdj)
    {
      Literal = literal;
      PositionAdjustment = posAdj;
    }

    public string Literal;
    public int PositionAdjustment;
  }
}
