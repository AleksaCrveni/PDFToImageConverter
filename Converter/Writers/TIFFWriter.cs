using Converter.FIleStructures;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converter.Writers
{
  public static class TIFFWriter
  {
    /// <summary>
    /// Writes random bilevel tiff file with random width or heigh depending on option values passed
    /// </summary>
    /// <param name="path">Filepath including filename</param>
    /// <param name="options">Options for image generation</param>
    /// <list type="bullet">
    /// <item>
    /// - BigEndian wether file is in big endian or little endian
    ///   -> default false
    ///   </item>
    /// </list>
    /// - BigEndian wether file is in big endian or little endian
    ///   -> default false
    /// - Width is width of the picture
    ///   -> default 0
    ///   -> if its 0 random value is generated based on MaxRandomWidth and MinRandomWidth value
    /// - Heigh is height of the picture
    ///   -> default 0
    ///   -> if its 0 random value is generated base d on MaxRandomHeight and MinRandomHeight value
    /// - MaxRandomWidth is maximum value width can get if its randomly generated
    ///   -> default is 1920
    ///   -> used when generated random value when width is 0
    ///   -> this value should NOT be 0 and must be bigger than MaxRandomWidth
    /// - MinRandomWidth is minimum value width can get if its randomly generated
    ///   -> default is 720
    ///   -> used when generated random value when width is 0
    ///   -> this value should NOT be 0 and must be smaller than MinRandomWidth
    /// - MaxRandomWidth is maximum value width can get if its randomly generated
    ///   -> default is 1920
    ///   -> used when generated random value when width is 0
    ///   -> this value should NOT be 0
    /// </param>
    public static void WriteRandomBilevelTiff(string path, TIFFWriterOptions options)
    {
      FileStream fs = File.Create(path);

      if (options.Width == 0)
        options.Width = Random.Shared.Next(options.MinRandomWidth, options.MaxRandomWidth + 1);
      if (options.Height == 0)
        options.Width = Random.Shared.Next(options.MinRandomHeight, options.MaxRandomHeight + 1);

      // use one buffer, always write in 8K intervals
      Span<byte> writeBuffer = options.AllowStackAlloct ? stackalloc byte[8192] : new byte[8192];
      WriteHeader(ref fs, ref writeBuffer, options.BigEndian);
      WriteRandomBilevelImageAndMetadata(ref fs, ref writeBuffer, ref options, Compression.NoCompression);
    }

    
    static void WriteHeader(ref FileStream fs, ref Span<byte> writeBuffer, bool bigEndian = false)
    {
      if (bigEndian)
      {
        writeBuffer[0] = (byte)'M';
        writeBuffer[1] = (byte)'M';
        writeBuffer[2] = 0;
        writeBuffer[3] = 42;
      }
      else
      {
        writeBuffer[0] = (byte)'I';
        writeBuffer[1] = (byte)'I';
        writeBuffer[2] = 42;
        writeBuffer[3] = 0;
      }
      // write IFD after data later
      writeBuffer[4] = 0;
      writeBuffer[5] = 0;
      writeBuffer[6] = 0;
      writeBuffer[7] = 0;

      // maybe dont have to write insantly
      fs.Write(writeBuffer.Slice(0, 8));
    }

    // TODO: fix all these castings and stuff about var sizes
    static void WriteRandomBilevelImageAndMetadata(ref FileStream fs, ref Span<byte> writeBuffer, ref TIFFWriterOptions options, Compression compression = Compression.NoCompression)
    {
      int stripSize = 8192;
      // suppoirt later
      if (compression != Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // divide by 8 because with no compression and bilevel values are either 0 or 1 and they are packed in 
      // bits
      ulong byteCount = ((uint)options.Width * (uint)options.Height) / 8;
      // write these last
      int remainder = Convert.ToInt32(byteCount % Convert.ToUInt32(writeBuffer.Length));
      // write in 8k Strips

      int imageDataStartPointer = (int)fs.Position;
      for (ulong i = 0; i < byteCount; i += (ulong)stripSize)
      {
        // read random value into each buffer stuff and then write
        Random.Shared.NextBytes(writeBuffer);
        // do entire buffer because we know we are in range and no need to refresh
        fs.Write(writeBuffer);
      }

      // write remainder
      Span<byte> remainderSizeBuffer = writeBuffer.Slice(0, remainder);
      Random.Shared.NextBytes(remainderSizeBuffer);
      fs.Write(remainderSizeBuffer);

      uint stripCount = (uint)byteCount / (uint)stripSize;
      if ((uint)stripCount % stripSize > 0)
        stripCount++;

      // stripCount = floor((ImageLength + RowsPerStrip - 1) / RowsPerStrip)
      int rowsPerStrip = (options.Height - 1) / (int)stripCount;// no need to floor since its 2 ints and it will do it by itself

      // can all be single strip but its best to do 8K at the time
      // -1 because of remainder

      int stripOffsetPointer = (int)fs.Position;
      for (int i = 0; i < stripCount; i += 4)
      {
        // little endian only!
        writeBuffer[i] = (byte)(imageDataStartPointer & 255);
        writeBuffer[i + 1] = (byte)(imageDataStartPointer >> 8);
        writeBuffer[i + 2] = (byte)(imageDataStartPointer >> 16);
        writeBuffer[i + 3] = (byte)(imageDataStartPointer >> 24);
        imageDataStartPointer += 4;
      }
      // * 4 because each strip is 4 bytes
      fs.Write(writeBuffer.Slice(0,(int)stripCount * 4));

      int stripCountPointer = (int)fs.Position;

      for (int i = 0; i < stripCount - 1; i += 4)
      {
        // little endian only!
        writeBuffer[i] = 8192 & 255;
        writeBuffer[i + 1] = 8192 >> 8;
        writeBuffer[i + 2] = 0;
        writeBuffer[i + 3] = 0;
      }

      int lastStripByte = (int)(stripCount - 1) * 4;
      writeBuffer[lastStripByte] = (byte)(remainder & 255);
      writeBuffer[lastStripByte + 1] = (byte)(remainder >> 8);
      writeBuffer[lastStripByte + 2] = (byte)(remainder >> 16);
      writeBuffer[lastStripByte + 3] = (byte)(remainder >> 24);

      fs.Write(writeBuffer.Slice(0, (int)stripCount * 4));

      int pos = 0;
      // write IFD 'header'
      writeBuffer[pos++] = 10;
      writeBuffer[pos++] = 0;
      
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.ImageLength, TagSize.SHORT, 1,
        (uint)options.Height);
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.ImageWidth, TagSize.SHORT, 1,
        (uint)options.Width);
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.Compression, TagSize.SHORT, 1,
        (uint)Compression.NoCompression);
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.PhotometricInterpretation, TagSize.SHORT, 1,
        (uint)PhotometricInterpretation.WhiteIsZero);
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.StripOffsetsPointer, TagSize.LONG, 1,
        (uint)stripOffsetPointer);
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.RowsPerStrip, TagSize.LONG, 1,
        (uint)rowsPerStrip);
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.StripByteCountsPointer, TagSize.LONG, 1,
        (uint)stripCountPointer);
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.ResolutionUnit, TagSize.SHORT, 1,
        (uint)ResolutionUnit.Inch);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 2 (2 *12) + 4
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.XResolution, TagSize.RATIONAL, 2,
        (uint)fs.Position + 2 + (10 *12) + 4);
      WriteIFDEntry(ref writeBuffer, ref pos, TagType.YResolution, TagSize.RATIONAL, 2,
        (uint)fs.Position + 2 + (10 * 12) + 4 + 8);

      // write 4 bytes of 0s for next IFD address
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;

      // write IFD offset in header first 4-7 bytes
      int currPos = (int)fs.Position;
      fs.Position = 4;
      Span<byte> headerOverWrite = stackalloc byte[4];
      headerOverWrite[0] = (byte)currPos;
      headerOverWrite[1] = (byte)(currPos >> 8);
      headerOverWrite[2] = (byte)(currPos >> 16);
      headerOverWrite[3] = (byte)(currPos >> 24);
      fs.Write(headerOverWrite);

      fs.Position = currPos;
      fs.Write(writeBuffer.Slice(0, pos - 1));

      // write 2 8B rationals
      pos = 0;
      // XRes
      writeBuffer[pos++] = 72;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;

      // YRes
      writeBuffer[pos++] = 72;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      fs.Write(writeBuffer.Slice(0, 16));
    }

    public static void WriteIFDEntry(ref Span<byte> writeBuffer, ref int pos,TagType tag, TagSize t, uint count, uint valueOrOffset)
    {
      // 12 bytes
      // maybe i dont nbeed to mask but just cast and it cuts off
      // Tag
      writeBuffer[pos++] = (byte)tag;
      writeBuffer[pos++] = (byte)((uint)tag >> 8);

      // Type
      writeBuffer[pos++] = (byte)t;
      writeBuffer[pos++] = (byte)((uint)t >> 8);

      // Count
      writeBuffer[pos++] = (byte)count;
      writeBuffer[pos++] = (byte)(count >> 8);
      writeBuffer[pos++] = (byte)(count >> 16);
      writeBuffer[pos++] = (byte)(count >> 24);

      // value
      writeBuffer[pos++] = (byte)valueOrOffset;
      writeBuffer[pos++] = (byte)(valueOrOffset >> 8);
      writeBuffer[pos++] = (byte)(valueOrOffset >> 16);
      writeBuffer[pos++] = (byte)(valueOrOffset >> 24);
    }
  }

  
  public struct TIFFWriterOptions()
  {
    public bool BigEndian = false;
    public int Width = 0;
    public int Height = 0;
    public ushort MaxRandomWidth = 1920;
    public ushort MaxRandomHeight = 1080;
    public ushort MinRandomWidth = 720;
    public ushort MinRandomHeight = 480;
    public bool AllowStackAlloct = false;
  }
}
