using Converter;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RasterPlayground
{
  public static class Utils
  {
    public static byte[] ConvertToWin32RGBBuffer(byte[] bitmap, int height, int width)
    {
      byte[] buff = new byte[height * width * 4];
      Array.Fill<byte>(buff, 255);
      int j = 0;
      for (int i = 0; i < bitmap.Length; i++)
      {
        byte val = (byte)(255 - bitmap[i]);
        buff[j] = val;
        buff[j + 1] = val;
        buff[j + 2] = val;
        j += 4;
      }

      return buff;
    }

    private static void Plot(byte[] bitmap, int width, int x, int y, double brightness)
    {
      Debug.Assert(brightness <= 1);
      bitmap[y * width + x] = (byte)(brightness * 255);
    }
    public static byte[] DrawLine(int width, int height, double x0, double y0, double x1, double y1)
    {
      byte[] bitmap = new byte[width * height];
      bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
      if (steep)
      {
        double temp;
        temp = x0;
        x0 = y0;
        y0 = temp;

        temp = x1;
        x1 = y1;
        y1 = temp;
      }

      // make sure x0 is left of x1
      if (x0 > x1)
      {
        double temp;
        temp = x1;
        x1 = x0;
        x0 = temp;

        temp = y1;
        y1 = y0;
        y0 = temp;
      }

      double dx = x1 - x0;
      double dy = y1 - y0;

      double gradient;
      if (dx == 0)
        gradient = 1;
      else
        gradient = dy / dx;

      // first endpoint
      int xEnd = (int)Math.Floor(x0);
      double yEnd = y0 + gradient * ((double)xEnd - x0);
      double xGap = 1 - (x0 - (double)xEnd);

      int xPxl1 = xEnd;
      int yPxl1 = (int)Math.Floor(yEnd);

      if (steep)
      {
        Plot(bitmap, width, yPxl1, xPxl1, MyMath.RFPart(yEnd) * xGap);
        Plot(bitmap, width, yPxl1 + 1, xPxl1, MyMath.FPart(yEnd) * xGap);
      }
      else
      {
        Plot(bitmap, width, xPxl1, yPxl1, MyMath.RFPart(yEnd) * xGap);
        Plot(bitmap, width, xPxl1, yPxl1 + 1, MyMath.FPart(yEnd) * xGap);
      }

      double interY = yEnd + gradient; // first y-intersection for the main loop

      // second end point
      xEnd = (int)Math.Ceiling(x1);
      yEnd = y1 + gradient * ((double)xEnd - x1);
      xGap = 1 - ((double)xEnd - x1);

      int xPxl2 = xEnd;
      int yPxl2 = (int)Math.Floor(yEnd);
      if (steep)
      {
        Plot(bitmap, width, yPxl2, xPxl2, MyMath.RFPart(yEnd) * xGap);
        Plot(bitmap, width, yPxl2 + 1, xPxl2, MyMath.FPart(yEnd) * xGap);
      }
      else
      {
        Plot(bitmap, width, xPxl2, yPxl2, MyMath.RFPart(yEnd) * xGap);
        Plot(bitmap, width, xPxl2, yPxl2 + 1, MyMath.FPart(yEnd) * xGap);
      }

      // main loop
      if (steep)
      {
        for (int x = xPxl1 + 1; x < xPxl2; x++)
        {
          Plot(bitmap, width, (int)Math.Floor(interY), x, MyMath.RFPart(interY));
          Plot(bitmap, width, (int)Math.Floor(interY) + 1, x, MyMath.FPart(interY));
          interY += gradient;
        }
      }
      else
      {
        for (int x = xPxl1 + 1; x < xPxl2; x++)
        {
          Plot(bitmap, width, x, (int)Math.Floor(interY), MyMath.RFPart(interY));
          Plot(bitmap, width, x, (int)Math.Floor(interY) + 1, MyMath.FPart(interY));
          interY += gradient;
        }
      }

      return bitmap;
    }
  }
}
