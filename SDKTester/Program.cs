using ceTe.DynamicPDF.Rasterizer;
using Converter;
using MuPDFCore;
// See https://aka.ms/new-console-template for more information

PdfRasterizer rasterizer = new PdfRasterizer(@"W:\PDFToImageConverter\Files\greek.pdf");

// Call the Draw method with output image name, image format and the DPI
rasterizer.Draw("EachPage.tiff", ImageFormat.TiffWithLzw, ImageSize.Dpi72);


//Initialise the MuPDF context. This is needed to open or create documents.
using MuPDFContext ctx = new MuPDFContext();

//Open a PDF document
using MuPDFDocument document = new MuPDFDocument(ctx, Files.BaseDocFilePath);

//Page index (page 0 is the first page of the document)
int pageIndex = 0;

//Zoom level, converting document units into pixels. For a PDF document, a 1x zoom level corresponds to a
//72dpi resolution.
double zoomLevel = 1;

//Save the first page as a PNG image with transparency, at a 1x zoom level (1pt = 1px).
document.SaveImage(pageIndex, zoomLevel, PixelFormats.RGBA, @"W:\PDFToImageConverter\Files\mupdf.png", RasterOutputFileTypes.PNG);
