namespace Converter.FileStructures.ICC
{
  public class ICCFile
  {
    
  }

  public class ICCProfile
  {
    public ICCHeader Header;
    public List<ICC_TagDef> TagDefinitions;
    public ICC_Data Data;
  }

  public class ICCHeader
  {
    public uint ProfileSize;
    public uint CMMType; // we wont check this
    public byte MajorVersion;
    public byte MinorVersion;
    public ICC_PROFILE_CLASS ProfileClass;
    public ICC_DATA_COLORSPACE DataColorSpace;
    public ICC_DATA_COLORSPACE ProfileConnectionSpace;
    public DateTime CreatedAt;
    public uint FileSignature; // should always be “acsp” 0x61637379
    public ICC_PRIMARY_PLATFORM Platform;
    public bool Embedded; // 0 not Embedded and 1 if embedded in file
    public bool Independent;
    public uint DeviceManufacturer; // we wont check this
    public uint DeviceModelField; // we wont check this
    public uint DeviceAttributeFields; // has rules how to be checked is actually bitflag of ICC_ATTRIBUTE
    // The profile connection space illuminant field shall contain the CIEXYZ values of the illuminant used for the profile connection space encoded as an XYZNumber. At present the only illuminant permitted for the profile connection space is D50 (where X= 0,9642; Y = 1,0 and z=0,8249). See Annex A for further details.
    // Does this mean this is always the same??
    public ICC_RENDERING_INTENT RenderingIntent;
    public ICC_XYZNumber ConnectionSpaceIlluminant;
    public uint ProfileCreator; // we wont check this
    public UInt128 ProfileID; // this is checksum?
    // rest 28 bytes are reserved for now
  }

  public class ICC_XYZNumber
  {
    public double X;
    public double Y;
    public double Z;
    
    public ICC_XYZNumber() { }
    public ICC_XYZNumber(double x, double y, double z)
    {
      X = x;
      Y = y;
      Z = z;
    }
  }

  public class ICC_TagDef
  {
    public ICC_TAG_TYPE Type;
    public int Offset; // we are just going to limit this to int
    public int Size; // we are just going to limit this to int
  }

  public class ICC_Data
  {
    public string Copyright;
    public string Description;
    public ICC_XYZNumber[] WhitePoint;
    public ICC_XYZNumber[] BlackPoint;
    public ICC_XYZNumber[] R_XYZ;
    public ICC_XYZNumber[] G_XYZ;
    public ICC_XYZNumber[] B_XYZ;
    public string DeviceMFGDesc;
    public string DeviceModelDesc;
    public string ViewingCondDesc;
    public ICC_ViewingConditionsType ViewingConditions;
    public ICC_XYZNumber[] Luminance;
    public ICC_MeasurementType Measurement;
    public ICC_TECHNOLOGY_SIGNATURE Technology;
  }

  public class ICC_ViewingConditionsType()
  {
    public ICC_XYZNumber Illuminant;
    public ICC_XYZNumber Surround;
    public ICC_STANDARD_ILLUMINANT IlluminantType;
  }

  public class ICC_MeasurementType()
  {
    public ICC_STANDARD_OBSERVER Observer;
    public ICC_XYZNumber Backing;
    public ICC_MEASUREMENT_GEOMETRY Geometry;
    public double Flare;
    public ICC_STANDARD_ILLUMINANT Illuminant;
  }

}
