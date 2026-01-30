using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.TTF;
using Converter.Parsers.PDF;
using Converter.StaticData;
using System.Text;

namespace Converter.Rasterizers
{
  public class TTFRasterizer : STBRasterizer, IRasterizer
  {
    private PDF_FontInfo _fontInfo;
    private PDF_FontEncodingData _encodingData;
    private TTF_Table_POST _ttfTablePOST;
    private TTF_Table_CMAP _ttfTableCMAP;
    private float _unitsPerEm = 1000f; // used to covnert from glyph to text space, for ttf its 1/1000 default value
    public TTFRasterizer(byte[] rawFontProgram, ref PDF_FontInfo fontInfo) : base (rawFontProgram, fontInfo.EncodingData.BaseEncoding)
    {
      _fontInfo = fontInfo;
      _encodingData = fontInfo.EncodingData;

      InitFont(); // should be called in derived class??
   }

    protected override void InitFont()
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
        0x00010000 => TTF_ScalarType.True,
        0x74727565 => TTF_ScalarType.True,
        0x4F54544F => TTF_ScalarType.Otto,
        0x74797031 => TTF_ScalarType.Typ1,
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
        offset = ReadUInt32(ref buffer, mainPos + 4 + 4) + (uint)_beginOfSfnt;
        // does not include padded bytes, so i want to include them as well to stay on long boundary
        // and tight as possible 
        length = ReadUInt32(ref buffer, mainPos + 4 + 4 + 4);
        mainPos += 16;
        pad = length % 4;
        length += pad;

        // case {num} where num is uint32 representation of 4 byte string that represent table tag
        // did this so I don't allocate string when comparing 
        tableBuffer = new FakeSpan((int)offset, (int)length);

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
          // fpgm - don't need this for now i Think
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

      _ttfTableCMAP = new TTF_Table_CMAP();
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
          case (ushort)TTF_PlatformID.Microsoft:
            switch (platformSpecificID)
            {
              case (ushort)TTF_MSPlatformSpecificID.MS_Symbol:
                _ttfTableCMAP.Index30SubtableOffset = tOff.cmap.Position + (int)offset;
                _ttf.IndexMapOffset = _ttfTableCMAP.Index30SubtableOffset;
                break;
              case (ushort)TTF_MSPlatformSpecificID.MS_UnicodeBMP:
                _ttfTableCMAP.Index31SubtableOffset = tOff.cmap.Position + (int)offset;
                _ttf.IndexMapOffset = _ttfTableCMAP.Index31SubtableOffset;
                break;
              case (ushort)TTF_MSPlatformSpecificID.MS_UnicodeFULL:
                _ttf.IndexMapOffset = tOff.cmap.Position + (int)offset;
                break;

            }
            break;
          case (ushort)TTF_PlatformID.Unicode:
            _ttf.IndexMapOffset = tOff.cmap.Position + (int)offset;
            break;
          case (ushort)TTF_PlatformID.Macintosh:
            switch (platformSpecificID)
            {
              case 0:
                _ttfTableCMAP.Index10SubtableOffset = tOff.cmap.Position + (int)offset;
                _ttf.IndexMapOffset = _ttfTableCMAP.Index10SubtableOffset;
                break;
              default:
                _ttf.IndexMapOffset = tOff.cmap.Position + (int)offset;
                break;
            }
            break;
        }
      }

      if (_ttfTableCMAP.Index30SubtableOffset > 0)
        _ttfTableCMAP.Format30 = ReadUInt16(ref buffer, _ttfTableCMAP.Index30SubtableOffset);

      if (_ttfTableCMAP.Index31SubtableOffset > 0)
        _ttfTableCMAP.Format31 = ReadUInt16(ref buffer, _ttfTableCMAP.Index31SubtableOffset);

      if (_ttfTableCMAP.Index10SubtableOffset > 0)
        _ttfTableCMAP.Format10 = ReadUInt16(ref buffer, _ttfTableCMAP.Index10SubtableOffset);


      if (_ttf.IndexMapOffset == 0)
        throw new InvalidDataException("Missing index map!");
      _ttf.CmapFormat = ReadUInt16(ref buffer, _ttf.IndexMapOffset);
      slice = buffer.Slice(tOff.head.Position, tOff.head.Length);
      _ttf.IndexToLocFormat = ReadUInt16(ref slice, 50);

      _ttf.FontDirectory = fd;
      _ttf.Offsets = tOff;
    }

    public override void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo)
    {
      STB_MakeGlyphBitmapSubpixel(ref bitmapArr, byteOffset, glyphWidth, glyphHeight, glyphStride, scaleX, scaleY, shiftX, shiftY, glyphInfo.Index);
    }

    // Page 274. Make this more robust, ok for basic start
    public void GetGlyphInfo(int codepoint, ref GlyphInfo glyphInfo)
    {
      // 0. Reset glyphInfo to default
      glyphInfo.Index = 0;
      glyphInfo.Name = string.Empty;

      // GlyphName sometime may not be needed in TTF, but do it for now
      // 1. Get correct glyphname based on encoding
      // 2. Get glyphIndex of given glyphname

      // unicode values aren't always right?? bug? other converters treat � as DDFE, but its actually FFFD (??), some encoding is wrong on my side?
      // UPDATE: I think that i should be passing codepoint be able to readunicode values, for this issue specifically we aren't able to process ligature since its over 65555 value (unicode)
      // TODO: adress at some point after we figure it out with Type1 font stuff

      if (codepoint > 255)
        codepoint = ' ';
      // single byte since its TTF
      byte b = (byte)(codepoint & 255);
      // if its non symbolic font encdoing are mac or win, ther shouldn't be anything in the differences array (or it should be empty in code)
      
      // 1.
      string glyphName = _encodingData.GetGlyphNameFromDifferences(b);
      // not found in differences array so we check encoding array
      // check from cmap if encoding is not defined
      if (glyphName == string.Empty)
      {
        if (b < _encodingArray.Length)
        {
          int glyphNameIndex = _encodingArray[b];
          glyphName = PDFEncodings.GetGlyphName(glyphNameIndex);
        } else
        {
          glyphName = ".notdef";
        }
        
      }

      // 2.
      int glyphIndex = 0;
      // first check if its post, if it  iseant read fyom Adobe Glyph List and cmap
      if (_ttf.Offsets.post.Position != 0)
      {
        glyphIndex = GetGlyphIndexFromPostTable(glyphName);
        if (glyphIndex != 0)
        {
          glyphInfo.Index = glyphIndex;
          glyphInfo.Name = glyphName;
          return;
        }
          
      }

      // if not found check adobe list
      List<int> unicodeValues = AdobeGlyphList.GetUnicodeValuesForGlyphName(glyphName);
      if (unicodeValues != null)
      {
        // is this ok?
        char character = (char)unicodeValues[0];
        glyphIndex = GetGlyphIndexFromCmap(character, _ttfTableCMAP.Index31SubtableOffset, _ttfTableCMAP.Format31);
        glyphInfo.Index = glyphIndex;
        glyphInfo.Name = glyphName;
        return;
      }
      return;
    }
    // This has to be called for each character because of widths array, it may or may not be same as advance in hmtx table 
    public (float scaleX, float scaleY) GetScale(int glyphIndex, double[,] textRenderingMatrix, float width)
    {
      int aw = 0;
      int lsb = 0;
      STB_GetGlyphHMetrics(glyphIndex, ref aw, ref lsb);

      float advance = aw / _unitsPerEm;
      float widthScale = width / advance;

      // 1 x 1 space that can be scaled (glyph -> text space)
      // later we will cache these values with unscaled vertex data and multiply on scale
      // so we dont have to compute vertexes each time but can just scale
      float scaleX = 1 / _unitsPerEm;
      float scaleY = 1 / _unitsPerEm;

      // scale advance
      if (widthScale > 0)
        scaleX *= widthScale;

      // scale text -> device space
      scaleX *= (float)textRenderingMatrix[0, 0];
      scaleY *= (float)textRenderingMatrix[1, 1];

      return (scaleX, scaleY);
    }


    // TODO: not sure if this is implemented right, test tomorrow
    private int GetGlyphIndexFromPostTable(string glyphName)
    {
      if (_ttfTablePOST is null)
        _ttfTablePOST = ParsePostTable();
      if (_ttfTablePOST.Format == 1)
      {
        for (int i = 0; i < PDFEncodings.PostTableF1MacGlyphEncoding.Length; i++)
        {
          if (glyphName == PDFEncodings.PostTableF1MacGlyphEncoding[i])
            return i;
        }
        return 0;
      } else if (_ttfTablePOST.Format == 2)
      {
        int index = -1;
        for (int i = 0; i < _ttfTablePOST.GlyphNames.Length; i++)
        {
          if (glyphName == _ttfTablePOST.GlyphNames[i])
          {
            index = i;
            break;
          }
        }

        if (index == -1)
        {
          for (int i = 0; i < PDFEncodings.PostTableF1MacGlyphEncoding.Length; i++)
          {
            if (glyphName == PDFEncodings.PostTableF1MacGlyphEncoding[i])
            {
              index = i;
              break;
            }
          }
        }

        // now get the entry in the index
        for (int i = 0; i < _ttfTablePOST.GlyphNameIndexes.Length; i++)
        {
          if (_ttfTablePOST.GlyphNameIndexes[i] == index)
          {
            return i;
          }
        }

        // not found
        return 0;
      }

      return 0;
    }
    private int GetGlyphIndexFromCmap(int unicodeCodepoint, int subTableOffset, int format)
    {
      ReadOnlySpan<byte> buffer = _buffer.AsSpan().Slice(subTableOffset);
      int startOffset = 0;
      if (format == 0)
      {
        ushort len = ReadUInt16(ref buffer, subTableOffset + 2);
        if (unicodeCodepoint < len - 6)
          return buffer[subTableOffset + 6 + unicodeCodepoint];
        return 0;
      }
      else if (format == 2)
      {
        throw new NotImplementedException();
      }
      else if (format == 4)
      {
        // std mapping for windows fonts: binary search collection of ranges (indexes are not tight as in format 6)
        ushort segCount = (ushort)(ReadUInt16(ref buffer, subTableOffset + 6) >> 1);
        ushort searchRange = (ushort)(ReadUInt16(ref buffer, subTableOffset + 8) >> 1);
        ushort entrySelector = ReadUInt16(ref buffer, subTableOffset + 10);
        ushort rangeShift = (ushort)(ReadUInt16(ref buffer, subTableOffset + 12) >> 1);

        // do a binary search of the segments
        uint endCount = (uint)subTableOffset + 14;
        uint search = endCount;

        if (unicodeCodepoint > ushort.MaxValue)
          return 0;

        // they lie from endCount .. endCount + segCount
        // but searchRange is the nearest power of two, so...
        if (unicodeCodepoint >= ReadUInt16(ref buffer, (int)(startOffset + search + rangeShift * 2)))
          search += (uint)rangeShift * 2;

        // now decrement to bias correctly to find smaallest
        search -= 2;
        while (entrySelector > 0)
        {
          ushort end;
          searchRange >>= 1;
          end = ReadUInt16(ref buffer, (int)(startOffset + search + searchRange * 2));
          if (unicodeCodepoint > end)
            search += (uint)searchRange * 2;
          entrySelector--;
        }
        search += 2;

        // do I really need separate scope?
        {
          ushort offset, start, last;
          ushort item = (ushort)(search - endCount >> 1);

          start = ReadUInt16(ref buffer, (int)(startOffset + subTableOffset + 14 + segCount * 2 + 2 + 2 * item));
          last = ReadUInt16(ref buffer, (int)(startOffset + endCount + 2 * item));
          if (unicodeCodepoint < start || unicodeCodepoint > last)
            return 0;

          offset = ReadUInt16(ref buffer, (int)(startOffset + subTableOffset + 14 + segCount * 6 + 2 + 2 * item));
          if (offset == 0)
            return (ushort)(unicodeCodepoint + ReadUInt16(ref buffer, (int)(startOffset + subTableOffset + 14 + segCount * 4 + 2 + 2 * item)));

          return ReadUInt16(ref buffer, (int)(startOffset + offset + (unicodeCodepoint - start) * 2 + subTableOffset + 14 + segCount * 6 + 2 + 2 * item));
        }
      }
      else if (format == 6)
      {
        ushort length = ReadUInt16(ref buffer, subTableOffset + 2);
        ushort lang = ReadUInt16(ref buffer, subTableOffset + 4);
        ushort firstCode = ReadUInt16(ref buffer, subTableOffset + 6);
        ushort entryCount = ReadUInt16(ref buffer, subTableOffset + 8);
        // ensure its in range, codepoints are desnly packed, which means they are in continuous array in order
        if (unicodeCodepoint >= firstCode && unicodeCodepoint - firstCode < entryCount)
          // * 2 because data in 2 bytes each in this format
          return ReadUInt16(ref buffer, subTableOffset + 10 + (unicodeCodepoint - firstCode) * 2);
        return 0;
      }
      else if (format == 8)
      {
        throw new NotImplementedException();
      }
      else if (format == 10)
      {
        throw new NotImplementedException();
      }
      else if (format == 12)
      {
        throw new NotImplementedException();
      }
      else if (format == 13)
      {
        throw new NotImplementedException();
      }
      else if (format == 14)
      {
        throw new NotImplementedException();
      }
      else
      {
        throw new InvalidDataException($"Format {format} is not valid CMAP format!");
      }

      return 0;
    }

    #region table loaders
    // TODO: Not sure if parsing is needed or we can just read from 'local' buffer each time
    // Separate for now
    private TTF_Table_POST ParsePostTable()
    {
      TTF_Table_POST t = new TTF_Table_POST();
      ReadOnlySpan<byte> buffer = _buffer.AsSpan().Slice(_ttf.Offsets.post.Position, _ttf.Offsets.post.Length);
      int format = ReadUInt16(ref buffer, 0); // 1, 2, 2.5, 3, 4
      t.Format = format;
      // Intial parsing only needed for second format
      if (format == 2)
      {
        int pos = 32;// offset to start of format 2 table data
        ushort numberOfGlyphs = ReadUInt16(ref buffer, pos);
        pos += 2;
        short[] glyphNameIndexes = new short[numberOfGlyphs];

        // find highest glyph under 257, its per format 2 spec
        int maxGlyph = 257;
        for (int i = 0; i < numberOfGlyphs; i++)
        {
          glyphNameIndexes[i] = ReadSignedInt16(ref buffer, pos);

          if (glyphNameIndexes[i] > maxGlyph)
            maxGlyph = glyphNameIndexes[i];
          pos += 2;
        }

        // max glyph are literal indexes (NOT OFFSETS), so maximum will be size of names string array
        maxGlyph -= 257;

        string[] glyphNames = new string[maxGlyph];
        Array.Fill(glyphNames, "");
        for (int i = 0; i < glyphNames.Length; i++)
        {
          if (pos >= buffer.Length - 1)
          {
            byte strSize = buffer[pos++];
            glyphNames[i] = ReadStringOfSize(ref buffer, pos, strSize);
            pos += strSize;
          }
        }

        t.GlyphNameIndexes = glyphNameIndexes;
        t.GlyphNames = glyphNames;
      }
      return t;
    }

    #endregion table loaders

    #region read helpers
    private string ReadStringOfSize(ref ReadOnlySpan<byte> buffer, int pos, int len)
    {
      return Encoding.Default.GetString(buffer.Slice(pos, len));
    }

    #endregion read helpers
  }
}
