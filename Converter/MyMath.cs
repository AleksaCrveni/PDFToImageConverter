using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converter
{
  public static class MyMath
  {
    public static double[,] RealIdentityMatrix3x3()
    {
      return new double[3,3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
    }

  }
}
