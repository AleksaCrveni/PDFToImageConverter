using Converter.FileStructures.ICC;
using Converter.Utils;

namespace Converter.Parsers.ICC
{
  // https://archive.color.org/files/ICC1V42.pdf
  public class ICCParser
  {
    public byte[] _buffer;
    // this will be used to check some specific things if its part of PDF 
    // since PDF only support some device color spaces and profiles
    public bool _partOfPDF;
    public ICCParser(byte[] buff, bool partOfPDF = true)
    {
      _buffer = buff;
      _partOfPDF = partOfPDF;
    }

    public ICCProfile Parse()
    {
      ICCProfile profile = new ICCProfile();
      profile.Header = ParseHeader();
      profile.TagDefinitions = ParseTagDefinitions();
      return profile;
    }
    public ICCHeader ParseHeader()
    {
      ICCHeader header = new ICCHeader();
      int pos = 0;
      ReadOnlySpan<byte> buffer = _buffer.AsSpan(0, 130);
      header.ProfileSize = BufferReader.ReadUInt32BE(ref buffer, ref pos);
      header.CMMType = BufferReader.ReadUInt32BE(ref buffer, ref pos);
      // we have to take half byte or something like that
      header.MajorVersion = buffer[pos++];
      header.MinorVersion = buffer[pos++];
      pos += 2; // skip 
      header.ProfileClass = (ICC_PROFILE_CLASS)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined<ICC_PROFILE_CLASS>(header.ProfileClass))
        throw new InvalidDataException("Invalid profile class!");

      header.DataColorSpace = (ICC_DATA_COLORSPACE)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined<ICC_DATA_COLORSPACE>(header.DataColorSpace))
        throw new InvalidDataException("Invalid data color space class!");

      if (_partOfPDF)
      {
        if (header.ProfileClass != ICC_PROFILE_CLASS.scnr
         && header.ProfileClass != ICC_PROFILE_CLASS.mntr
         && header.ProfileClass != ICC_PROFILE_CLASS.prtr
         && header.ProfileClass != ICC_PROFILE_CLASS.spac)
          throw new InvalidDataException("Profile class not supported in PDF!");

        if (header.DataColorSpace != ICC_DATA_COLORSPACE.RGB
         && header.DataColorSpace != ICC_DATA_COLORSPACE.GRAY
         && header.DataColorSpace != ICC_DATA_COLORSPACE.CMYK
         && header.DataColorSpace != ICC_DATA_COLORSPACE.Lab)
          throw new InvalidCastException("Data Color Space not supported in PDF!");
      }

      header.ProfileConnectionSpace = (ICC_DATA_COLORSPACE)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined<ICC_DATA_COLORSPACE>(header.ProfileConnectionSpace))
        throw new InvalidDataException("Invalid profile connection space space class!");

      if (header.ProfileClass != ICC_PROFILE_CLASS.link)
      {
        if (header.ProfileConnectionSpace != ICC_DATA_COLORSPACE.XYZ
          && header.ProfileConnectionSpace != ICC_DATA_COLORSPACE.Lab)
          throw new InvalidDataException("Invalid ProfileConnectionSpace and ProfileClass combination!");
      }

      header.CreatedAt = ParseICCDateTime(ref buffer, ref pos);
      header.FileSignature = BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (header.FileSignature != 0x61637370) // value from spec is wrong here this is correct one
        throw new InvalidDataException("Invalid file signature!");

      header.Platform = (ICC_PRIMARY_PLATFORM)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined<ICC_PRIMARY_PLATFORM>(header.Platform))
        throw new InvalidDataException("Invalid PrimayPlatform!");

      uint profileFlags = BufferReader.ReadUInt32BE(ref buffer, ref pos);
      header.Embedded = (byte)profileFlags == 0 ? false : true;
      header.Independent = profileFlags >> 2 == 0 ? false : true;
      
      header.DeviceManufacturer = BufferReader.ReadUInt32BE(ref buffer, ref pos);
      header.DeviceModelField = BufferReader.ReadUInt32BE(ref buffer, ref pos);

      pos += 4; // we skip first 32 bits since bata is 32 LSB
      header.DeviceAttributeFields = BufferReader.ReadUInt32BE(ref buffer, ref pos);

      pos += 2;
      header.RenderingIntent = (ICC_RENDERING_INTENT)BufferReader.ReadUInt16BE(ref buffer, ref pos);
      if (!Enum.IsDefined<ICC_RENDERING_INTENT>(header.RenderingIntent))
        throw new InvalidDataException("Invalid Rendering Intent");

      pos += 12; // always just set D50 as default since its only permitted
      header.ConnectionSpaceIlluminant = new ICC_XYZNumber(0.9642, 1, 0.8249);

      header.ProfileCreator = BufferReader.ReadUInt32BE(ref buffer, ref pos);

      // calculate checksum
      return header;  
    }


    public List<ICC_TagDef> ParseTagDefinitions()
    {

      int pos = 128;
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      int tagCount = (int)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      List<ICC_TagDef> list = new List<ICC_TagDef>(tagCount);
      int i = 0;
      while (i++ < tagCount)
      {
        ICC_TagDef tDef = new ICC_TagDef();
        tDef.Type = (ICC_TAG_TYPE)BufferReader.ReadUInt32BE(ref buffer, ref pos);
        if (!Enum.IsDefined<ICC_TAG_TYPE>(tDef.Type))
          throw new InvalidDataException("Invalid tag signature!");

        tDef.Offset = BufferReader.ReadUInt32BE(ref buffer, ref pos);
        tDef.Size = BufferReader.ReadUInt32BE(ref buffer, ref pos);
        list.Add(tDef);
      }
      return list;
    }
    public DateTime ParseICCDateTime(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      int year = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      int month = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      int day = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      int hour = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      int minute = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      int second = BufferReader.ReadUInt16BE(ref buffer, ref pos);

      return new DateTime(year, month, day, hour, minute, second);
    }

  }
}
