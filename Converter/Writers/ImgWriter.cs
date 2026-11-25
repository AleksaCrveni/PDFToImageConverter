using Converter.FileStructures.General;
using Converter.Writers.TIFF;
using Converter.FileStructures.TIFF;
namespace Converter.Writers
{
  public static class ImgWriter
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileNameNoExtension">Just name of the file and extension will be appended based on type</param>
    /// <param name="img"></param>
    public static void Write(string fileNameNoExtension, Img img)
    {
      string extension = img.Type switch
      {
        IMG_TYPE.JPG => "jpg",
        IMG_TYPE.PNG => "png",
        IMG_TYPE.TIFF => "tiff",
        IMG_TYPE.BMP => "bmp",
        _ => throw new NotSupportedException("File extension not supported")
      };

      using (FileStream fs = File.Create($"{fileNameNoExtension}.{extension}"))
      {
        Write(fs, img);  
      }
    }
    public static void Write(Stream s, Img img)
    {
      if (img.Type == IMG_TYPE.TIFF)
      {
        // support just rgb for our purposes for now
        if (img.BytesPerPixel == 3 && img.BitsPerSample == 8)
        {
          TIFFRGBWriter w = new TIFFRGBWriter(s);
          TIFFWriterOptions options = new TIFFWriterOptions()
          {
            BitsPerSample = img.BitsPerSample,
            Compression = TIFF_Compression.NoCompression,
            Height = img.Height,
            Width = img.Width,
          };

          w.WriteImageWithBuffer(ref options, img.RawPixels);
        }
        else
        {
          throw new NotSupportedException("Format not supported currently!");
        }
      }
      else
      {
        throw new NotSupportedException("Format not supported currently!");
      }

    }
  }
}
