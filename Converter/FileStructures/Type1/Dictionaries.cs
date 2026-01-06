using Converter.FileStructures.PDF;

namespace Converter.FileStructures.Type1
{
  public class TYPE1_FontDict
  {
    public TYPE1_FontInfo FontInfo;
    // enum
    public object FontName;
    public string[] Encoding;
    // public int PaintType; Not used in PDF
    public int FontType;
    public double[,] FontMatrix;
    public PDF_Rect FontBBox;
    public int UniqueID;
    public Dictionary<object, object> Metrics;
    public double StrokeWidth;
    public TYPE1_Private Private;
    public Dictionary<object, TYPE1_CharString> CharStrings;
    public string FontID;
  }

  public class TYPE1_Private
  {
    public string RDProc;
    public string NDProc;
    public string NPProc;
    public List<byte[]> Subrs;
    public object[] OtherSubrs;
    public int UniqueID;
    public double[] BlueValues;
    public double[] OtherBlues;
    public double[] FamilyBlues;
    public object[] FamilyOtherBlues;
    public double BlueScale;
    public double BlueShift;
    public double BlueFuzz;
    public double[] StdHW;
    public double[] StdVW;
    public double[] StemSnapH;
    public double[] StemSnapV;
    public bool ForceBold;
    public int LanguageGroup;
    public string Password;
    public ushort LenIV = 4;
    [Obsolete("Docs say that its obsolete and that values are always 16 16")]
    public (int a, int b) MinFeature = (16, 16);
    public bool RndStemUp;
  }

  public class TYPE1_FontInfo
  {
    public string Version;
    public string Notice;
    public string FullName;
    public string FamilyName;
    public string Weight;
    public double ItalicAngle;
    public bool IsFixedPitch;
    public double UnderlinePosition;
    public double UnderlineThickness;
  }

  public class TYPE1_CharString
  {

  }
}
