using Converter.FileStructures.PNG;
using Converter.Utils;
using Converter.Utils.PNG;
using System.IO.Compression;

namespace Converter.Writers.PNG
{
  public static class PNGWriter
  {
    public static readonly byte[] MagicBytes = [137, 80, 78, 71, 13, 10, 26, 10];
    public static void Write(string filepath, PNGFile file)
    {
      CRC32Impl crc = new CRC32Impl();
      Stream stream = File.Create(filepath);
      int pos = 0;
      byte[] mem = new byte[120];
      Span<byte> arr = mem.AsSpan();
      PositionIncrBufferWriter buffer = new PositionIncrBufferWriter(ref arr, false);
      Array.Copy(MagicBytes, mem, MagicBytes.Length);
      pos += MagicBytes.Length;
      buffer.WriteUnsigned32ToBuffer(ref pos, 13);
      buffer.WriteUnsigned32ToBuffer(ref pos, (uint)PNG_CHUNK_TYPE.IHDR);
      buffer.WriteSigned32ToBuffer(ref pos, file.Width);
      buffer.WriteSigned32ToBuffer(ref pos, file.Height);
      arr[pos++] = file.BitDepth;
      arr[pos++] = (byte)file.ColorType;
      arr[pos++] = (byte)file.Compression;
      arr[pos++] = 0;
      arr[pos++] = (byte)PNG_INTERLANCE.NONE;
      uint crcVal = (uint)crc.CRC(arr.Slice(4, pos - 4));
      buffer.WriteUnsigned32ToBuffer(ref pos, crcVal);
      stream.Write(arr.Slice(0, pos));

      WriteIDAT(stream, file, file.RawIDAT);
      WriteIEND(stream);
      stream.Flush();
    }
    // TODO: there is limit of writing singleadat and its Int32.MaxValue
    // Fix to split into multiple IDATs later
    public static void WriteIDAT(Stream stream, PNGFile file, byte[] rawData)
    {
      uint rowSize = PNGHelper.GetRowSize(PNGHelper.GetBitsPerPixel(file.ColorSheme, file.BitDepth), file.Width);
      byte[] mem = new byte[12];
      Span<byte> tempBuffer = mem.AsSpan();
      PositionIncrBufferWriter buffer = new PositionIncrBufferWriter(ref tempBuffer, false);
      int pos = 0;
      // 1. We write dummy len and IDAT value 
      buffer.WriteUnsigned32ToBuffer(ref pos, 0);
      buffer.WriteUnsigned32ToBuffer(ref pos, (uint)PNG_CHUNK_TYPE.IDAT);

      stream.Write(tempBuffer.Slice(0, 8));
      // 2. We write compressedData and save position since we will have to go back 
      long startPos = stream.Position;
      byte[] rowData = new byte[rowSize];
      ZLibStream zLib = new ZLibStream(stream, CompressionMode.Compress);
      for (int i = 0; i < file.Height; i++)
      {
        rowData[0] = 0; // no filter
        //Random.Shared.NextBytes(rowData.AsSpan(1));
        Array.ConstrainedCopy(rawData, (int)(rowSize - 1) * i, rowData, 1, rowData.Length - 1);
        zLib.Write(rowData, 0, rowData.Length);
      }
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

      CRC32Impl crc = new CRC32Impl();
      crc.UpdateCRC(tempBuffer.Slice(4, 4)); // IDAT value
      crc.UpdateCRC(compressedData);

      stream.Position = startPos - 8;
      pos = 0;
      buffer.WriteUnsigned32ToBuffer(ref pos, (uint)compressedData.Length);
      stream.Write(tempBuffer.Slice(0, 4));

      // 4. Go back and write CRC on the end of the chunk
      buffer.WriteUnsigned32ToBuffer(ref pos, (uint)crc.GetFinalCRC());
      stream.Position = endPos;
      stream.Write(tempBuffer.Slice(4, 4));
    }

    public static void WriteIEND(Stream stream)
    {
      CRC32Impl crc = new CRC32Impl();
      byte[] mem = new byte[12];
      Span<byte> arr = mem.AsSpan();
      PositionIncrBufferWriter buffer = new PositionIncrBufferWriter(ref arr, false);
      int pos = 0;
      buffer.WriteUnsigned32ToBuffer(ref pos, 0);
      uint cName = (uint)PNG_CHUNK_TYPE.IEND;
      uint crcVal = (uint)crc.CRC(arr.Slice(4, 4));
      buffer.WriteUnsigned32ToBuffer(ref pos, cName);
      buffer.WriteUnsigned32ToBuffer(ref pos, crcVal);
      stream.Write(arr.Slice(0, 12));
    }
  }
}
