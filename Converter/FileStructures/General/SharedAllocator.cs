namespace Converter.FileStructures.General
{
  public class SharedAllocator
  {
    public byte[]? Buffer;
    public Range Range;
    public bool IsSharedArray;

    public SharedAllocator () { }
    public SharedAllocator(byte[] buffer, int offset, int len, bool isSharedArray)
    {
      Buffer = buffer;
      Range = new Range(offset, offset + len);
      IsSharedArray = isSharedArray;
    }
    
  }

}
