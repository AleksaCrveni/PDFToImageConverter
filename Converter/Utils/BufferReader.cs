using System.Buffers.Binary;

namespace Converter.Utils
{
  // TODO: Force inline?
  public static class BufferReader
  {
    #region General
    /// <summary>
    /// Copies
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="pos"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    public static byte[] ReadNextNBytes(ref Span<byte> buffer, ref int pos, int n)
    {
      byte[] arr = buffer.Slice(pos, n).ToArray();
      pos += n;
      return arr;
    }
    #endregion General

    #region LittleEndian
    public static int ReadInt32LE(ref Span<byte> buffer, ref int pos)
    {
      int num = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }

    public static uint ReadUInt32LE(ref Span<byte> buffer, ref int pos)
    {
      uint num = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }

    public static short ReadInt16LE(ref Span<byte> buffer, ref int pos)
    {
      short num = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(pos, 2));
      pos += 2;
      return num;
    }

    public static ushort ReadUInt16LE(ref Span<byte> buffer, ref int pos)
    {
      ushort num = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(pos, 2));
      pos += 2;
      return num;
    }

    public static int ReadInt32LE(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      int num = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }

    public static uint ReadUInt32LE(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      uint num = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }

    public static short ReadInt16LE(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      short num = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(pos, 2));
      pos += 2;
      return num;
    }

    public static ushort ReadUInt16LE(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ushort num = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(pos, 2));
      pos += 2;
      return num;
    }
    #endregion LittleEndian

    #region BigEndian
    public static int ReadInt32BE(ref Span<byte> buffer, ref int pos)
    {
      int num = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }

    public static uint ReadUInt32BE(ref Span<byte> buffer, ref int pos)
    {
      uint num = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }

    public static short ReadInt16BE(ref Span<byte> buffer, ref int pos)
    {
      short num = BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(pos, 2));
      pos += 2;
      return num;
    }

    public static ushort ReadUInt16BE(ref Span<byte> buffer, ref int pos)
    {
      ushort num = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
      pos += 2;
      return num;
    }

    public static int ReadInt32BE(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      int num = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }

    public static uint ReadUInt32BE(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      uint num = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
      pos += 4;
      return num;
    }

    public static short ReadInt16BE(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      short num = BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(pos, 2));
      pos += 2;
      return num;
    }

    public static ushort ReadUInt16BE(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ushort num = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
      pos += 2;
      return num;
    }

    #endregion BigEndian
  }
}
