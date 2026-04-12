using Converter.FileStructures.ICC;
using Converter.Utils;
using System.Text;

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
      ParseHeader(profile);
      ParseTagDefinitions(profile);
      ParseTagData(profile);
      return profile;
    }
    public void ParseHeader(ICCProfile profile)
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
      if (!Enum.IsDefined(header.ProfileClass))
        throw new InvalidDataException("Invalid profile class!");

      header.DataColorSpace = (ICC_DATA_COLORSPACE)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined(header.DataColorSpace))
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
      if (!Enum.IsDefined(header.ProfileConnectionSpace))
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
      if (!Enum.IsDefined(header.Platform))
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
      if (!Enum.IsDefined(header.RenderingIntent))
        throw new InvalidDataException("Invalid Rendering Intent");

      pos += 12; // always just set D50 as default since its only permitted
      header.ConnectionSpaceIlluminant = new ICC_XYZNumber(0.9642, 1, 0.8249);

      header.ProfileCreator = BufferReader.ReadUInt32BE(ref buffer, ref pos);

      // calculate checksum
      
      profile.Header = header;  
    }

    public void ParseTagDefinitions(ICCProfile profile)
    {
      int pos = 128;
      ReadOnlySpan<byte> buffer = _buffer.AsSpan();
      int tagCount = (int)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      List<ICC_TagDef> list = new List<ICC_TagDef>(tagCount);
      int i = 0;
      while (i < tagCount)
      {
        ICC_TagDef tDef = new ICC_TagDef();
        tDef.Type = (ICC_TAG_TYPE)BufferReader.ReadUInt32BE(ref buffer, ref pos);
        if (!Enum.IsDefined(tDef.Type))
          throw new InvalidDataException("Invalid tag signature!");

        tDef.Offset = BufferReader.ReadInt32BE(ref buffer, ref pos);
        tDef.Size = BufferReader.ReadInt32BE(ref buffer, ref pos);
        list.Add(tDef);
        i++;
      }
      profile.TagDefinitions = list;
    }

    public void ParseTagData(ICCProfile profile)
    {
      profile.Data = new ICC_Data();
      ReadOnlySpan<byte> tagBuffer;
      foreach (ICC_TagDef tag in profile.TagDefinitions)
      {
        tagBuffer = _buffer.AsSpan().Slice(tag.Offset, tag.Size);
        int pos = 0;

        // make sure ds is known
        ICC_DS_TYPE ds = (ICC_DS_TYPE)BufferReader.ReadUInt32BE(ref tagBuffer, ref pos); 
        if (!Enum.IsDefined(ds))
          throw new InvalidDataException("Unknown Structure Type!");
        pos += 4; // skip reserved

        switch (tag.Type)
        {
          case ICC_TAG_TYPE.A2B0:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.A2B1:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.A2B2:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.bXYZ:
            ParseBXYZ(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.bTRC:
            ParseBTRC(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.B2A0:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.B2A1:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.B2A2:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.calt:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.targ:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.chad:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.chrm:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.clro:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.clrt:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.clot:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.cprt:
            ParseCopyRight(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.dmnd:
            ParseDeviceMFGDesc(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.dmdd:
            ParseDeviceModelDesc(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.gamt:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.kTRC:
            ParseKTRC(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.gXYZ:
            ParseGXYZ(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.gTRC:
            ParseGTRC(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.lumi:
            ParseLuminance(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.meas:
            ParseMeasurement(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.bkpt:
            ParseBlackPoint(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.wtpt:
            ParseWhitePoint(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.ncl2:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.resp:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.pre0:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.pre1:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.pre2:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.desc:
            ParseDescription(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.pseq:
            throw new NotImplementedException("Tag not supported");
            break;
          case ICC_TAG_TYPE.rXYZ:
            ParseRXYZ(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.rTRC:
            ParseRTRC(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.tech:
            ParseTechnology(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.vued:
            ParseViewingCondDesc(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.view:
            ParseViewingConditions(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
          case ICC_TAG_TYPE.dscm:
            ParseAppleMLProfileDesc(profile, tag.Type, ds, ref tagBuffer, ref pos);
            break;
        }
      }
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

    #region TagParsing
    public void ParseCopyRight(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.Copyright = ds switch
      {
        ICC_DS_TYPE.TEXT => ParseText(ref buffer, ref pos),
        ICC_DS_TYPE.MULTI_LOCALIZED_UNICODE => ParseMultiLocalizedUnicode(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }
    public void ParseDescription(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.Description = ds switch
      {
        ICC_DS_TYPE.TEXT => ParseText(ref buffer, ref pos),
        ICC_DS_TYPE.MULTI_LOCALIZED_UNICODE => ParseMultiLocalizedUnicode(ref buffer, ref pos),
        ICC_DS_TYPE.TEXT_DESCRIPTION => ParseTextDescription(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }
    public void ParseWhitePoint(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.WhitePoint = ds switch
      {
        ICC_DS_TYPE.XYZ => ParseXYZType(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseBlackPoint(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.BlackPoint = ds switch
      {
        ICC_DS_TYPE.XYZ => ParseXYZType(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseRXYZ(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.R_XYZ = ds switch
      {
        ICC_DS_TYPE.XYZ => ParseXYZType(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseGXYZ(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.G_XYZ = ds switch
      {
        ICC_DS_TYPE.XYZ => ParseXYZType(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseBXYZ(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.B_XYZ = ds switch
      {
        ICC_DS_TYPE.XYZ => ParseXYZType(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseDeviceMFGDesc(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.DeviceMFGDesc = ds switch
      {
        ICC_DS_TYPE.TEXT_DESCRIPTION => ParseTextDescription(ref buffer, ref pos),
        ICC_DS_TYPE.MULTI_LOCALIZED_UNICODE => ParseMultiLocalizedUnicode(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseDeviceModelDesc(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.DeviceModelDesc = ds switch
      {
        ICC_DS_TYPE.TEXT_DESCRIPTION => ParseTextDescription(ref buffer, ref pos),
        ICC_DS_TYPE.MULTI_LOCALIZED_UNICODE => ParseMultiLocalizedUnicode(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseViewingCondDesc(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.DeviceModelDesc = ds switch
      {
        ICC_DS_TYPE.TEXT_DESCRIPTION => ParseTextDescription(ref buffer, ref pos),
        ICC_DS_TYPE.MULTI_LOCALIZED_UNICODE => ParseMultiLocalizedUnicode(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseViewingConditions(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.ViewingConditions = ds switch
      {
        ICC_DS_TYPE.VIEWING_CONDITIONS => ParseViewingConditionsType(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseLuminance(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.Luminance = ds switch
      {
        ICC_DS_TYPE.XYZ => ParseXYZType(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseMeasurement(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.Measurement = ds switch
      {
        ICC_DS_TYPE.MEASUREMENT => ParseMeasurementType(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseTechnology(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.Technology = ds switch
      {
        ICC_DS_TYPE.SIGNATURE => ParseSignature<ICC_TECHNOLOGY_SIGNATURE>(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    public void ParseRTRC(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ICC_TRC trc = new ICC_TRC();
      trc.Type = ds;
      trc.Points = ds switch
      {
        ICC_DS_TYPE.CURVE => ParseCurve(ref buffer, ref pos),
        ICC_DS_TYPE.PARAMETRIC_CURVE => ParseParametricCurve(ref buffer, ref pos), 
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };

      profile.Data.R_TRC = trc;
    }

    public void ParseGTRC(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ICC_TRC trc = new ICC_TRC();
      trc.Type = ds;
      trc.Points = ds switch
      {
        ICC_DS_TYPE.CURVE => ParseCurve(ref buffer, ref pos),
        ICC_DS_TYPE.PARAMETRIC_CURVE => ParseParametricCurve(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };

      profile.Data.G_TRC = trc;
    }

    public void ParseBTRC(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ICC_TRC trc = new ICC_TRC();
      trc.Type = ds;
      trc.Points = ds switch
      {
        ICC_DS_TYPE.CURVE => ParseCurve(ref buffer, ref pos),
        ICC_DS_TYPE.PARAMETRIC_CURVE => ParseParametricCurve(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };

      profile.Data.B_TRC = trc;
    }

    public void ParseKTRC(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ICC_TRC trc = new ICC_TRC();
      trc.Type = ds;
      trc.Points = ds switch
      {
        ICC_DS_TYPE.CURVE => ParseCurve(ref buffer, ref pos),
        ICC_DS_TYPE.PARAMETRIC_CURVE => ParseParametricCurve(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };

      profile.Data.K_TRC = trc;
    }

    public void ParseAppleMLProfileDesc(ICCProfile profile, ICC_TAG_TYPE tagType, ICC_DS_TYPE ds, ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      profile.Data.AppleMLProfileDesc = ds switch
      {
        ICC_DS_TYPE.TEXT_DESCRIPTION => ParseTextDescription(ref buffer, ref pos),
        ICC_DS_TYPE.MULTI_LOCALIZED_UNICODE => ParseMultiLocalizedUnicode(ref buffer, ref pos),
        _ => throw new NotSupportedException($"Structure {ds} not supported for {tagType.ToString()} Tag!"),
      };
    }

    #endregion TagParsing

    #region StructureParsing
    public string ParseText(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      string res = Encoding.ASCII.GetString(buffer.Slice(pos, buffer.Length - 8));
      pos += buffer.Length - 8;
      return res;
    }

    public string ParseMultiLocalizedUnicode(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      int n = BufferReader.ReadInt32BE(ref buffer, ref pos); // limit to int w
      int recordSize = BufferReader.ReadInt32BE(ref buffer, ref pos);
      if (recordSize != 12)
        throw new NotSupportedException("Not supported recordSize!");

      ICC_MLUC_LANG lang = (ICC_MLUC_LANG)BufferReader.ReadUInt16BE(ref buffer, ref pos);
      pos += 2;
      int firstLen = BufferReader.ReadInt32BE(ref buffer, ref pos);
      int firstOffset = BufferReader.ReadInt32BE(ref buffer, ref pos);
      if (lang == ICC_MLUC_LANG.EN || n == 1)
      {
        // not sure if this is right encoding
        return Encoding.BigEndianUnicode.GetString(buffer.Slice(firstOffset, firstLen));
      }
      int recStart;
      
      for (int i = 1; i < n; i++)
      {
        recStart = 28 + (12 * (i - 1)) - 1;
        lang = (ICC_MLUC_LANG)BufferReader.ReadInt32BE(ref buffer, ref recStart);
        if (lang == ICC_MLUC_LANG.EN)
        {
          int len = BufferReader.ReadInt32BE(ref buffer, ref recStart);
          int offset = BufferReader.ReadInt32BE(ref buffer, ref recStart);
          return Encoding.BigEndianUnicode.GetString(buffer.Slice(offset, firstLen));
        }
      }

      return Encoding.BigEndianUnicode.GetString(buffer.Slice(firstOffset, firstLen));
    }
    // 2001 spec
    // ASCII only supported
    public string ParseTextDescription(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      int ASCIILen = BufferReader.ReadInt32BE(ref buffer, ref pos);
      string res = Encoding.ASCII.GetString(buffer.Slice(pos, ASCIILen));
      pos += buffer.Length - 8 - 4 - ASCIILen; // this may cause issue if tag contains other ds than desc as well
      // just parse all and based on those numbers move pos i thinks
      return res;
    }

    public ICC_XYZNumber[] ParseXYZType(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      int arrSize = (buffer.Length - pos) / 12;
      ICC_XYZNumber[] arr = new ICC_XYZNumber[arrSize];
      int i = 0;
      while (i < arrSize)
      {
        arr[i++] = ParseXYZNumber(ref buffer, ref pos);
      }

      return arr;
    }

    public ICC_XYZNumber ParseXYZNumber(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ICC_XYZNumber num = new ICC_XYZNumber();
      num.X = ParseS15Fixed16Number(ref buffer, ref pos);
      num.Y = ParseS15Fixed16Number(ref buffer, ref pos);
      num.Z = ParseS15Fixed16Number(ref buffer, ref pos);
      return num;
    }
    // we should be able to just use float, but we mostly use double in this project so we should just use it here as well
    // fix later if needed
    public double ParseS15Fixed16Number(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      short num = BufferReader.ReadInt16BE(ref buffer, ref pos);
      ushort dec = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      double res = num;
      res += dec / 65536d;
      return res;
    }

    public double ParseU16Fixed16number(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ushort num = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      ushort dec = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      double res = num;
      res += dec / 65536d;
      return res;
    }

    public double ParseU8Fixed8Number(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ushort num = buffer[pos++];
      ushort dec = buffer[pos++];
      double res = num;
      res += dec / 256;
      return res;
    }

    public ICC_ViewingConditionsType ParseViewingConditionsType(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ICC_ViewingConditionsType vc = new ICC_ViewingConditionsType();
      vc.Illuminant = ParseXYZNumber(ref buffer, ref pos);
      vc.Surround = ParseXYZNumber(ref buffer, ref pos);
      vc.IlluminantType = (ICC_STANDARD_ILLUMINANT)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined(vc.IlluminantType))
        throw new InvalidDataException("Unknown Illuminant Type");

      return vc;
    }

    public ICC_MeasurementType ParseMeasurementType(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      ICC_MeasurementType m = new ICC_MeasurementType();

      m.Observer = (ICC_STANDARD_OBSERVER)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined(m.Observer))
        throw new InvalidDataException("Unknown Standard Observer!");

      m.Backing = ParseXYZNumber(ref buffer, ref pos);

      m.Geometry = (ICC_MEASUREMENT_GEOMETRY)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined(m.Geometry))
        throw new InvalidDataException("Unknown Measurement Geometry!");

      double flare = ParseU16Fixed16number(ref buffer, ref pos);
      if (flare < 0 || flare > 1)
        throw new InvalidDataException("Invalid Measurement Flare!");
      m.Flare = flare;

      m.Illuminant = (ICC_STANDARD_ILLUMINANT)BufferReader.ReadUInt32BE(ref buffer, ref pos);
      if (!Enum.IsDefined(m.Illuminant))
        throw new InvalidDataException("Unknown Illuminant!");

      return m;
    }

    // NOTE: see if this can be better
    public TEnum ParseSignature<TEnum>(ref ReadOnlySpan<byte> buffer, ref int pos) where TEnum : Enum
    {
      TEnum t = (TEnum)Enum.ToObject(typeof(TEnum), BufferReader.ReadUInt32BE(ref buffer, ref pos));
      if (!Enum.IsDefined(typeof(TEnum), t))
        throw new InvalidDataException("Cant match data to signature!");
      return t;
    }

    // we will return double with curve because in some cases it can be u8fixed8number..........
    public List<double> ParseCurve(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      List<double> list = new List<double>();
      int n = BufferReader.ReadInt32BE(ref buffer, ref pos);
      if (n == 0)
      {
        return list;
      }
      else if (n == 1)
      {
        list.Add(ParseU8Fixed8Number(ref buffer, ref pos));
      }
      else
      {
        for (int i = 0; i < n; i++)
        {
          list.Add(BufferReader.ReadUInt16BE(ref buffer, ref pos));
        }
      }
      return list;
    }

    public List<double> ParseParametricCurve(ref ReadOnlySpan<byte> buffer, ref int pos)
    {
      List<double> list = new List<double>();
      ushort functionType = BufferReader.ReadUInt16BE(ref buffer, ref pos);
      pos += 2;
      int n = functionType switch
      {
        0 => 1,
        1 => 3,
        2 => 4,
        3 => 5,
        4 => 7
      };

      for (int i = 0; i < n; i++)
        list.Add(ParseS15Fixed16Number(ref buffer, ref pos));
      return list;
    }

    
    #endregion StructureParsing
  }
}
