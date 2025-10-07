namespace Converter.FileStructures.TIFF
{
  public struct TIFF_BilevelData
  {
    public int StripOffsetsPointer;
    public int StripByteCounterOffsets;
    public int ImageDataOffset;
    public int StripCount;
    public int RowsPerStrip;
  }
  public struct TIFF_GrayscaleData
  {
    public int StripOffsetsPointer;
    public int StripByteCounterOffsets;
    public int ImageDataOffset;
    public int StripCount;
    public int RowsPerStrip;
    public int BitsPerSample;
  }
}
