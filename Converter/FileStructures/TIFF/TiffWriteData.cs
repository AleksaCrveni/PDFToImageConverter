namespace Converter.FileStructures.TIFF
{
  public struct BilevelData
  {
    public int StripOffsetsPointer;
    public int StripByteCounterOffsets;
    public int ImageDataOffset;
    public int StripCount;
    public int RowsPerStrip;
  }
  public struct GrayscaleData
  {
    public int StripOffsetsPointer;
    public int StripByteCounterOffsets;
    public int ImageDataOffset;
    public int StripCount;
    public int RowsPerStrip;
    public int BitsPerSample;
  }
}
