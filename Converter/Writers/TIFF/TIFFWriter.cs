using Converter.FileStructures.TIFF;
using Converter.Utils;

namespace Converter.Writers.TIFF
{
  public static class TIFFWriter
  {
   
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

    public static void WriteRandomPaletteTiff(string path, TIFFWriterOptions options)
    {
      WriteTIFFMain(path, options, TIFFType.Palette);
    }
    
    public static void WriteRandomRGBFullColorTiff(string path, TIFFWriterOptions options)
    {
      WriteTIFFMain(path, options, TIFFType.RGBFullColor);
    }
    static void WriteTIFFMain(string path, TIFFWriterOptions options, TIFFType tiffType)
    {
      if (File.Exists(path))
        File.Delete(path);
      Stream fs = File.Create(path);

      if (options.Width == 0)
        options.Width = Random.Shared.Next(options.MinRandomWidth, options.MaxRandomWidth + 1);
      if (options.Height == 0)
        options.Height = Random.Shared.Next(options.MinRandomHeight, options.MaxRandomHeight + 1);

      // use one buffer, always write in 8K intervals
      Span<byte> writeBuffer = options.AllowStackAlloct ? stackalloc byte[8192] : new byte[8192];
      SelfContainedBufferWriter writer = new SelfContainedBufferWriter(ref writeBuffer, options.IsLittleEndian);
      TIFFInternals.WriteHeader(ref fs, ref writeBuffer, options.IsLittleEndian);

      switch (tiffType)
      {
        case TIFFType.Bilevel:
          WriteRandomBilevelImageAndMetadata(ref fs, ref writer, ref options, TIFF_Compression.NoCompression);
          break;
        case TIFFType.Grayscale:
          WriteRandomGrayScaleImageAndMetadata(ref fs, ref writer, ref options, TIFF_Compression.NoCompression);
          break;
        case TIFFType.Palette:
          WriteRandomPaletteImageAndMetadata(ref fs, ref writer, ref options, TIFF_Compression.NoCompression);
          break;
        case TIFFType.RGBFullColor:
          WriteRandomRGBImageAndMetadata(ref fs, ref writer, ref options, TIFF_Compression.NoCompression);
          break;
        default:
          break;
      }
    }


    // TODO: fix all these castings and stuff about var sizes
    static void WriteRandomBilevelImageAndMetadata(ref Stream fs, ref SelfContainedBufferWriter writer, ref TIFFWriterOptions options, TIFF_Compression compression = TIFF_Compression.NoCompression)
    {
      
      fs.Dispose();
    }

    static void WriteRandomGrayScaleImageAndMetadata(ref Stream fs, ref SelfContainedBufferWriter writer, ref TIFFWriterOptions
      options, TIFF_Compression compression = TIFF_Compression.NoCompression)
    {
      int pos = 0;
      // allowed either 4 or 8
      uint bitsPerSample = 8;
      // support later
      if (compression != TIFF_Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // divide by 8 because with no compression and bilevel values are either 0 or 1 and they are packed in bits
      ulong byteCount = (uint)options.Width * (uint)options.Height / (8 / bitsPerSample);

      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      int stripSize = TIFFInternals.DEFAULT_STRIP_SIZE;

      TIFFInternals.CalculateStripAndRowInfo(byteCount, options.Height, ref stripSize, out uint stripCount, out int rowsPerStrip, out int remainder);
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
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ImageWidth, TIFF_TagSize.SHORT, 1,
        (uint)options.Width);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ImageLength, TIFF_TagSize.SHORT, 1,
        (uint)options.Height);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.BitsPerSample, TIFF_TagSize.SHORT, 1,
        bitsPerSample);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.Compression, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_Compression.NoCompression);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.PhotometricInterpretation, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_PhotometricInterpretation.WhiteIsZero);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.StripOffsetsPointer, TIFF_TagSize.LONG, stripCount,
        (uint)stripOffsetPointer);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.RowsPerStrip, TIFF_TagSize.LONG, 1,
        (uint)rowsPerStrip);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.StripByteCountsPointer, TIFF_TagSize.LONG, stripCount,
        (uint)stripCountPointer);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 2 (2 *12) + 4
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.XResolution, TIFF_TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + tagCount * 12 + 4));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.YResolution, TIFF_TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + tagCount * 12 + 4 + 8));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ResolutionUnit, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_ResolutionUnit.Inch);

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

    static void WriteRandomPaletteImageAndMetadata(ref Stream fs, ref SelfContainedBufferWriter writer, ref TIFFWriterOptions options, TIFF_Compression compression = TIFF_Compression.NoCompression)
    {
      int pos = 0;
      // allowed either 4 or 8
      uint bitsPerSample = 8;
      // support later
      if (compression != TIFF_Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // divide by 8 because with no compression and bilevel values are either 0 or 1 and they are packed in bits
      ulong byteCount = (uint)options.Width * (uint)options.Height / (8 / bitsPerSample);

      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      int stripSize = TIFFInternals.DEFAULT_STRIP_SIZE;

      TIFFInternals.CalculateStripAndRowInfo(byteCount, options.Height, ref stripSize, out uint stripCount, out int rowsPerStrip, out int remainder);
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
      int paletteChannelSize = MyMath.IntPow(2, bitsPerSample);
      // IFD
      pos = 0;
      int tagCount = 12;
      // write IFD 'header' lenght
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)tagCount);
      // These tags should be in sequence according to JHOVE validator
      // this means that they should be written from smallest to largest enum values
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ImageWidth, TIFF_TagSize.SHORT, 1,
        (uint)options.Width);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ImageLength, TIFF_TagSize.SHORT, 1,
        (uint)options.Height);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.BitsPerSample, TIFF_TagSize.SHORT, 1,
        bitsPerSample);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.Compression, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_Compression.NoCompression);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.PhotometricInterpretation, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_PhotometricInterpretation.Pallete);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.StripOffsetsPointer, TIFF_TagSize.LONG, stripCount,
        (uint)stripOffsetPointer);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.RowsPerStrip, TIFF_TagSize.LONG, 1,
        (uint)rowsPerStrip);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.StripByteCountsPointer, TIFF_TagSize.LONG, stripCount,
        (uint)stripCountPointer);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 2 (2 *12) + 4
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.XResolution, TIFF_TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + tagCount * 12 + 4));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.YResolution, TIFF_TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + tagCount * 12 + 4 + 8));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ResolutionUnit, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_ResolutionUnit.Inch);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ColorMap, TIFF_TagSize.SHORT, (ushort)(3 * paletteChannelSize),
        (uint)(fs.Position + 2 + tagCount * 12 + 4 + 16));

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

      fs.Write(writer._buffer.Slice(0, pos + 3 * 256 * 2));
      fs.Flush();
      fs.Dispose();
    }

    static void WriteRandomRGBImageAndMetadata(ref Stream fs, ref SelfContainedBufferWriter writer, ref TIFFWriterOptions
      options, TIFF_Compression compression = TIFF_Compression.NoCompression)
    {
      int pos = 0;
      // allowed either 4 or 8
      uint bitsPerSample = 8;
      uint samplesPerPixel = 3;
      // support later
      if (compression != TIFF_Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      ulong byteCount = (uint)options.Width * (uint)options.Height * samplesPerPixel;

      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      int stripSize = TIFFInternals.DEFAULT_STRIP_SIZE;

      TIFFInternals.CalculateStripAndRowInfo(byteCount, options.Height, ref stripSize, out uint stripCount, out int rowsPerStrip, out int remainder);
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
      int tagCount = 12;
      // write IFD 'header' lenght
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)tagCount);
      // These tags should be in sequence according to JHOVE validator
      // this means that they should be written from smallest to largest enum values
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ImageWidth, TIFF_TagSize.SHORT, 1,
        (uint)options.Width);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ImageLength, TIFF_TagSize.SHORT, 1,
        (uint)options.Height);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.BitsPerSample, TIFF_TagSize.SHORT, 3,
        (uint)(fs.Position + 2 + tagCount * 12 + 4));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.Compression, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_Compression.NoCompression);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.PhotometricInterpretation, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_PhotometricInterpretation.RGB);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.StripOffsetsPointer, TIFF_TagSize.LONG, stripCount,
        (uint)stripOffsetPointer);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.SamplesPerPixel, TIFF_TagSize.SHORT, 1,
        samplesPerPixel);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.RowsPerStrip, TIFF_TagSize.LONG, 1,
        (uint)rowsPerStrip);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.StripByteCountsPointer, TIFF_TagSize.LONG, stripCount,
        (uint)stripCountPointer);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 2 (2 *12) + 4 + 6 (bitsPersample 3 8's))
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.XResolution, TIFF_TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + tagCount * 12 + 4 + 6));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.YResolution, TIFF_TagSize.RATIONAL, 1,
        (uint)(fs.Position + 2 + tagCount * 12 + 4 + 6 + 8));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ResolutionUnit, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_ResolutionUnit.Inch);
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
      // bitsPerSample - 3 8s in 16 bytes each
      writer.WriteUnsigned16ToBuffer(ref pos, 8);
      writer.WriteUnsigned16ToBuffer(ref pos, 8);
      writer.WriteUnsigned16ToBuffer(ref pos, 8);
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

    

    

   
  }

  /// <summary> Options for TIFF Writer
  /// </summary>
  public class TIFFWriterOptions()
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

    public TIFF_Compression Compression = TIFF_Compression.NoCompression;
  }

  enum TIFFType
  { 
    Bilevel,
    Grayscale,
    Palette,
    RGBFullColor,
  }

}