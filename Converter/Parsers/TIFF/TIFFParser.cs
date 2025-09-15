using Converter.FIleStructures;
namespace Converter.Parsers.FileParsers
{
  // NOTE: FOR NOW JUST SUPPORT LITTLE ENDIAN
  public class TIFFParser
  {
    private static readonly int KB = 1024;
    public TIFFParser()
    {

    }

    public void Parse(string filePath)
    {
      Stream stream = File.OpenRead(filePath);
      TIFFFile file = new TIFFFile();
      file.Stream = stream;
      ParseHeader(file);
      ParseIFD(file);
      ParseImageData(file);
    }
    
    public void ParseImageData(TIFFFile file)
    {
      // First check max stripCount so we can allocate just once and not for each 'image'(IFD)
      uint maxStripCount = 0;
      foreach (TIFFData d in file.TIFFs)
      {
        uint stripCount = d.Tag.ImageLength / d.Tag.RowsPerStrip;
        if (d.Tag.ImageLength % d.Tag.RowsPerStrip > 0)
          stripCount++;
        if (stripCount > maxStripCount)
          maxStripCount = stripCount;
      }
      // Mini Arena with double pointer testing
      // * 4 because we are loading uint32 each strip
      // * 2 because we want to load strip byte count into same arena
      // usually do 8KB, but do 16 check beucase we will alloc 2 arrays
      // ARENA can't work easily, as I cant read into span starting at half
      Span<byte> stripOffsetsBuffer = maxStripCount * 4 <= KB * 4 ? stackalloc byte[(int)maxStripCount * 4] : new byte[maxStripCount * 4];
      Span<byte> stripCountsBuffer = maxStripCount * 4 <= KB * 4 ? stackalloc byte[(int)maxStripCount * 4] : new byte[maxStripCount * 4];
      Tag tag;
      TIFFData data;
      for (int z = 0; z < file.TIFFs.Count; z++)
      {
        data = file.TIFFs[z];
        tag = data.Tag;
        uint stripCount = (uint)Math.Floor((tag.ImageLength + tag.RowsPerStrip -1) / (decimal)tag.RowsPerStrip);
       

        // Load strip offsets
        file.Stream.Position = (long)tag.StripOffsetsPointer;
        int readBytes = file.Stream.Read(stripOffsetsBuffer);
        if (readBytes != stripOffsetsBuffer.Length)
          throw new InvalidDataException("Invalid strip offsets actual value!");

        // Load strip counts
        file.Stream.Position = (long)tag.StripByteCountsPointer;
        readBytes = file.Stream.Read(stripCountsBuffer);
        if (readBytes != stripCountsBuffer.Length)
          throw new InvalidDataException("Invalid strip counts actual value!");

        // if its strip count is 1, stripcounts is not pointer but value ??
        if (stripCount == 1)
        {
          uint byteCountVal = BitConverter.ToUInt32(stripCountsBuffer);
          file.Stream.Position = (long)tag.StripOffsetsPointer;
          //TODO: have some limit so we don't run out of memory
          // usually file is very large just heap alloc
          byte[] imageDataBuffer = new byte[(int)tag.StripByteCountsPointer];
          readBytes = file.Stream.Read(imageDataBuffer);
          if (readBytes != imageDataBuffer.Length)
            throw new InvalidDataException("Invalid strip byte counts value! Unexpected EOS!");

          // copy
          data.FullImageData = imageDataBuffer;
          continue;
        }

        // stripCount is lenght because we maybe have larger array but we want to read only what we know its ours
        for (int i= 0; i < stripCount*4; i +=4)
        {
          uint offset = BitConverter.ToUInt32(stripOffsetsBuffer.Slice(i, 4));
          uint byteCount = BitConverter.ToUInt32(stripCountsBuffer.Slice(i, 4));
          if (offset + byteCount > file.Stream.Length)
          {
            throw new Exception("TIF-29");
          }
        }

        // JUST FOR DEBUG
        Span<byte> imgRawArr = new byte[BitConverter.ToUInt32(stripCountsBuffer.Slice(stripCountsBuffer.Length - 4, 4)) + (stripCountsBuffer.Length - 4) / 4 * 8192];
        file.Stream.Position = 8;
        file.Stream.Read(imgRawArr);
      }
    }

    public void ParseHeader(TIFFFile file)
    {
      TIFFHeader header = file.Header;
      Span<byte> buffer = stackalloc byte[8];
      int readBytes = file.Stream.Read(buffer);
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

      // think about using binary primitives
      header.FirstIFDByteOffset = BitConverter.ToInt32(buffer.Slice(4, 4));
      file.Header = header;
    }
    // NoDE => number of directory entries
    public void ParseIFD(TIFFFile file)
    {
      file.Stream.Position = file.Header.FirstIFDByteOffset;
      // alloc separate buffer and then load entire IFD
      Span<byte> NoDEBuffer = stackalloc byte[2];
      int bytesRead = file.Stream.Read(NoDEBuffer);
      if (bytesRead < 2)
        throw new InvalidDataException("Invalid IFD.");
      short NoDE = BitConverter.ToInt16(NoDEBuffer);

      // * 12 because IFD is 12 bytes long, + 4 for next IFD offset
      int sizeOfAllDirectoryEntries = NoDE * 12 + 4;
      Span<byte> buffer = sizeOfAllDirectoryEntries <= 4 * KB ? stackalloc byte[sizeOfAllDirectoryEntries] : new byte[sizeOfAllDirectoryEntries];
      int readBytes = file.Stream.Read(buffer);
      if (readBytes < sizeOfAllDirectoryEntries)
        throw new InvalidDataException("Invalid IFD NoDE.");

      // -4 for next IFD position
      TIFFData tiffData = new TIFFData();
      Tag tag = new Tag();
      ushort tagValue = 0;
      ushort type = 0;
      uint count = 0;
      uint valueOrOffset;
      // usedd
      Span<byte> rationalBuffer = stackalloc byte[8];
      // something  is wrong with my offsets or slices....... it gives wrong values, maybe something im offset by 1 or something?/
      for (int i = 0; i < buffer.Length - 4; i += 12)
      {
        tagValue = BitConverter.ToUInt16(buffer.Slice(i, 2));
        type = BitConverter.ToUInt16(buffer.Slice(i + 2, 2));
        count = BitConverter.ToUInt32(buffer.Slice(i + 4, 4));

        // can just read this it will work for both 
        valueOrOffset = BitConverter.ToUInt32(buffer.Slice(i + 8, 4));

        // make enums for tag values
        // don't do casting because some values can have 0 and some can't and some are required and 
        // we can't rely on defaulting, so just check and assign where you can't cast based on range
        // IMPROVE THIS SO ACCOUNT FOR Type.......
        switch (tagValue)
        {
          case 254:
            tag.NewSubfileType = valueOrOffset;
            break;
          case 256:
            tag.ImageWidth = valueOrOffset;
            break;
          case 257:
            tag.ImageLength = valueOrOffset;
            break;
          case 258:
            tag.BitsPerSample = (ushort)valueOrOffset;
            break;
          case 259:
            Compression c = valueOrOffset switch
            {
              1 => Compression.NoCompression,
              2 => Compression.CCITT,
              32773 => Compression.PackBits,
              _ => throw new InvalidDataException("Invalid compression tag value!")
            };
            tag.Compression = c;
            break;
          case 262:
            PhotometricInterpretation p = valueOrOffset switch
            {
              >= 0 and <= 4 => (PhotometricInterpretation)valueOrOffset,
              _ => throw new InvalidDataException("Invalid photometric interpretation tag value!")
            };
            tag.PhotometricInterpretation = p;
            break;
          case 266:
            if (valueOrOffset < 1 || valueOrOffset > 2)
              throw new InvalidDataException("Invalid fill ordrer tag value!");
            tag.FillOrder = (ushort)valueOrOffset;
            break;
          case 273:
            // process later
            tag.StripOffsetsPointer = valueOrOffset;
            break;
          case 274:
            if (valueOrOffset < 1 || valueOrOffset > 8)
              throw new InvalidDataException("Invalid orientation tag value!");
            tag.Orientation = (ushort)valueOrOffset;
            break;
          case 277:
            // not sure if there is any validation here
            tag.SamplesPerPixel = (ushort)valueOrOffset;
            break;
          case 278:
            tag.RowsPerStrip = valueOrOffset;
            break;
          case 279:
            // process later
            tag.StripByteCountsPointer = valueOrOffset;
            break;
          case 282:
            // this is pointer
            rationalBuffer.Clear();
            file.Stream.Position = valueOrOffset;
            bytesRead = file.Stream.Read(rationalBuffer);
            if (bytesRead != 8)
              throw new InvalidDataException("Invalid rational. Unexpected EOS!");
            uint numerator = BitConverter.ToUInt32(rationalBuffer.Slice(0, 4));
            uint denominator = BitConverter.ToUInt32(rationalBuffer.Slice(4, 4));
            tag.XResolution = numerator / (double)denominator;
            break;
          case 283:

            rationalBuffer.Clear();
            file.Stream.Position = valueOrOffset;
            bytesRead = file.Stream.Read(rationalBuffer);
            if (bytesRead != 8)
              throw new InvalidDataException("Invalid rational. Unexpected EOS!");
            numerator = BitConverter.ToUInt32(rationalBuffer.Slice(0, 4));
            denominator = BitConverter.ToUInt32(rationalBuffer.Slice(4, 4));
            tag.YResolution = numerator / (double)denominator;
            break;
          case 284:
            if (valueOrOffset < 1 || valueOrOffset > 2)
              throw new InvalidDataException("Invalid planar configuration value!");
            tag.PlanarConfiguration = (ushort)valueOrOffset;
            break;
          case 296:
            ResolutionUnit r = valueOrOffset switch
            {
              1 => ResolutionUnit.NoAbsoluteUnitOfMe,
              2 => ResolutionUnit.Inch,
              3 => ResolutionUnit.Centimeter,
              _ => throw new InvalidDataException("Invalid Resolution Unit tag value!")
            };
            tag.ResolutionUnit = r;
            break;
          case 297:
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
      tiffData.Tag = tag;
      file.TIFFs.Add(tiffData);
      // next IFD is last 4 bytes but read only 1 for now
      // its 0 if this is last IFD
    }
  }
}