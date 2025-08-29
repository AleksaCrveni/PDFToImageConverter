using Converter.FIleStructures;
using System.Runtime.CompilerServices;

namespace Converter.Writers
{
  public static class TIFFWriter
  {
    private static int DEFAULT_STRIP_SIZE = 8192;
    /// <summary>
    /// Writes random bilevel tiff file with random width or heigh depending on option values passed
    /// </summary>
    /// <param name="path">Filepath including filename</param>
    /// <param name="options">Options for image generation</param>
    public static void WriteRandomBilevelTIFF(string path, TIFFWriterOptions options)
    {
      WriteTIFFMain(path, options, TIFFType.Bilevel);
    }
    /// <summary>
    /// Writes random grayscale tiff file with random width or heigh depending on option values passed
    /// </summary>
    /// <param name="path">Filepath including filename</param>
    /// <param name="options">Options for image generation</param>
    public static void WriteRandomGrayscaleTIFF(string path, TIFFWriterOptions options)
    {
      WriteTIFFMain(path, options, TIFFType.Grayscale);
    }

    public static void WriteRandomPaletteTiff (string path, TIFFWriterOptions options)
    {
      WriteTIFFMain(path, options, TIFFType.Palette);
    }
    static void WriteTIFFMain(string path, TIFFWriterOptions options, TIFFType tiffType)
    {
      FileStream fs = File.Create(path);

      if (options.Width == 0)
        options.Width = Random.Shared.Next(options.MinRandomWidth, options.MaxRandomWidth + 1);
      if (options.Height == 0)
        options.Height = Random.Shared.Next(options.MinRandomHeight, options.MaxRandomHeight + 1);

      // use one buffer, always write in 8K intervals
      Span<byte> writeBuffer = options.AllowStackAlloct ? stackalloc byte[8192] : new byte[8192];
      BufferWriter writer = new BufferWriter(ref writeBuffer, options.IsLittleEndian);
      WriteHeader(ref fs, ref writeBuffer, options.IsLittleEndian);

      switch (tiffType)
      {
        case TIFFType.Bilevel:
          WriteRandomBilevelImageAndMetadata(ref fs, ref writer, ref options, Compression.NoCompression);
          break;
        case TIFFType.Grayscale:
          WriteRandomGrayScaleImageAndMetadata(ref fs, ref writer, ref options, Compression.NoCompression);
          break;
        case TIFFType.Palette:
          WriteRandomPaletteImageAndMetadata(ref fs, ref writer, ref options, Compression.NoCompression);
          break;
        default:
          break;
      }
    }

    // No need to use BufferWriter its ok
    static void WriteHeader(ref FileStream fs, ref Span<byte> writeBuffer, bool isLittleEndian = true)
    {
      if (!isLittleEndian)
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

      // maybe dont have to write instantly
      fs.Write(writeBuffer.Slice(0, 8));
    }

    // TODO: fix all these castings and stuff about var sizes
    static void WriteRandomBilevelImageAndMetadata(ref FileStream fs, ref BufferWriter writer, ref TIFFWriterOptions options, Compression compression = Compression.NoCompression)
    {
      int pos = 0;
      
      // support later
      if (compression != Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // divide by 8 because with no compression and bilevel values are either 0 or 1 and they are packed in bits
      ulong byteCount = ((uint)options.Width * (uint)options.Height) / 8;

      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      int stripSize = DEFAULT_STRIP_SIZE;

      CalculateStripAndRowInfo(byteCount, options.Height, ref stripSize, out uint stripCount, out int rowsPerStrip, out int remainder);
      int imageDataStartPointer = (int)fs.Position;
      for (ulong i = 0; i < byteCount; i += (ulong)stripSize)
      {
        // read random value into each buffer stuff and then write
        Random.Shared.NextBytes(writer._buffer);
        // do entire buffer because we know we are in range and no need to refresh
        fs.Write(writer._buffer);
      }

      // write remainder
      Span<byte> remainderSizeBuffer = writer._buffer.Slice(0, remainder);
      Random.Shared.NextBytes(remainderSizeBuffer);
      fs.Write(remainderSizeBuffer);

      int stripOffsetPointer = (int)fs.Position;
      pos = 0;
      for (int i = 0; i < stripCount; i++)
      {
        // little endian only!
        writer.WriteUnsigned32ToBuffer(ref pos, (uint)imageDataStartPointer);
        imageDataStartPointer += stripSize;
      }
      fs.Write(writer._buffer.Slice(0,pos));

      int stripCountPointer = (int)fs.Position;
      pos = 0;
      for (int i = 0; i < stripCount - 1; i++)
      {
        writer.WriteUnsigned32ToBuffer(ref pos, (uint)stripSize);
      }

      if (remainder == 0)
        remainder = stripSize;
      writer.WriteUnsigned32ToBuffer(ref pos, (uint)remainder);

      fs.Write(writer._buffer.Slice(0, pos));

      pos = 0;
      int tagCount = 11;
      // write IFD 'header' lenght
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)tagCount);
      // These tags should be in sequence according to JHOVE validator
      // this means that they should be written from smallest to largest enum values
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageWidth, TagSize.SHORT, 1,
        (uint)options.Width);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageLength, TagSize.SHORT, 1,
        (uint)options.Height);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.BitsPerSample, TagSize.SHORT, 1,
        1);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.Compression, TagSize.SHORT, 1,
        (uint)Compression.NoCompression);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.PhotometricInterpretation, TagSize.SHORT, 1,
        (uint)PhotometricInterpretation.WhiteIsZero);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.StripOffsetsPointer, TagSize.LONG, stripCount,
        (uint)stripOffsetPointer);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.RowsPerStrip, TagSize.LONG, 1,
        (uint)rowsPerStrip);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.StripByteCountsPointer, TagSize.LONG, stripCount,
        (uint)stripCountPointer);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 2 (2 *12) + 4
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.XResolution, TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + (tagCount * 12) + 4));
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.YResolution, TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + (tagCount * 12) + 4 + 8));
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ResolutionUnit, TagSize.SHORT, 1,
        (uint)ResolutionUnit.Inch);

      // write 4 bytes of 0s for next IFD address
      writer.WriteUnsigned32ToBuffer(ref pos, 0);

      // get IFD start pos before we write again
      uint IFDStartPos = (uint)fs.Position;
      fs.Write(writer._buffer.Slice(0, pos));

      // write IFD offset in header first 4-7 bytes
      fs.Position = 4;
      pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, IFDStartPos);
      fs.Write(writer._buffer.Slice(0, pos));

      // go back to end
      fs.Seek(0, SeekOrigin.End);

      pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, 72);
      writer.WriteUnsigned32ToBuffer(ref pos, 1);

      // YRes
      writer.WriteUnsigned32ToBuffer(ref pos, 72);
      writer.WriteUnsigned32ToBuffer(ref pos, 1);

      fs.Write(writer._buffer.Slice(0, pos));
      fs.Flush();
      fs.Dispose();
    }

    static void WriteRandomGrayScaleImageAndMetadata(ref FileStream fs, ref BufferWriter writer, ref TIFFWriterOptions
      options, Compression compression = Compression.NoCompression)
    {
      int pos = 0;
      // allowed either 4 or 8
      uint bitsPerSample = 8;
      // support later
      if (compression != Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // divide by 8 because with no compression and bilevel values are either 0 or 1 and they are packed in bits
      ulong byteCount = ((uint)options.Width * (uint)options.Height) / (8 / bitsPerSample);

      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      int stripSize = DEFAULT_STRIP_SIZE;

      CalculateStripAndRowInfo(byteCount, options.Height, ref stripSize, out uint stripCount, out int rowsPerStrip, out int remainder);
      int imageDataStartPointer = (int)fs.Position;
      // write data
      for (ulong i = 0; i < byteCount; i += (ulong)stripSize)
      {
        // read random value into each buffer stuff and then write
        Random.Shared.NextBytes(writer._buffer);
        // do entire buffer because we know we are in range and no need to refresh
        fs.Write(writer._buffer);
      }

      Span<byte> remainderSizeBuffer = writer._buffer.Slice(0, remainder);
      Random.Shared.NextBytes(remainderSizeBuffer);
      fs.Write(remainderSizeBuffer);

      // Write byte offsets
      int stripOffsetPointer = (int)fs.Position;
      pos = 0;
      for (int i = 0; i < stripCount; i++)
      {
        // little endian only!
        writer.WriteUnsigned32ToBuffer(ref pos, (uint)imageDataStartPointer);
        imageDataStartPointer += stripSize;
      }
      fs.Write(writer._buffer.Slice(0, pos));

      // write counts
      int stripCountPointer = (int)fs.Position;
      pos = 0;
      for (int i = 0; i < stripCount - 1; i++)
      {
        writer.WriteUnsigned32ToBuffer(ref pos, (uint)stripSize);
      }

      // write remainder
      if (remainder == 0)
        remainder = stripSize;
      writer.WriteUnsigned32ToBuffer(ref pos, (uint)remainder);

      fs.Write(writer._buffer.Slice(0, pos));

      // IFD
      pos = 0;
      int tagCount = 11;
      // write IFD 'header' lenght
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)tagCount);
      // These tags should be in sequence according to JHOVE validator
      // this means that they should be written from smallest to largest enum values
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageWidth, TagSize.SHORT, 1,
        (uint)options.Width);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageLength, TagSize.SHORT, 1,
        (uint)options.Height);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.BitsPerSample, TagSize.SHORT, 1,
        bitsPerSample);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.Compression, TagSize.SHORT, 1,
        (uint)Compression.NoCompression);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.PhotometricInterpretation, TagSize.SHORT, 1,
        (uint)PhotometricInterpretation.WhiteIsZero);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.StripOffsetsPointer, TagSize.LONG, stripCount,
        (uint)stripOffsetPointer);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.RowsPerStrip, TagSize.LONG, 1,
        (uint)rowsPerStrip);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.StripByteCountsPointer, TagSize.LONG, stripCount,
        (uint)stripCountPointer);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 2 (2 *12) + 4
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.XResolution, TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + (tagCount * 12) + 4));
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.YResolution, TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + (tagCount * 12) + 4 + 8));
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ResolutionUnit, TagSize.SHORT, 1,
        (uint)ResolutionUnit.Inch);

      // write 4 bytes of 0s for next IFD address
      writer.WriteUnsigned32ToBuffer(ref pos, 0);

      // get IFD start pos before we write again
      uint IFDStartPos = (uint)fs.Position;
      fs.Write(writer._buffer.Slice(0, pos));

      // write IFD offset in header first 4-7 bytes
      fs.Position = 4;
      pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, IFDStartPos);
      fs.Write(writer._buffer.Slice(0, pos));

      // go back to end
      fs.Seek(0, SeekOrigin.End);

      pos = 0;
      // XRes
      writer.WriteUnsigned32ToBuffer(ref pos, 72);
      writer.WriteUnsigned32ToBuffer(ref pos, 1);

      // YRes
      writer.WriteUnsigned32ToBuffer(ref pos, 72);
      writer.WriteUnsigned32ToBuffer(ref pos, 1);

      fs.Write(writer._buffer.Slice(0, pos));
      fs.Flush();
      fs.Dispose();
    }

    static void WriteRandomPaletteImageAndMetadata(ref FileStream fs, ref BufferWriter writer, ref TIFFWriterOptions options, Compression compression = Compression.NoCompression)
    {
      int pos = 0;
      // allowed either 4 or 8
      uint bitsPerSample = 8;
      // support later
      if (compression != Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // divide by 8 because with no compression and bilevel values are either 0 or 1 and they are packed in bits
      ulong byteCount = ((uint)options.Width * (uint)options.Height) / (8 / bitsPerSample);

      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      int stripSize = DEFAULT_STRIP_SIZE;

      CalculateStripAndRowInfo(byteCount, options.Height, ref stripSize, out uint stripCount, out int rowsPerStrip, out int remainder);
      int imageDataStartPointer = (int)fs.Position;
      // write data
      for (ulong i = 0; i < byteCount; i += (ulong)stripSize)
      {
        // read random value into each buffer stuff and then write
        Random.Shared.NextBytes(writer._buffer);
        // do entire buffer because we know we are in range and no need to refresh
        fs.Write(writer._buffer);
      }

      Span<byte> remainderSizeBuffer = writer._buffer.Slice(0, remainder);
      Random.Shared.NextBytes(remainderSizeBuffer);
      fs.Write(remainderSizeBuffer);

      // Write byte offsets
      int stripOffsetPointer = (int)fs.Position;
      pos = 0;
      for (int i = 0; i < stripCount; i++)
      {
        // little endian only!
        writer.WriteUnsigned32ToBuffer(ref pos, (uint)imageDataStartPointer);
        imageDataStartPointer += stripSize;
      }
      fs.Write(writer._buffer.Slice(0, pos));

      // write counts
      int stripCountPointer = (int)fs.Position;
      pos = 0;
      for (int i = 0; i < stripCount - 1; i++)
      {
        writer.WriteUnsigned32ToBuffer(ref pos, (uint)stripSize);
      }

      // write remainder
      if (remainder == 0)
        remainder = stripSize;
      writer.WriteUnsigned32ToBuffer(ref pos, (uint)remainder);

      fs.Write(writer._buffer.Slice(0, pos));

      // size of each R G B channels
      int paletteChannelSize = IntPow(2, bitsPerSample);
      // IFD
      pos = 0;
      int tagCount = 12;
      // write IFD 'header' lenght
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)tagCount);
      // These tags should be in sequence according to JHOVE validator
      // this means that they should be written from smallest to largest enum values
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageWidth, TagSize.SHORT, 1,
        (uint)options.Width);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageLength, TagSize.SHORT, 1,
        (uint)options.Height);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.BitsPerSample, TagSize.SHORT, 1,
        bitsPerSample);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.Compression, TagSize.SHORT, 1,
        (uint)Compression.NoCompression);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.PhotometricInterpretation, TagSize.SHORT, 1,
        (uint)PhotometricInterpretation.Pallete);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.StripOffsetsPointer, TagSize.LONG, stripCount,
        (uint)stripOffsetPointer);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.RowsPerStrip, TagSize.LONG, 1,
        (uint)rowsPerStrip);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.StripByteCountsPointer, TagSize.LONG, stripCount,
        (uint)stripCountPointer);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 2 (2 *12) + 4
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.XResolution, TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + (tagCount * 12) + 4));
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.YResolution, TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + (tagCount * 12) + 4 + 8));
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ResolutionUnit, TagSize.SHORT, 1,
        (uint)ResolutionUnit.Inch);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ColorMap, TagSize.SHORT, (ushort)(3 * paletteChannelSize),
        (uint)(fs.Position + 2 + (tagCount * 12) + 4 + 16));

      // write 4 bytes of 0s for next IFD address4
      writer.WriteUnsigned32ToBuffer(ref pos, 0);

      // get IFD start pos before we write again
      uint IFDStartPos = (uint)fs.Position;
      fs.Write(writer._buffer.Slice(0, pos));

      // write IFD offset in header first 4-7 bytes
      fs.Position = 4;
      pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, IFDStartPos);
      fs.Write(writer._buffer.Slice(0, pos));

      // go back to end
      fs.Seek(0, SeekOrigin.End);

      pos = 0;
      // XRes
      writer.WriteUnsigned32ToBuffer(ref pos, 72);
      writer.WriteUnsigned32ToBuffer(ref pos, 1);

      // YRes
      writer.WriteUnsigned32ToBuffer(ref pos, 72);
      writer.WriteUnsigned32ToBuffer(ref pos, 1);

      // ColorMap

      // 3 * 256 * 2 = 1536 size
      Random.Shared.NextBytes(writer._buffer.Slice(pos, 256 * 2));
      Random.Shared.NextBytes(writer._buffer.Slice(pos + 256 * 2, 256 * 2));
      Random.Shared.NextBytes(writer._buffer.Slice(pos + 256 * 4, 256 * 2));

      fs.Write(writer._buffer.Slice(0, pos + (3 * 256 * 2)));
      fs.Flush();
      fs.Dispose();
    }

    static void WriteIFDEntryToBuffer(ref BufferWriter writer, ref int pos,TagType tag, TagSize t, uint count, uint valueOrOffset)
    {
      // 12 bytes
      // tag
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)tag);
      // Type
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)t);
      // Count
      writer.WriteUnsigned32ToBuffer(ref pos, count);
      // value
      writer.WriteUnsigned32ToBuffer(ref pos, valueOrOffset);
    }

    static void CalculateStripAndRowInfo(ulong byteCount, int height, ref int stripSize, out uint stripCount, out int rowsPerStrip, out int remainder)
    {
      stripSize = stripSize - (stripSize % height);
      stripCount = (uint)Math.Ceiling(byteCount / (decimal)stripSize);
      if ((uint)byteCount % stripSize > 0)
        stripCount++;

      rowsPerStrip = height - 1 / (int)stripCount;
      remainder = Convert.ToInt32((uint)byteCount % stripSize);
    }

    static int IntPow(int x, uint pow)
    {
      if (pow > 32)
        throw new Exception("Power too big!");

      int ret = 1;
      while (pow != 0)
      {
        if ((pow & 1) == 1)
          ret *= x;
        x *= x;

        if (BitConverter.IsLittleEndian)
          pow >>= 1;
        else
          pow <<= 1;
      }
      return ret;
    }
  }

  /// <summary> Options for TIFF Writer
  /// </summary>
  public struct TIFFWriterOptions()
  {
    /// <summary>
    /// Declares wether TIFF Image should be written as BigEndian(MM) or LittleEndian(II)
    /// </summary>
    public bool IsLittleEndian = true;

    /// <summary>
    /// Width of the image. If it's 0, random value is generated based on MinRandomWidth and MinRandomWidth value. Default is 0.
    /// </summary>
    public int Width = 0;

    /// <summary>
    /// Height of the image. If it's 0, random value is generated based on MinRandomHeight and MinRandomHeight value. Default is 0.
    /// </summary>
    public int Height = 0;

    /// <summary>
    /// Maximum width that can be rolled in case Width isn't specified, should be larger than MinRandomWidth and non zero. Default is 1920.
    /// </summary>
    public ushort MaxRandomWidth = 1920;

    /// <summary>
    /// Maximum width that can be rolled in case Width isn't specified, should be larger than MinRandomHeight and non zero. Default is 1080.
    /// </summary>
    public ushort MaxRandomHeight = 1080;

    /// <summary>
    /// Minimum width that can be rolled in case Width isn't specified, should be smaller than MaxRandomWidth and non zero. Default is 128.
    /// </summary>
    public ushort MinRandomWidth = 128;

    /// <summary>
    /// Minimum height that can be rolled in case Width isn't specified, should be smaller than MaxRandomHeight and non zero. Default is 128.
    /// </summary>
    public ushort MinRandomHeight = 128;

    /// <summary>
    /// Allow writer buffer to be allocated on the stack. Buffer size is 8192 bytes. If false, buffer will be allocated on heap. Default is false.
    /// </summary>
    public bool AllowStackAlloct = false;
  }

  enum TIFFType
  { 
    Bilevel,
    Grayscale,
    Palette
  }

}