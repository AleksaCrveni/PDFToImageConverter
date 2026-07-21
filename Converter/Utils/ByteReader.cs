using System.Buffers.Binary;

namespace Converter.Utils
{
  /// <summary>
  /// 3213155623th implementation of binary reader on this forsaken project, hopefully this would be final one and I will eventually migrate others to this
  /// ProperImplementaiotn of _bufferReader where owner doesn't have to hold track of _pos
  /// </summary>
  public ref struct ByteReader
  {
    public ReadOnlySpan<byte> _buffer;
    private int _pos = 0;

    public ByteReader(ReadOnlySpan<byte> buffer) => _buffer = buffer;
    public ByteReader(Span<byte> buffer) => _buffer = buffer;
    public ByteReader(byte[] buffer) => _buffer = buffer.AsSpan();

    #region General
    /// <summary>
    /// Copies
    /// </summary>
    /// <param name="_buffer"></param>
    /// <param name="_pos"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    public byte[] ReadNextNBytes(int n)
    {
      byte[] arr = _buffer.Slice(_pos, n).ToArray();
      _pos += n;
      return arr;
    }

    public ReadOnlySpan<byte> SliceBuffer(int n)
    {
      _pos += n;
      return _buffer.Slice(_pos - n, n);
    }

    public byte ReadByte() => _buffer[_pos++];
    /// <summary>
    /// NOTE(@Aleksa) Don't care to check size atm since if its out of index something upstream went wrong
    /// </summary>
    /// <returns></returns>
    public byte PeekByte() => _buffer[_pos];
    public void SkipNextByte() => _pos++;
    #endregion General

    #region LittleEndian
    public int ReadInt32LE()
    {
      int num = BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(_pos, 4));
      _pos += 4;
      return num;
    }

    public uint ReadUInt32LE()
    {
      uint num = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(_pos, 4));
      _pos += 4;
      return num;
    }

    public short ReadInt16LE()
    {
      short num = BinaryPrimitives.ReadInt16LittleEndian(_buffer.Slice(_pos, 2));
      _pos += 2;
      return num;
    }

    public ushort ReadUInt16LE()
    {
      ushort num = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Slice(_pos, 2));
      _pos += 2;
      return num;
    }

    public double ReadDoubleLE()
    {
      double num = BinaryPrimitives.ReadDoubleLittleEndian(_buffer.Slice(_pos, 8));
      _pos += 8;
      return num;
    }

    #endregion LittleEndian

    #region BigEndian
    public int ReadInt32BE()
    {
      int num = BinaryPrimitives.ReadInt32BigEndian(_buffer.Slice(_pos, 4));
      _pos += 4;
      return num;
    }

    public uint ReadUInt32BE()
    {
      uint num = BinaryPrimitives.ReadUInt32BigEndian(_buffer.Slice(_pos, 4));
      _pos += 4;
      return num;
    }

    public short ReadInt16BE()
    {
      short num = BinaryPrimitives.ReadInt16BigEndian(_buffer.Slice(_pos, 2));
      _pos += 2;
      return num;
    }

    public ushort ReadUInt16BE()
    {
      ushort num = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Slice(_pos, 2));
      _pos += 2;
      return num;
    }

    public double ReadDoubleBE()
    {
      double num = BinaryPrimitives.ReadDoubleBigEndian(_buffer.Slice(_pos, 8));
      _pos += 8;
      return num;
    }

    #endregion BigEndian

    public void SetPos(int pos) => _pos = pos;
    public int GetPos() => _pos;
    public bool IsEOF() => _pos >= _buffer.Length;
  }
}
