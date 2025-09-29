using Converter.FileStructures.TIFF;

namespace Converter.Writers.TIFF
{
  // Write empty and then save stream and be able to modify it
  public class TIFFBilevelWriter : ITIFFWriter,  IDisposable
  {
    Stream _stream;
    private bool disposedValue;
    private ulong imageDataLength;
    private BilevelData data;
    public TIFFBilevelWriter(string destination)
    {
      _stream = File.Create(destination);
      if (_stream == null)
        throw new Exception("Unable to create file stream to destination!");
      data = new BilevelData();
    }

    public TIFFBilevelWriter(Stream destinationStream)
    {
      _stream = destinationStream;
      _stream.Position = 0;
      data = new BilevelData();
    }
    public void WriteEmptyImage(ref TIFFWriterOptions options)
    {
      WriteImageMain(ref options, ImgDataMode.EMPTY);
    }
    public void WriteRandomImage(ref TIFFWriterOptions options)
    {
      if (options.Width == 0)
        options.Width = Random.Shared.Next(options.MinRandomWidth, options.MaxRandomWidth + 1);
      if (options.Height == 0)
        options.Height = Random.Shared.Next(options.MinRandomHeight, options.MaxRandomHeight + 1);
      WriteImageMain(ref options, ImgDataMode.RANDOM);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="bitmap">basically describes image data to paint, where 1 entry is 1 pixel</param>
    public void WriteIntoImageData(int x, int y, int[,] bitmap)
    {
      
    }
    public void WriteImageWithBuffer(ref TIFFWriterOptions options, byte[] buffer)
    {
      if (options.Width * options.Height != buffer.Length)
        throw new InvalidDataException("Width and Height don't match buffer supplied");
      WriteImageMain(ref options, ImgDataMode.BUFFER_SUPPLIED, buffer);
    }

    private void WriteImageMain(ref TIFFWriterOptions options, ImgDataMode mode, byte[]? suppliedBuffer = null)
    {
      Span<byte> writeBuffer = options.AllowStackAlloct ? stackalloc byte[8192] : new byte[8192];
      BufferWriter writer = new BufferWriter(ref writeBuffer, options.IsLittleEndian);
      TIFFInternals.WriteHeader(ref _stream, ref writeBuffer, options.IsLittleEndian);
      WriteRandomImage(ref writer, ref options, mode, suppliedBuffer);
    }

    private void WriteEmptyImageData(ref BufferWriter writer, ulong byteCount, ulong stripSize, int remainder)
    {
      writer._buffer.Fill(0);
      // TODO: Fill with 1 depending on isZero
      for (ulong i = 0; i < byteCount; i += (ulong)stripSize)
      {
        // read random value into each buffer stuff and then write
        // do entire buffer because we know we are in range and no need to refresh
        _stream.Write(writer._buffer);
      }

      // write remainder
      Span<byte> remainderSizeBuffer = writer._buffer.Slice(0, remainder);
      _stream.Write(remainderSizeBuffer);
    }

    private void WriteRandomImageData(ref BufferWriter writer, ulong byteCount, ulong stripSize, int remainder)
    {
      for (ulong i = 0; i < byteCount; i += (ulong)stripSize)
      {
        // read random value into each buffer stuff and then write
        Random.Shared.NextBytes(writer._buffer);
        // do entire buffer because we know we are in range and no need to refresh
        _stream.Write(writer._buffer);
      }

      // write remainder
      Span<byte> remainderSizeBuffer = writer._buffer.Slice(0, remainder);
      Random.Shared.NextBytes(remainderSizeBuffer);
      _stream.Write(remainderSizeBuffer);
    }
    private void WriteRandomImage(ref BufferWriter writer, ref TIFFWriterOptions options, ImgDataMode mode, byte[]? suppliedBuffer = null)
    {
      int pos = 0;
      // support later
      if (options.Compression != Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // divide by 8 because with no compression and bilevel values are either 0 or 1 and they are packed in bits
      ulong byteCount = (uint)options.Width * (uint)options.Height / 8;
      imageDataLength = byteCount;
      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      int stripSize = TIFFInternals.DEFAULT_STRIP_SIZE;

      TIFFInternals.CalculateStripAndRowInfo(byteCount, options.Height, ref stripSize, out uint stripCount, out int rowsPerStrip, out int remainder);
      data.StripCount = (int)stripCount;
      data.RowsPerStrip = (int)rowsPerStrip;
      data.ImageDataOffset = (int)_stream.Position;

      // Write image data
      switch (mode)
      {
        case ImgDataMode.EMPTY:
          WriteEmptyImageData(ref writer, byteCount, (ulong)stripSize, remainder);
          break;
        case ImgDataMode.RANDOM:
          WriteRandomImageData(ref writer, byteCount, (ulong)stripSize, remainder);
          break;
        case ImgDataMode.BUFFER_SUPPLIED:
          _stream.Write(suppliedBuffer);
          break;
      }

      data.StripOffsetsPointer = (int)_stream.Position;
      pos = 0;
      for (int i = 0; i < stripCount; i++)
      {
        // little endian only!
        writer.WriteUnsigned32ToBuffer(ref pos, (uint)data.ImageDataOffset);
        data.ImageDataOffset += stripSize;
      }
      _stream.Write(writer._buffer.Slice(0, pos));

      data.StripByteCounterOffsets = (int)_stream.Position;
      pos = 0;
      for (int i = 0; i < stripCount - 1; i++)
      {
        writer.WriteUnsigned32ToBuffer(ref pos, (uint)stripSize);
      }

      if (remainder == 0)
        remainder = stripSize;
      writer.WriteUnsigned32ToBuffer(ref pos, (uint)remainder);

      _stream.Write(writer._buffer.Slice(0, pos));

      pos = 0;
      WriteIFD(ref writer, ref options, ref data, ref pos);

      // write 4 bytes of 0s for next IFD address
      writer.WriteUnsigned32ToBuffer(ref pos, 0);

      // get IFD start pos before we write again
      uint IFDStartPos = (uint)_stream.Position;
      _stream.Write(writer._buffer.Slice(0, pos));

      // write IFD offset in header first 4-7 bytes
      _stream.Position = 4;
      pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, IFDStartPos);
      _stream.Write(writer._buffer.Slice(0, pos));

      // go back to end
      _stream.Seek(0, SeekOrigin.End);

      pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, 72);
      writer.WriteUnsigned32ToBuffer(ref pos, 1);

      // YRes
      writer.WriteUnsigned32ToBuffer(ref pos, 72);
      writer.WriteUnsigned32ToBuffer(ref pos, 1);

      _stream.Write(writer._buffer.Slice(0, pos));
      _stream.Flush();
        }

    private void WriteIFD(ref BufferWriter writer,ref TIFFWriterOptions options, ref BilevelData data, ref int pos)
    {
      int tagCount = 11;
      // write IFD 'header' lenght
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)tagCount);
      // These tags should be in sequence according to JHOVE validator
      // this means that they should be written from smallest to largest enum values
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageWidth, TagSize.SHORT, 1,
        (uint)options.Width);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ImageLength, TagSize.SHORT, 1,
        (uint)options.Height);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.BitsPerSample, TagSize.SHORT, 1,
        1);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.Compression, TagSize.SHORT, 1,
        (uint)Compression.NoCompression);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.PhotometricInterpretation, TagSize.SHORT, 1,
        (uint)PhotometricInterpretation.WhiteIsZero);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.StripOffsetsPointer, TagSize.LONG, (uint)data.StripCount,
        (uint)data.StripOffsetsPointer);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.RowsPerStrip, TagSize.LONG, 1,
        (uint)data.RowsPerStrip);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.StripByteCountsPointer, TagSize.LONG, (uint)data.StripCount,
        (uint)data.StripByteCounterOffsets);
      // Have to write 2 more entiries so start offsets for rationals will be _stream.Position + 2 (2 *12) + 4
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.XResolution, TagSize.RATIONAL, 1,
        (uint)(_stream.Position + 2 + tagCount * 12 + 4));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.YResolution, TagSize.RATIONAL, 1,
        (uint)(_stream.Position + 2 + tagCount * 12 + 4 + 8));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TagType.ResolutionUnit, TagSize.SHORT, 1,
        (uint)ResolutionUnit.Inch);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          _stream.Dispose();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        // TODO: set large fields to null
        disposedValue = true;
      }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~TIFFBilevelWriter()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }
}