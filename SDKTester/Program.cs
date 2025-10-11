using ceTe.DynamicPDF.Rasterizer;
using Converter;
// See https://aka.ms/new-console-template for more information

PdfRasterizer rasterizer = new PdfRasterizer(Files.Sample);

// Call the Draw method with output image name, image format and the DPI
rasterizer.Draw("EachPage.tiff", ImageFormat.TiffWithLzw, ImageSize.Dpi72);
