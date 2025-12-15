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
    public object RDProc;
    public object NDProc;
    public object NPProc;
    public object[] Subrs;
    public object[] OtherSubrs;
    public int UniqueID;
    public object[] BlueValues;
    public object[] OtherBlues;
    public object[] FamilyBlues;
    public object[] FamilyOtherBlues;
    public double BlueScale;
    public int BlueShift;
    public int BlueFuzz;
    public object[] StdHW;
    public object[] StdVW;
    public object[] StemSnapH;
    public object[] StemSnapV;
    public bool ForceBold;
    public int LanguageGroup;
    public int Password;
    public int LenIV;
    public object[] MinFeature;
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
