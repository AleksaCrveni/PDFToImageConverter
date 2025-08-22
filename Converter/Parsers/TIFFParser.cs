using Converter.FIleStructures;
using System.Buffers.Binary;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Converter.Parsers
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
    }
    
    public void ParseImageData(TIFFFile file)
    {
      
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
        valueOrOffset = BitConverter.ToUInt32(buffer.Slice(i + 8, 4));
        // make enums for tag values
        // don't do casting because some values can have 0 and some can't and some are required and 
        // we can't rely on defaulting, so just check and assign where you can't cast based on range
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
              (> 0) and (< 4) => (PhotometricInterpretation)valueOrOffset,
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
            tag.StripOffsets = valueOrOffset;
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
            tag.StripByteCounts = valueOrOffset;
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
            tag.XResolution = (double)numerator / (double)denominator;
            break;
          case 283:

            rationalBuffer.Clear();
            file.Stream.Position = valueOrOffset;
            bytesRead = file.Stream.Read(rationalBuffer);
            if (bytesRead != 8)
              throw new InvalidDataException("Invalid rational. Unexpected EOS!");
            numerator = BitConverter.ToUInt32(rationalBuffer.Slice(0, 4));
            denominator = BitConverter.ToUInt32(rationalBuffer.Slice(4, 4));
            tag.YResolution = (double)numerator / (double)denominator;
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
            ushort totalPages = (ushort)(valueOrOffset); // can just cutoff
            if (totalPages != 0)
              file.TotalPages = totalPages;
            tag.PageNumber = pageNumber;
            break;
          default:
             throw new NotSupportedException($"Tag not supported add it. {tagValue}"); // this is just for development purposes
        }
      }
      file.Tags.Add(tag);
      // next IFD is last 4 bytes but read only 1 for now
    }
  }
}