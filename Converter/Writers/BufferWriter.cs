using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converter.Writers
{
  /// <summary>
  /// Helper struct to write to buffer with specified options that are passed in constructor
  /// So we don't have to pass little/big endian and other future settings at each function call
  /// </summary>
  public ref struct BufferWriter
  {
    public Span<byte> _buffer;
    public bool _isLittleEndian;
    public  BufferWriter(ref Span<byte> buffer, bool isLittleEndian = true)
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
        _buffer[pos++] = (byte)value;
        _buffer[pos++] = (byte)(value >> 8);
      } else
      {
        _buffer[pos++] = (byte)(value >> 8);
        _buffer[pos++] = (byte)value;
      }
    }

    /// <summary>
    /// Writes 32 byte unsigned value to buffer, doesn't check if value can fit because we use span
    /// </summary>
    /// <param name="writeBuffer">Reference of write buffer as span</param>
    /// <param name="pos">Position within buffer that we need to write to</param>
    /// <param name="value">16 byte value to write to buffer</param>
    /// <param name="isLittleEndian">Endianess</param>
    public void WriteUnsigned32ToBuffer(ref int pos, uint value)
    {
      // some files like TIFF can be either big or small endian so we have to write
      // that way as well
      if (_isLittleEndian)
      {
        _buffer[pos++] = (byte)value;
        _buffer[pos++] = (byte)(value >> 8);
        _buffer[pos++] = (byte)(value >> 16);
        _buffer[pos++] = (byte)(value >> 24);
      }
      else
      {
        _buffer[pos++] = (byte)(value >> 24);
        _buffer[pos++] = (byte)(value >> 16);
        _buffer[pos++] = (byte)(value >> 8);
        _buffer[pos++] = (byte)value;
      }
    }
  }
}
