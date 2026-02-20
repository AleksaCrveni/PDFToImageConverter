using Converter.FileStructures.PNG;
using Converter.Utils;
using Converter.Utils.PNG;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;

namespace Converter.Parsers.Images.PNG
{
  public static class PNGParser
  {
    public static PNGFile Parse(string filename)
    {
      PNGFile file = new PNGFile();
      Stream stream = File.OpenRead(filename);
      VerifyHeader(stream);
      ParseChunks(file, stream);
      return file;
    }
    
    public static void VerifyHeader(Stream stream)
    {
      Span<byte> span = stackalloc byte[8];
      int bytesRead = stream.Read(span);
      if (bytesRead != span.Length)
        throw new InvalidDataException("Invalid PNG!");
      // convert into 2 ints and check?
      if (span[0] != 0x89 &&
          span[1] != 0x50 &&
          span[2] != 0x4E &&
          span[3] != 0x47 &&
          span[4] != 0x0D &&
          span[5] != 0x0A &&
          span[6] != 0x1A &&
          span[7] != 0x0A)
        throw new InvalidDataException("Invalid PNG Header!");
    }
    // TODO: prob just load entire data at once, do some perf testing
    // For now just load chunks
    // TODO: clean up this logic later
    public static void ParseChunks(PNGFile file, Stream stream)
    {
      CRC32Impl crc = new CRC32Impl();
      Span<byte> buffer = new byte[3 * 256 + 64];
      // first read IHDR and next len and type
      // len/type + content + crc + next chunk len/type
      int bytesRead = stream.Read(buffer.Slice(0, 8 + 13 + 4 + 8));
      uint len = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(0, 4));
      PNG_CHUNK_TYPE chunkType = (PNG_CHUNK_TYPE)BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4, 4));
      
      if (len == 0 || chunkType != PNG_CHUNK_TYPE.IHDR)
        throw new InvalidDataException("Invalid Data exception!");

      // TODO: this may not work for isnanely large pictures
      crc.UpdateCRC(buffer.Slice(4, 4 + (int)len));
      ParseIHDR(file, ref buffer, crc);
      

      len = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(25, 4));
      chunkType = (PNG_CHUNK_TYPE)BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(29, 4));

      PNG_CHUNK_TYPE lastChunk = PNG_CHUNK_TYPE.IHDR;
      bool seenIDAT = false;
      uint currCrc = 0;
      bool EOF = false;
      while (!EOF)
      {
        crc.Reset();
        crc.UpdateCRC(buffer.Slice(bytesRead - 4, 4)); // CRC Chunktype
        // IDAT will use own buffer
        if (chunkType != PNG_CHUNK_TYPE.IDAT)
        {
          Debug.Assert(len < Int32.MaxValue);
          // len + CRC + next len/type if exist
          int sizeToRead = (int)len + 4 + 8;

          bytesRead = stream.Read(buffer.Slice(0, sizeToRead));
          if (sizeToRead - bytesRead == 8) // we didnt read next len/type which indicates EOF (but last chunk must be IEND)
          {
            if (chunkType != PNG_CHUNK_TYPE.IEND)
              throw new InvalidDataException("IEND must be last!");
            EOF = true;
          }
          else if (sizeToRead - bytesRead != 0)
          {
            throw new InvalidDataException("Invalid chunk formed!");
          }
          else if (chunkType == PNG_CHUNK_TYPE.IEND) 
          {
            // this is the case that IEND is randomly in the middle of the file
            // Also , since we check that IHDR is first and that this must be always and ONLY at the end
            // we can guarantee that IDAT is in between them so we dont have to track if we have seen IEND preivously
            throw new InvalidDataException("IEND must be last!");
          }

          crc.UpdateCRC(buffer.Slice(0, (int)len));
          currCrc = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice((int)len, 4));
        }

        switch (chunkType)
        {
          case PNG_CHUNK_TYPE.IHDR:
            throw new InvalidDataException("IHDR chunk already processed");
          case PNG_CHUNK_TYPE.IDAT:
            if (seenIDAT && lastChunk != PNG_CHUNK_TYPE.IDAT)
              throw new InvalidDataException("IDAT chunks must appear consecutively!");
            seenIDAT = true;
            ParseIDAT(file, stream, len, crc);
            // read next 8 so that can be used
            bytesRead = stream.Read(buffer.Slice(0, 12));
            if (bytesRead != 12)
              throw new InvalidDataException("IDAT can't be last chunk!");
            currCrc = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(0, 4));
            break;
          case PNG_CHUNK_TYPE.PLTE:
            throw new NotImplementedException();
            break;
          case PNG_CHUNK_TYPE.tRNS:
            throw new NotImplementedException();
            break;
          case PNG_CHUNK_TYPE.NULL:
            throw new InvalidDataException("Unknown chunk!");
            break;
          default:
            // skip
            // we calcualted CRC already
            break;
        }
        // shortcut
        if (EOF)
          break;

        if (!crc.VerifyCheckSum(currCrc))
          throw new InvalidDataException($"Invalid CRC32 for {chunkType} chunk!");

        lastChunk = chunkType;
        // this is safe because at this point we know that bytesToRead is always expected
        len = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(bytesRead - 8, 4));
        chunkType = (PNG_CHUNK_TYPE)BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(bytesRead - 4, 4));
      }
    }

    public static void ParseIHDR(PNGFile file, ref Span<byte> buffer, CRC32Impl crc)
    {
      int pos = 8;
      file.Width = BufferReader.ReadInt32BE(ref buffer, ref pos);
      file.Height = BufferReader.ReadInt32BE(ref buffer, ref pos);
      file.BitDepth = buffer[pos++];
      byte colorType = buffer[pos++];
      if (colorType == 1 || colorType == 5 || colorType > 6)
        throw new InvalidDataException("Invalid ColorType!");
      file.ColorType = (PNG_COLOR_TYPE)colorType;
      if ((byte)file.ColorType == 3)
        file.SampleDepth = 8;
      else
        file.SampleDepth = file.BitDepth;

      switch (file.BitDepth)
      {
        case 1:
          switch ((PNG_COLOR_TYPE) colorType)
          {
            case PNG_COLOR_TYPE.GRAYSCALE:
              file.ColorSheme = PNG_COLOR_SCHEME.G1;
              break;
            case PNG_COLOR_TYPE.PALLETE:
              file.ColorSheme = PNG_COLOR_SCHEME.P1;
              break;
            default:
              throw new InvalidDataException("Invalid ColorType/BitDepth combination!");
          }
          break;
        case 2:
          switch ((PNG_COLOR_TYPE) colorType)
          {
            case PNG_COLOR_TYPE.GRAYSCALE:
              file.ColorSheme = PNG_COLOR_SCHEME.G2;
              break;
            case PNG_COLOR_TYPE.PALLETE:
              file.ColorSheme = PNG_COLOR_SCHEME.P2;
              break;
            default:
              throw new InvalidDataException("Invalid ColorType/BitDepth combination!");
          }
          break;
        case 4:
          switch ((PNG_COLOR_TYPE) colorType)
          {
            case PNG_COLOR_TYPE.GRAYSCALE:
              file.ColorSheme = PNG_COLOR_SCHEME.G4;
              break;
            case PNG_COLOR_TYPE.PALLETE:
              file.ColorSheme = PNG_COLOR_SCHEME.P4;
              break;
            default:
              throw new InvalidDataException("Invalid ColorType/BitDepth combination!");
          }
          break;
        case 8:
          switch ((PNG_COLOR_TYPE) colorType)
          {
            case PNG_COLOR_TYPE.GRAYSCALE:
              file.ColorSheme = PNG_COLOR_SCHEME.G8;
              break;
            case PNG_COLOR_TYPE.TRUECOLOR:
              file.ColorSheme = PNG_COLOR_SCHEME.TC8;
              break;
            case PNG_COLOR_TYPE.PALLETE:
              file.ColorSheme = PNG_COLOR_SCHEME.P8;
              break;
            case PNG_COLOR_TYPE.GRAYSCALE_ALPHA:
              file.ColorSheme = PNG_COLOR_SCHEME.GA8;
              break;
            case PNG_COLOR_TYPE.TRUECOLOR_ALPHA:
              file.ColorSheme = PNG_COLOR_SCHEME.TCA8;
              break;
            default:
              throw new InvalidDataException("Invalid ColorType/BitDepth combination!");
          }
          break;
        case 16:
          switch ((PNG_COLOR_TYPE) colorType)
          {
            case PNG_COLOR_TYPE.GRAYSCALE:
              file.ColorSheme = PNG_COLOR_SCHEME.G16;
              break;
            case PNG_COLOR_TYPE.TRUECOLOR:
              file.ColorSheme = PNG_COLOR_SCHEME.TC16;
              break;
            case PNG_COLOR_TYPE.GRAYSCALE_ALPHA:
              file.ColorSheme = PNG_COLOR_SCHEME.GA16;
              break;
            case PNG_COLOR_TYPE.TRUECOLOR_ALPHA:
              file.ColorSheme = PNG_COLOR_SCHEME.TCA16;
              break;
            default:
              throw new InvalidDataException("Invalid ColorType/BitDepth combination!");
          }
          break;
      }

      byte comp = buffer[pos++];
      if (comp != 0)
        throw new InvalidDataException("Invalid Compression!");
      file.Compression = 0;

      byte filter = buffer[pos++];
      if (filter != 0)
        throw new InvalidCastException("Invalid Filter!");

      byte interlance = buffer[pos++];
      if (interlance != 0 && interlance != 1)
        throw new InvalidCastException("Invalid Interlance!");
      file.Interlance = (PNG_INTERLANCE)interlance;

      if (!crc.VerifyCheckSum(BufferReader.ReadUInt32BE(ref buffer, ref pos)))
        throw new InvalidDataException("Invalid CRC32 for IHDR chunk!");
    }

    public static void ParseIDAT(PNGFile file, Stream stream, uint len, CRC32Impl crc)
    {
      // TODO: support uint reads and not just int
      Debug.Assert(len < Int32.MaxValue);
      byte[] arr = ArrayPool<byte>.Shared.Rent((int)len);
      int bytesRead = stream.Read(arr, 0, (int)len);
      if (bytesRead != len)
        throw new InvalidDataException("Invalid IDAT chunk data!");
      crc.UpdateCRC(arr.AsSpan().Slice(0, (int)len));
      ZLibStream zLib = DecompressionHelper.GetZLibStreamDecompress(arr);

      byte bitsPerPixel = PNGHelper.GetBitsPerPixel(file.ColorSheme, file.BitDepth);
      // do we really need this?? We should prob use this for rowSizeCalc
      int bytesPerPixel = PNGHelper.GetBytesPerPixel(bitsPerPixel);

      uint rowSize = PNGHelper.GetRowSize(bitsPerPixel, file.Width);
      // maybe we should see if it overflowed and wrapped?
      byte[] currRow = new byte[rowSize];
      byte[] prevRow = new byte[rowSize];

      byte[] output = new byte[(rowSize - 1) * file.Height];
      if (file.Interlance == PNG_INTERLANCE.NONE)
      {
        // already decomposed
        byte f = 0;
        for (int i = 0; i < file.Height; i++)
        {
          // Read is limited to int but row can be uint..??
          // TODO: see how to deal with this or just assume row that is over int max value is not valid..?(prob now)
          bytesRead = zLib.Read(currRow);
          if (bytesRead != currRow.Length)
            throw new InvalidDataException("Invalid pixel data row length data!");

          Span<byte> currData = currRow.AsSpan().Slice(1);
          Span<byte> prevData = prevRow.AsSpan().Slice(1);

          byte filter = currRow[0];
          Debug.Assert(filter == 0);
          switch (filter)
          {
            case (byte)PNG_FILTER.NONE:
              break;

            case (byte)PNG_FILTER.SUB:
              break;
            case (byte)PNG_FILTER.UP:
              break;
            case (byte)PNG_FILTER.AVERAGE:
              break;
            case (byte)PNG_FILTER.PAETH:
              break;
            default:
              throw new InvalidDataException("Unknown Filter Type!");
          }

          Array.ConstrainedCopy(currRow, 1, output, i * currData.Length, currData.Length);
          Array.Copy(currRow, prevRow, currRow.Length); // Set currentRow to be prev
        }
        file.RawIDAT = output;
      }
      else if (file.Interlance == PNG_INTERLANCE.ADAM7)
      {
        throw new NotSupportedException("ADAM7 not supported");
      }
    } 
  }
}
