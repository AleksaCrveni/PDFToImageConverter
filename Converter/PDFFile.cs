using Converter.Parsers;
namespace Converter
{
  public class PDFFile
  {
    public PDFVersion PdfVersion { get; set; } = PDFVersion.INVALID;
    public ulong LastCrossReferenceOffset { get; set; }
    public Trailer Trailer { get; set; }
    public List<CRefEntry> CrossReferenceEntries { get; set; }
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

  // Spec reference on page 51
  // Table 15
  public struct Trailer
  {

    public uint Size;
    public uint Prev;
    public (uint, uint) RootIR;
    // not sure what it is, fix later
    public (uint, uint) EncryptIR;
    public (uint, uint) InfoIR;
    public string[] ID;
    // Only in hybrid-reference file
    // The byte offset in the decoded stream from the bgegging of the file of a cross reference stream
    public int XrefStm;
  }

  // Cross reference entry
  // This should maybe be ref struct
  public struct CRefEntry
  {
    public UInt64 TenDigitValue;
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
}
