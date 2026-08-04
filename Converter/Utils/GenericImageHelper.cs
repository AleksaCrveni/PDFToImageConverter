namespace Converter.Utils
{
  public static class GenericImageHelper
  {
    /// <summary>
    /// NOTE(@Aleksa): currently we use very simple Nearest Neighbour algorithm which isn't that great and produces bad AA,
    /// look into Bilinear Interpolation algorithm
    /// </summary>
    /// <param name="original"></param>
    /// <param name="originalHeight"></param>
    /// <param name="originalWidth"></param>
    /// <param name="output"></param>
    /// <param name="outputHeight"></param>
    /// <param name="outputWidth"></param>
    public static void ScaleRGBImage(byte[] original, int originalHeight, int originalWidth, byte[] output, int outputHeight, int outputWidth)
    {
      float xScaleFactor = (originalWidth - 1) / ((float)outputWidth - 1);
      float yScaleFactor = (originalHeight - 1) / ((float)outputHeight - 1);
      //Array.Fill<byte>(output, 255);
      for (int y = 0; y < outputHeight; y++)
      {
        for (int x = 0; x < outputWidth; x++)
        {
          int originalX = (int)MathF.Round(x * xScaleFactor);
          int originalY = (int)MathF.Round(y * yScaleFactor);
          int outPos = (y * outputWidth + x) * 3;
          int origPos = (originalY * originalWidth + originalX) * 3;
          output[outPos] = original[origPos];
          output[outPos + 1] = original[origPos + 1];
          output[outPos + 2] = original[origPos + 2];
        }
      }
    }
  }
}
