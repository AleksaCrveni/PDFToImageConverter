using Converter.FileStructures.TIFF;
using System.Buffers;
using System.Buffers.Binary;

namespace Converter.Parsers.Images.TIFF
{
  // NOTE: FOR NOW JUST SUPPORT LITTLE ENDIAN
  
  public static class TIFFParser
  {
    private static readonly int KB = 1024;
  
    public static TIFFFile? Parse(string filePath)
    {

      using (FileStream fs = File.OpenRead(filePath)) 
      {
        return Parse(fs);
      }
    }

    public static TIFFFile? Parse(Stream s)
    {
      TIFFFile file = new TIFFFile();
      ParseHeader(file, s);
      ParseIFD(file, s);
      ParseImageData(file, s);
      return file;
    }

    public static TIFF_DATA? GetImage(TIFFFile file, int pageIndex = 0)
    {
      if (pageIndex + 1 > file.TIFFs.Count)
        return null;

      foreach (TIFF_DATA ifd in file.TIFFs)
      {
        if (ifd.Metadata.PageNumber == pageIndex)
          return ifd;
      }

      return null;
    }
    
    public static void ParseImageData(TIFFFile file, Stream s)
    {
      TIFF_IFD_DATA ifd;
      TIFF_DATA tiff;
      for (int z = 0; z < file.TIFFs.Count; z++)
      {
        tiff = file.TIFFs[z];
        ifd = tiff.Metadata;
        List<uint> stripOffsets;
        List<uint> stripByteCounts;
        uint totalByteCount = 0;
        if (ifd.StripOffsetCount == 1)
        {
          stripOffsets = new List<uint>(1) { ifd.StripOffsetsPointer };
          stripByteCounts = new List<uint>(1) { ifd.StripByteCountsPointer };
          totalByteCount = ifd.StripByteCountsPointer;
        }
        else
        {
          int offsetBufferSize = ifd.StripOffsetCount * 4;
          int countsBufferSize = ifd.StripByteCountCount * 4;
          
          Span<byte> stripOffsetsBuffer = new byte[offsetBufferSize];// don't do any smart pre alocc for now, alamost laways there will be 1 TIFF per file, improve later if needed
          Span<byte> stripCountsBuffer = new byte[countsBufferSize];
          // Load strip offsets
          s.Position = (long)ifd.StripOffsetsPointer;
          int readBytes = s.Read(stripOffsetsBuffer);
          if (readBytes != stripOffsetsBuffer.Length)
            throw new InvalidDataException("Invalid strip offsets actual value!");

          // Load strip counts
          s.Position = (long)ifd.StripByteCountsPointer;
          readBytes = s.Read(stripCountsBuffer);
          if (readBytes != stripCountsBuffer.Length)
            throw new InvalidDataException("Invalid strip counts actual value!");

          if (stripOffsetsBuffer.Length != stripCountsBuffer.Length)
            throw new InvalidDataException("Invalid strip/offsets values!");

          //TODO(@Aleksa): We could just read here directly instead of making lists, fix when have nothing else to do
          stripOffsets = new List<uint>(offsetBufferSize);
          stripByteCounts = new List<uint>(offsetBufferSize);
          for (int i = 0; i < stripCountsBuffer.Length; i += 4)
          {
            uint offset = 0;
            uint byteCount = 0;
            if (file.Header.IsLittleEndian)
            {
              offset = BinaryPrimitives.ReadUInt32LittleEndian(stripOffsetsBuffer.Slice(i, 4));
              byteCount = BinaryPrimitives.ReadUInt32LittleEndian(stripCountsBuffer.Slice(i, 4));
            }
            else
            {
              offset = offset = BinaryPrimitives.ReadUInt32BigEndian(stripOffsetsBuffer.Slice(i, 4));
              byteCount = BinaryPrimitives.ReadUInt32BigEndian(stripCountsBuffer.Slice(i, 4));
            }

            if (offset + byteCount > s.Length)
            {
              throw new Exception("Invalid strip/offsets. Offset + count are larger than file length!`");
            }
            stripOffsets.Add(offset);
            stripByteCounts.Add(byteCount);
            totalByteCount += byteCount;
          }
        }

        if (ifd.Compression != TIFF_Compression.NoCompression)
          throw new NotSupportedException("Decompression not supported yet!");

        byte[] output = new byte[totalByteCount];
        totalByteCount = 0;

        //TODO(@Aleksa): Make this work for really large images if needed!
        for (int i = 0; i < stripOffsets.Count; i++)
        {
          uint stripOffset = stripOffsets[i];
          uint byteCount = stripByteCounts[i];
          s.Position = stripOffset;
          int readBytes = s.Read(output, (int)totalByteCount, (int)byteCount);
          if (readBytes != byteCount)
            throw new InvalidDataException("Unexpected EOF!");
          totalByteCount += byteCount;
        }

        tiff.RawPixelData = output;
      }
    }

    public static void ParseHeader(TIFFFile file, Stream s)
    {
      TIFF_Header header = file.Header;
      Span<byte> buffer = stackalloc byte[8];
      int readBytes = s.Read(buffer);
      if (readBytes < 8)
        throw new InvalidCastException("Invalid TIFF File. File too short!");
      // 0 - 1 byte order II for little endian, MM for big endian
      // 2 - 3 needs to be 42, 2 bytes because of little and big endian
      // 4 - 7 byte offset to first (0th) IFD
      if (buffer[0] == 'I' && buffer[1] == 'I')
        header.IsLittleEndian = true;
      else
        header.IsLittleEndian = false;

      // check if its valid TIFF file
      // b2 must be 42 if its little endian or b3 if its big endian
      if (!(header.IsLittleEndian == true && buffer[2] == 42)
        && !(header.IsLittleEndian == false && buffer[3] == 42))
        throw new InvalidDataException("Invalid TIFF Header. 42 check failed");

      if (header.IsLittleEndian)
        header.FirstIFDByteOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4, 4));
      else
        header.FirstIFDByteOffset = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(4, 4));
      file.Header = header;
    }
    // NoDE => number of directory entries
    public static void ParseIFD(TIFFFile file, Stream s)
    {
      s.Position = file.Header.FirstIFDByteOffset;
      // alloc separate buffer and then load entire IFD
      Span<byte> NoDEBuffer = stackalloc byte[2];
      int bytesRead = s.Read(NoDEBuffer);
      if (bytesRead < 2)
        throw new InvalidDataException("Invalid IFD.");

      short NoDE = 0;
      if (file.Header.IsLittleEndian)
        NoDE = BinaryPrimitives.ReadInt16LittleEndian(NoDEBuffer);
      else
        NoDE = BinaryPrimitives.ReadInt16BigEndian(NoDEBuffer);

      // * 12 because IFD is 12 bytes long, + 4 for next IFD offset
      int sizeOfAllDirectoryEntries = NoDE * 12 + 4;
      Span<byte> buffer = sizeOfAllDirectoryEntries <= 4 * KB ? stackalloc byte[sizeOfAllDirectoryEntries] : new byte[sizeOfAllDirectoryEntries];
      int readBytes = s.Read(buffer);
      if (readBytes < sizeOfAllDirectoryEntries)
        throw new InvalidDataException("Invalid IFD NoDE.");

      // -4 for next IFD position
      TIFF_DATA tiffData = new TIFF_DATA();
      TIFF_IFD_DATA tag = new TIFF_IFD_DATA();
      TIFF_TagType tagValue = 0;
      TIFF_TagSize type = 0;
      uint count = 0;
      uint valueOrOffset;
      // usedd
      Span<byte> rationalBuffer = stackalloc byte[8];
      // something  is wrong with my offsets or slices....... it gives wrong values, maybe something im offset by 1 or something?/
      for (int i = 0; i < buffer.Length - 4; i += 12)
      {
        tagValue = 0; 
        type = 0;     
        count = 0;    
        // can just read this it will work for both 
        valueOrOffset = 0; 
        if (file.Header.IsLittleEndian)
        {
          tagValue = (TIFF_TagType)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(i, 2));
          type = (TIFF_TagSize)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(i + 2, 2));
          count = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(i + 4, 4));
          // this checks arent really needed for little endian because of byte order but keep it for now to make clear why we are doing it for big endian
          if (count * (int)type > 4 || type == TIFF_TagSize.LONG) // its pointer so its 4 bytes, but referenced values are size of type
          {
            valueOrOffset = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(i + 8, 4));
          }
          else if (type == TIFF_TagSize.SHORT)
          {
            valueOrOffset = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(i + 8, 2));
          }
          else
          {
            throw new NotImplementedException();
          }

          valueOrOffset = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(i + 8, 4));
        }
        else
        {
          tagValue = (TIFF_TagType)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(i, 2));
          type = (TIFF_TagSize)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(i + 2, 2));
          count = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(i + 4, 4));
          if (count * (int)type > 4 || type == TIFF_TagSize.LONG) // its pointer so its 4 bytes, but referenced values are size of type
          {
            valueOrOffset = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(i + 8, 4));
          }
          else if (type == TIFF_TagSize.SHORT)
          {
            valueOrOffset = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(i + 8, 2));
          }
          else
          {
            throw new NotImplementedException();
          }
          
          
        }
        // make enums for tag values
        // don't do casting because some values can have 0 and some can't and some are required and 
        // we can't rely on defaulting, so just check and assign where you can't cast based on range
        // IMPROVE THIS SO ACCOUNT FOR Type.......
        switch (tagValue)
        {
          case TIFF_TagType.NewSubfileType:
            tag.NewSubfileType = valueOrOffset;
            break;
          case TIFF_TagType.ImageWidth:
            tag.ImageWidth = valueOrOffset;
            break;
          case TIFF_TagType.ImageLength:
            tag.ImageHeight = valueOrOffset;
            break;
          case TIFF_TagType.BitsPerSample:
            tag.BitsPerSampleCount = (ushort)count;
            tag.BitsPerSample = (ushort)valueOrOffset;
            if (tag.BitsPerSampleCount > 1) // valueOrOffset is offset
            {
              // for now just support this
              List<ushort> values = new List<ushort>((int)count);
              int byteLen = (int)count * 2;
              byte[] buff = ArrayPool<byte>.Shared.Rent(byteLen);
              Span<byte> arr = buff.AsSpan(0, byteLen);

              s.Position = valueOrOffset;
              bytesRead = s.Read(arr);
              if (bytesRead != arr.Length)
                throw new InvalidDataException("Invalid BitsPerSample offset!");
              if (file.Header.IsLittleEndian)
              {
                int bytePointer = 0;
                for (int index = 0; index < count; index ++)
                {
                  values.Add(BinaryPrimitives.ReadUInt16LittleEndian(arr.Slice(bytePointer, 2)));
                  bytePointer += 2;
                }
              }
              else
              {
                int bytePointer = 0;
                for (int index = 0; index < count; index++)
                {
                  values.Add(BinaryPrimitives.ReadUInt16BigEndian(arr.Slice(bytePointer, 2)));
                  bytePointer += 2;
                }
              }

              tag.BitsPerSample = values[0];
              for (int index = 0; index < count; index++)
                if (tag.BitsPerSample != values[index])
                  throw new InvalidDataException("Only equal bits per samples supporeted!");
            }
            break;
          case TIFF_TagType.Compression:
            TIFF_Compression c = valueOrOffset switch
            {
              1 => TIFF_Compression.NoCompression,
              2 => TIFF_Compression.CCITT,
              32773 => TIFF_Compression.PackBits,
              _ => throw new InvalidDataException("Invalid compression tag value!")
            };
            tag.Compression = c;
            break;
          case TIFF_TagType.PhotometricInterpretation:
            TIFF_PhotometricInterpretation p = valueOrOffset switch
            {
              >= 0 and <= 4 => (TIFF_PhotometricInterpretation)valueOrOffset,
              _ => throw new InvalidDataException("Invalid photometric interpretation tag value!")
            };
            tag.PhotometricInterpretation = p;
            break;
          case TIFF_TagType.FillOrder:
            if (valueOrOffset < 1 || valueOrOffset > 2)
              throw new InvalidDataException("Invalid fill ordrer tag value!");
            tag.FillOrder = (ushort)valueOrOffset;
            break;
          case TIFF_TagType.StripOffsetsPointer:
            // process later
            tag.StripOffsetsPointer = valueOrOffset;
            tag.StripOffsetCount = (int)count;
            break;
          case TIFF_TagType.Orientation:
            if (valueOrOffset < 1 || valueOrOffset > 8)
              throw new InvalidDataException("Invalid orientation tag value!");
            tag.Orientation = (ushort)valueOrOffset;
            break;
          case TIFF_TagType.SamplesPerPixel:
            // not sure if there is any validation here
            tag.SamplesPerPixel = (ushort)valueOrOffset;
            break;
          case TIFF_TagType.RowsPerStrip:
            tag.RowsPerStrip = valueOrOffset;
            break;
          case TIFF_TagType.StripByteCountsPointer:
            // process later
            tag.StripByteCountsPointer = valueOrOffset;
            tag.StripByteCountCount = (int)count;
            break;
          case TIFF_TagType.XResolution:
            // this is pointer
            rationalBuffer.Clear();
            s.Position = valueOrOffset;
            bytesRead = s.Read(rationalBuffer);
            if (bytesRead != 8)
              throw new InvalidDataException("Invalid rational. Unexpected EOS!");
            uint numerator = 0; 
            uint denominator = 0;
            if (file.Header.IsLittleEndian)
            {
              numerator = BinaryPrimitives.ReadUInt32LittleEndian(rationalBuffer.Slice(0, 4));
              denominator = BinaryPrimitives.ReadUInt32LittleEndian(rationalBuffer.Slice(4, 4));
            }
            else
            {
              numerator = BinaryPrimitives.ReadUInt32BigEndian(rationalBuffer.Slice(0, 4));
              denominator = BinaryPrimitives.ReadUInt32BigEndian(rationalBuffer.Slice(4, 4));
            }
            tag.XResolution = numerator / (double)denominator;
            break;
          case TIFF_TagType.YResolution:

            rationalBuffer.Clear();
            s.Position = valueOrOffset;
            bytesRead = s.Read(rationalBuffer);
            if (bytesRead != 8)
              throw new InvalidDataException("Invalid rational. Unexpected EOS!");
            numerator = 0;
            denominator = 0;

            if (file.Header.IsLittleEndian)
            {
              numerator = BinaryPrimitives.ReadUInt32LittleEndian(rationalBuffer.Slice(0, 4));
              denominator = BinaryPrimitives.ReadUInt32LittleEndian(rationalBuffer.Slice(4, 4));
            }
            else
            {
              numerator = BinaryPrimitives.ReadUInt32BigEndian(rationalBuffer.Slice(0, 4));
              denominator = BinaryPrimitives.ReadUInt32BigEndian(rationalBuffer.Slice(4, 4));
            }
            tag.YResolution = numerator / (double)denominator;
            break;
          case TIFF_TagType.PlanarConfiguration:
            if (valueOrOffset < 1 || valueOrOffset > 2)
              throw new InvalidDataException("Invalid planar configuration value!");
            tag.PlanarConfiguration = (ushort)valueOrOffset;
            break;
          case TIFF_TagType.ResolutionUnit:
            TIFF_ResolutionUnit r = valueOrOffset switch
            {
              1 => TIFF_ResolutionUnit.NoAbsoluteUnitOfMe,
              2 => TIFF_ResolutionUnit.Inch,
              3 => TIFF_ResolutionUnit.Centimeter,
              _ => throw new InvalidDataException("Invalid Resolution Unit tag value!")
            };
            tag.ResolutionUnit = r;
            break;
          case TIFF_TagType.PageNumber:
            // first short in long is pagenubmer, second part is total pages
            ushort pageNumber = (ushort)(valueOrOffset >> 16);
            ushort totalPages = (ushort)valueOrOffset; // can just cutoff
            if (totalPages != 0)
              file.TotalPages = totalPages;
            tag.PageNumber = pageNumber;
            break;
          default:
            //throw new NotSupportedException($"Tag not supported add it. {tagValue}"); // this is just for development purposes
            break;
        }
       
      }
      tiffData.Metadata = tag;
      file.TIFFs.Add(tiffData);
      // next IFD is last 4 bytes but read only 1 for now
      // its 0 if this is last IFD
      file.TotalPages = file.TIFFs.Count;
    }
    
  }
}