using Converter.FIleStructures;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
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
        options.Height = Random.Shared.Next(options.MinRandomHeight, options.MaxRandomHeight + 1);

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
      // write 2 8B rationals
      int pos = 0;
      // XRes
     
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
      if ((uint)byteCount% stripSize > 0)
        stripCount++;

      // stripCount = floor((ImageLength + RowsPerStrip - 1) / RowsPerStrip)
      // uradi minus remainder i podeli ponovoi i probaj onda??

      int rowsPerStrip = (int)Math.Ceiling((decimal)(options.Height - 1) / (int)stripCount);// no need to floor since its 2 ints and it will do it by itself

      uint stripCountCheck = (uint)Math.Floor((options.Height + rowsPerStrip - 1) / (decimal)rowsPerStrip);
      if (stripCountCheck != stripCount)
        throw new Exception("SHIT");
      // can all be single strip but its best to do 8K at the time
      // -1 because of remainder

      int stripOffsetPointer = (int)fs.Position;
      pos = 0;
      for (int i = 0; i < stripCount; i++)
      {
        // little endian only!
        writeBuffer[pos++] = (byte)(imageDataStartPointer & 255);
        writeBuffer[pos++] = (byte)(imageDataStartPointer >> 8);
        writeBuffer[pos++] = (byte)(imageDataStartPointer >> 16);
        writeBuffer[pos++] = (byte)(imageDataStartPointer >> 24);
        imageDataStartPointer += stripSize;
      }
      // * 4 because each strip is 4 bytes
      fs.Write(writeBuffer.Slice(0,pos));

      int stripCountPointer = (int)fs.Position;

      pos = 0;
      for (int i = 0; i < stripCount - 1; i++)
      {
        // little endian only!
        writeBuffer[pos++] = (byte)(stripSize & 255);
        writeBuffer[pos++] = (byte)(stripSize >> 8);
        writeBuffer[pos++] = 0;
        writeBuffer[pos++] = 0;
      }

      //int lastStripByte = (int)(stripCount - 1) * 4;
      writeBuffer[pos++] = (byte)(remainder & 255);
      writeBuffer[pos++] = (byte)(remainder >> 8);
      writeBuffer[pos++] = (byte)(remainder >> 16);
      writeBuffer[pos++] = (byte)(remainder >> 24);

      fs.Write(writeBuffer.Slice(0, pos));

      pos = 0;
      // write IFD 'header'
      writeBuffer[pos++] = 10;
      writeBuffer[pos++] = 0;

      // apparently these tags should be written in sequence?????
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.ImageWidth, TagSize.SHORT, 1,
        (uint)options.Width);
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.ImageLength, TagSize.SHORT, 1,
        (uint)options.Height);
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.Compression, TagSize.SHORT, 1,
        (uint)Compression.NoCompression);
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.PhotometricInterpretation, TagSize.SHORT, 1,
        (uint)PhotometricInterpretation.WhiteIsZero);
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.StripOffsetsPointer, TagSize.LONG, stripCount,
        (uint)stripOffsetPointer);
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.RowsPerStrip, TagSize.LONG, 1,
        (uint)rowsPerStrip);
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.StripByteCountsPointer, TagSize.LONG, stripCount,
        (uint)stripCountPointer);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 2 (2 *12) + 4
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.XResolution, TagSize.RATIONAL, 1,
        (uint)fs.Position + 2 + (10 * 12) + 4);
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.YResolution, TagSize.RATIONAL, 1,
        (uint)fs.Position + 2 + (10 * 12) + 4 + 8);
      WriteIFDEntryToBuffer(ref writeBuffer, ref pos, TagType.ResolutionUnit, TagSize.SHORT, 1,
        (uint)ResolutionUnit.Inch);

      // write 4 bytes of 0s for next IFD address
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;

      // write IFD offset in header first 4-7 bytes
      int IFDStartPos = (int)fs.Position;
      fs.Position = 4;
      Span<byte> headerOverWrite = stackalloc byte[4];
      headerOverWrite[0] = (byte)IFDStartPos;
      headerOverWrite[1] = (byte)(IFDStartPos >> 8);
      headerOverWrite[2] = (byte)(IFDStartPos >> 16);
      headerOverWrite[3] = (byte)(IFDStartPos >> 24);
      fs.Write(headerOverWrite);


      fs.Seek(0, SeekOrigin.End);
      fs.Write(writeBuffer.Slice(0, pos));

      pos = 0;
      writeBuffer[pos++] = 72;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 1;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;

      // YRes
      writeBuffer[pos++] = 72;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 1;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;
      writeBuffer[pos++] = 0;

      fs.Write(writeBuffer.Slice(0, 16));
      fs.Flush();
      fs.Dispose();
    }

    public static void WriteIFDEntryToBuffer(ref Span<byte> writeBuffer, ref int pos,TagType tag, TagSize t, uint count, uint valueOrOffset)
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
