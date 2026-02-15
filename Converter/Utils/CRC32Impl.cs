namespace Converter.Utils
{
  public class CRC32Impl
  {
    public ulong[] Table;
    public ulong c;
    
    public CRC32Impl()
    {
      InitCRC32Table();
      Reset();
    }

    public void InitCRC32Table()
    {
      Table = new ulong[256];
      ulong c;
      int n, k;
      for (n = 0; n < 256; n++)
      {
        c = (ulong) n;
        for (k = 0; k < 8; k++)
        {
          if ((c & 1) != 0)
            c = 0xedb88320L ^ (c >> 1);
          else
            c = c >> 1;
        }
        Table[n] = c;
      }
    }
    
    public void UpdateCRC(byte[] buffer)
    {
      UpdateCRC(buffer.AsSpan<byte>());
    }

    public void UpdateCRC(ReadOnlySpan<byte> buffer)
    {
      int n;
      for (n = 0; n < buffer.Length; n++)
        c = Table[(c ^ buffer[n]) & 0xFF] ^ (c >> 8);
    }


    public ulong CRC(byte[] buffer)
    {
      return CRC(buffer.AsSpan());
    }

    public ulong CRC(ReadOnlySpan<byte> buffer)
    {
      Reset();
      UpdateCRC(buffer);
      return c ^ 0xFFFFFFFF;
    }

    public bool VerifyCheckSum(uint crc32) => crc32 == (c ^ 0xFFFFFFFF);

    public void Reset() => c = 0xFFFFFFFF;
  }
}
