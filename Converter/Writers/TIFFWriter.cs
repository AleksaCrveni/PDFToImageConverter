using Converter.FIleStructures;

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
      BufferWriter writer = new BufferWriter(ref writeBuffer, options.IsLittleEndian);
      WriteHeader(ref fs, ref writeBuffer, options.IsLittleEndian);
      WriteRandomBilevelImageAndMetadata(ref fs, ref writer, ref options, Compression.NoCompression);
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
      int stripSize = 8192;
      // support later
      if (compression != Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // divide by 8 because with no compression and bilevel values are either 0 or 1 and they are packed in bits
      ulong byteCount = ((uint)options.Width * (uint)options.Height) / 8;

      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      stripSize = stripSize - (stripSize % options.Height);
      uint stripCount = (uint)Math.Ceiling(byteCount / (decimal)stripSize);
      if ((uint)byteCount % stripSize > 0)
        stripCount++;

      int rowsPerStrip = options.Height - 1 / (int)stripCount;
      int remainder = Convert.ToInt32((uint)byteCount % stripSize);

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
      // write IFD 'header' lenght
      writer.WriteUnsigned16ToBuffer(ref pos, 10);
      // These tags should be in sequence according to JHOVE validator
      // this means that they should be written from smallest to largest enum values
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageWidth, TagSize.SHORT, 1,
        (uint)options.Width);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageLength, TagSize.SHORT, 1,
        (uint)options.Height);
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
        (uint)fs.Position + 2 + (10 * 12) + 4);
      WriteIFDEntryToBuffer(ref writer, ref pos, TagType.YResolution, TagSize.RATIONAL, 1,
        (uint)fs.Position + 2 + (10 * 12) + 4 + 8);
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

    public static void WriteIFDEntryToBuffer(ref BufferWriter writer, ref int pos,TagType tag, TagSize t, uint count, uint valueOrOffset)
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
  }

  
  public struct TIFFWriterOptions()
  {
    public bool IsLittleEndian = true;
    public int Width = 0;
    public int Height = 0;
    public ushort MaxRandomWidth = 1920;
    public ushort MaxRandomHeight = 1080;
    public ushort MinRandomWidth = 128;
    public ushort MinRandomHeight = 128;
    public bool AllowStackAlloct = false;
  }
}
