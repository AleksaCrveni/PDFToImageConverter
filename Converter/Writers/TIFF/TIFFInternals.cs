using Converter.FileStructures.TIFF;
using Converter.Utils;

namespace Converter.Writers.TIFF
{
  public class TIFFInternals
  {
    public static readonly int DEFAULT_STRIP_SIZE = 8192;
    public static void WriteIFDEntryToBuffer(ref SelfContainedBufferWriter writer, ref int pos, TIFF_TagType tag, TIFF_TagSize t, uint count, uint valueOrOffset)
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

    public static void CalculateStripAndRowInfo(ulong byteCount, int height, ref int stripSize, out uint stripCount, out int rowsPerStrip, out int remainder)
    {
      stripSize = stripSize - stripSize % height;
      stripCount = (uint)Math.Ceiling(byteCount / (decimal)stripSize);
      if ((uint)byteCount % stripSize > 0)
        stripCount++;

      rowsPerStrip = height - 1 / (int)stripCount;
      remainder = Convert.ToInt32((uint)byteCount % stripSize);
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
      // write IFD after data later
      writeBuffer[4] = 0;
      writeBuffer[5] = 0;
      writeBuffer[6] = 0;
      writeBuffer[7] = 0;

      // maybe dont have to write instantly
      fs.Write(writeBuffer.Slice(0, 8));
    }
  }
}
