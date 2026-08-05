using Converter.FileStructures.PDF.GraphicsInterpreter;
using System.Diagnostics;

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
    public static void ScaleImage(byte[] original, int originalHeight, int originalWidth, byte[] output, int outputHeight, int outputWidth, PDFGI_ColorChannel channelCount)
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
          int outPos = (y * outputWidth + x) * (int)channelCount;
          int origPos = (originalY * originalWidth + originalX) * (int)channelCount;
          output[outPos] = original[origPos];
          if (channelCount == PDFGI_ColorChannel.RGB)
          {
            output[outPos + 1] = original[origPos + 1];
            output[outPos + 2] = original[origPos + 2];
          }
        }
      }
    }

    /// <summary>
    /// Should be called BEFORE scaling
    /// Also i am not sure which cobinations of channels will be here so make it a bit more robust
    /// I At least think that maskChannelCount <= imageChannelCount will always be true
    /// </summary>
    /// <param name="image"></param>
    /// <param name="imageChannelCount"></param>
    /// <param name="height"></param>
    /// <param name="width"></param>
    /// <param name="mask"></param>
    /// <param name="maskChannelCount"></param>
    /// <param name="isShape"></param>
    /// <exception cref="NotImplementedException"></exception>
    public static void ApplyMask(byte[] image, PDFGI_ColorChannel imageChannelCount, byte[] mask, PDFGI_ColorChannel maskChannelCount, int height, int width,bool isShape)
    {
      
      if (isShape)
      {
        throw new NotImplementedException("ISShape masks not supported yet!");
      }
      else // its opacity
      {
        int iROffset = 0;
        int iGOffset = 0;
        int iBOffset = 0;
        int mROffset = 0;
        int mGOffset = 0;
        int mBOffset = 0;

        for (int y = 0; y < height; y++)
        {
          for (int x = 0; x < width; x++)
          {
            int imgPixelPos = (y * width + x) * (int)imageChannelCount;
            int maskPixelPos = (y * width + x) * (int)maskChannelCount;
            if (imageChannelCount == PDFGI_ColorChannel.GRAY)
            {
              iROffset = 0;
              iGOffset = 0;
              iBOffset = 0;
            }
            else
            {
              iROffset = 0;
              iGOffset = 1;
              iBOffset = 2;
            }

            if (maskChannelCount == PDFGI_ColorChannel.GRAY)
            {
              mROffset = 0;
              mGOffset = 0;
              mBOffset = 0;
            }
            else
            {
              mROffset = 0;
              mGOffset = 1;
              mBOffset = 2;
            }
            
            for (int i = 0; i < 3; i++)
            {
              image[imgPixelPos + iROffset] = (byte)(255 - (mask[maskPixelPos + mROffset] / 255f) * (255 - image[imgPixelPos + iROffset]));
              image[imgPixelPos + iGOffset] = (byte)(255 - (mask[maskPixelPos + mGOffset] / 255f) * (255 - image[imgPixelPos + iGOffset]));
              image[imgPixelPos + iBOffset] = (byte)(255 - (mask[maskPixelPos + mBOffset] / 255f) * (255 - image[imgPixelPos + iBOffset]));
            }
          }
        }

      }
    }
  }
}
