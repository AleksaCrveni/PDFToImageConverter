using Converter.FileStructures.CompositeFonts;
using Converter.FileStructures.PDF;

namespace Converter.StaticData
{
  public class CompositeFontConstants
  {
    // 14 standard Type 1 fonts that do not have font descriptor Pre PDF1.5 and need special processing
    public readonly static string[] Standard14Fonts =
    {
      "Times-Roman",
      "Helvetica",
      "Courier",
      "Symbol",
      "Times-Bold",
      "Helvetica-Bold",
      "Courier-Bold",
      "ZapfDingbats",
      "Times-Italic",
      "HelveticaOblique",
      "Courier-Oblique",
      "Times-BoldItalic",
      "Helvetica-BoldOblique",
      "Courier-BoldOblique"
    };

    // Table 118 & 119
    public readonly static Dictionary<string, (CFCharacterCollections collection, PDF_Version version)[]> PredefinedCMAPs = new Dictionary<string, (CFCharacterCollections collection, PDF_Version version)[]>()
    {
      // Chinese Simplified
      { "GB-EUC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_5) } },
      { "GB-EUC-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_5) } },
      { "GBpc-EUC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_5) } },
      { "GBpc-EUC-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_0, PDF_Version.V1_5) } },
      { "GBK-EUC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_5) } },
      { "GBK-EUC-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_5) } },
      { "GBKp-EUC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_5) } },
      { "GBKp-EUC-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_5) } },
      { "GBK2K-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_5) } },
      { "GBK2K-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_5) } },
      { "UniGB-UCS2-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_5) } },
      { "UniGB-UCS2-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_5) } },
      { "UniGB-UTF16-H", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_5) } },
      { "UniGB-UTF16-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeGB1_4, PDF_Version.V1_5) } },
      // Chinese traditional
      { "B5pc-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_5) } },
      { "B5pc-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_5) } },
      { "HKscs-B5-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_3, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_3, PDF_Version.V1_5) } },
      { "HKscs-B5-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_3, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_3, PDF_Version.V1_5) } },
      { "ETen-B5-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_5) } },
      { "ETen-B5-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_5) } },
      { "ETenms-B5-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_5) } },
      { "ETenms-B5-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_5) } },
      { "CNS-EUC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_5) } },
      { "CNS-EUC-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_5) } },
      { "UniCNS-UCS2-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_3, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_3, PDF_Version.V1_5) } },
      { "UniCNS-UCS2-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeCNS1_3, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeCNS1_3, PDF_Version.V1_5) } },
      { "UniCNS-UTF16-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_4, PDF_Version.V1_5) } },
      { "UniCNS-UTF16-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeCNS1_4, PDF_Version.V1_5) } },
      // Japanese
      { "83pv-RKSJ-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_5) } },
      { "90ms-RKSJ-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_5) } },
      { "90ms-RKSJ-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_5) } },
      { "90msp-RKSJ-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_5) } },
      { "90msp-RKSJ-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_5) } },
      { "90pv-RKSJ-H", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_5) } },
      { "Add-RKSJ-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_5) } },
      { "Add-RKSJ-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_5) } },
      { "EUC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_5) } },
      { "EUC-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_5) } },
      { "Ext-RKSJ-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_5) } },
      { "Ext-RKSJ-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_5) } },
      { "H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_5) } },
      { "V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_1, PDF_Version.V1_5) } },
      { "UniJIS-UCS2-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_4, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_4, PDF_Version.V1_5) } },
      { "UniJIS-UCS2-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_4, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_4, PDF_Version.V1_5) } },
      { "UniJIS-UCS2-HW-H", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_4, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_4, PDF_Version.V1_5) } },
      { "UniJIS-UCS2-HW-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_2, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeJapan1_4, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeJapan1_4, PDF_Version.V1_5) } },
      { "UniJIS-UTF16-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeJapan1_5, PDF_Version.V1_5) } },
      { "UniJIS-UTF16-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeJapan1_5, PDF_Version.V1_5) } },
      // Korean
      { "KSC-EUC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_5) } },
      { "KSC-EUC-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_5) } },
      { "KSCms-UHC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_5) } },
      { "KSCms-UHC-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_5) } },
      { "KSCms-UHC-HW-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_5) } },
      { "KSCms-UHC-HW-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_5) } },
      { "KSCpc-EUC-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_2),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_0, PDF_Version.V1_5) } },
      { "UniKS-UCS2-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_5) } },
      { "UniKS-UCS2-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_3),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_4),
        (CFCharacterCollections.AdobeKorea1_1, PDF_Version.V1_5) } },
      { "UniKS-UTF16-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_2, PDF_Version.V1_5) } },
      { "UniKS-UTF16-V", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_2, PDF_Version.V1_5) } },
      // Generic
      { "Identity-H", new (CFCharacterCollections collection, PDF_Version version) [] {
        (CFCharacterCollections.AdobeKorea1_2, PDF_Version.V1_5) } },
      { "Identity-V", new (CFCharacterCollections collection, PDF_Version version) [] { 
        (CFCharacterCollections.AdobeKorea1_2, PDF_Version.V1_5) } }
    };
    
  }
}
