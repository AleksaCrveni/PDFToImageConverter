namespace Converter.FileStructures.Huffman
{
  public class HUFF_HuffmanNode
  {
    public char Char;
    public uint Freq;
    public HUFF_HuffmanNode? Left;
    public HUFF_HuffmanNode? Right;
  }

  public class HUFF_HuffmanNodeByte
  {
    public byte Val;
    public uint Freq;
    public HUFF_HuffmanNodeByte? Left;
    public HUFF_HuffmanNodeByte? Right;
  }
}
