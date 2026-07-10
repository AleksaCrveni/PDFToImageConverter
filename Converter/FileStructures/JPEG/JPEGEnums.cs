namespace Converter.FileStructures.JPEG
{
  public enum JPEG_MARKERS : ushort
  {
    NULL = 0,
    SOF0  = 0xFFC0, // Start of Frame 0 - Baseline DCT
    SOF1  = 0xFFC1, // Start of Frame 1 - Extended Sequential DCT
    SOF2  = 0xFFC2, // Start of Frame 2 - Progressive DCT
    SOF3  = 0xFFC3, // Start of Frame 3 - Lossless (sequential)
    DHT   = 0xFFC4, // Define Huffman Table
    SOF5  = 0xFFC5, // Start of Frame 5 - Differential sequential DCT
    SOF6  = 0xFFC6, // Start of Frame 6 - Differential progressive DCT
    SOF7  = 0xFFC7, // Start of Frame 7 - Differential Lossless (sequential)
    JPG   = 0xFFC8, // JPEG Extensions
    SOF9  = 0xFFC9, // Start of Frame 9 - Extended sequential DCT, Arithmetic coding
    SOF10 = 0xFFCA, // Start of Frame 10 - Progressive DCT, Arithmetic coding
    SOF11 = 0xFFCB, // Start of Frame 11 - Lossless (sequential), Arithmetic coding
    DAC   = 0xFFCC, // Define Arithmetic coding
    SOF13 = 0xFFCD, // Start of Frame 13 - Differential sequential DCT, Arithmetic coding
    SOF14 = 0xFFCE, // Start of Frame 14 - Differential progressive DCT, Arithmetic coding
    SOF15 = 0xFFCF, // Start of Frame 15 - Differential lossless (sequential), Arithmetic coding
    RST0  = 0xFFD0, // Restart Marker 0
    RST1  = 0xFFD1, // Restart Marker 1
    RST2  = 0xFFD2, // Restart Marker 2
    RST3  = 0xFFD3, // Restart Marker 3
    RST4  = 0xFFD4, // Restart Marker 4
    RST5  = 0xFFD5, // Restart Marker 5
    RST6  = 0xFFD6, // Restart Marker 6
    RST7  = 0xFFD7, // Restart Marker 7
    SOI   = 0xFFD8, // Start of Image
    EOI   = 0xFFD9, // End of Image
    SOS   = 0xFFDA, // Start of Scan,
    DQT   = 0xFFDB, // Define Quantization Table
    DNL   = 0xFFDC, // Define  Number of Lines
    DRI   = 0xFFDD, // Define Restart Interval
    DHP   = 0xFFDE, //  Define Hierarchical Progression
    EXP   = 0xFFDF, // Expand Reference Component
    APP0  = 0xFFE0, // Application Segment 0 - JFIF JEPG IMage & AVI1 - Motion JPEG (MJPG)
    APP1  = 0xFFE1, // Application Segment 1 - EXIF Metadatta, TIFF IFD Format, JPEG Thumbnail (160x120), ADOBE XMP
    APP2  = 0xFFE2, // Application Segment 2 - ICC Color profile, FlashPix
    APP3  = 0xFFE3, // Application Segment 3 - JPS Tag for Steroscopic JPEG Images
    APP4  = 0xFFE4, // Application Segment 4
    APP5  = 0xFFE5, // Application Segment 5
    APP6  = 0xFFE6, // Application Segment 6
    APP7  = 0xFFE7, // Application Segment 7
    APP8  = 0xFFE8, // Application Segment 8
    APP9  = 0xFFE9, // Application Segment 9
    APP10 = 0xFFEA, // Application Segment 10
    APP11 = 0xFFEB, // Application Segment 11
    APP12 = 0xFFEC, // Application Segment 12
    APP13 = 0xFFED, // Application Segment 13
    APP14 = 0xFFEE, // Application Segment 14
    APP15 = 0xFFEF, // Application Segment 15
    JPG0  = 0xFFF0, // JPEG Extension 0
    JPG1  = 0xFFF1, // JPEG Extension 1
    JPG2  = 0xFFF2, // JPEG Extension 2
    JPG3  = 0xFFF3, // JPEG Extension 3
    JPG4  = 0xFFF4, // JPEG Extension 4
    JPG5  = 0xFFF5, // JPEG Extension 5
    JPG6  = 0xFFF6, // JPEG Extension 6
    JPG7  = 0xFFF7, // Lossless JPEG (SOF48)
    JPG8  = 0xFFF8, // Lossless JPEG EXtension Parameters (LSE)
    JPG9  = 0xFFF9, // JPEG Extension 9,
    JPG10 = 0xFFFA, // JPEG Extension 10,
    JPG11 = 0xFFFB, // JPEG Extension 11,
    JPG12 = 0xFFFC, // JPEG Extension 12,
    JPG13 = 0xFFFD, // JPEG Extension 13,
    COM   = 0xFFFE, // Comment
  }

  public enum JPEG_UNIT_TYPE
  {
    ASPECT_RATIO,
    DOTS_PER_INCH,
    DOTS_PER_CM
  }

  
  public enum JPEG_HUFFMAN_TABLE_TYPE : byte
  {
    AC,
    DC
  }

}
