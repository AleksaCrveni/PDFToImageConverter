using Converter.FileStructures.General;
using Converter.FileStructures.PNG;
using Converter.FileStructures.TIFF;
using Converter.Parsers.Images.PNG;
using Converter.Parsers.Images.TIFF;
using Converter.Utils.PNG;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Converter.Parsers.Images
{
  public static class ImgLoader
  {
    public static Img Load(string path)
    {
      Img img;
      string extension = path.Split('.').Last();
      IMG_TYPE imgType = extension.ToLower() switch
      {
        "tiff" => IMG_TYPE.TIFF,
        "png" => IMG_TYPE.PNG,
        "bmp" => IMG_TYPE.BMP,
        "jpg" or "jpeg" => IMG_TYPE.JPG,
        _ => throw new NotSupportedException("Image format not supported!")
      };
      using (FileStream fs = File.OpenRead(path))
      {
        img = Load(fs, imgType);
      }
      return img;
    }

    /// <summary>
    /// Load image from page
    /// </summary>
    /// <param name="stream">Stream that contains data with position at the start of the image</param>
    /// <param name="type">Image type</param>
    /// <param name="page">Page index for formats that can contain multiple images in one file i.e TIFF</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="Exception"></exception>
    public static Img Load(Stream stream, IMG_TYPE type, int page = 0)
    {
      Img? img = null;
      if (type == IMG_TYPE.TIFF)
      {
        TIFFFile? f = TIFFParser.Parse(stream);
        if (f != null)
        {
          img = new Img();
          img.Type = type;
          TIFF_DATA? tiff = TIFFParser.GetImage(f, page);
          if (tiff == null)
            throw new Exception($"Image with index {page} not found!");

          img.Height = (int)tiff.Metadata.ImageHeight;
          img.Width = (int)tiff.Metadata.ImageWidth;
          img.BytesPerPixel = (double)(tiff.Metadata.SamplesPerPixel * tiff.Metadata.BitsPerSample);
          img.BitsPerSample = (int)tiff.Metadata.BitsPerSample;
          img.RawPixels = tiff.RawPixelData;
        }
      }
      else if (type == IMG_TYPE.JPG)
      {
        throw new NotSupportedException("Image format not supported!");
      }
      else if (type == IMG_TYPE.PNG)
      {
        PNGFile png = PNGParser.Parse(stream);
        img = new Img();
        img.Type = type;
        img.Width = png.Width;
        img.Height = png.Height;
        img.BitsPerSample = png.BitDepth;
        img.BytesPerPixel = (double)PNGHelper.GetBytesPerPixel(PNGHelper.GetBitsPerPixel(png.ColorSheme, png.BitDepth)); // this must be integer, i think somewhere in spec its mentioned
        img.RawPixels = png.RawPixelData;
      }
      else if (type == IMG_TYPE.BMP)
      {
        throw new NotSupportedException("Image format not supported!");
      }
      else
      {
        throw new NotSupportedException("Image format not supported!");
      }

      if (img == null)
        throw new Exception("Unexpected error!");
      return img;
    }

  }
}
