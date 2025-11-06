using Converter.FileStructures.General;
using Converter.Rasterizers;

namespace Converter.FileStructures.PDF
{
  public class PDFFile
  {
    public PDF_Version PdfVersion { get; set; } = PDF_Version.Null;
    public long LastCrossReferenceOffset { get; set; }
    public PDF_Trailer Trailer { get; set; }
    // List of generations where each generation has object
    public List<PDF_CRefEntry> CrossReferenceEntries { get; set; }
    public PDF_Catalog Catalog { get; set; }
    // 0 will be root
    public List<PDF_PageTree> PageTrees { get; set; }
    public List<PDF_PageInfo> PageInformation { get; set; }
    public Stream Stream { get; set; }
    public List<(int key, PDF_ObjectStream data)> ObjectStreams { get; set; }
    public TargetConversion Target { get; set; } = TargetConversion.TIFF_GRAYSCALE;
    public PDF_Options Options;
  }

  public struct PDF_Options
  {
    public PDF_Options() { }
    public bool AllowStack = false;
  }
  // Spec reference on page 51
  // Table 15
  public struct PDF_Trailer
  {

    public int Size;
    public int Prev;
    public (int, int) RootIR;
    // not sure what it is, fix later
    public (int, int) EncryptIR;
    public (int, int) InfoIR;
    public string[] ID;
    // Only in hybrid-reference file
    // The byte offset in the decoded stream from the bgegging of the file of a cross reference stream
    public int XrefStm;
  }

  // Cross reference entry
  // This should maybe be ref struct
  public struct PDF_CRefEntry
  {
    public int Index; // may later be refactored not to use this
    public long TenDigitValue;
    public ushort GenerationNumber;
    public int StreamIR; // used only when its Indirect entry type
    // Index in Object stream
    public int IndexInOS; // used onlmy when its Indirect entry type
    public PDF_XrefEntryType EntryType;
    // this is cached because it maybe 
    // this shouldn't be cached
    // I should have some object stream list of cached decoded streams as they may contain multiple objcets
    // and this should be index and position into there!
    public byte[] Buffer; // used with indirect entry type

    public static bool operator ==(PDF_CRefEntry a, PDF_CRefEntry b)
    {
      if (a.TenDigitValue != b.TenDigitValue)
        return false;
      if (a.GenerationNumber != b.GenerationNumber)
        return false;
      if (a.EntryType != b.EntryType)
        return false;

      return true;
    }
    public static bool operator !=(PDF_CRefEntry a, PDF_CRefEntry b)
    {
      if (a.TenDigitValue == b.TenDigitValue)
        return false;
      if (a.GenerationNumber == b.GenerationNumber)
        return false;
      if (a.EntryType == b.EntryType)
        return false;

      return true;
    }
  }

  // TODO: Implement Extenions, PageLabels, Names,ViewerPreferences, OpenAction, AA, AcroForm, URI, StructTreeRoot
  // MarkInfo, SpiderInfo, PieceInfo, OCProperties, Perms, Legal, Collection, Requirements, OutputIntent parsing
  // Note: Docs on Page 81 - Table 28
  public struct PDF_Catalog
  {
    public PDF_Catalog() { }
    public PDF_Version Version = PDF_Version.Null;
    public Dictionary<object, object> Extensions;
    public (int, int) PagesIR;
    public object PageLabels;
    public Dictionary<object, object> Names;
    public (int, int) DestsIR;
    public Dictionary<object, object> ViewerPreferences;
    // this actually name type
    public PDF_PageLayout PageLayout = PDF_PageLayout.SinglePage;
    // this actually name type
    public PDF_PageMode PageMode = PDF_PageMode.UserNone;
    public (int, int) OutlinesIR;
    public (int, int) ThreadsIR;
    public object OpenAction;
    public Dictionary<object, object> AA;
    public Dictionary<object, object> URI;
    public Dictionary<object, object> AcroForm;
    public (int, int) MetadataIR;
    public Dictionary<object, object> StructTreeRoot;
    public Dictionary<object, object> MarkInfo;
    public string Lang;
    public Dictionary<object, object> SpiderInfo;
    public List<string> OutputIntents;
    public Dictionary<object, object> PieceInfo;
    public Dictionary<object, object> OCProperties;
    public Dictionary<object, object> Perms;
    public Dictionary<object, object> Legal;
    public List<string> Requirements;
    public Dictionary<object, object> Collection;
    public bool NeedsRendering = false;
  }

  // Table 29
  // TODO: Add page atrributes
  public struct PDF_PageTree
  {
    public List<(int, int)> KidsIRs;
    public (int, int) ParentIR;
    public (int, int) ResourcesIR;
    public int Count;
    public PDF_Rect MediaBox;
  }
  // Table 30
  // Resources - 
  public struct PDF_PageInfo
  {
    public PDF_PageInfo() { }
    public (int, int) ParentIR;
    public DateTime LastModified;
    public (int, int) ResourcesIR; // use generic dict but later implement it right Table 33
    public PDF_Rect MediaBox; // 7.9.5
    public PDF_Rect CropBox; // defualt value is media box also check 14.11.2
    public PDF_Rect BleedBox;
    public PDF_Rect TrimBox;
    public PDF_Rect ArtBox;
    public Dictionary<object, object> BoxColorInfo;
    public (int, int) ContentsIR; // I don't know if this can be array of IR, docs aren't clear, search more samples
    public int Rotate;
    public Dictionary<object, object> Group; // 11.4.7
    public byte[] Thumb;
    public List<(int, int)> B; // use list for now, idk size
    public double Dur;
    public Dictionary<object, object> Trans;
    public List<string> Annots;
    public Dictionary<object, object> AA;
    public byte[] Metadata;
    public Dictionary<object, object> PieceInfo;
    public int StructParents;
    public List<string> ID;
    public int PZ;
    public Dictionary<object, object> SeparationInfo;
    public PDF_Tabs Tabs;
    public object TemplateInstantiated; // not sure about this one 
    public Dictionary<object, object> PresSteps;
    public double UserUnit = 1.0; // multiplies of 1/72 inch  
    public Dictionary<object, object> VP;
    // This is actual data from ResourcesIR dictionary
    public PDF_ResourceDict ResourceDict;
    public PDF_CommonStreamDict ContentDict;
  }

  // Table 33
  public struct PDF_ResourceDict()
  {
    public Dictionary<object, object> ExtGState;
    public List<PDF_ColorSpaceData> ColorSpace;
    public Dictionary<object, object> Pattern;
    public Dictionary<object, object> Shading;
    public Dictionary<object, object> XObject;
    // key is arbitary so it has to be a string and its used to reference this fonts
    public List<PDF_FontData> Font;
    public List<string> ProcSet;
    public Dictionary<object, object> Properties;
  }

  public struct PDF_ColorSpaceDictionary
  {
    public PDF_CommonStreamDict CommonStreamDict;
    public int N;
    public PDF_ColorSpace Alternate; // Alternate colour space in case one specified in color space is not supported
    public object[] Range;
    public object Metadata;
  }

  public struct PDF_ColorSpaceData
  {
    public string Key;
    public List<PDF_ColorSpaceInfo> ColorSpaceInfo;
  }

  public struct PDF_ColorSpaceInfo
  {
    public PDF_ColorSpace ColorSpaceFamily;
    public PDF_ColorSpaceDictionary Dict;
  }

  public struct PDF_FontData
  {
    public string Key;
    public PDF_FontInfo FontInfo;
    public IRasterizer Rasterizer;
  }

  // Table 111
  public struct PDF_FontInfo()
  {
    public PDF_FontType SubType;
    public string Name;
    // postscript name of the font, can be parsed later if needed
    // this is actually enum??
    public string BaseFont;
    public int FirstChar;
    public int LastChar;
    // can be indirect val
    public int[] Widths;
    // is IR
    public PDF_FontDescriptor FontDescriptor;
    // can be name of dict, but just do enum for now
    public PDF_FontEncodingData EncodingData;
    public byte[] ToUnicode;
  }

  // Table 114
  public class PDF_FontEncodingData
  {
    public PDF_FontEncodingType BaseEncoding;
    public List<(int code, string val)> Differences;

    public PDF_FontEncodingData()
    {
      Differences = new List<(int code, string val)>();
    }
    /// <summary>
    /// Returns empty string if it doesn't exist
    /// TODO: See if binary search would be faster here
    /// </summary>
    public string GetGlyphNameFromDifferences(int codepoint)
    {
      foreach ((int startCode, string val) in Differences)
      {
        if (startCode == codepoint)
          return val;
      };

      return string.Empty;
    }
  }

  // Table 122
  public struct PDF_FontDescriptor
  {
    // should be same as FontInfo.BaseFont
    public string FontName;
    // Byte string
    // Used for type 3 fonts in tagged documnts
    public string FontFamily;
    // Used for type 3 fonts in tagged documnts
    public PDF_FontStretch FontStretch;
    // valid values are 100,200,300,400,500,600,700,800,900
    public int FontWeight;
    public PDF_FontFlags Flags;
    public PDF_Rect FontBBox;
    public int ItalicAngle;
    public int Ascent;
    public int Descent;
    public int Leading;
    public int CapHeight;
    public int XHeight;
    public int StemV;
    public int StemH;
    public int AvgWidth;
    public int MaxWidth;
    public int MissingWidth;
    public PDF_FontFileInfo FontFile;
    public PDF_FontType FontType;
    // byte string
    public string CharSet;
  }

  // Table5  + Table 127
  // Length + Filter  + Table 127
  public struct PDF_FontFileInfo
  {
    public PDF_CommonStreamDict CommonStreamInfo;
    public PDF_FontFileInfo()
    {

    }
    public int Length1;
    public int Length2;
    public int Length3;
    public PDF_FontFileSubtype Subtype;
    public byte[] Metadata;
  }

  // 7.4.1 & Table 5
  // TODO: Expand this
  public struct PDF_CommonStreamDict 
  {
    public PDF_CommonStreamDict()
    {

    }
    // for some reason some pdf files have this as IR instead of direct value
    // so just in case i will support both for length
    public long Length;
    public List<PDF_Filter> Filters = new List<PDF_Filter>() { PDF_Filter.Null };
    public byte[] RawStreamData;
  }

  // 7.9.5
  // TODO: check if we can use 16 bit ints
  public struct PDF_Rect()
  {
    public void FillRect(double a, double b, double c, double d)
    {
      llX = a;
      llY = b;
      urX = c;
      urY = d;
    }

    public static bool operator ==(PDF_Rect a, PDF_Rect b)
    {
      if (a.urX != b.urX)
        return false;
      if (a.urY != b.urY)
        return false;
      if (a.llX != b.llX)
        return false;
      if (a.llY != b.llY)
        return false;

      return true;
    }
    public static bool operator !=(PDF_Rect a, PDF_Rect b)
    {
      if (a.urX == b.urX)
        return false;
      if (a.urY == b.urY)
        return false;
      if (a.llX == b.llX)
        return false;
      if (a.llY == b.llY)
        return false;

      return true;
    }
    // ll -> lower left
    // ur -> upper right
    public double llX;
    public double llY;
    public double urX;
    public double urY;
  }
  public class PDF_ObjectStream
  {
    public int N;
    public int First;
    public (int, int) ExtendsIR;
    public PDF_CommonStreamDict CommonStreamDict;
    public List<(int objId, int offset)> Offsets;
  }
}
