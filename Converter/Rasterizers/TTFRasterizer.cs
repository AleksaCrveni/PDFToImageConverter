using Converter.FileStructures.PDF;
using Converter.FileStructures.TTF;
using Converter.Parsers.PDF;
using Converter.StaticData;
using System.Buffers.Binary;
using System.Text;

namespace Converter.Rasterizers
{
  public class TTFRasterizer : IFontHelper
  {
    private byte[] _rawFontProgram;
    private STBTrueType _ttfParser;
    private PDF_FontData _fontData;
    private PDF_FontEncodingData _encodingData;
    private PDF_FontEncodingSource _encodingSource;
    private int[] _encodingArray;
    private TTF_Table_POST _ttfTablePOST;
    private TTF_Table_CMAP _ttfTableCMAP;
    private float _unitsPerEm = 1000f; // used to covnert from glyph to text space, for ttf its 1/1000 default value
    public TTFRasterizer(byte[] rawFontProgram, ref PDF_FontData fontData)
    {
      _rawFontProgram = rawFontProgram;
      _fontData = fontData;
      _encodingData = fontData.FontInfo.EncodingData;

      _ttfParser = new STBTrueType();
      _ttfParser.Init(ref _rawFontProgram);
      _ttfParser.InitFont();
      SetCorrectEncoding();
    }

    // Page 274. Make this more robust, ok for basic start
    public (int glyphIndex, string glyphName) GetGlyphInfo(char c)
    {
      // GlyphName sometime may not be needed in TTF, but do it for now
      // 1. Get correct glyphname based on encoding
      // 2. Get glyphIndex of given glyphname

      // unicode values aren't always right?? bug? other converters treat � as DDFE, but its actually FFFD (??), some encoding is wrong on my side?
      if ((int)c > 255)
        c = ' ';
      // single byte
      byte b = (byte)(c & 255);
      // if its non symbolic font encdoing are mac or win, ther shouldn't be anything in the differences array (or it should be empty in code)
      
      // 1.
      string glyphName = _encodingData.GetGlyphNameFromDifferences(b);
      // not found in differences array so we check encoding array
      // check from cmap if encoding is not defined
      if (glyphName == string.Empty)
      {
        int glyphNameIndex= _encodingArray[b];
        if (glyphNameIndex < PDFEncodings.StandardGlyphNames.Length)
          glyphName = PDFEncodings.StandardGlyphNames[glyphNameIndex];
        else
          glyphName = ".notdef";
      }

      // 2.
      int glyphIndex = 0;
      // first check if its post, if it  iseant read fyom Adobe Glyph List and cmap
      if (_ttfParser._ttf.Offsets.post.Position != 0)
      {
        glyphIndex = GetGlyphIndexFromPostTable(glyphName);
        if (glyphIndex != 0)
          return (glyphIndex, glyphName);
      }

      // if not found check adobe list
      List<int> unicodeValues = AdobeGlyphList.GetUnicodeValuesForGlyphName(glyphName);
      if (unicodeValues != null)
      {
        // is this ok?
        char character = (char)unicodeValues[0];
        if (_ttfTableCMAP == null)
          _ttfTableCMAP = ParseCmapTable();

        glyphIndex = GetGlyphIndexFromCmap(character, _ttfTableCMAP.Index31SubtableOffset, _ttfTableCMAP.Format31);
        return (glyphIndex, glyphName);
      }
      return (0, "");
    }
    // This has to be called for each character because of widths array, it may or may not be same as advance in hmtx table 
    public (float scaleX, float scaleY) GetScale(int glyphIndex, double[,] textRenderingMatrix, float width)
    {
      int aw = 0;
      int lsb = 0;
      _ttfParser.GetGlyphHMetrics(glyphIndex, ref aw, ref lsb);

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
      ReadOnlySpan<byte> buffer = _rawFontProgram.AsSpan().Slice(subTableOffset);
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
        // TODO: instead of >> 1, just /2 ??
        ushort segCount = (ushort)(ReadUInt16(ref buffer, subTableOffset + 6) >> 1); // >> 1 is basically / 2 
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

    private void SetCorrectEncoding()
    {
      // I am not sure who much of all of this is needed
      // I know this is done in _ttfParser.InitFont() -- its ok for now, keep _ttfParser pure
      ReadOnlySpan<byte> buffer = _rawFontProgram.AsSpan().Slice(_ttfParser._ttf.Offsets.cmap.Position, _ttfParser._ttf.Offsets.cmap.Length);
      // Find number of cmap subtables and check encodings
      ushort numOfCmapSubtables = ReadUInt16(ref buffer, 2);
      ReadOnlySpan<byte> encodingSubtable;
      ushort platformID;
      ushort platformSpecificID;
      uint offset = 0;

      bool microsoft3_1Present = false;
      int microsoft3_1Offset = 0;

      bool microsoft3_0Present = false;
      int microsoft3_0Offset = 0;

      bool mac1_0Present = false;
      int mac1_0Offset = 0;
      for (int i = 0; i < numOfCmapSubtables; i++)
      {
        // 4 -> skip cmap index, 8 is size of encoding subtable and there can be multiple
        encodingSubtable = buffer.Slice(4 + 8 * i, 8);
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
                microsoft3_0Present = true;
                microsoft3_0Offset = _ttfParser._ttf.Offsets.cmap.Position + (int)offset;
                break;
              case (ushort)TTF_MSPlatformSpecificID.MS_UnicodeBMP:
                microsoft3_1Present = true;
                microsoft3_1Offset = _ttfParser._ttf.Offsets.cmap.Position + (int)offset;
                break;
            }
            break;
          case (ushort)TTF_PlatformID.Macintosh:
            switch (platformSpecificID)
            {
              case 1:
                mac1_0Present = true;
                mac1_0Offset = _ttfParser._ttf.Offsets.cmap.Position + (int)offset;
                break;
            }
            break;
        }
      }

      // CMAP SHOULD BE USED IF there is cmap dict and if there is encoidng then use encoding xdd
      // set PDF_FontEncodingSource
      if (_encodingData.BaseEncoding == PDF_FontEncodingType.WinAnsiEncoding)
      {
        _encodingArray = PDFEncodings.WinAnsiEncoding; // shouldn't this be adobe encoding??
        _encodingSource = PDF_FontEncodingSource.ENCODING;
      } else if (_encodingData.BaseEncoding == PDF_FontEncodingType.MacRomanEncoding)
      {
        _encodingArray = PDFEncodings.MacRomanEncoding;
        _encodingSource = PDF_FontEncodingSource.ENCODING;
      } else if (_encodingData.BaseEncoding == PDF_FontEncodingType.Null)
      {
        _encodingArray = new int[0];
        _encodingSource = PDF_FontEncodingSource.CMAP;
        // TODO: These should be some more work done with prepending cmap data with some bytes, but figure out that later

      } else if (_encodingData.BaseEncoding == PDF_FontEncodingType.StandardEncoding)
      {
        _encodingArray = PDFEncodings.AdobeSandardEncoding;
        _encodingSource = PDF_FontEncodingSource.ENCODING;
      } else
      {
        throw new InvalidDataException("Invalid font encoding!");
      }




    }

    #region table loaders
    // TODO: Not sure if parsing is needed or we can just read from 'local' buffer each time
    // Separate for now
    private TTF_Table_POST ParsePostTable()
    {
      TTF_Table_POST t = new TTF_Table_POST();
      ReadOnlySpan<byte> buffer = _rawFontProgram.AsSpan().Slice(_ttfParser._ttf.Offsets.post.Position, _ttfParser._ttf.Offsets.post.Length);
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

    private TTF_Table_CMAP ParseCmapTable()
    {
      TTF_Table_CMAP t = new TTF_Table_CMAP();
      ReadOnlySpan<byte> buffer = _rawFontProgram.AsSpan().Slice(_ttfParser._ttf.Offsets.cmap.Position, _ttfParser._ttf.Offsets.cmap.Length);
      // Find number of cmap subtables and check encodings
      ushort numOfCmapSubtables = ReadUInt16(ref buffer, 2);
      ReadOnlySpan<byte> encodingSubtable;
      ushort platformID;
      ushort platformSpecificID;
      uint offset = 0;
      for (int i = 0; i < numOfCmapSubtables; i++)
      {
        // 4 -> skip cmap index, 8 is size of encoding subtable and there can be multiple
        encodingSubtable = buffer.Slice(4 + 8 * i, 8);
        platformID = ReadUInt16(ref encodingSubtable, 0);
        platformSpecificID = ReadUInt16(ref encodingSubtable, 2);
        offset = ReadUInt32(ref encodingSubtable, 4);

        // not sure if this check is even require need to be done here, stb_truetype only does this 
        switch (platformID)
        {
          case (ushort)TTF_PlatformID.Microsoft:
            switch (platformSpecificID)
            {
              case (ushort)TTF_MSPlatformSpecificID.MS_UnicodeBMP:
                t.Index31SubtableOffset = _ttfParser._ttf.Offsets.cmap.Position + (int)offset;
                break;
            }
            break;
          case (ushort)TTF_PlatformID.Macintosh:
            t.Index10SubtableOffset = _ttfParser._ttf.Offsets.cmap.Position + (int)offset;
            break;
        }
      }

      if (t.Index31SubtableOffset > 0)
        t.Format31 = ReadUInt16(ref buffer, t.Index31SubtableOffset);

      if (t.Index10SubtableOffset > 0)
        t.Format10 = ReadUInt16(ref buffer, t.Index10SubtableOffset);

      return t;
    }
    #endregion table loaders

    #region read helpers
    private string ReadStringOfSize(ref ReadOnlySpan<byte> buffer, int pos, int len)
    {
      return Encoding.Default.GetString(buffer.Slice(pos, len));
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
    #endregion read helpers
  }
}
