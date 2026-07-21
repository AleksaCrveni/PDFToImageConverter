using Converter.FileStructures.JPEG;
using System.Diagnostics;

namespace Converter.Utils.JPEG
{
  /*
   * This impelmentation of JPEGBitReader is inspired by/altered/simplified and slower version of
    https://github.com/yigolden-oss/JpegLibrary/blob/main/src/JpegLibrary/JpegBitReader.cs
    which has MIT licence that is included bellow
  */
  //  IT License

  //Copyright(c) 2019-2021 yigolden

  //Permission is hereby granted, free of charge, to any person obtaining a copy
  //of this software and associated documentation files(the "Software"), to deal
  //in the Software without restriction, including without limitation the rights
  //to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  //copies of the Software, and to permit persons to whom the Software is
  //furnished to do so, subject to the following conditions:

  //The above copyright notice and this permission notice shall be included in all
  //copies or substantial portions of the Software.

  //THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  //IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  //FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
  //AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  //LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  //OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  //SOFTWARE.
  public ref struct JPEGBitReader
  {
    public BitReaderBuffer buffer;
    public JPEG_MARKERS nextMarker; // _nextMarker==0: No marker is encountered in the stream; otherwise the next marker in the stream after the bits read in the buffer.
    public JPEGBitReader()
    {
      buffer = new BitReaderBuffer();
    }
    
    public void AdvanceAlignByte(ref ByteReader r)
    {
      buffer.Size = (byte)(buffer.Size - (buffer.Size % 8));
      FillBuffer(ref r);
    }

    public void SkipBits(int len, out bool isMarkerEncountered, ref ByteReader r)
    {
      if (buffer.Size < len)
      {
        if (!LoadBits(len, out isMarkerEncountered, ref r))
          throw new InvalidDataException("Unable to load bits!");
      }
      buffer.Size = (byte)(buffer.Size - len);
      isMarkerEncountered = false;
    }
    /// <summary>
    /// NOTE(@Aleksa): I think that since we load in entire jpeg in byte[] we should throw if IsEOF() is true
    /// </summary>
    public void FillBuffer(ref ByteReader r)
    {
      while (buffer.Size < 32)
      {
        if (nextMarker != 0)
          break;

        if (r.IsEOF())
          break;

        byte b = r.ReadByte();
        if (b == 0xFF)
        {
          if (r.IsEOF())
            break;

          b = r.PeekByte();

          // Its padding byte so we skip and continue reading
          if (b == 0xFF)
            continue;

          r.SkipNextByte(); // move pos in read since we peeked
          if (b != 0)
          {
            nextMarker = (JPEG_MARKERS)((0xFF << 8) | b);
            // check if its valid marker
            if (!Enum.IsDefined<JPEG_MARKERS>(nextMarker))
              throw new InvalidDataException("Malformed scan data. Invalid Marker sequence!");
            break;
          }

          b = 0xFF;
        }
        buffer.Val = (buffer.Val << 8) | b;
        buffer.Size += 8;
      }
    }
    public int ReadBits(int length, out bool isMarkerEncountered, ref ByteReader r)
    {
      if (buffer.Size < length)
      {
        FillBuffer(ref r);
        // didnt load any bits since we hit marker
        if (buffer.Size < length)
        {
          Debug.Assert(true, "This shouldn't happen!");
          isMarkerEncountered = buffer.Size == 0 && nextMarker != 0;
          return default;
        }
      }
      buffer.Size -= (byte)length;
      isMarkerEncountered = false;
      // get first length bits from left to right
      return (int)(buffer.Val >> buffer.Size) & ((1 << length) - 1);
    }

    public int PeekBits(int length, out int bitsPeeked, ref ByteReader r)
    {
      if (buffer.Size < length)
      {
        FillBuffer(ref r);
        // 
        if (buffer.Size < length)
        {
          Debug.Assert(true, "This shouldn't happen!");
          bitsPeeked = buffer.Size;
          // I guess this max up to length bits
          return ((int)buffer.Val << (length - buffer.Size)) & ((1 << length) - 1) | ((1 << (length - buffer.Size)) - 1); 
        }
      }
      int remainingBIts = buffer.Size - length;
      bitsPeeked = length;
      return (int)(buffer.Val >> remainingBIts) & ((1 << length) - 1);
    }

    public bool LoadBits(int length, out bool isMarkerEncountered, ref ByteReader r)
    {
      FillBuffer(ref r);
      if (buffer.Size < length)
      {
        isMarkerEncountered = buffer.Size == 0 && nextMarker != 0;
        return false;
      }
      isMarkerEncountered = false;
      return true;
    }

    public JPEG_MARKERS ReadMarker()
    {
      if (buffer.Size == 0)
      {
        JPEG_MARKERS m = nextMarker;
        nextMarker = 0;
        return m;
      }

      return JPEG_MARKERS.NULL;
    }

    public JPEG_MARKERS PeekMarker() => buffer.Size == 0 ? nextMarker : 0;
  }


  public ref struct BitReaderBuffer
  {
    public ulong Val; // at least 32 bits
    public byte Pos; // 0-64 (usually within 0-(31+7) if its on a boundary
    // this isn't always same because we may encounter marker or simply arent on boundary Or EOF but that would make it illegal jpeg since it 
    // should end with EOI marker
    public byte Size; 
  }
    
}
