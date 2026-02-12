using Converter.FileStructures.BMP;
using Converter.Utils;
using System.Diagnostics;

namespace Converter.Writers.BMP
{
  // OPtimize memory usege
  public static class BMPWriter
  {

    public static void WriteRandomBMP(string filepath, ref readonly BMPWriterOptions options)
    {
      Stream fs = File.Create(filepath);
      WriteRandomBMP(fs, in options);
    }

    public static void WriteRandomBMP(Stream stream, ref readonly BMPWriterOptions options)
    {
      // support only no compression for now
      Debug.Assert(options.Compression == 0);

      uint rowSize = (uint)((int)MathF.Ceiling((((int)options.Type) * options.Width) / 32f) * 4);
      (uint fileSize, uint imageDataOffset) sizeData = CalculateFileSizeAndImageOffset(in options, rowSize);

      byte[] rowBuffer = new byte[rowSize];
      int height = Math.Abs(options.Height);
      uint imageSize = (uint)(rowSize * height);
      
      // or just use rowBuffer if its big enough
      int pos = 0;
      WriteHeader(stream, in options, sizeData.fileSize, sizeData.imageDataOffset);
      WriteDIBHeader(stream, in options, imageSize);

      for (int i = 0; i < height; i++)
      {
        Random.Shared.NextBytes(rowBuffer);
        stream.Write(rowBuffer);
      }
      stream.Flush();
    }
    public static void WriteHeader(Stream stream, ref readonly BMPWriterOptions options, uint fileSize, uint imageDataOffset)
    {

      Span<byte> buffer = new byte[14];
      int pos = 0;
      // BM = 19778
      BufferWriter.WriteInt16LE(ref buffer, ref pos, 19778);
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, fileSize);
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, 0);
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, imageDataOffset);
      stream.Write(buffer);
    } 

    public static void WriteDIBHeader(Stream stream, ref readonly BMPWriterOptions options, uint imageSize)
    {
      // call appropciate header based on type later
      WriteDIBWinInfoHeader(stream, in options, imageSize);
    }

    /// <summary>
    /// Write BITMAPINFOHEADER
    /// </summary>
    /// <param name="strea"></param>
    /// <param name=""></param>
    public static void WriteDIBWinInfoHeader(Stream stream, ref readonly BMPWriterOptions options, uint imageSize)
    {
      // just to fit anyhting for now fix later
      Span<byte> buffer = new byte[2048];
      int pos = 0;
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, 40); 
      BufferWriter.WriteInt32LE(ref buffer, ref pos, options.Width); // BitmapWidth 
      BufferWriter.WriteInt32LE(ref buffer, ref pos, options.Direction == BMP_DIRECTION.TOP_DOWN ? -options.Height : options.Height); // BitmapHeight
      BufferWriter.WriteUInt16LE(ref buffer, ref pos, 1); // Num of planes
      BufferWriter.WriteUInt16LE(ref buffer, ref pos, 1); // bpp
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, 0); // Compression
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, imageSize); // valid to be 0 if comp 0 - ImageSize
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, 0); // Xres
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, 0); // Yres

      // dont write color table for mono
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, 0); // Colors used - White and Black // even if this is 0 we have to add color table of 2 colors it seems or some readers wont read it??
      BufferWriter.WriteUInt32LE(ref buffer, ref pos, 0); // Important colors - 0 means all
      if (options.Type != BMP_TYPE.MONO)
      {
        // add color table or w/e
      }
      else
      {
        // Even 
        BufferWriter.WriteUInt32LE(ref buffer, ref pos, 0); // black
        BufferWriter.WriteUInt32LE(ref buffer, ref pos, 16777215); // white
      }
      stream.Write(buffer.Slice(0, pos));
    }

    public static (uint size, uint imageDataOffset) CalculateFileSizeAndImageOffset(ref readonly BMPWriterOptions options, uint rowSize)
    {
      (uint size, uint imageDataOffset) res = (0, 0);
      res.size = 14; // header
      // TODO: currently only support Mono, not sure which headers are used by other types
      // Header + color table if present (4 bytes * numOfCOlors)
      res.size += options.Type switch
      {
        BMP_TYPE.MONO => (int)BMP_DIB_HEADER_TYPE.BITMAPINFOHEADER + 8,
        BMP_TYPE.PAL4 => (int)BMP_DIB_HEADER_TYPE.BITMAPINFOHEADER + 4 * 16,
        BMP_TYPE.PAL8 => (int)BMP_DIB_HEADER_TYPE.BITMAPINFOHEADER + 4 * 256,
        BMP_TYPE.RGB16 => (int)BMP_DIB_HEADER_TYPE.BITMAPINFOHEADER,
        BMP_TYPE.RGB24 => (int)BMP_DIB_HEADER_TYPE.BITMAPINFOHEADER,
        _ => 0
      };

      if (res.size == 14)
        throw new Exception("invalid BMP Type!");

      // extras, do switch case later

      // Image Data
      res.imageDataOffset = res.size;
      res.size += (uint)(rowSize * Math.Abs(options.Height));
      return res;
    }
  }
}

