using Converter;
using Converter.Parsers;

var pdfParser = new PdfParser();
//FileStream fs =  File.OpenRead(fnBaseDoc);

pdfParser.SaveStingRepresentationToDisk(Files.BaseDocFilePath);
pdfParser.SaveStingRepresentationToDisk(Files.F2_0_2020);
pdfParser.SaveStingRepresentationToDisk(Files.Report);
//ulong baseDocOffset = pdfParser.Parse(pathBaseDoc);
//ulong logoOffset = pdfParser.Parse(path2_0_2020);
//ulong reportOffset = pdfParser.Parse(pathReport);


//Console.WriteLine($"BaseDoc offset: {baseDocOffset}\nLogoOffset: {logoOffset}\nReport offset: {reportOffset}");
Console.ReadKey();
int y = 5;