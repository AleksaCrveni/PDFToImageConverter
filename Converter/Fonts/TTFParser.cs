using Converter.FileStructures;
using System.Buffers.Binary;
using System.Reflection.Metadata.Ecma335;


namespace Converter.Fonts
{
  /// <summary>
  /// TrueTypeFont is big endian
  /// </summary>
  public ref struct TTFParser
  {
    private ReadOnlySpan<byte> _buffer;
    private TrueTypeFont _ttf;
    private int pos;
    // size of byte in bits, for some reason some archs have non 8 bit byte size
    private int byteSize;
    // use endOfArr internally to know if you reached end of array or not
    private uint endOfArr;
    private uint beginOfSfnt;
    public void Init(ref ReadOnlySpan<byte> buffer, ref TrueTypeFont ttf)
    {
      _buffer = buffer;
      _ttf = ttf;
      InitInternal();
    }


    public void Init(ref Span<byte> buffer, ref TrueTypeFont ttf)
    {
      _buffer = (ReadOnlySpan<byte>)buffer;
      _ttf = ttf;
      InitInternal();
    }

    public void InitInternal()
    {
      pos = 0;
      byteSize = 8;
      endOfArr = 0;
      beginOfSfnt = 0;
    }

    public void Parse()
    {
      ParseFontDirectory();
    }

    public void ParseFontDirectory()
    {
      FontDirectory fd = new FontDirectory();
      TableOffsets tOff = new TableOffsets();
      
      uint scalarType = ReadUInt32();
      fd.ScalarType = scalarType switch
      {
        // true
        0x00010000 => ScalarType.True,
        0x74727565 => ScalarType.True,
        0x4F54544F => ScalarType.Otto,
        0x74797031 => ScalarType.Typ1,
        _ => throw new InvalidDataException("Invalid scalar type in the embedded true font!")
      };

      fd.NumTables = ReadUInt16();
      fd.SearchRange= ReadUInt16();
      fd.EntrySelector = ReadUInt16();
      fd.RangeShift = ReadUInt16();

      uint tag = 0;
      uint checkSum = 0;
      uint offset = 0;
      uint length;
      uint pad = 0;
      ReadOnlySpan<byte> tableBuffer;
      // TODO: Optimize to do binary search?
      for (int i = 0; i < fd.NumTables; i++)
      {
        tag = ReadUInt32();
        checkSum = ReadUInt32();
        // offset from beggning of sfnt , thats why we add here
        offset = ReadUInt32() + beginOfSfnt;
        // does not include padded bytes, so i want to include them as well to stay on long boundary
        // and tight as possible 
        length = ReadUInt32();
        pad = length % 4;
        length += pad;

        // case {num} where num is uint32 representation of 4 byte string that represent table tag
        // did this so I don't allocate string when comparing 

        // SPECIAL CASE FOR HEAD
        if (tag == 1751474532)
        {
          // do something special
        }
        tableBuffer = _buffer.Slice((int)offset, (int)length);

        if (checkSum != 0 && checkSum != CalculateCheckSum(ref tableBuffer, length))
          throw new InvalidDataException("Check sum failed!");

        switch (tag)
        {
          // cmap
          case 1668112752:
            tOff.cmap = tableBuffer;
            break;
          // glyf
          case 1735162214:
            tOff.glyf = tableBuffer;
            break;
          // head
          case 1751474532:
            tOff.head = tableBuffer;
            break;
          // hhea
          case 1751672161:
            tOff.hhea = tableBuffer;
            break;
          // hmtx
          case 1752003704:
            tOff.hmtx = tableBuffer;
            break;
          // loca
          case 1819239265:
            tOff.loca = tableBuffer;
            break;
          // maxp
          case 1835104368:
            tOff.maxp = tableBuffer;
            break;
          // name
          case 1851878757:
            tOff.name = tableBuffer;
            break;
          // post
          case 1886352244:
            tOff.post = tableBuffer;
            break;
          // cvt 
          case 1668707360:
            tOff.cvt = tableBuffer;
            break;
          // fpgm 
          case 1718642541:
            tOff.fpgm = tableBuffer;
            break;
          // hdmx 
          case 1751412088:
            tOff.hdmx = tableBuffer;
            break;
          // kern 
          case 1801810542:
            tOff.kern = tableBuffer;
            break;
          // OS/2 
          case 1330851634:
            tOff.OS_2 = tableBuffer;
            break;
          // prep 
          case 1886545264:
            tOff.prep = tableBuffer;
            break;
          default:
            // during development only!
#if DEBUG
            throw new Exception("Tag not implemented yet!");
#endif
            break;
          }
      }

      // TODO: cmpa is needed only for non CIDFont dicts
      if (
        tOff.head.Length == 0 ||
        tOff.hhea.Length == 0 ||
        tOff.loca.Length == 0 ||
        tOff.maxp.Length == 0 ||
        tOff.cvt.Length  == 0 ||
        tOff.prep.Length == 0 ||
        tOff.glyf.Length == 0 ||
        tOff.hmtx.Length == 0 ||
        tOff.fpgm.Length == 0 ||
        tOff.cmap.Length == 0   )
      {
        throw new InvalidDataException("Missing one of the required tables!");
      }

      _ttf.FontDirectory = fd;
      _ttf.Offsets = tOff;
    }

    private uint ReadUInt32()
    {
      uint res = BinaryPrimitives.ReadUInt32BigEndian(_buffer.Slice(pos, 4));
      pos += 4;
      return res;
    }
    private int ReadSignedInt32()
    {
      int res = BinaryPrimitives.ReadInt32BigEndian(_buffer.Slice(pos, 4));
      pos += 4;
      return res;
    }

    private ushort ReadUInt16()
    {
      ushort res = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Slice(pos, 2));
      pos += 2;
      return res;
    }

    private short ReadSignedInt16()
    {
      short res = BinaryPrimitives.ReadInt16BigEndian(_buffer.Slice(pos, 2));
      pos += 2;
      return res;
    }

    private byte ReadByte()
    {
      return _buffer[pos++];
    }

    private uint CalculateCheckSum(ref ReadOnlySpan<byte> tableBuffer, uint numOfBytesInTable)
    {
      uint sum = 0;
      uint nLongs = (numOfBytesInTable + 3) / 4;
      int i = 0;
      while (nLongs-- > 0)
      {
        sum += BinaryPrimitives.ReadUInt32BigEndian(tableBuffer.Slice(i, 4));
        i += 4;
      }
      return sum;
    }
  }
}
