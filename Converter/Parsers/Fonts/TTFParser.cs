using Converter.FileStructures;
using Converter.FileStructures.TIFF;
using System.Buffers.Binary;

namespace Converter.Parsers.Fonts
{
  /// <summary>
  /// TrueTypeFont is big endian
  /// Values that are passed to read functions of compared in switch cases are not random
  /// They are all defined in reference manual https://developer.apple.com/fonts/TrueType-Reference-Manual/
  /// </summary>
  public class TTFParser
  {
    private byte[] _buffer;
    private TrueTypeFont _ttf;
    // size of byte in bits, for some reason some archs have non 8 bit byte size
    private int byteSize;
    // use endOfArr internally to know if you reached end of array or not
    private uint endOfArr;
    private uint beginOfSfnt;
    
    public void Init(ref byte[]buffer)
    {
      _buffer = buffer;
      _ttf = new TrueTypeFont();
      byteSize = 8;
      endOfArr = 0;
      beginOfSfnt = 0;
    }

    public void ParseFontDirectory()
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      FontDirectory fd = new FontDirectory();
      TableOffsets tOff = new TableOffsets();
      int mainPos = 0;
      uint scalarType = ReadUInt32(ref buffer, mainPos);
      mainPos += 4;
      fd.ScalarType = scalarType switch
      {
        // true
        0x00010000 => ScalarType.True,
        0x74727565 => ScalarType.True,
        0x4F54544F => ScalarType.Otto,
        0x74797031 => ScalarType.Typ1,
        _ => throw new InvalidDataException("Invalid scalar type in the embedded true font!")
      };

      fd.NumTables = ReadUInt16(ref buffer, mainPos);
      fd.SearchRange = ReadUInt16(ref buffer, mainPos + 2);
      fd.EntrySelector = ReadUInt16(ref buffer, mainPos + 4);
      fd.RangeShift = ReadUInt16(ref buffer, mainPos + 6);
      mainPos += 8;

      uint tag = 0;
      uint checkSum = 0;
      uint offset = 0;
      uint length;
      uint pad = 0;

      FakeSpan tableBuffer;
      // TODO: Optimize to do binary search?
      for (int i = 0; i < fd.NumTables; i++)
      {
        tag = ReadUInt32(ref buffer, mainPos);
        checkSum = ReadUInt32(ref buffer, mainPos + 4);
        // offset from beggning of sfnt , thats why we add here
        offset = ReadUInt32(ref buffer, mainPos + 4 + 4) + beginOfSfnt;
        // does not include padded bytes, so i want to include them as well to stay on long boundary
        // and tight as possible 
        length = ReadUInt32(ref buffer, mainPos + 4 + 4 + 4);
        mainPos += 16;
        pad = length % 4;
        length += pad;

        // case {num} where num is uint32 representation of 4 byte string that represent table tag
        // did this so I don't allocate string when comparing 

        // SPECIAL CASE FOR HEAD
        if (tag == 1751474532)
        {
          // do something special
        }
        tableBuffer = new FakeSpan((int)offset, (int)length);

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
        tOff.cvt.Length == 0 ||
        tOff.prep.Length == 0 ||
        tOff.glyf.Length == 0 ||
        tOff.hmtx.Length == 0 ||
        tOff.fpgm.Length == 0 ||
        tOff.cmap.Length == 0)
      {
        throw new InvalidDataException("Missing one of the required tables!");
      }
      ReadOnlySpan<byte> slice = buffer.Slice(tOff.maxp.Position, tOff.maxp.Length);
      _ttf.NumOfGlyphs = ReadUInt16(ref slice, 4);
      _ttf.Svg = -1; // ??
      slice = buffer.Slice(tOff.cmap.Position, tOff.cmap.Length);
      // Find number of cmap subtables and check encodings
      ushort numOfCmapSubtables = ReadUInt16(ref slice, 2);
      ReadOnlySpan<byte> encodingSubtable;
      ushort platformID;
      ushort platformSpecificID;
      offset = 0;
      for (int i = 0; i < numOfCmapSubtables; i++)
      {
        // 4 -> skip cmap index, 8 is size of encoding subtable and there can be multiple
        encodingSubtable = slice.Slice(4 + 8 * i, 8);
        platformID = ReadUInt16(ref encodingSubtable, 0);
        platformSpecificID = ReadUInt16(ref encodingSubtable, 2);
        offset = ReadUInt32(ref encodingSubtable, 4);

        // not sure if this check is even require need to be done here, stb_truetype only does this 
        switch (platformID)
        {
          case (ushort)FileStructures.PlatformID.Microsoft:
            switch (platformSpecificID)
            {
              case (ushort)MSPlatformSpecificID.MS_UnicodeBMP:
              case (ushort)MSPlatformSpecificID.MS_UnicodeFULL:
                _ttf.IndexMapOffset = tOff.cmap.Position + (int)offset;
                break;

            }
            break;
          case (ushort)FileStructures.PlatformID.Unicode:
            _ttf.IndexMapOffset = tOff.cmap.Position + (int)offset;
            break;
          case (ushort)FileStructures.PlatformID.Macintosh:
            _ttf.IndexMapOffset = tOff.cmap.Position + (int)offset;
            break;
        }
      }

      if (_ttf.IndexMapOffset == 0)
        throw new InvalidDataException("Missing index map!");
      _ttf.CmapFormat = ReadUInt16(ref buffer, (int)_ttf.IndexMapOffset);
      slice = buffer.Slice(tOff.head.Position, tOff.head.Length);
      _ttf.IndexToLocFormat = ReadUInt16(ref slice, 50);

      _ttf.FontDirectory = fd;
      _ttf.Offsets = tOff;
    }

    /// <summary>
    /// Gets Glyphs Height Metrics via codepoint
    /// if codepoint is cached GetGlyphHMetrics could be called directly
    /// </summary>
    public void GetCodepointHMetrics(int unicodeCodepoint, ref int advanceWidth, ref int leftSideBearing)
    {
      int glyphIndex = FindGlyphIndex(unicodeCodepoint);
      GetGlyphHMetrics(glyphIndex, ref advanceWidth, ref leftSideBearing);
    }

    public void GetGlyphHMetrics(int glyphIndex, ref int advanceWidth, ref int leftSideBearing)
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      ushort numOfLongHorMetrics = ReadUInt16(ref buffer, _ttf.Offsets.hhea.Position + 34);
      if (glyphIndex < numOfLongHorMetrics)
      {
        // 4 * glyph index because each HorMetrics struct is 4 bytes (2 for advancedWidth and 2 for leftSideBearing)
        advanceWidth = ReadSignedInt16(ref buffer, _ttf.Offsets.hdmx.Position + 4 * glyphIndex);
        leftSideBearing = ReadSignedInt16(ref buffer, _ttf.Offsets.hdmx.Position + 4 * glyphIndex + 2);
      } else
      {
        advanceWidth = ReadSignedInt16(ref buffer, _ttf.Offsets.hdmx.Position + 4 * (numOfLongHorMetrics -1));
        leftSideBearing = ReadSignedInt16(ref buffer, _ttf.Offsets.hdmx.Position + 4 * numOfLongHorMetrics + 2*(glyphIndex - numOfLongHorMetrics)) ;
      }
    }

    // TODO: See if caching format data is better
    public int FindGlyphIndex(int unicodeCodepoint)
    {
      if (_ttf.CmapFormat == 0)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 2)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 4)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 6)
      {
        ReadOnlySpan<byte> buffer = _buffer.AsSpan();
        ushort length = ReadUInt16(ref buffer, _ttf.IndexMapOffset + 2);
        ushort lang = ReadUInt16(ref buffer, _ttf.IndexMapOffset + 4);
        ushort firstCode = ReadUInt16(ref buffer, _ttf.IndexMapOffset + 6);
        ushort entryCount = ReadUInt16(ref buffer, _ttf.IndexMapOffset + 8);
        // ensure its in range, codepoints are desnly packed, which means they are in continuous array in order
        if (unicodeCodepoint >= firstCode && (unicodeCodepoint - firstCode) < entryCount)
          // * 2 because data in 2 bytes each in this format
          return ReadUInt16(ref buffer, _ttf.IndexMapOffset + 10 + (unicodeCodepoint - firstCode) * 2);
        return 0;
      } else if (_ttf.CmapFormat == 8)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 10)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 12)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 13)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 14)
      {
        throw new NotImplementedException();
      }
      else
      {
        throw new InvalidDataException($"Format {_ttf.CmapFormat} is not valid CMAP format!");
      }

      return 0;
    }

    public float ScaleForPixelHeight(float lineHeight)
    {
      ReadOnlySpan<byte> hhea = _buffer.AsSpan().Slice(_ttf.Offsets.hhea.Position, _ttf.Offsets.hhea.Length);
      int fHeight = ReadSignedInt16(ref hhea, 4) - ReadSignedInt16(ref hhea, 6);
      return lineHeight / fHeight;
    }

    private uint ReadUInt32(ref ReadOnlySpan<byte> buffer, int pos)
    {
      return BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(pos, 4));
    }
    private int ReadSignedInt32(ref ReadOnlySpan<byte> buffer, int pos)
    {
      return BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(pos, 4));
    }

    private ushort ReadUInt16(ref ReadOnlySpan<byte> buffer, int pos)
    {
      return BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(pos, 2));
    }

    private short ReadSignedInt16(ref ReadOnlySpan<byte> buffer, int pos)
    {
      return BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(pos, 2));

    }

    private byte ReadByte(ref ReadOnlySpan<byte> buffer, int pos)
    {
      return buffer[pos];
    }

    private uint CalculateCheckSum(ref FakeSpan dataPos, uint numOfBytesInTable)
    {
      ReadOnlySpan<byte> tableBuffer = new ReadOnlySpan<byte>(_buffer, dataPos.Position, dataPos.Length);
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
