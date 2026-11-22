using System;
using System.Collections.Generic;
using System.Text;

namespace Converter.FileStructures.General
{
  public class Img
  {
    public byte[] RawPixels;
    public int Height;
    public int Width;
    /// <summary>
    /// This is double in case where some image formats have bits per sample as 4 and samples per pixel 3, so bytes per pixel would be 1.5
    /// </summary>
    public double BytesPerPixel;
    public int BitsPerSample;
    public IMG_TYPE Type;
  }

  public enum IMG_TYPE
  {
    JPG,
    PNG,
    TIFF,
    BMP
  }

}
