using Converter.FileStructures.PDF;
using Converter.FileStructures.PNG;
using Converter.Utils;
using Converter.Utils.PNG;
using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;

namespace Converter.Writers.PNG
{
  // TODO: Optimize later when we suppoprt a lot more of PNG
  public static class PNGWriter
  {
    public static readonly byte[] MagicBytes = [137, 80, 78, 71, 13, 10, 26, 10];
    public static void Write(string filepath, PNGFile file)
    {
      Stream stream = File.Create(filepath);
      int arrLen = 256 * 3 + 64; // PLTE + w/e
      byte[] rentedArr = ArrayPool<byte>.Shared.Rent(arrLen);
      Span<byte> arr = rentedArr.AsSpan();
      PositionIncrBufferWriter writer = new PositionIncrBufferWriter(ref arr, false);
      CRC32Impl crc = new CRC32Impl();
      WriteIHDR(stream, file, crc, ref writer);
      if (file.ColorType == PNG_COLOR_TYPE.PALLETE)
      {
        int len = ComputeRandomPallete(file.BitDepth, rentedArr);
        WriteChunk(stream, PNG_CHUNK_TYPE.PLTE, arr.Slice(0, len), crc, ref writer, true);
      }
      WriteIDAT(stream, file, crc, ref writer);
      WriteIEND(stream, crc, ref writer);
      stream.Flush();
      stream.Close();
      stream.Dispose();
      ArrayPool<byte>.Shared.Return(rentedArr);
    }

    public static void WriteIHDR(Stream stream, PNGFile file, CRC32Impl crc, ref PositionIncrBufferWriter writer)
    {
      crc.Reset();
      int pos = 0;
      stream.Write(MagicBytes);
      writer.WriteUnsigned32ToBuffer(ref pos, 13);
      writer.WriteUnsigned32ToBuffer(ref pos, (uint)PNG_CHUNK_TYPE.IHDR);
      writer.WriteSigned32ToBuffer(ref pos, file.Width);
      writer.WriteSigned32ToBuffer(ref pos, file.Height);
      writer._buffer[pos++] = file.BitDepth;
      writer._buffer[pos++] = (byte)file.ColorType;
      writer._buffer[pos++] = (byte)file.Compression;
      writer._buffer[pos++] = 0;
      writer._buffer[pos++] = (byte)PNG_INTERLANCE.NONE;
      uint crcVal = (uint)crc.CRC(writer._buffer.Slice(4, pos - 4));
      writer.WriteUnsigned32ToBuffer(ref pos, crcVal);
      stream.Write(writer._buffer.Slice(0, pos));
    }

    // TODO: there is limit of writing singleadat and its Int32.MaxValue
    // Fix to split into multiple IDATs later
    public static void WriteIDAT(Stream stream, PNGFile file, CRC32Impl crc, ref PositionIncrBufferWriter writer)
    {
      crc.Reset();

      file.ColorSheme = PNGHelper.GetColorScheme(file.BitDepth, file.ColorType);
      uint rowSize = PNGHelper.GetRowSize(PNGHelper.GetBitsPerPixel(file.ColorSheme, file.BitDepth), file.Width);
      int pos = 0;
      // 1. We write dummy len and IDAT value 
      writer.WriteUnsigned32ToBuffer(ref pos, 0);
      writer.WriteUnsigned32ToBuffer(ref pos, (uint)PNG_CHUNK_TYPE.IDAT);

      stream.Write(writer._buffer.Slice(0, 8));
      // 2. We write compressedData and save position since we will have to go back 
      long startPos = stream.Position;
      byte[] rowBuffer = new byte[rowSize];
      ZLibStream zLib = new ZLibStream(stream, CompressionMode.Compress);

      if (file.RawIDAT != null)
        WriteSuppliedBuffer(file, zLib, file.RawIDAT, rowBuffer);
      else
        WriteRandomBuffer(file, zLib, rowBuffer);

      zLib.Flush();
      long endPos = stream.Position;

      // 3. Go back to write actual Len and calccualte CRC of compressed data;
      // -8 to be at len
      stream.Position = startPos;

      // TODO: do this better later
      uint compressedLen = (uint)(endPos - startPos);
      Span<byte> compressedData = new byte[compressedLen];
      int readBytes = stream.Read(compressedData);
      if (readBytes != compressedData.Length)
        throw new Exception("Invalid compression of data!");

      crc.UpdateCRC(writer._buffer.Slice(4, 4)); // IDAT value
      crc.UpdateCRC(compressedData);

      stream.Position = startPos - 8;
      pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, (uint)compressedData.Length);
      stream.Write(writer._buffer.Slice(0, 4));

      // 4. Go back and write CRC on the end of the chunk
      writer.WriteUnsigned32ToBuffer(ref pos, (uint)crc.GetFinalCRC());
      stream.Position = endPos;
      stream.Write(writer._buffer.Slice(4, 4));
    }

    public static void WriteIEND(Stream stream, CRC32Impl crc, ref PositionIncrBufferWriter writer)
    {
      crc.Reset();
      int pos = 0;
      writer.WriteUnsigned32ToBuffer(ref pos, 0);
      uint cName = (uint)PNG_CHUNK_TYPE.IEND;
      uint crcVal = (uint)crc.CRC(writer._buffer.Slice(4, 4));
      writer.WriteUnsigned32ToBuffer(ref pos, cName);
      writer.WriteUnsigned32ToBuffer(ref pos, crcVal);
      stream.Write(writer._buffer.Slice(0, 12));
    }

    public static void WriteSuppliedBuffer(PNGFile file, ZLibStream zLib, byte[] suppliedBuffer, byte[] row)
    {

      for (int i = 0; i < file.Height; i++)
      {
        row[0] = 0; // no filter
        Array.ConstrainedCopy(suppliedBuffer, (int)(row.Length - 1) * i, row, 1, row.Length - 1);
        zLib.Write(row, 0, row.Length);
      }
    }

    public static void WriteRandomBuffer(PNGFile file, ZLibStream zLib, byte[] row)
    {
      for (int i = 0; i < file.Height; i++)
      {
        row[0] = 0; // no filter
        Random.Shared.NextBytes(row.AsSpan(1));
        zLib.Write(row, 0, row.Length);
      }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="chunkType"></param>
    /// <param name="data"></param>
    /// <param name="crc"></param>
    /// <param name="writer"></param>
    /// <param name="writerShared">Means that writer and data are the same, so we have to do things  a bit differently</param>
    public static void WriteChunk(Stream stream, PNG_CHUNK_TYPE chunkType, Span<byte> data, CRC32Impl crc, ref PositionIncrBufferWriter writer, bool writerShared)
    {
      crc.Reset();
      int currPos = 0;
      if (writerShared)
        currPos = data.Length;
      int startPos = currPos;
      writer.WriteUnsigned32ToBuffer(ref currPos, (uint)data.Length);
      writer.WriteUnsigned32ToBuffer(ref currPos, (uint)chunkType);
      crc.UpdateCRC(writer._buffer.Slice(startPos + 4, 4));
      crc.UpdateCRC(data);
      writer.WriteUnsigned32ToBuffer(ref currPos, (uint)crc.GetFinalCRC());

      // Len + chunkType
      stream.Write(writer._buffer.Slice(startPos, 8));
      // Data
      stream.Write(data);
      // CRC
      stream.Write(writer._buffer.Slice(startPos + 8, 4));
    }

    public static int ComputeGrayPallete(byte bitDepth, byte[] buffer)
    {
      bitDepth = bitDepth > 8 ? (byte)8 : bitDepth;
      int numOfColors = MyMath.IntPow(2, bitDepth);
      for (int i = 0; i < numOfColors * 3; i+= 3)
      {
        byte val = (byte)(i / 3);
        buffer[i] = val;
        buffer[i + 1] = val;
        buffer[i + 2] = val;
      }
      return numOfColors * 3;
    }

    public static int ComputeRandomPallete(byte bitDepth, byte[] buffer)
    {
      bitDepth = bitDepth > 8 ? (byte)8 : bitDepth;
      int numOfColors = MyMath.IntPow(2, bitDepth);
      Span<byte> usableArr = buffer.AsSpan().Slice(0, numOfColors * 3);
      for (int i = 0; i < numOfColors * 3; i += 3)
      {
        Random.Shared.NextBytes(usableArr.Slice(i, 3));
      }
      return numOfColors * 3;
    }

  }
}
