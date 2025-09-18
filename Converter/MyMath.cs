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

    public static int IntPow(int x, uint pow)
    {
      if (pow > 32)
        throw new Exception("Power too big!");

      int ret = 1;
      while (pow != 0)
      {
        if ((pow & 1) == 1)
          ret *= x;
        x *= x;

        if (BitConverter.IsLittleEndian)
          pow >>= 1;
        else
          pow <<= 1;
      }
      return ret;
    }
  }
}
