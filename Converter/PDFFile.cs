using Converter.Parsers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;
using System.Text;
namespace Converter
{
  public class PDFFile
  {
    public PDFVersion PdfVersion { get; set; } = PDFVersion.Null;
    public long LastCrossReferenceOffset { get; set; }
    public Trailer Trailer { get; set; }
    public List<CRefEntry> CrossReferenceEntries { get; set; }
    public Catalog Catalog { get; set; }
    // 0 will be root
    public List<PageTree> PageTrees { get; set; }
    public List<PageInfo> PageInformation { get; set; }
    public Stream Stream { get; set; }
  }

  public enum PDFVersion
  {
    Null = 0,
    V1_0,
    V1_1,
    V1_2,
    V1_3,
    V1_4,
    V1_5,
    V1_6,
    V1_7,
    V1_7_2008,
    V2_0,
    V2_0_2020
  }

  public enum PageLayout
  {
    SinglePage,
    OneColumn,
    TwoColumnLeft,
    TwoColumnRight,
    TwoPageLeft,
    TwoPageRight,
  }

  public enum PageMode
  {
    UserNone,
    UseOutlines,
    FullScreen,
    UseOC,
    UseAttachments
  }

  public enum Tabs
  {
    Null = 0,
    R,
    C,
    S
  }

  // Spec reference on page 51
  // Table 15
  public struct Trailer
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
  public struct CRefEntry
  {
    public Int64 TenDigitValue;
    public UInt16 GenerationNumber;
    public byte EntryType;

    public static bool operator ==(CRefEntry a, CRefEntry b)
    {
      if (a.TenDigitValue != b.TenDigitValue)
        return false;
      if (a.GenerationNumber != b.GenerationNumber)
        return false;
      if (a.EntryType != b.EntryType)
        return false;

      return true;
    }
    public static bool operator !=(CRefEntry a, CRefEntry b)
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
  public struct Catalog
  {
    public Catalog() { }
    public PDFVersion Version = PDFVersion.Null;
    public Dictionary<object, object> Extensions;
    public (int, int) PagesIR;
    public object PageLabels;
    public Dictionary<object, object> Names;
    public (int, int) DestsIR;
    public Dictionary<object, object> ViewerPreferences;
    // this actually name type
    public PageLayout PageLayout = PageLayout.SinglePage;
    // this actually name type
    public PageMode PageMode = PageMode.UserNone;
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
  public struct PageTree
  {
    public List<(int, int)> KidsIRs;
    public (int, int) ParentIR;
    public int Count;
    public Rect MediaBox;
  }
  // Table 30
  // Resources - 
  public struct PageInfo
  {
    public PageInfo() { }
    public (int, int) ParentIR;
    public DateTime LastModified;
    public (int, int) ResourcesIR; // use generic dict but later implement it right Table 33
    public Rect MediaBox; // 7.9.5
    public Rect CropBox; // defualt value is media box also check 14.11.2
    public Rect BleedBox;
    public Rect TrimBox;
    public Rect ArtBox;
    public Dictionary<object, object> BoxColorInfo;
    public (int, int) ContentsIR; // I dontknow if this can be array of IR, docs aren't clear, search more samples
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
    public Tabs Tabs;
    public object TemplateInstantiated; // not sure about this one 
    public Dictionary<object, object> PresSteps;
    public double UserUnit = 1.0; // multiplies of 1/72 inch  
    public Dictionary<object, object> VP;
    // This is actual data from ResourcesIR dictionary
    public ResourceDict ResourceDict;
    public ContentDict ContentDict;
  }
  
  // Table 33
  public struct ResourceDict()
  {
    public Dictionary<object, object> ExtGState;
    public Dictionary<object, object> ColorSpace;
    public Dictionary<object, object> Pattern;
    public Dictionary<object, object> Shading;
    public Dictionary<object, object> XObject;
    public Dictionary<object, object> Font;
    public List<string> ProcSet;
    public Dictionary<object, object> Properties;
  }

  // 7.4.1 & Table 5
  // TODO: Expand this
  public struct ContentDict
  {
    // for some reason some pdf files have this as IR instead of direct value
    // so just in case i will support both for length
    public long Length;
    public List<Filter> Filters;
    public string DecodedStreamData;
  }

  public enum Filter
  {
    Null = 0,
    ASCIIHexDecode,
    ASCII85Decode,
    LZWDecode,
    FlateDecode,
    RunLengthDecode,
    CCITTFaxDecode,
    JBIG2Decode,
    DCTDecode,
    JPXDecode,
    Crypt
  }


  // 7.9.5
  // TODO: check if we can use 16 bit ints
  public struct Rect()
  {
    public void FillRect(double a, double b, double c, double d)
    {
      llX = (double)a;
      llY = (double)b;
      urX = (double)c;
      urY = (double)d;
    }

    public static bool operator == (Rect a, Rect b)
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
    public static bool operator != (Rect a, Rect b)
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

}
