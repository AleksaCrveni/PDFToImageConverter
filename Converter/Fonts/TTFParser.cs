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
    private uint endOfArr;
    private uint beginOfSfnt;
    public void Init(ref TrueTypeFont _ttf, ReadOnlySpan<byte> buffer)
    {
      _buffer = buffer;
      InternalInit(ref _ttf);
     
    }

    public void Init(ref TrueTypeFont _ttf, Span<byte> buffer)
    {
      _buffer = buffer;
      InternalInit(ref _ttf);
    }
    private void InternalInit(ref TrueTypeFont _ttf)
    {
      ttf = _ttf;
      pos = 0;
      byteSize = 8;
      endOfArr = 0;
      beginOfSfnt = 8;
    }
    
    
    public void Parse()
    {
      ParseFontDirectory();
    }

    public void ParseFontDirectory()
    {
      FontDirectory fd = new FontDirectory();
      uint scalarType = ReadUInt32();
      fd.ScalarType = scalarType switch
      {
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
      // TODO: Optimize to do binary search
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
        if (tag == 1684104552)
        {
          // do something special
        }
        switch (tag)
        {
          // cmap
          case 1885433187:
            ttf.cmap = _buffer.Slice((int)offset, (int)length);
            break;
          // glyf
          case 1719233639:
            ttf.glyf = _buffer.Slice((int)offset, (int)length);
            break;
          // head
          case 1684104552:
            ttf.head = _buffer.Slice((int)offset, (int)length);
            break;
          // hhea
          case 1634035816:
            ttf.hhea = _buffer.Slice((int)offset, (int)length);
            break;
          // hmtx
          case 2020896104:
            ttf.hmtx = _buffer.Slice((int)offset, (int)length);
            break;
          // loca
          case 1633906540:
            ttf.loca = _buffer.Slice((int)offset, (int)length);
            break;
          // maxp
          case 1886937453:
            ttf.maxp = _buffer.Slice((int)offset, (int)length);
            break;
          // name
          case 1701667182:
            ttf.name = _buffer.Slice((int)offset, (int)length);
            break;
          // post
          case 1953722224:
            ttf.post = _buffer.Slice((int)offset, (int)length);
            break;
          // cvt 
          case 544503395:
            ttf.cvt = _buffer.Slice((int)offset, (int)length);
            break;
          // fpgm 
          case 1835495526:
            ttf.fpgm = _buffer.Slice((int)offset, (int)length);
            break;
          // hdmx 
          case 2020435048:
            ttf.hdmx = _buffer.Slice((int)offset, (int)length);
            break;
          // kern 
          case 1852990827:
            ttf.kern = _buffer.Slice((int)offset, (int)length);
            break;
          // OS/2 
          case 841962319:
            ttf.OS_2 = _buffer.Slice((int)offset, (int)length);
            break;
          // prep 
          case 1885696624:
            ttf.prep = _buffer.Slice((int)offset, (int)length);
            break;
          default:
            // during development only!
#if DEBUG
            throw new Exception("Tag not implemented yet!");
#endif
            break;
        }
      }
    }

    private uint ReadUInt32()
    {
      uint res = BitConverter.ToUInt32(_buffer.Slice(pos, 4));
      pos += 4;
      return res;
    }
    private int ReadSignedInt32()
    {
      int res = BitConverter.ToInt32(_buffer.Slice(pos, 4));
      pos += 4;
      return res;
    }

    private ushort ReadUInt16()
    {
      ushort res = BitConverter.ToUInt16(_buffer.Slice(pos, 2));
      pos += 2;
      return res;
    }

    private short ReadSignedInt16()
    {
      short res = BitConverter.ToInt16(_buffer.Slice(pos, 2));
      pos += 2;
      return res;
    }

    private byte ReadByte()
    {
      return _buffer[pos++];
    }
  }
}
