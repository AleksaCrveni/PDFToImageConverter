using Converter.FileStructures.TIFF;
using Converter.Utils;
using System.Diagnostics;
using System.Security.Principal;

namespace Converter.Writers.TIFF
{
  public class TIFFRGBWriter : ITIFFWriter, IDisposable
  {
    Stream _stream;
    private bool disposedValue;
    private ulong imageDataLength;
    public TIFF_RGBData data;


    public TIFFRGBWriter(string destination)
    {
      _stream = File.Create(destination);
      if (_stream == null)
        throw new Exception("Unable to create file stream to destination!");
      data = new TIFF_RGBData();
    }

    public TIFFRGBWriter(Stream destinationStream)
    {
      _stream = destinationStream;
      _stream.Position = 0;
      data = new TIFF_RGBData();
    }
    public void WriteEmptyImage(ref TIFFWriterOptions options)
    {
      WriteImageMain(ref options, TIFF_ImgDataMode.EMPTY);
    }

    public void WriteImageWithBuffer(ref TIFFWriterOptions options, byte[] buffer)
    {
      if ((options.Width * options.Height * 3 * options.BitsPerSample / 8f) != buffer.Length)
        throw new InvalidDataException("Width and Height don't match buffer supplied");
      WriteImageMain(ref options, TIFF_ImgDataMode.BUFFER_SUPPLIED, buffer);
    }
    public void WriteRandomImage(ref TIFFWriterOptions options)
    {
      if (options.Width == 0)
        options.Width = Random.Shared.Next(options.MinRandomWidth, options.MaxRandomWidth + 1);
      if (options.Height == 0)
        options.Height = Random.Shared.Next(options.MinRandomHeight, options.MaxRandomHeight + 1);
      WriteImageMain(ref options, TIFF_ImgDataMode.RANDOM);
    }
    public void WriteIntoImageData(int x, int y, int[,] bitmap)
    {
      throw new NotImplementedException();
    }
    private void WriteImageMain(ref TIFFWriterOptions options, TIFF_ImgDataMode mode, byte[]? suppliedBuffer = null)
    {
      Span<byte> writeBuffer = options.AllowStackAlloct ? stackalloc byte[8192] : new byte[8192];
      PositionIncrBufferWriter writer = new PositionIncrBufferWriter(ref writeBuffer, options.IsLittleEndian);
      TIFFInternals.WriteHeader(ref _stream, ref writeBuffer, options.IsLittleEndian);
      WriteImage(ref writer, ref options, mode, suppliedBuffer);
    }
    public void WriteEmptyImageData(ref PositionIncrBufferWriter writer, ulong byteCount, ulong stripSize, int remainder)
    {
      writer._buffer.Fill(0);
      for (ulong i = 0; i < byteCount; i += (ulong)stripSize)
      {
        // read random value into each buffer stuff and then write
        // do entire buffer because we know we are in range and no need to refresh
        _stream.Write(writer._buffer);
      }

      Span<byte> remainderSizeBuffer = writer._buffer.Slice(0, remainder);
      _stream.Write(remainderSizeBuffer);
    }
    public void WriteSuppliedBufferData(byte[] suppliedBuffer)
    {
      _stream.Write(suppliedBuffer);
    }
    public void WriteRandomImageData(ref PositionIncrBufferWriter writer, ulong byteCount, ulong stripSize, int remainder)
    {
      for (ulong i = 0; i < byteCount; i += (ulong)stripSize)
      {
        // read random value into each buffer stuff and then write
        Random.Shared.NextBytes(writer._buffer);
        // do entire buffer because we know we are in range and no need to refresh
        _stream.Write(writer._buffer);
      }

      Span<byte> remainderSizeBuffer = writer._buffer.Slice(0, remainder);
      Random.Shared.NextBytes(remainderSizeBuffer);
      _stream.Write(remainderSizeBuffer);
    }
    public void WriteImage(ref PositionIncrBufferWriter writer, ref TIFFWriterOptions options, TIFF_ImgDataMode mode, byte[]? suppliedBuffer = null)
    {
      int pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, 8);

      data.BitsPerSample = options.BitsPerSample;
      data.SamplesPerPixel = 3;
      ulong byteCount = (uint)options.Width * (uint)options.Height * (uint)data.SamplesPerPixel;

      // write in ~8k Strips
      // smallest stripsize that can be used where rowsPerStrip will be whole number
      // get closest number to 8192 that is dividable by 8192 / options.height
      int stripSize = TIFFInternals.DEFAULT_STRIP_SIZE;

      TIFFInternals.CalculateStripAndRowInfo(byteCount, options.Height, ref stripSize, out uint stripCount, out int rowsPerStrip, out int remainder);
      data.StripCount = (int)stripCount;
      data.RowsPerStrip = (int)rowsPerStrip;
      Debug.Assert(stripCount == 1 && rowsPerStrip == options.Height);
      
      int tagCount = 12;
      int ifdSectionSize = tagCount * 12;
      ifdSectionSize += 4; // 4 to zero next IFD
      ifdSectionSize += 2; // 2 for tagCount on start of IFD
      data.ImageDataOffset = (int)_stream.Position + ifdSectionSize + 22 + 4; // 22 bytes after ifd and zero section for bits persample, xres and yres and 4 for ifd position at the start
      data.InitialImageDataOffset = (int)_stream.Position + ifdSectionSize + 22 + 4;
      data.StripOffsetsPointer = data.ImageDataOffset;
      data.StripByteCounterOffsets = (uint)byteCount;

      WriteIFD(ref writer, ref options, ref data, ref pos, tagCount);
      // write 4 bytes of 0s for next IFD address
      writer.WriteUnsigned32ToBuffer(ref pos, 0);
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




      _stream.Write(writer._buffer.Slice(0, pos));
      // make this optionable
      Debug.Assert(_stream.Position == data.ImageDataOffset);
      // support later
      if (options.Compression != TIFF_Compression.NoCompression)
        throw new NotImplementedException("This Compression not suppported yet!");

      // write data
      switch (mode)
      {
        case TIFF_ImgDataMode.EMPTY:
          WriteEmptyImageData(ref writer, byteCount, (ulong)stripSize, remainder);
          break;
        case TIFF_ImgDataMode.RANDOM:
          WriteRandomImageData(ref writer, byteCount, (ulong)stripSize, remainder);
          break;
        case TIFF_ImgDataMode.BUFFER_SUPPLIED:
          WriteSuppliedBufferData(suppliedBuffer);
          break;

      }

      _stream.Flush();
    }

    private void WriteIFD(ref PositionIncrBufferWriter writer, ref TIFFWriterOptions options, ref TIFF_RGBData data, ref int pos, int tagCount)
    {
      writer.WriteUnsigned16ToBuffer(ref pos, (ushort)tagCount);

      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ImageWidth, TIFF_TagSize.SHORT, 1,
         (uint)options.Width);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ImageLength, TIFF_TagSize.SHORT, 1,
        (uint)options.Height);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.BitsPerSample, TIFF_TagSize.SHORT, 3,
        (uint)(_stream.Position + 6 + tagCount * 12 + 4)); 
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.Compression, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_Compression.NoCompression);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.PhotometricInterpretation, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_PhotometricInterpretation.RGB);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.StripOffsetsPointer, TIFF_TagSize.LONG,1 ,
        (uint)data.StripOffsetsPointer);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.SamplesPerPixel, TIFF_TagSize.SHORT, 1,
        (uint)data.SamplesPerPixel);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.RowsPerStrip, TIFF_TagSize.LONG, 1,
        (uint)data.RowsPerStrip);
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.StripByteCountsPointer, TIFF_TagSize.LONG, 1,
        (uint)data.StripByteCounterOffsets);
      // Have to write 2 more entiries so start offsets for rationals will be fs.Position + 6 (2 *12) + 4 + 6 (bitsPersample 3 8's))
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.XResolution, TIFF_TagSize.RATIONAL, 1,
        (uint)(_stream.Position + 6 + tagCount * 12 + 4 + 6));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.YResolution, TIFF_TagSize.RATIONAL, 1,
        (uint)(_stream.Position + 6 + tagCount * 12 + 4 + 6 + 8));
      TIFFInternals.WriteIFDEntryToBuffer(ref writer, ref pos, TIFF_TagType.ResolutionUnit, TIFF_TagSize.SHORT, 1,
        (uint)TIFF_ResolutionUnit.Inch);
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
