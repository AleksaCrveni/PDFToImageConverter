using Converter.FileStructures.Huffman;

namespace Converter.Utils
{
  /// <summary>
  /// this is slighly altered code from my other project
  /// https://github.com/AleksaCrveni/Compression-Codings
  /// </summary>
  public static class HuffmanUtils
  {
    public static HUFF_HuffmanNodeByte BuildTreeByteVer(List<(byte val, byte freq)> freqMap)
    {
      PriorityQueue<HUFF_HuffmanNodeByte, uint> pq = new PriorityQueue<HUFF_HuffmanNodeByte, uint>();
      foreach ((byte val, byte freq) data in freqMap)
      {
        HUFF_HuffmanNodeByte node = new HUFF_HuffmanNodeByte();
        node.Val = data.val;
        node.Freq = data.freq;
        pq.Enqueue(node, data.freq);
      }

      // building up tree from bottom
      while (pq.Count > 1)
      {
        HUFF_HuffmanNodeByte right = pq.Dequeue();
        HUFF_HuffmanNodeByte left = pq.Dequeue();

        HUFF_HuffmanNodeByte parentNode = new HUFF_HuffmanNodeByte();
        parentNode.Freq = right.Freq + left.Freq;
        parentNode.Right = right;
        parentNode.Left = left;
        parentNode.Val = 0;
        pq.Enqueue(parentNode, parentNode.Freq);
      }

      return pq.Dequeue();
    }
  }
}
