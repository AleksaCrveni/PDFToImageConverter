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
          end = ReadUInt16(ref buffer, (int)(_ttf.StartOffset + search + rangeShift * 2));
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
      ascent  = ReadSignedInt32(ref buffer, _ttf.Offsets.hhea.Position + 4);
      descent = ReadSignedInt32(ref buffer, _ttf.Offsets.hhea.Position + 6);
      lineGap = ReadSignedInt32(ref buffer, _ttf.Offsets.hhea.Position + 8);
    }

    public float ScaleForPixelHeight(float lineHeight)
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      int fHeight = ReadSignedInt32(ref buffer, _ttf.Offsets.hhea.Position + 4) - ReadSignedInt32(ref buffer, _ttf.Offsets.hhea.Position + 6);
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
        g2 = _ttf.Offsets.glyf.Position + (int)ReadUInt32(ref buffer, _ttf.Offsets.loca.Position + glyphIndex * 4 +4);
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

        xMin = ReadUInt16(ref buffer, g + 2);
        yMin = ReadUInt16(ref buffer, g + 4);
        xMax = ReadUInt16(ref buffer, g + 6);
        yMax = ReadUInt16(ref buffer, g + 8);
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
        ix0 = (int)Math.Floor  ( x0 * scaleX + shiftX);
        iy0 = (int)Math.Floor  (-y1 * scaleY + shiftY);
        ix1 = (int)Math.Ceiling( x1 * scaleX + shiftX);
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

    public void TesselateCubic(ref List<PointF> points, ref int numOfPoints, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float objspaceFlatnessSquared, int n)
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

        TesselateCubic(ref points, ref numOfPoints , x0, y0, x01, y01, xa, ya, mx, my, objspaceFlatnessSquared, n + 1);
        TesselateCubic(ref points, ref numOfPoints , mx, my, xb, yb, x23, y23, x3, y3, objspaceFlatnessSquared, n + 1);
      }
      else
      {
        AddPoint(ref points, numOfPoints, x3, y3);
        numOfPoints++;
      }
    }

    // tesselate until threshold p is happy
    // TODO: warped to compensate for non-linearn stretching (??)
    public int TesselateCurve(ref List<PointF> points, ref int numOfPoints, float x0, float y0, float x1, float y1, float x2, float y2, float objspaceFlatnessSquared, int n)
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
        TesselateCurve(ref points, ref numOfPoints, x0, y0, (x0 + x1) / 2.0f, (y0 + y1) / 2.0f, mx, my, objspaceFlatnessSquared, n + 1);
        TesselateCurve(ref points, ref numOfPoints, mx, my, (x1 + x2) / 2.0f, (y1 + y2) / 2.0f, x2, y2, objspaceFlatnessSquared, n + 1);
      }
      else
      {
        AddPoint(ref points, numOfPoints, x2, y2);
        numOfPoints++;
      }
      return 1;
    }

    public void AddPoint(ref List<PointF> points, int n, float x, float y)
    {
      if (points.Count == 0)
        return;
      PointF p = new PointF();
      p.X = x;
      p.Y = y;
      points[n] = p;
    }

    public List<PointF> FlattenCurves(ref List<TTFVertex> vertices, int numOfVerts, float objspaceFlatness, ref List<int> windingLengths, ref int windingCount)
    {
      List<PointF> points = new List<PointF>();
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

      windingCount = n;
      if (n == 0)
        return new List<PointF>();
      
      // make two passes through the points so we don't need to allocate too much?? (look at this later to be more C# like)
      for (pass =0; pass < 2; ++pass)
      {
        float x = 0;
        float y = 0;
        if (pass == 1)
          points.Clear(); // redundant just for clarity that its used in second pass

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
              AddPoint(ref points, numOfPoints++, x, y);
              break;
            case (byte)VMove.VLINE:
              x = vertex.x;
              y = vertex.y;
              AddPoint(ref points, numOfPoints++, x, y);
              break;
            case (byte)VMove.VCURVE:
              TesselateCurve(ref points, ref numOfPoints, x, y, vertex.cx, vertex.cy, vertex.x, vertex.y, objspaceFlatnessSquared, 0);
              x = vertex.x;
              y = vertex.y;
              break;
            case (byte)VMove.VCUBIC:
              TesselateCubic(ref points, ref numOfPoints, x, y, vertex.cx, vertex.cy, vertex.cx1, vertex.cy1, vertex.x, vertex.y, objspaceFlatness, 0);
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

      // TOOD: continue
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
      vertex = vertices[numOfVertices++];
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
      ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>();
      int g = GetGlyphOffset(ref buffer, glyphIndex);

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

        m = numOfContours + 2 * numOfContours; // loose bound how many vertices we need
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
              short val = (short)(ReadByte(ref buffer, origPointsOffset) * 256);
              val += ReadByte(ref buffer, origPointsOffset + 1);
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
              short val = (short)(ReadByte(ref buffer, origPointsOffset) * 256);
              val += ReadByte(ref buffer, origPointsOffset + 1);
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
        for (i = 0; i < numOfVertices; i++)
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

            vertex = vertices[numOfVertices++];
            SetVertex(ref vertex, (byte)VMove.VMOVE, sx, sy, 0, 0);
            wasOff = false;
            nextMove = 1 + ReadUInt16(ref buffer, endPtsOfContoursOffset + j * 2);
            j++;
          } else
          {
            vertex = vertices[numOfVertices++];
            // if its a currve
            if ((flags & 1) != 1)
            {
              if (wasOff) // two off-curv control points in a row means interpolate an on-curve midpoint
                SetVertex(ref vertex, (byte)VMove.VCURVE, (cx + x) >> 1, (cy + y) >> 1, cx, cy);
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
      while (nLongs-- > 0)
      {
        sum += BinaryPrimitives.ReadUInt32BigEndian(tableBuffer.Slice(i, 4));
        i += 4;
      }
      return sum;
    }
  }
}
