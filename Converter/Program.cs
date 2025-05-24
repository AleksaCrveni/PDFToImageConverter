using Converter.Parsers;
using System.Text;

string filesPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Files");

string fnBaseDoc = "BaseDoc.pdf";
string fn2_0_2020 = "PDF2.0.2020.pdf";
string fnReport = "Report.pdf";

string pathBaseDoc = Path.Combine(filesPath, fnBaseDoc);
string path2_0_2020 = Path.Combine(filesPath, fn2_0_2020);
string pathReport = Path.Combine(filesPath, fnReport);




var pdfParser = new PdfParser();
//FileStream fs =  File.OpenRead(fnBaseDoc);

pdfParser.SaveStingRepresentationToDisk(pathBaseDoc);
pdfParser.SaveStingRepresentationToDisk(path2_0_2020);
pdfParser.SaveStingRepresentationToDisk(pathReport);
//ulong baseDocOffset = pdfParser.Parse(pathBaseDoc);
//ulong logoOffset = pdfParser.Parse(path2_0_2020);
//ulong reportOffset = pdfParser.Parse(pathReport);


//Console.WriteLine($"BaseDoc offset: {baseDocOffset}\nLogoOffset: {logoOffset}\nReport offset: {reportOffset}");
Console.ReadKey();
int y = 5;