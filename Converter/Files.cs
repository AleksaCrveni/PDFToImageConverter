namespace Converter
{
  public static class Files
  {
    public static string RootFolder { get; set; }
    public static string BaseDocFilePath { get; set; }
    public static string F2_0_2020 { get; set; }
    public static string Report { get; set; }
    public static string SmallTest { get; set; }
    public static string Sample { get; set; }
    public static string HelloTiff { get; set; }
    public static string BilevelTiff { get; set; }
    public static string CreateTestTiff { get; set; }
    static Files()
    { 
      RootFolder = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "Files");
      BaseDocFilePath = Path.Combine(RootFolder, "BaseDoc.pdf");
      F2_0_2020 = Path.Combine(RootFolder, "PDF2.0.2020.pdf");
      Report = Path.Combine(RootFolder, "Report.pdf");
      SmallTest = Path.Combine(RootFolder, "SmallTest.txt");
      Sample = Path.Combine(RootFolder, "sample.pdf");
      HelloTiff = Path.Combine(RootFolder, "HelloTiff.tif");
      BilevelTiff = Path.Combine(RootFolder, "BilevelTiff.tif");
      CreateTestTiff = Path.Combine(RootFolder, "testCreateTIFF.tif");
    }
  }
}
