using Converter.FileStructures.PDF.GraphicsInterpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

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

    public static void MultiplyMatrixes3x3(double[,] mA, double[,] mB, ref double[,] mR)
    {
      mR[0, 0] = mA[0, 0] * mB[0, 0] + mA[0, 1] * mB[1, 0] + mA[0, 2] * mB[2, 0];
      mR[0, 1] = mA[0, 0] * mB[0, 1] + mA[0, 1] * mB[1, 1] + mA[0, 2] * mB[2, 1];
      mR[0, 2] = mA[0, 0] * mB[0, 2] + mA[0, 1] * mB[1, 2] + mA[0, 2] * mB[2, 2];

      mR[1, 0] = mA[1, 0] * mB[0, 0] + mA[1, 1] * mB[1, 0] + mA[1, 2] * mB[2, 0];
      mR[1, 1] = mA[1, 0] * mB[0, 1] + mA[1, 1] * mB[1, 1] + mA[1, 2] * mB[2, 1];
      mR[1, 2] = mA[1, 0] * mB[0, 2] + mA[1, 1] * mB[1, 2] + mA[1, 2] * mB[2, 2];

      mR[2, 0] = mA[2, 0] * mB[0, 0] + mA[2, 1] * mB[1, 0] + mA[2, 2] * mB[2, 0];
      mR[2, 1] = mA[2, 0] * mB[0, 1] + mA[2, 1] * mB[1, 1] + mA[2, 2] * mB[2, 1];
      mR[2, 2] = mA[2, 0] * mB[0, 2] + mA[2, 1] * mB[1, 2] + mA[2, 2] * mB[2, 2];
    }
  }
}
