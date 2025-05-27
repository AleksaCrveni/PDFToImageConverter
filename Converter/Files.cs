using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Converter
{
  public static class Files
  {
    public static string RootFolder { get; set; }
    public static string BaseDocFilePath { get; set; }
    public static string F2_0_2020 { get; set; }
    public static string Report { get; set; }
    static Files()
    { 
      RootFolder = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "Files");
      BaseDocFilePath = Path.Combine(RootFolder, "BaseDoc.pdf");
      F2_0_2020 = Path.Combine(RootFolder, "PDF2.0.2020.pdf");
      Report = Path.Combine(RootFolder, "Report.pdf");

    }
  }
}
