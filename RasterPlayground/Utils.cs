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

    private static void Plot(byte[] bitmap, int width, int x, int y, float brightness)
    {
      Debug.Assert(brightness <= 1);
      bitmap[y * width + x] = (byte)(brightness * 255);
    }
    public static byte[] DrawLine(int width, int height, float x0, float y0, float x1, float y1)
    {
      byte[] bitmap = new byte[width * height];
      bool steep = MathF.Abs(y1 - y0) > MathF.Abs(x1 - x0);
      if (steep)
      {
        float temp;
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
        float temp;
        temp = x1;
        x1 = x0;
        x0 = temp;

        temp = y1;
        y1 = y0;
        y0 = temp;
      }

      float dx = x1 - x0;
      float dy = y1 - y0;

      float gradient;
      if (dx == 0)
        gradient = 1;
      else
        gradient = dy / dx;

      // first endpoint
      int xEnd = (int)MathF.Floor(x0);
      float yEnd = y0 + gradient * ((float)xEnd - x0);
      float xGap = 1 - (x0 - (float)xEnd);

      int xPxl1 = xEnd;
      int yPxl1 = (int)MathF.Floor(yEnd);

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

      float interY = yEnd + gradient; // first y-intersection for the main loop

      // second end point
      xEnd = (int)MathF.Ceiling(x1);
      yEnd = y1 + gradient * ((float)xEnd - x1);
      xGap = 1 - ((float)xEnd - x1);

      int xPxl2 = xEnd;
      int yPxl2 = (int)MathF.Floor(yEnd);
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
          Plot(bitmap, width, (int)MathF.Floor(interY), x, MyMath.RFPart(interY));
          Plot(bitmap, width, (int)MathF.Floor(interY) + 1, x, MyMath.FPart(interY));
          interY += gradient;
        }
      }
      else
      {
        for (int x = xPxl1 + 1; x < xPxl2; x++)
        {
          Plot(bitmap, width, x, (int)MathF.Floor(interY), MyMath.RFPart(interY));
          Plot(bitmap, width, x, (int)MathF.Floor(interY) + 1, MyMath.FPart(interY));
          interY += gradient;
        }
      }

      return bitmap;
    }
  }
}
