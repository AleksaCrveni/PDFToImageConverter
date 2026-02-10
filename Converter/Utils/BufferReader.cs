using System.Buffers.Binary;

namespace Converter.Utils
{
  public static class BufferReader
  {
    public static int ReadInt32IntLittleEndian(ref Span<byte> buffer, ref int pos)
    {
      int num = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }
  }
}
