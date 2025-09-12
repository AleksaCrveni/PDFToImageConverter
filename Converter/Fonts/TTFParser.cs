using Converter.FileStructures;
using System.Reflection.Metadata.Ecma335;


namespace Converter.Fonts
{
  public ref struct TTFParser
  {
    public ReadOnlySpan<byte> _buffer;
    private TrueTypeFont ttf;
    private int pos;
    // size of byte in bits, for some reason some archs have non 8 bit byte size
    private int byteSize;
    // use endOfArr internally to know if you reached end of array or not
    public void Init(ref TrueTypeFont _ttf, ReadOnlySpan<byte> buffer)
    {
      _buffer = buffer;
      ttf = _ttf;
      pos = 0;
      byteSize = 8;
    }

    public void Init(ref TrueTypeFont _ttf, Span<byte> buffer)
    {
      _buffer = buffer;
      ttf = _ttf;
      pos = 0;
      byteSize = 8;
    }

    private uint ReadUInt32(ref uint endOfArr)
    {
      if (pos + byteSize * 4 > _buffer.Length)
      {
        endOfArr = 1;
        return 0;
      }
      uint res = BitConverter.ToUInt32(_buffer.Slice(pos, 4));
      pos += 4;
      return res;
    }
    private int ReadSignedInt32(ref uint endOfArr)
    {
      if (pos + byteSize * 4 > _buffer.Length)
      {
        endOfArr = 1;
        return 0;
      }
      int res = BitConverter.ToInt32(_buffer.Slice(pos, 4));
      pos += 4;
      return res;
    }

    private ushort ReadUInt16(ref uint endOfArr)
    {
      if (pos + byteSize * 2 > _buffer.Length)
      {
        endOfArr = 1;
        return 0;
      }
      ushort res = BitConverter.ToUInt16(_buffer.Slice(pos, 2));
      pos += 2;
      return res;
    }

    private short ReadSignedInt16(ref uint endOfArr)
    {
      if (pos + byteSize * 2 > _buffer.Length)
      {
        endOfArr = 1;
        return 0;
      }
      short res = BitConverter.ToInt16(_buffer.Slice(pos, 2));
      pos += 2;
      return res;
    }

    private byte ReadByte(ref uint endOfArr)
    {
      if (pos + byteSize > _buffer.Length)
      {
        endOfArr = 1;
        return 0;
      }

      return _buffer[pos++];
    }
  }
}
