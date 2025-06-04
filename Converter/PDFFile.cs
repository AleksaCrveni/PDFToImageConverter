using Converter.Parsers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;
using System.Text;
namespace Converter
{
  public class PDFFile
  {
    public PDFVersion PdfVersion { get; set; } = PDFVersion.INVALID;
    public long LastCrossReferenceOffset { get; set; }
    public Trailer Trailer { get; set; }
    public List<CRefEntry> CrossReferenceEntries { get; set; }
    public Catalog Catalog { get; set; }
  }

  public enum PDFVersion
  {
    INVALID,
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
    public PDFVersion Version = PDFVersion.INVALID;
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
    public object[] OutputIntents;
    public Dictionary<object, object> PieceInfo;
    public Dictionary<object, object> OCProperties;
    public Dictionary<object, object> Perms;
    public Dictionary<object, object> Legal;
    public object[] Requirements;
    public Dictionary<object, object> Collection;
    public bool NeedsRendering = false;
  }
}
