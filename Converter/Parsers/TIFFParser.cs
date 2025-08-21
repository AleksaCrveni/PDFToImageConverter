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
      Tags tags = new Tags();
      short tag = 0;
      short type = 0;
      int count = 0;
      int valueOrOffset;
      // something  is wrong with my offsets or slices....... it gives wrong values, maybe something im offset by 1 or something?/
      for (int i = 0; i < buffer.Length - 4; i += 12)
      {
        tag = BitConverter.ToInt16(buffer.Slice(i, 2));
        type = BitConverter.ToInt16(buffer.Slice(i + 2, 2));
        count = BitConverter.ToInt32(buffer.Slice(i + 4, 4));
        valueOrOffset = BitConverter.ToInt32(buffer.Slice(i + 8, 4));

        switch (tag)
        {
          case 254:
            tags.NewSubfileType = valueOrOffset;
            break;
          case 256:
            tags.ImageWidth = valueOrOffset;
            break;
          case 257:
            tags.ImageLength = valueOrOffset;
            break;
          case 258:
            tags.BitsPerSample = (short)valueOrOffset;
            break;
          case 259:
            if (valueOrOffset != 1 && valueOrOffset != 2 && valueOrOffset != 32773)
              throw new InvalidDataException("Invalid compression tag value!");
            tags.Compression = (short)valueOrOffset;
            break;
          case 262:
            if (valueOrOffset != 0 && valueOrOffset !=1)
              throw new InvalidDataException("Invalid photometric interpretation tag value!");
            tags.PhotometricInterpretation = (short)valueOrOffset;
            break;
          case 273:
            tags.StripOffsets = valueOrOffset;
            break;
          case 278:
            tags.RowsPerStrip = valueOrOffset;
            break;
          case 279:
            tags.StripByteCounts = valueOrOffset;
            break;
          case 282:
            // is this pointer?
            tags.XResolution = valueOrOffset;
            break;
          case 283:
            tags.YResolution = valueOrOffset;
            break;
          case 296:
            if (valueOrOffset < 1 || valueOrOffset > 3)
              throw new InvalidDataException("Invalid Resolution Unit tag value!");
            tags.ResolutionUnit = (short)valueOrOffset;
            break;
          default:
            throw new NotSupportedException($"Tag not supported add it. {tag}"); // this is just for development purposes
        }
      }
    }
  }
}