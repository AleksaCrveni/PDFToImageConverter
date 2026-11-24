using Converter.FileStructures.TIFF;
using Converter.Utils;

namespace Converter.Writers.TIFF
{
  public class TIFFInternals
  {
    public static readonly int DEFAULT_STRIP_SIZE = 8192;
    public static void WriteIFDEntryToBuffer(ref PositionIncrBufferWriter writer, ref int pos, TIFF_TagType tag, TIFF_TagSize t, uint count, uint valueOrOffset)
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

    /// <summary>
    /// fuck strips and offsets and stuff, just write as one strip almost all of the readers read it that way anyway
    /// </summary>
    /// <param name="byteCount"></param>
    /// <param name="height"></param>
    /// <param name="stripSize"></param>
    /// <param name="stripCount"></param>
    /// <param name="rowsPerStrip"></param>
    /// <param name="remainder"></param>
    public static void CalculateStripAndRowInfo(ulong byteCount, int height, ref int stripSize, out uint stripCount, out int rowsPerStrip, out int remainder)
    {
      stripSize = (int)byteCount;
      rowsPerStrip = height;
      stripCount = 1;
      remainder = 0;
    }

    // No need to use BufferWriter its ok
    public static void WriteHeader(ref Stream fs, ref Span<byte> writeBuffer, bool isLittleEndian = true)
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
     

      // maybe dont have to write instantly
      fs.Write(writeBuffer.Slice(0, 4));
    }
  }
}
