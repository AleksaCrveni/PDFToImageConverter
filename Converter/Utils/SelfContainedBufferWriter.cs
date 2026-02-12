using System.Buffers.Binary;

namespace Converter.Utils
{
  /// <summary>
  /// Helper struct to write to buffer with specified options that are passed in constructor
  /// So we don't have to pass little/big endian and other future settings at each function call
  /// Difference betwween this and BufferWriter is that this object can hold some state (like little endian)
  /// </summary>
  public ref struct SelfContainedBufferWriter
  {
    public Span<byte> _buffer;
    public bool _isLittleEndian;
    public  SelfContainedBufferWriter(ref Span<byte> buffer, bool isLittleEndian = true)
    {
      _buffer = buffer;
      _isLittleEndian = isLittleEndian;
    }
    /// <summary>
    /// Writes 16 byte unsigned value to buffer, doesn't check if value can fit because we use span
    /// </summary>
    /// <param name="writeBuffer">Reference of write buffer as span</param>
    /// <param name="pos">Position within buffer that we need to write to</param>
    /// <param name="value">16 byte value to write to buffer</param>
    /// <param name="isLittleEndian">Endianess</param>
    public void WriteUnsigned16ToBuffer(ref int pos, ushort value)
    {
      // some files like TIFF can be either big or small endian so we have to write
      // that way as well
      if (_isLittleEndian)
      {
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(pos, 2), value);
        pos += 2;
      } else
      {
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.Slice(pos, 2), value);
        pos += 2;
      }
    }

    /// <summary>
    /// Writes 32 byte unsigned value to buffer, doesn't check if value can fit because we use span
    /// </summary>
    /// <param name="writeBuffer">Reference of write buffer as span</param>
    /// <param name="pos">Position within buffer that we need to write to</param>
    /// <param name="value">16 byte value to write to buffer</param>
    /// <param name="isLittleEndian">Endianess</param>
    /// TODO: can this be optmized/
    public void WriteUnsigned32ToBuffer(ref int pos, uint value)
    {
      // some files like TIFF can be either big or small endian so we have to write
      // that way as well
      if (_isLittleEndian)
      {
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(pos, 4), value);
        pos += 4;
      }
      else
      {
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.Slice(pos, 4), value);
        pos += 4;
      }
    }
  }
}
