using Converter.FileStructures;
using Converter.FileStructures.TIFF;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public RASTERIZER_VERSION RasterizerVersion = RASTERIZER_VERSION.V2;
    public void Init(ref byte[] buffer)
    {
      _buffer = buffer;
      _ttf = new TrueTypeFont();
      byteSize = 8;
      endOfArr = 0;
      beginOfSfnt = 0;
    }

    public void InitFont()
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
      uint computedCheckSum = 0;
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
        tableBuffer = new FakeSpan((int)offset, (int)length);
        // SPECIAL CASE FOR HEAD
        if (tag == 1751474532)
        {
          // do something special
        }
        else
        {
          computedCheckSum = CalculateCheckSum(ref tableBuffer, length);
          if (checkSum != 0 && checkSum != computedCheckSum)
            throw new InvalidDataException($"Check sum failed! For tag {tag}. Expected: {checkSum}. Computed: {computedCheckSum}");
        }

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
          // gpos
          case 1196445523:
            tOff.gpos = tableBuffer;
            break;
          default:
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


    public void GetGlyphHMetrics(int glyphIndex, ref int advanceWidth, ref int leftSideBearing)
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      ushort numOfLongHorMetrics = ReadUInt16(ref buffer, _ttf.Offsets.hhea.Position + 34);
      if (glyphIndex < numOfLongHorMetrics)
      {
        // 4 * glyph index because each HorMetrics struct is 4 bytes (2 for advancedWidth and 2 for leftSideBearing)
        advanceWidth = ReadSignedInt16(ref buffer, _ttf.Offsets.hmtx.Position + 4 * glyphIndex);
        leftSideBearing = ReadSignedInt16(ref buffer, _ttf.Offsets.hmtx.Position + 4 * glyphIndex + 2);
      } else
      {
        advanceWidth = ReadSignedInt16(ref buffer, _ttf.Offsets.hmtx.Position + 4 * (numOfLongHorMetrics - 1));
        leftSideBearing = ReadSignedInt16(ref buffer, _ttf.Offsets.hmtx.Position + 4 * numOfLongHorMetrics + 2 * (glyphIndex - numOfLongHorMetrics));
      }
    }

    // TODO: See if caching format data is better
    public int FindGlyphIndex(int unicodeCodepoint)
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      if (_ttf.CmapFormat == 0)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 2)
      {
        throw new NotImplementedException();
      } else if (_ttf.CmapFormat == 4)
      {
        // std mapping for windows fonts: binary search collection of ranges (indexes are not tight as in format 6)
        // TODO: instead of >> 1, just /2 ??
        ushort segCount = (ushort)(ReadUInt16(ref buffer, _ttf.IndexMapOffset + 6) >> 1); // >> 1 is basically / 2 
        ushort searchRange = (ushort)(ReadUInt16(ref buffer, _ttf.IndexMapOffset + 8) >> 1);
        ushort entrySelector = ReadUInt16(ref buffer, _ttf.IndexMapOffset + 10);
        ushort rangeShift = (ushort)(ReadUInt16(ref buffer, _ttf.IndexMapOffset + 12) >> 1);

        // do a binary search of the segments
        uint endCount = (uint)_ttf.IndexMapOffset + 14;
        uint search = endCount;

        if (unicodeCodepoint > ushort.MaxValue)
          return 0;

        // they lie from endCount .. endCount + segCount
        // but searchRange is the nearest power of two, so...
        if (unicodeCodepoint >= ReadUInt16(ref buffer, (int)(_ttf.StartOffset + search + rangeShift * 2)))
          search += (uint)rangeShift * 2;

        // now decrement to bias correctly to find smaallest
        search -= 2;
        while (entrySelector > 0)
        {
          ushort end;
          searchRange >>= 1;
          end = ReadUInt16(ref buffer, (int)(_ttf.StartOffset + search + searchRange * 2));
          if (unicodeCodepoint > end)
            search += (uint)searchRange * 2;
          entrySelector--;
        }
        search += 2;

        // do I really need separate scope?
        {
          ushort offset, start, last;
          ushort item = (ushort)((search - endCount) >> 1);

          start = ReadUInt16(ref buffer, (int)(_ttf.StartOffset + _ttf.IndexMapOffset + 14 + segCount * 2 + 2 + 2 * item));
          last = ReadUInt16(ref buffer, (int)(_ttf.StartOffset + endCount + 2 * item));
          if (unicodeCodepoint < start || unicodeCodepoint > last)
            return 0;

          offset = ReadUInt16(ref buffer, (int)(_ttf.StartOffset + _ttf.IndexMapOffset + segCount * 6 + 2 + 2 * item));
          if (offset == 0)
            return (ushort)(unicodeCodepoint + ReadUInt16(ref buffer, (int)(_ttf.StartOffset + _ttf.IndexMapOffset + 14 + segCount * 4 + 2 + 2 * item)));

          return ReadUInt16(ref buffer, (int)(_ttf.StartOffset + offset + (unicodeCodepoint - start) * 2 + _ttf.IndexMapOffset + 14 + segCount * 6 + 2 + 2 * item));
        }
      } else if (_ttf.CmapFormat == 6)
      {
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


    /// <summary>
    /// Gets Glyphs Height Metrics via codepoint
    /// if codepoint is cached GetGlyphHMetrics could be called directly
    /// </summary>
    public void GetCodepointHMetrics(int unicodeCodepoint, ref int advanceWidth, ref int leftSideBearing)
    {
      int glyphIndex = FindGlyphIndex(unicodeCodepoint);
      GetGlyphHMetrics(glyphIndex, ref advanceWidth, ref leftSideBearing);
    }

    public void GetFontVMetrics(ref int ascent, ref int descent, ref int lineGap)
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      ascent = ReadSignedInt16(ref buffer, _ttf.Offsets.hhea.Position + 4);
      descent = ReadSignedInt16(ref buffer, _ttf.Offsets.hhea.Position + 6);
      lineGap = ReadSignedInt16(ref buffer, _ttf.Offsets.hhea.Position + 8);
    }

    public float ScaleForPixelHeight(float lineHeight)
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      int fHeight = ReadSignedInt16(ref buffer, _ttf.Offsets.hhea.Position + 4) - ReadSignedInt16(ref buffer, _ttf.Offsets.hhea.Position + 6);
      return lineHeight / fHeight;
    }
    public int GetGlyphOffset(ref ReadOnlySpan<byte> buffer, int glyphIndex)
    {
      int g1, g2;
      // glyph index out of range
      if (glyphIndex >= _ttf.NumOfGlyphs)
        return -1;
      // uknown index -> glyph map format
      if (_ttf.IndexToLocFormat >= 2)
        return -1;

      if (_ttf.IndexToLocFormat == 0)
      {
        g1 = _ttf.Offsets.glyf.Position + ReadUInt16(ref buffer, _ttf.Offsets.loca.Position + glyphIndex * 2) * 2;
        g2 = _ttf.Offsets.glyf.Position + ReadUInt16(ref buffer, _ttf.Offsets.loca.Position + glyphIndex * 2 + 2) * 2;
      } else
      {
        // not sure about this cast
        g1 = _ttf.Offsets.glyf.Position + (int)ReadUInt32(ref buffer, _ttf.Offsets.loca.Position + glyphIndex * 4);
        g2 = _ttf.Offsets.glyf.Position + (int)ReadUInt32(ref buffer, _ttf.Offsets.loca.Position + glyphIndex * 4 + 4);
      }

      // if length is 0 return -1??
      if (g1 == g2)
        return -1;

      return g1;
    }
    public bool GetGlyphBox(int glyphIndex, ref int xMin, ref int yMin, ref int xMax, ref int yMax)
    {
      // TODO: if cff = true its open type font?
      if (_ttf.Cff)
      {
        throw new NotImplementedException();
      } else
      {

        ReadOnlySpan<byte> buffer = _buffer.AsSpan();
        int g = GetGlyphOffset(ref buffer, glyphIndex);
        if (g < 0)
          return false;

        xMin = ReadSignedInt16(ref buffer, g + 2);
        yMin = ReadSignedInt16(ref buffer, g + 4);
        xMax = ReadSignedInt16(ref buffer, g + 6);
        yMax = ReadSignedInt16(ref buffer, g + 8);
      }
      return true;
    }

    #region antialiasing software rasterizer
    public void GetGlyphBitmapBoxSubpixel(int glyphIndex, float scaleX, float scaleY, float shiftX, float shiftY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
      int x0 = 0;
      int y0 = 0;
      int x1 = 0;
      int y1 = 0;
      // gets contour points of the glyph
      if (!GetGlyphBox(glyphIndex, ref x0, ref y0, ref x1, ref y1))
      {
        ix0 = 0;
        iy0 = 0;
        ix1 = 0;
        iy1 = 0;
      } else
      {
        // is this subpixel since we round it?
        ix0 = (int)Math.Floor(x0 * scaleX + shiftX);
        iy0 = (int)Math.Floor(-y1 * scaleY + shiftY);
        ix1 = (int)Math.Ceiling(x1 * scaleX + shiftX);
        iy1 = (int)Math.Ceiling(-y0 * scaleY + shiftY);
      }
    }

    public void GetGlyphBitmapBox(int glyphIndex, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
      GetGlyphBitmapBoxSubpixel(glyphIndex, scaleX, scaleY, 0, 0, ref ix0, ref iy0, ref ix1, ref iy1);
    }

    public void GetCodepointBitmapBoxSubpixel(int unicodeCodepoint, float scaleX, float scaleY, float shiftX, float shiftY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
      int glyphIndex = FindGlyphIndex(unicodeCodepoint);
      GetGlyphBitmapBoxSubpixel(glyphIndex, scaleX, scaleY, shiftX, shiftY, ref ix0, ref iy0, ref ix1, ref iy1);
    }

    public void GetCodepointBitmapBox(int unicodeCodepoint, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
    {
      GetCodepointBitmapBoxSubpixel(unicodeCodepoint, scaleX, scaleY, 0, 0, ref ix0, ref iy0, ref ix1, ref iy1);
    }
    #endregion antialiasing software rasterizer

    #region kerning

    public int GetGlyphKernInfoAdvance(int glyphIndex1, int glyphIndex2)
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan(_ttf.Offsets.kern.Position, _ttf.Offsets.kern.Length);
      uint needle, straw;

      int l, r, m;

      // we only look at the first table. it must be 'horizontal' and format 0.
      if (ReadUInt16(ref buffer, 2) < 1) // number of tables, need at least 1
        return 0;
      if (ReadUInt16(ref buffer, 8) != 1) // horizontal flag must be set in format
        return 0;

      l = 0;
      r = ReadUInt16(ref buffer, 10) - 1;
      needle = (uint)glyphIndex1 << 16 | (uint)glyphIndex2;
      while (l <= r)
      {
        m = (l + r) >> 1;
        straw = ReadUInt32(ref buffer, 18 + (m * 6)); // note: unaligned read
        if (needle < straw)
          r = m - 1;
        else if (needle > straw)
          l = m + 1;
        else
          return ReadUInt16(ref buffer, 22 + (m * 6));
      }
      return 0;
    }

    public int GetGlyphGPOSInfoAdvance(int glyphIndex1, int glyphIndex2)
    {
      throw new NotImplementedException("Open type not supported yet");
    }

    public int GetGlyphKernAdvance(int glyphIndex1, int glyphIndex2)
    {
      int xAdvance = 0;
      if (_ttf.Offsets.gpos.Length > 0)
        xAdvance += GetGlyphGPOSInfoAdvance(glyphIndex1, glyphIndex2);
      else if (_ttf.Offsets.kern.Length > 0)
        xAdvance += GetGlyphKernInfoAdvance(glyphIndex1, glyphIndex2);
      return xAdvance;
    }

    public int GetCodepointKernAdvance(int ch1, int ch2)
    {
      if (_ttf.Offsets.kern.Length == 0 && _ttf.Offsets.gpos.Length == 0) // if no kerning table, don't waste time looking up both codepoint->glyphs
        return 0;
      return GetGlyphKernAdvance(FindGlyphIndex(ch1), FindGlyphIndex(ch2));
    }
    #endregion kerning

    #region rasterizer

    public float SizedTriangleArea(float height, float width)
    {
      return height * width / 2;
    }
    public float SizedTrapezoidArea(float height, float topWidth, float bottomWidth)
    {
      Debug.Assert(topWidth >= 0);
      Debug.Assert(bottomWidth >= 0);
      return (topWidth + bottomWidth) / 2.0f * height;
    }
    public float PositionTrapezoidArea(float height, float tx0, float tx1, float bx0, float bx1)
    {
      return SizedTrapezoidArea(height, tx1 - tx0, bx1 - bx0);
    }

    // the edge passed in here does not cross the vertical line at x or the vertical line at x+1
    // (i.e. it has already been clipped to those)
    public void HandleClippedEdgeV2(Span<float> scanline, int x, ref ActiveEdgeV2 edge, float x0, float y0, float x1, float y1)
    {
      if (y0 == y1)
        return;
      Debug.Assert(y0 < y1);
      Debug.Assert(edge.sy <= edge.ey);
      if (y0 > edge.ey)
        return;
      if (y1 < edge.sy)
        return;
      if (y0 < edge.sy)
      {
        x0 += (x1 - x0) * (edge.sy - y0) / (y1 - y0);
        y0 = edge.sy;
      }
      if (y1 > edge.ey)
      {
        x1 += (x1 - x0) * (edge.ey - y1) / (y1 - y0);
        y1 = edge.ey;
      }

      if (x0 == x)
        Debug.Assert(x1 <= x + 1);
      else if (x0 == x + 1)
        Debug.Assert(x1 >= x);
      else if (x0 <= x)
        Debug.Assert(x1 <= x);
      else if (x0 >= x + 1)
        Debug.Assert(x1 >= x + 1);
      else
        Debug.Assert(x1 >= x && x1 <= x + 1);

      if (x0 <= x && x1 <= x)
        scanline[x] += edge.direction * (float)(y1 - y0);
      else if (x0 >= x + 1 && x1 >= x + 1)
        ;
      else
      {
        Debug.Assert(x0 >= x && x0 <= x + 1 && x1 >= x && x1 <= x + 1);
        scanline[x] += edge.direction * (y1 - y0) * (1 - ((x0 - x) + (x1 - x)) / 2); // coverage = 1 - average x position
      }
    }

    public void FillActiveEdgesNewV2(Span<float> scanline, Span<float> scanline2, int len, ref List<ActiveEdgeV2> activeEdges, float yTop)
    {
      float yBottom = yTop + 1;
      int i;
      ActiveEdgeV2 edge;
      Span<float> scanlineFill = scanline2.Slice(1);
      for (i =0; i < activeEdges.Count; i++)
      {
        // brute force every pixel

        // compute intersection points with top & bottom
        edge = activeEdges[i];
        Debug.Assert(edge.ey >= yTop);

        if (edge.fdx == 0)
        {
          float x0 = edge.fx;
          if (x0 < len)
          {
            if (x0 >= 0)
            {
              HandleClippedEdgeV2(scanline, (int)x0, ref edge, x0, yTop, x0, yBottom);
              HandleClippedEdgeV2(scanline2, (int)x0 + 1, ref edge, x0, yTop, x0, yBottom);
            }
            else
            {
              HandleClippedEdgeV2(scanline2, 0, ref edge, x0, yTop, x0, yBottom);
            }
          }
        }
        else
        {
          float x0 = edge.fx;
          float dx = edge.fdx;
          float xb = x0 + dx;
          float xTop, xBottom;
          float sy0, sy1;
          float dy = edge.fdy;
          Debug.Assert(edge.sy <= yBottom && edge.ey >= yTop);

          // compute endpoints of line segment clipped to this scanline (if the
          // line segment starts on this scanline. x0 is the intersection of the
          // line with y_top, but that may be off the line segment.

          if (edge.sy > yTop)
          {
            xTop = x0 + dx * (edge.sy - yTop);
            sy0 = edge.sy;
          }
          else
          {
            xTop = x0;
            sy0 = yTop;
          }
          if (edge.ey < yBottom)
          {
            xBottom = x0 + dx * (edge.ey - yTop);
            sy1 = edge.ey;
          }
          else
          {
            xBottom = xb;
            sy1 = yBottom;
          }

          if (xTop >= 0 && xBottom >= 0 && xTop < len && xBottom < len)
          {
            // from here on, we don't have to range check x values

            if ((int)xTop == (int)xBottom)
            {
              float height;
              // simple case, only spans one pixel
              int x = (int)xTop;
              height = (sy1 - sy0) * edge.direction;
              Debug.Assert(x >= 0 && x < len);
              scanline[x] += PositionTrapezoidArea(height, xTop, x + 1.0f, xBottom, x + 1.0f);
              scanlineFill[x] += height; // everything right of this pixel is filled
            }
            else
            {
              int x, x1, x2;
              float yCrossing, yFinal, step, sign, area;
              // covers 2+ pixels
              if (xTop > xBottom)
              {
                // flip scanline vertically; signed area is the same
                float t;
                sy0 = yBottom - (sy0 - yTop);
                sy1 = yBottom - (sy1 - yTop);
                t = sy0;
                sy0 = sy1;
                sy1 = t;

                t = xBottom;
                xBottom = xTop;
                xTop = t;

                dx = -dx;
                dy = -dy;
                t = x0;
                x0 = xb;
                xb = t;
              }

              Debug.Assert(dy >= 0);
              Debug.Assert(dx >= 0);

              x1 = (int)xTop;
              x2 = (int)xBottom;

              // compute intersection with y axis at x1+1
              yCrossing = yTop + dy * (x1 + 1 - x0);

              // compute intersection with y axis at x2
              yFinal = yTop + dy * (x2 - x0);

              //           x1    x_top                            x2    x_bottom
              //     y_top  +------|-----+------------+------------+--------|---+------------+
              //            |            |            |            |            |            |
              //            |            |            |            |            |            |
              //       sy0  |      Txxxxx|............|............|............|............|
              // y_crossing |            *xxxxx.......|............|............|............|
              //            |            |     xxxxx..|............|............|............|
              //            |            |     /-   xx*xxxx........|............|............|
              //            |            | dy <       |    xxxxxx..|............|............|
              //   y_final  |            |     \-     |          xx*xxx.........|............|
              //       sy1  |            |            |            |   xxxxxB...|............|
              //            |            |            |            |            |            |
              //            |            |            |            |            |            |
              //  y_bottom  +------------+------------+------------+------------+------------+
              //
              // goal is to measure the area covered by '.' in each pixel

              // if x2 is right at the right edge of x1, y_crossing can blow up, github #1057
              // @TODO: maybe test against sy1 rather than y_bottom?
              if (yCrossing > yBottom)
                yCrossing = yBottom;

              sign = edge.direction;

              // area of the rectangle covered from sy0..yCrossing
              area = sign * (yCrossing - sy0);

              // area of the triangle (x_top,sy0), (x1+1,sy0), (x1+1,yCrossing)
              scanline[x1] += SizedTriangleArea(area, x1 + 1 - xTop);

              // check if final yCrossing is blown up; no test case for this
              if (yFinal > yBottom)
              {
                yFinal = yBottom;
                dy = (yFinal - yCrossing) / (x2 - (x1 + 1)); // if denom=0, y_final = y_crossing, so y_final <= y_bottom
              }

              // in second pixel, area covered by line segment found in first pixel
              // is always a rectangle 1 wide * the height of that line segment; this
              // is exactly what the variable 'area' stores. it also gets a contribution
              // from the line segment within it. the THIRD pixel will get the first
              // pixel's rectangle contribution, the second pixel's rectangle contribution,
              // and its own contribution. the 'own contribution' is the same in every pixel except
              // the leftmost and rightmost, a trapezoid that slides down in each pixel.
              // the second pixel's contribution to the third pixel will be the
              // rectangle 1 wide times the height change in the second pixel, which is dy.

              step = sign * dy * 1; // dy is dy/dx, change in y for every 1 change in x,
                                    // which multiplied by 1-pixel-width is how much pixel area changes for each step in x
                                    // so the area advances by 'step' every time

              for (x = x1 + 1; x < x2; ++x)
              {
                scanline[x] += area + step / 2; // area of trapezoid is 1*step/2
                area += step;
              }
              Debug.Assert(MathF.Abs(area) <= 1.01f); // accumulated error from area += step unless we round step down
              Debug.Assert(sy1 > yFinal - 0.01f);

              // area covered in the last pixel is the rectangle from all the pixels to the left,
              // plus the trapezoid filled by the line segment in this pixel all the way to the right edge
              scanline[x2] += area + sign * PositionTrapezoidArea(sy1 - yFinal, (float)x2, x2 + 1.0f, xBottom, x2 + 1.0f);

              // the rest of the line is filled based on the total height of the line segment in this pixel
              scanlineFill[x2] += sign * (sy1 - sy0);
            }
          }
          else
          {
            // if edge goes outside of box we're drawing, we require
            // clipping logic. since this does not match the intended use
            // of this library, we use a different, very slow brute
            // force implementation
            // note though that this does happen some of the time because
            // x_top and x_bottom can be extrapolated at the top & bottom of
            // the shape and actually lie outside the bounding box
            int x;

            for (x = 0; x < len; ++x)
            {
              // cases:
              //
              // there can be up to two intersections with the pixel. any intersection
              // with left or right edges can be handled by splitting into two (or three)
              // regions. intersections with top & bottom do not necessitate case-wise logic.
              //
              // the old way of doing this found the intersections with the left & right edges,
              // then used some simple logic to produce up to three segments in sorted order
              // from top-to-bottom. however, this had a problem: if an x edge was epsilon
              // across the x border, then the corresponding y position might not be distinct
              // from the other y segment, and it might ignored as an empty segment. to avoid
              // that, we need to explicitly produce segments based on x positions.

              // rename variables to clearly-defined pairs
              float y0 = yTop;
              float x1 = (float)(x);
              float x2 = (float)(x + 1);
              float x3 = xb;
              float y3 = yBottom;

              // x = e->x + e->dx * (y-yTop)
              // (y-yTop) = (x - e->x) / e->dx
              // y = (x - e->x) / e->dx + yTop
              float y1 = (x - x0) / dx + yTop;
              float y2 = (x + 1 - x0) / dx + yTop;

              if (x0 < x1 && x3 > x2)
              {         // three segments descending down-right
                HandleClippedEdgeV2(scanline, x, ref edge, x0, y0, x1, y1);
                HandleClippedEdgeV2(scanline, x, ref edge, x1, y1, x2, y2);
                HandleClippedEdgeV2(scanline, x, ref edge, x2, y2, x3, y3);
              }
              else if (x3 < x1 && x0 > x2)
              {  // three segments descending down-left
                HandleClippedEdgeV2(scanline, x, ref edge, x0, y0, x2, y2);
                HandleClippedEdgeV2(scanline, x, ref edge, x2, y2, x1, y1);
                HandleClippedEdgeV2(scanline, x, ref edge, x1, y1, x3, y3);
              }
              else if (x0 < x1 && x3 > x1)
              {  // two segments across x, down-right
                HandleClippedEdgeV2(scanline, x, ref edge, x0, y0, x1, y1);
                HandleClippedEdgeV2(scanline, x, ref edge, x1, y1, x3, y3);
              }
              else if (x3 < x1 && x0 > x1)
              {  // two segments across x, down-left
                HandleClippedEdgeV2(scanline, x, ref edge, x0, y0, x1, y1);
                HandleClippedEdgeV2(scanline, x, ref edge, x1, y1, x3, y3);
              }
              else if (x0 < x2 && x3 > x2)
              {  // two segments across x+1, down-right
                HandleClippedEdgeV2(scanline, x, ref edge, x0, y0, x2, y2);
                HandleClippedEdgeV2(scanline, x, ref edge, x2, y2, x3, y3);
              }
              else if (x3 < x2 && x0 > x2)
              {  // two segments across x+1, down-left
                HandleClippedEdgeV2(scanline, x, ref edge, x0, y0, x2, y2);
                HandleClippedEdgeV2(scanline, x, ref edge, x2, y2, x3, y3);
              }
              else
              {  // one segment
                HandleClippedEdgeV2(scanline, x, ref edge, x0, y0, x3, y3);
              }
            }
          }
        }
      }
    }

    public ActiveEdgeV2 NewActiveEdgeV2(ref TTFEdge edge, int offX, float startPoint)
    {
      ActiveEdgeV2 aEdges;
      float dxdy = (edge.x1 - edge.x0) / (edge.y1 - edge.y0);
      aEdges.fdx = dxdy;
      aEdges.fdy = dxdy != 0.0f ? (1.0f / dxdy) : 0.0f;
      aEdges.fx = edge.x0 + dxdy * (startPoint- edge.y0);
      aEdges.fx -= offX;
      aEdges.direction = edge.Invert ? 1.0f : -1.0f;
      aEdges.sy = edge.y0;
      aEdges.ey = edge.y1;
      return aEdges;
    }
    // directly AA rasterize edges w/o supersampling
    public void RasterizeSortedEdgesV2(ref BmpS result, List<TTFEdge> edges, int n, int vSubSample, int offX, int offY)
    {
      // List is just native implementation to get things going, its not most efficient because we are doing random removal of elements
      // TODO: fix later
      List<ActiveEdgeV2> activeEdges = new List<ActiveEdgeV2>();
      int y, i;
      int j = 0;
      Span<float> scanlineData = new float[129]; // ? why 129
      Span<float> scanline;
      Span<float> scanline2;

      // Why is vSubSample passed here???
      if (result.W > 64)
        scanline = new float[result.W * 2 + 1];
      else
        scanline = scanlineData; // ??
      scanline2 = scanline.Slice(result.W);

      y = offY;
      TTFEdge edge = edges[n];
      edge.y0 = (offY + result.H) + 1;
      edges[n] = edge;
      ActiveEdgeV2 z;
      i = 0;
      int eIndex = 0;
      while (j < result.H)
      {
        float scanYTop = y;
        float scanYBottom = y + 1;

        // update active edges
        // remove all active edges that terminate before the top of this scanline
        i = 0;
        while (i < activeEdges.Count)
        {
          z = activeEdges[i];
          if (z.ey <= scanYTop)
          {
            Debug.Assert(z.direction != 0);
            activeEdges.RemoveAt(i);
          }
          else
          {
            i++;
          }
        }

        // insert all edges that start before the bottom of this scanline 
        edge = edges[eIndex];
        while (edge.y0 <= scanYBottom && i < edges.Count)
        {
          if (edge.y0 != edge.y1)
          {
            z = NewActiveEdgeV2(ref edge, offX, scanYTop);
            if (j == 0 && offY != 0)
            {
              if (z.ey < scanYTop)
              {
                // this can happen due to subpixel positioning and some kind of fp rounding error i think
                z.ey = scanYTop;
              }
            }
            Debug.Assert(z.ey >= scanYTop, "z.ey is bigger than scanYTop");

            activeEdges.Insert(0, z);
          }
          eIndex++;
          edge = edges[eIndex];
        }

        // Fix bug here, scanlines aren't filled properly
        if (activeEdges.Count > 0)
          FillActiveEdgesNewV2(scanline, scanline2, result.W, ref activeEdges, scanYTop);

        {
          float sum = 0;
          for (i = 0; i < result.W; i++)
          {
            float k;
            int m;
            sum += scanline2[i];
            k = scanline[i] + sum;
            k = MathF.Abs(k) * 255 + 0.5f;
            m = (int) k;
            if (m > 255)
              m = 255;
            result.Pixels[result.Offset + j * result.Stride + i] = (byte)m;
          }
        }

        // advance all the edges
        for (i = 0; i < activeEdges.Count; i++)
        {
          z = activeEdges[i];
          z.fx += z.fdx;
          activeEdges[i] = z;
        }

        y++;
        j++;
      }
    }


    /// <summary>
    /// returns true if y0 of first edge is smaller than y0 of second edge
    /// </summary>
    /// <returns>bool</returns>
    private bool CompareEdge(TTFEdge a, TTFEdge b)
    {
      return a.y0 < b.y0;
    }

    private void SortEdgesInsSort(ref Span<TTFEdge> edges, int n)
    {
      int i, j;
      TTFEdge tempEdge;
      TTFEdge a;
      for (i = 1; i < n; i++)
      {
        tempEdge = edges[i];
        a = tempEdge;
        j = i;
        while (j > 0)
        {
          Span<TTFEdge> b = edges.Slice(j - 1);
          bool c = CompareEdge(a, b[0]);
          if (!c)
            break;

          edges[j] = edges[j - 1];
          j--;
        }

        if (i != j)
          edges[j] = tempEdge;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="edges"></param>
    /// <param name="sp">start position, to imitate pointer arithmetic, fix later</param>
    /// <param name="n"></param>
    private void SortEdgesQuickSort(ref Span<TTFEdge> oRefEdges, int n, bool startSecond = false)
    {
      Span<TTFEdge> edges;
      if (startSecond == false)
        edges = oRefEdges;
      else
        edges = oRefEdges.Slice(1);
      TTFEdge tempEdge;
      while (n > 12)
      {
        tempEdge = new TTFEdge();
        int m, i, j;
        bool c01, c12, c;
        // compute median of three
        m = n >> 1;
        c01 = CompareEdge(edges[0], edges[m]);
        c12 = CompareEdge(edges[m], edges[n-1]);
        /* if 0 >= mid >= end, or 0 < mid < end, then use mid */
        if (c01 != c12)
        {
          /* otherwise, we'll need to swap something else to middle */
          int z;
          c = CompareEdge(edges[0], edges[n - 1]);
          /* 0>mid && mid<n:  0>n => n; 0<n => 0 */
          /* 0<mid && mid>n:  0>n => 0; 0<n => n */
          z = (c == c12) ? 0 : n - 1;
          tempEdge = edges[z];
          edges[z] = edges[m];
          edges[m] = tempEdge;
        }
        /* now p[m] is the median-of-three */
        /* swap it to the beginning so it won't move around */
        tempEdge = edges[0];
        edges[0] = edges[m];
        edges[m] = tempEdge;

        /* partition loop */
        i = 1;
        j = n - 1;
        for (; ; )
        {
          /* handling of equality is crucial here */
          /* for sentinels & efficiency with duplicates */
          for (; ; ++i)
          {
            if (!CompareEdge(edges[i], edges[0])) break;
          }
          for (; ; --j)
          {
            if (!CompareEdge(edges[0], edges[j])) break;
          }
          /* make sure we haven't crossed */
          if (i >= j) break;
          tempEdge = edges[i];
          edges[i] = edges[j];
          edges[j] = tempEdge;

          ++i;
          --j;
        }

        /* recurse on smaller side, iterate on larger */
        if (j < (n - i))
        {
          SortEdgesQuickSort(ref edges, j);
          oRefEdges = edges.Slice(1);
          n = n - i;
        }
        else
        {
          SortEdgesQuickSort(ref edges, n - i, true);
          n = j;
        }

      }
    }

    // TODO: edges should be array
    private void SortEdges(ref List<TTFEdge> edges, int n)
    {
      // TODO: Make edges array instead of list, so we don't have to copy
      TTFEdge[] edgesArr = edges.ToArray();
      Span<TTFEdge> edgesSpan = edgesArr.AsSpan();

      SortEdgesQuickSort(ref edgesSpan, n);
      SortEdgesInsSort(ref edgesSpan, n);

      edges = edgesArr.ToList();
    }

    private void InternalRasterize(ref BmpS result, ref List<PointF> points, ref List<int> wCount, int windings, float scaleX, float scaleY, float shiftX, float shiftY, int offX, int offY, bool invert)
    {
      float yScaleInv = invert ? -scaleY : scaleY;
      List<TTFEdge> edges = new List<TTFEdge>();
      int n, i, j, k, m;

      int vSubSample;
      if (RasterizerVersion == RASTERIZER_VERSION.V1)
      {
        vSubSample = result.H < 8 ? 15 : 5;
      }
      else
      {
        vSubSample = 1;
      }

      n = 0;
      // TODO: optimize this
      for (i = 0; i < windings; ++i)
        n += wCount[i];

      for (i = 0; i < n + 1 ; i++)
        edges.Add(new TTFEdge());

      n = 0;
      m = 0;
      TTFEdge e;
      for (i = 0; i < windings; ++i)
      {
        int pointsIndex = m;
        m += wCount[i];
        j = wCount[i] - 1;
        for (k = 0; k < wCount[i]; j = k++)
        {
          int a = k;
          int b = j;

          // skip the edge if horizontal
          if (points[pointsIndex + j].Y == points[pointsIndex + k].Y)
            continue;
          // add edge from j to k to the list
          e = new TTFEdge();
          e.Invert = false;
          if (invert ? points[pointsIndex + j].Y > points[pointsIndex + k].Y : points[pointsIndex + j].Y < points[pointsIndex + k].Y)
          {
            e.Invert = true;
            a = j;
            b = k;
          }
          e.x0 = points[pointsIndex + a].X * scaleX + shiftX;
          e.y0 = (points[pointsIndex + a].Y * yScaleInv + shiftY) * vSubSample;
          e.x1 = points[pointsIndex + b].X * scaleX + shiftX;
          e.y1 = (points[pointsIndex + b].Y * yScaleInv + shiftY) * vSubSample;

          edges[n] = e;
          n++;
        }
      }

      // now sort the edges by their highest point (should snap to integer, and then by x)
      //STBTT_sort(e, n, sizeof(e[0]), stbtt__edge_compare);
      // DEBUG: here actual count is 19
      SortEdges(ref edges, n);
      
      // TODO:  scanelines rasterization for both V1 and V2
      if (RasterizerVersion == RASTERIZER_VERSION.V1)
      {

      }
      else
      {
        RasterizeSortedEdgesV2(ref result, edges, n, vSubSample, offX, offY);
      }
    }

    public void TesselateCubic(List<PointF> points, ref int numOfPoints, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float objspaceFlatnessSquared, int n)
    {
      // TODO: this "flatness" calculation is just made-up nonsense that seems to work well enough
      float dx0 = x1 - x0;
      float dy0 = y1 - y0;
      float dx1 = x2 - x1;
      float dy1 = y2 - y1;
      float dx2 = x3 - x2;
      float dy2 = y3 - y2;
      float dx = x3 - x0;
      float dy = y3 - y0;
      float longLen = (float)(Math.Sqrt(dx0 * dx0 + dy0 * dy0) + Math.Sqrt(dx1 * dx1 + dy1 * dy1) + Math.Sqrt(dx2 * dx2 + dy2 * dy2));
      float shortLen = (float)Math.Sqrt(dx * dx + dy * dy);
      float flatness_squared = longLen * longLen - shortLen * shortLen;

      if (flatness_squared > objspaceFlatnessSquared)
      {
        float x01 = (x0 + x1) / 2;
        float y01 = (y0 + y1) / 2;
        float x12 = (x1 + x2) / 2;
        float y12 = (y1 + y2) / 2;
        float x23 = (x2 + x3) / 2;
        float y23 = (y2 + y3) / 2;

        float xa = (x01 + x12) / 2;
        float ya = (y01 + y12) / 2;
        float xb = (x12 + x23) / 2;
        float yb = (y12 + y23) / 2;

        float mx = (xa + xb) / 2;
        float my = (ya + yb) / 2;

        TesselateCubic(points, ref numOfPoints , x0, y0, x01, y01, xa, ya, mx, my, objspaceFlatnessSquared, n + 1);
        TesselateCubic(points, ref numOfPoints , mx, my, xb, yb, x23, y23, x3, y3, objspaceFlatnessSquared, n + 1);
      }
      else
      {
        AddPoint(points, numOfPoints, x3, y3);
        numOfPoints++;
      }
    }

    // tesselate until threshold p is happy
    // TODO: warped to compensate for non-linearn stretching (??)
    public int TesselateCurve(List<PointF> points, ref int numOfPoints, float x0, float y0, float x1, float y1, float x2, float y2, float objspaceFlatnessSquared, int n)
    {
      // midpoint
      float mx = (x0 + 2 * x1 + x2) / 4;
      float my = (y0 + 2 * y1 + y2) / 4;
      // versus directly drawn line
      float dx = (x0 + x2) / 2 - mx;
      float dy = (y0 + y2) / 2 - my;

      if (n > 16) // 65536 segments on one curve better be enough!
        return 1;
      if (dx * dx + dy * dy > objspaceFlatnessSquared) // half-pixel error allowed.... need to be smaller if AA
      {
        TesselateCurve(points, ref numOfPoints, x0, y0, (x0 + x1) / 2.0f, (y0 + y1) / 2.0f, mx, my, objspaceFlatnessSquared, n + 1);
        TesselateCurve(points, ref numOfPoints, mx, my, (x1 + x2) / 2.0f, (y1 + y2) / 2.0f, x2, y2, objspaceFlatnessSquared, n + 1);
      }
      else
      {
        AddPoint(points, numOfPoints, x2, y2);
        numOfPoints++;
      }
      return 1;
    }

    public void AddPoint(List<PointF>? points, int n, float x, float y)
    {
      if (points is null)
        return;

      PointF p = new PointF();
      p.X = x;
      p.Y = y;
      points[n] = p;
    }

    public List<PointF> FlattenCurves(ref List<TTFVertex> vertices, int numOfVerts, float objspaceFlatness, ref List<int> windingLengths, ref int windingCount)
    {
      List<PointF>? points = null;
      int numOfPoints = 0;

      float objspaceFlatnessSquared = objspaceFlatness * objspaceFlatness;
      int i, pass;
      int n = 0;
      int start = 0;

      // count how many "moves" there are to get the countour count
      for (i = 0; i < numOfVerts; i++)
      {
        if (vertices[i].type == (byte)VMove.VMOVE)
          n++;
      }

      // TODO: optimize this
      for (i = 0; i < n; i++)
        windingLengths.Add(0);

      windingCount = n;
      if (n == 0)
        return new List<PointF>();
      
      // make two passes through the points so we don't need to allocate too much?? (look at this later to be more C# like)
      for (pass =0; pass < 2; ++pass)
      {
        float x = 0;
        float y = 0;
        if (pass == 1)
        {
          // TODO: really should be using arrays....
          points = new List<PointF>();
          for (i = 0; i < numOfPoints; i++)
            points.Add(new PointF());
        }

        numOfPoints = 0;
        n = -1;
        TTFVertex vertex;
        for (i =0; i < numOfVerts; i++)
        {
          vertex = vertices[i];
          switch (vertex.type)
          {
            case (byte)VMove.VMOVE:
              // start next countour
              if (n >= 0)
                windingLengths[n] = numOfPoints - start;
              n++;
              start = numOfPoints;
              x = vertex.x;
              y = vertex.y;
              AddPoint(points, numOfPoints++, x, y);
              break;
            case (byte)VMove.VLINE:
              x = vertex.x;
              y = vertex.y;
              AddPoint(points, numOfPoints++, x, y);
              break;
            case (byte)VMove.VCURVE:
              TesselateCurve(points, ref numOfPoints, x, y, vertex.cx, vertex.cy, vertex.x, vertex.y, objspaceFlatnessSquared, 0);
              x = vertex.x;
              y = vertex.y;
              break;
            case (byte)VMove.VCUBIC:
              TesselateCubic(points, ref numOfPoints, x, y, vertex.cx, vertex.cy, vertex.cx1, vertex.cy1, vertex.x, vertex.y, objspaceFlatness, 0);
              x = vertex.x;
              y = vertex.y;
              break;
            default:
              break;
          }
        }
        windingLengths[n] = numOfPoints - start;
      }

      return points;
    }

    public void Rasterize(ref BmpS result, float flatnessInPixels, ref List<TTFVertex> vertices, int numOfVerts, float scaleX, float scaleY, float shiftX, float shiftY, int xOff, int yOff, bool invert)
    {
      float scale = scaleX > scaleY ? scaleY : scaleX;
      int windingCount = 0;
      List<int> windingLengths = new List<int>();
      List<PointF> windings = FlattenCurves(ref vertices, numOfVerts, flatnessInPixels / scale, ref windingLengths, ref windingCount);
      if (windings.Count > 0)
        InternalRasterize(ref result, ref windings, ref windingLengths, windingCount, scaleX, scaleY, shiftX, shiftY, xOff, yOff, invert);
    }

    public void SetVertex(ref TTFVertex vertex, byte type, int x, int y, int cx, int cy)
    {
      vertex.type = type;
      vertex.x  = (short) x;
      vertex.y  = (short) y;
      vertex.cx = (short)cx;
      vertex.cy = (short)cy;
    }

    public int CloseShape(ref List<TTFVertex> vertices, int numOfVertices, bool wasOff, bool startOff, int sx, int sy, int scx, int scy, int cx, int cy)
    {
      TTFVertex vertex;
      vertex = vertices[numOfVertices];
      if (startOff)
      {
        if (wasOff)
          SetVertex(ref vertex, (byte)VMove.VCURVE, (cx + scx) >> 1, (cy + scy) >> 1, cx, cy);
        SetVertex(ref vertex, (byte)VMove.VCURVE, sx, sy, scx, scy);
      } else
      {
        if (wasOff)
          SetVertex(ref vertex, (byte)VMove.VCURVE, sx, sy, cx, cy);
        else
          SetVertex(ref vertex, (byte)VMove.VLINE, sx, sy, 0, 0);
      }
      vertices[numOfVertices++] = vertex;
      return numOfVertices;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="glyphIndex"></param>
    /// <param name="vertices"></param>
    /// <returns>Number of vertices</returns>
    public int GetGlyphShapeTT(int glyphIndex, ref List<TTFVertex> vertices)
    {
      short numOfContours;
      int endPtsOfContoursOffset;
      // *data
      int startOffset = (int)_ttf.StartOffset;
      int numOfVertices = 0;
      TTFVertex vertex;
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      int g = GetGlyphOffset(ref buffer, glyphIndex);

      if (g < 0)
        return 0;

      numOfContours = ReadSignedInt16(ref buffer, startOffset + g);

      if (numOfContours > 0)
      {
        // Simples shapes
        byte flags = 0;
        byte flagCount = 0;
        int j = 0;
        int insLen, i, m, numOfFlags, off;
        bool wasOff = false;
        bool startOff = false;
        int x, y, cx, cy, sx, sy, scx, scy;
        int pointsOffset;
        int origPointsOffset;
        int nextMove = 0;
        // go to end of Table 14
        endPtsOfContoursOffset = startOffset + g + 10;

        // ins is instruction length, total bytes neededfor instruction
        // Table 15: Simple glyph definition - Insturction length
        // numOfCounters * 2 skips endOfCountours array
        insLen = ReadUInt16(ref buffer, endPtsOfContoursOffset + numOfContours * 2);

        // pointsOffset is offset in array of flags in table 15
        // we skip up to insLen and then entire instructuion array that is 1 byte each ( +insLen)
        pointsOffset = endPtsOfContoursOffset + numOfContours * 2 + 2 + insLen;
        origPointsOffset = pointsOffset;
        // i have no idea which data this is , its like last value of contours array
        // maybe because its point indice its + 1??
        numOfFlags = 1 + ReadUInt16(ref buffer, endPtsOfContoursOffset + numOfContours * 2 - 2);

        m = numOfFlags + 2 * numOfContours; // loose bound how many vertices we need
        if (vertices == null)
          vertices = new List<TTFVertex>();
        
        // in first pass, we load uninterpreted data into the allocated array
        // above, shifted to the end of the array so we won't overwrite it when
        // we create our final data starting from the front
        
        // offset for uninterpreted data
        off = m - numOfFlags;
        // TODO: do this for now because thats how its done in stb
        // refactor oonce understood how its used better
        for (i =0; i < off + numOfFlags; i++)
        {
          vertex = new TTFVertex();
          vertices.Add(vertex);
        }
        // 1. Load flags
        for (i = 0; i < numOfFlags; i++)
        {
          if (flagCount == 0)
          {
            flags = ReadByte(ref buffer, pointsOffset++);
            if ((flags & 8) == 8) // TODO: is this right?!
              flagCount = ReadByte(ref buffer, pointsOffset++);
          }
          else
            flagCount--;
          // not most efficient
          vertex = vertices[off + i];
          vertex.type = flags;
          vertices[off + i] = vertex;
        }

        // 2. Load x coordinates
        x = 0;
        for (i =0; i < numOfFlags; i++)
        {
          flags = vertices[off + i].type;
          if ((flags & 2) == 2)
          {
            short dx = ReadByte(ref buffer, pointsOffset++);
            x += ((flags & 16) == 16) ? dx : -dx; //?
          } else
          {
            if ((flags & 16) != 16)
            {
              short val = (short)(ReadByte(ref buffer, pointsOffset) * 256);
              val += ReadByte(ref buffer, pointsOffset + 1);
              x = x + val;
              pointsOffset += 2;
            }
          }

          vertex = vertices[off + i];
          vertex.x = (short)x;
          vertices[off + i] = vertex;
        }

        // 3. Load y coordinates
        y = 0;
        for (i = 0; i < numOfFlags; i++)
        {
          flags = vertices[off + i].type;
          if ((flags & 4) == 4)
          {
            short dy = ReadByte(ref buffer, pointsOffset++);
            y += ((flags & 32) == 32) ? dy : -dy; //?
          }
          else
          {
            if ((flags & 32) != 32)
            {
              short val = (short)(ReadByte(ref buffer, pointsOffset) * 256);
              val += ReadByte(ref buffer, pointsOffset + 1);
              y = y + val;
              pointsOffset += 2;
            }
          }
          vertex = vertices[off + i];
          vertex.y = (short)y;
          vertices[off + i] = vertex;
        }

        // 4. Converert to familiar format
        numOfVertices = 0;
        cx = cy = sx = sy = scx = scy = 0;
        for (i = 0; i < numOfFlags; i++)
        {
          // curr
          vertex = vertices[off + i];
          flags = vertex.type;
          x = vertex.x;
          y = vertex.y;

          if (nextMove == i)
          {
            if (i != 0)
              numOfVertices = CloseShape(ref vertices, numOfVertices, wasOff, startOff, sx, sy, scx, scy, cx, cy);

            startOff = (flags & 1) != 1;
            // next
            vertex = vertices[off + i + 1];
            if (startOff)
            {
              // if we start off with an off-curve point, then when we need to find a point on the curve
              // where we can start, and we need to save some state for when we wraparound.
              scx = x;
              scy = y;
              byte type = vertex.type;
              if ((type & 1) != 1)
              {
                // next point is also a curve point, so interpolate an on point curve
                sx = (x + vertex.x) >> 1;
                sy = (y + vertex.y) >> 1;
              }
              else
              {
                // otherwise just use the next point as our start point
                sx = vertex.x;
                sy = vertex.y;
                i++;
              }
            }
            else
            {
              sx = x;
              sy = y;
            }

            vertex = vertices[numOfVertices];
            SetVertex(ref vertex, (byte)VMove.VMOVE, sx, sy, 0, 0);
            vertices[numOfVertices++] = vertex;
            wasOff = false;
            nextMove = 1 + ReadUInt16(ref buffer, endPtsOfContoursOffset + j * 2);
            j++;
          } else
          {
            vertex = vertices[numOfVertices];
            // if its a currve
            if ((flags & 1) != 1)
            {
              // two off-curv control points in a row means interpolate an on-curve midpoint
              if (wasOff)
              {
                SetVertex(ref vertex, (byte)VMove.VCURVE, (cx + x) >> 1, (cy + y) >> 1, cx, cy);
                vertices[numOfVertices++] = vertex;
              }
                cx = x;
              cy = y;
              wasOff = true;
            }
            else
            {
              if (wasOff)
                SetVertex(ref vertex, (byte)VMove.VCURVE, x, y, cx, cy);
              else
                SetVertex(ref vertex, (byte)VMove.VLINE, x, y, 0, 0);

              vertices[numOfVertices++] = vertex;
              wasOff = false;
            }

            
          }
        }
        numOfVertices = CloseShape(ref vertices, numOfVertices, wasOff, startOff, sx, sy, scx, scy, cx, cy);
      }
      else if (numOfContours < 0)
      {
        // Compound shapes
        bool more = true;
        int compOffset = (int)_ttf.StartOffset + g + 10;
        numOfVertices = 0;
        vertex = new TTFVertex();
        List<TTFVertex> compVertex = new List<TTFVertex>();
        List<TTFVertex> tempVertex = new List<TTFVertex>();
        Span<float> mtx = stackalloc float[6] { 1, 0, 0, 1, 0, 0 };
        while (more)
        {
          // setup
          ushort flags, gidx;
          float m, n;
          mtx[0] = 1;
          mtx[1] = 0;
          mtx[2] = 0;
          mtx[3] = 1;
          mtx[4] = 0;
          mtx[5] = 0;
          int compNumVerts = 0;
          int i;
          compVertex.Clear();
          tempVertex.Clear();
          // end setup

          flags = (ushort)ReadSignedInt16(ref buffer, compOffset);
          compOffset += 2;
          gidx = (ushort)ReadSignedInt16(ref buffer, compOffset);
          compOffset += 2;

          if ((flags & 2) == 2) // XY values
          {
            // ??
            if ((flags & 1) == 1)
            {
              mtx[4] = ReadSignedInt16(ref buffer, compOffset);
              compOffset += 2;
              mtx[5] = ReadSignedInt16(ref buffer, compOffset);
              compOffset += 2;
            } else
            {
              mtx[4] = ReadByte(ref buffer, compOffset++);
              mtx[5] = ReadByte(ref buffer, compOffset++);
            }
          } else
          {
            // matching point
            throw new NotImplementedException();
          }

          if ((flags &  (1 << 3)) != 0) // we have a scale
          {
            mtx[0] = mtx[3] = ReadSignedInt16(ref buffer, compOffset) / 16384.0f;
            compOffset += 2;
            mtx[1] = mtx[2] = 0;
          } else if ((flags & (1 << 6)) != 0) // we have an x and y scale
          {
            mtx[0] = ReadSignedInt16(ref buffer, compOffset) / 16384.0f;
            compOffset += 2;
            mtx[1] = mtx[2] = 0;
            mtx[3] = ReadSignedInt16(ref buffer, compOffset) / 16384.0f;
            compOffset += 2;
          } else if ((flags & (1 << 7)) != 0)
          {
            mtx[0] = ReadSignedInt16(ref buffer, compOffset) / 16384.0f;
            compOffset += 2;
            mtx[1] = ReadSignedInt16(ref buffer, compOffset) / 16384.0f;
            compOffset += 2;
            mtx[2] = ReadSignedInt16(ref buffer, compOffset) / 16384.0f;
            compOffset += 2;
            mtx[3] = ReadSignedInt16(ref buffer, compOffset) / 16384.0f;
            compOffset += 2;
          }

          // Find transformation scales
          m = (float)Math.Sqrt(mtx[0] * mtx[0] + mtx[1] * mtx[1]);
          n = (float)Math.Sqrt(mtx[2] * mtx[2] + mtx[3] * mtx[3]);

          // Get indexed glyph
          compNumVerts = GetGlyphShape(gidx, ref compVertex);
          if (compNumVerts > 0)
          {
            // Transform vertices
            for (i =0; i < compNumVerts; i++)
            {
              vertex = compVertex[i];
              short x, y;
              x = vertex.x;
              y = vertex.y;
              vertex.x = (short)(m * (mtx[0] * x + mtx[2] * y + mtx[4]));
              vertex.y = (short)(m * (mtx[1] * x + mtx[3] * y + mtx[5]));

              x = vertex.cx;
              y = vertex.cy;
              vertex.cx = (short)(m * (mtx[0] * x + mtx[2] * y + mtx[4]));
              vertex.cy = (short)(m * (mtx[1] * x + mtx[3] * y + mtx[5]));
            }

            // Append vertices
            // null check not needed?
            if (numOfVertices > 0 && vertices != null)
            {
              tempVertex.AddRange(vertices);
            }
            tempVertex.AddRange(compVertex);
            vertices.Clear(); // needed?
            vertices = tempVertex;
            numOfVertices += compNumVerts;
          }

          more = (flags & (1 << 5)) > 0;
        }
      }
      return numOfVertices;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="glyphIndex"></param>
    /// <param name="vertices"></param>
    /// <returns>Number of vertices</returns>
    /// <exception cref="NotImplementedException"></exception>
    public int GetGlyphShape(int glyphIndex, ref List<TTFVertex> vertices)
    {
      if (_ttf.Cff)
      {
        throw new NotImplementedException();
        return 0;
      }

      return GetGlyphShapeTT(glyphIndex, ref vertices);
    }

    public void MakeGlyphBitmapSubpixel(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, int glyphIndex)
    {
      int ix0 = 0;
      int iy0 = 0;
      int ix1 = 0;
      int iy1 = 0;
      List<TTFVertex> vertices = new List<TTFVertex>();
      int numOfVerts = GetGlyphShape(glyphIndex, ref vertices);
      BmpS gbm = new BmpS();
      
      GetGlyphBitmapBoxSubpixel(glyphIndex, scaleX, scaleY, shiftX, shiftY, ref ix0, ref iy0, ref ix1, ref iy1);
      gbm.Pixels = bitmapArr;
      gbm.W = glyphWidth;
      gbm.H = glyphHeight;
      gbm.Stride = glyphStride;
      gbm.Offset = byteOffset;

      if (gbm.W > 0 && gbm.H > 0)
        Rasterize(ref gbm, 0.35f, ref vertices, numOfVerts, scaleX, scaleY, shiftX, shiftY, ix0, iy0, true);
    }
    public void MakeCodepointBitmapSubpixel(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, int unicodeCodepoint)
    {
      int glyphIndex = FindGlyphIndex(unicodeCodepoint);
      MakeGlyphBitmapSubpixel(ref bitmapArr, byteOffset, glyphWidth, glyphHeight, glyphStride, scaleX, scaleY, shiftX, shiftY, glyphIndex);
    }

    public void MakeCodepointBitmap(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, int unicodeCodepoint)
    {
      MakeCodepointBitmapSubpixel(ref bitmapArr, byteOffset, glyphWidth, glyphHeight, glyphStride, scaleX, scaleY, 0, 0, unicodeCodepoint);
    }
    #endregion rasterizer
    #region reader functions
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
    #endregion reader functions
    private uint CalculateCheckSum(ref FakeSpan dataPos, uint numOfBytesInTable)
    {
      ReadOnlySpan<byte> tableBuffer = new ReadOnlySpan<byte>(_buffer, dataPos.Position, dataPos.Length);
      uint sum = 0;
      uint nLongs = (numOfBytesInTable + 3) / 4;
      int i = 0;
      int remainder = dataPos.Length % 4;
      while (nLongs-- > 0 & i < dataPos.Length - remainder)
      {
        sum += BinaryPrimitives.ReadUInt32BigEndian(tableBuffer.Slice(i, 4));
        i += 4;
      }

      uint r = 0;
      int n = 24;
      while (i < dataPos.Length)
      {
        r = (r & tableBuffer[i]) << n;
        n -= 8;
        i++;
      }
      sum += r;
      return sum;
    }
  }
}
