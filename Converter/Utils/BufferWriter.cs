using System.Buffers.Binary;

namespace Converter.Utils
{
  public static class BufferWriter
  {
    #region LittleEndian
    public static void WriteInt32LE(ref Span<byte> buffer, ref int pos, int value)
    {
      BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(pos, 4), value);
      pos += 4;
    }

    public static void WriteUInt32LE(ref Span<byte> buffer, ref int pos, uint value)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(pos, 4), value);
      pos += 4;
    }

    public static void WriteInt16LE(ref Span<byte> buffer, ref int pos, short value)
    {
      BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(pos, 2), value);
      pos += 2;
    }

    public static void WriteUInt16LE(ref Span<byte> buffer, ref int pos, ushort value)
    {
      BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(pos, 2), value);
      pos += 2;
    }
    #endregion LittleEndian

    #region BigEndian
    public static void WriteInt32BE(ref Span<byte> buffer, ref int pos, int value)
    {
      BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(pos, 4), value);
      pos += 4;
    }

    public static void WriteUInt32BE(ref Span<byte> buffer, ref int pos, uint value)
    {
      BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(pos, 4), value);
      pos += 4;
    }

    public static void WriteInt16BE(ref Span<byte> buffer, ref int pos, short value)
    {
      BinaryPrimitives.WriteInt16BigEndian(buffer.Slice(pos, 2), value);
      pos += 2;
    }

    public static void WriteUInt16BE(ref Span<byte> buffer, ref int pos, ushort value)
    {
      BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(pos, 2), value);
      pos += 2;
    }
    #endregion BigEndian
  }
}
