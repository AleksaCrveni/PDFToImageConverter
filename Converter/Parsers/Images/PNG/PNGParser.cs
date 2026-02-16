using Converter.FileStructures.PNG;
using Converter.Utils;
using System.Buffers.Binary;
using System.Diagnostics;

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
      bool EOF = false;

      len = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(25, 4));
      chunkType = (PNG_CHUNK_TYPE)BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(29, 4));
      uint currCrc = 0;
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
            // call updateCRC over buffer and return CrC value
            // 
            //currCrc = ParseIDAT(crc);
            // read next 8
            bytesRead = stream.Read(buffer.Slice(0, 8));
            if (bytesRead != 8)
              throw new InvalidDataException("IDAT can't be last chunk!");
            break;
          case PNG_CHUNK_TYPE.NULL:
            throw new InvalidDataException("Unknown chunk!");
          default:
            // skip
            break;
        }
        // shortcut
        if (EOF)
          break;

        if (!crc.VerifyCheckSum(currCrc))
          throw new InvalidDataException($"Invalid CRC32 for {chunkType} chunk!");
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
      byte bitDepth = buffer[pos++];
      if (bitDepth != 1 && bitDepth != 2 && bitDepth != 4 && bitDepth != 8 && bitDepth != 16)
        throw new InvalidDataException("Invalid BitDepth value!");
      byte colorType = buffer[pos++];
      if (colorType != 0 && colorType != 2 && colorType != 3 && colorType != 4 && colorType != 6)
        throw new InvalidDataException("Invalid ColorType value!");

      // Don't check for 0 because we already parsed bitDepth
      // TODO: Maybe we can just check combinations here instead of doing separate checks
      if ((colorType == 2 || colorType == 4 || colorType == 8) && bitDepth != 8 && bitDepth != 16)
          throw new InvalidDataException("Invalid BitDepth and ColorType combination!");
      else if (colorType == 3 && bitDepth != 1 && bitDepth != 2 && bitDepth != 4 && bitDepth != 8)
        throw new InvalidDataException("Invalid BitDepth and ColorType combination!");

      file.BitDepth = bitDepth;
      file.ColorType = (PNG_COLOR_TYPE)colorType;
      if ((byte)file.ColorType == 3)
        file.SampleDepth = 8;
      else
        file.SampleDepth = bitDepth;

      byte comp = buffer[pos++];
      if (comp != 0)
        throw new InvalidDataException("Invalid Compression!");
      file.Compression = 0;

      byte filter = buffer[pos++];
      if (filter != 0)
        throw new InvalidCastException("Invalid Filter!");
      file.Filter = 0;

      byte interlance = buffer[pos++];
      if (interlance != 0 && interlance != 1)
        throw new InvalidCastException("Invalid Interlance!");
      file.Interlance = (PNG_INTERLANCE)interlance;

      if (!crc.VerifyCheckSum(BufferReader.ReadUInt32BE(ref buffer, ref pos)))
        throw new InvalidDataException("Invalid CRC32 for IHDR chunk!");
    }
  }
}
