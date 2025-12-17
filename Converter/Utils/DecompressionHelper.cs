using Converter.FileStructures.PDF;
using System.IO.Compression;

namespace Converter.Utils
{
  public static class DecompressionHelper
  {
    public static byte[] DecodeFilter(ref ReadOnlySpan<byte> inputBuffer, List<PDF_Filter> filters)
    {
      // first just do single filter
      PDF_Filter f = filters[0];
      if (f == PDF_Filter.Null)
        return new byte[1];

      byte[] decoded;
      switch (f)
      {
        case PDF_Filter.Null:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.ASCIIHexDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.ASCII85Decode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.LZWDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.FlateDecode:
          // figure out if its gzip, base deflate or zlib decompression
          Stream decompressor;

          var arr = inputBuffer.ToArray();
          var compressStream = new MemoryStream(arr);
          byte b0 = inputBuffer[0];
          byte b1 = inputBuffer[1];
          // account for big/lttiel end
          // not sure if in deflate stream this can be first byte
          if (CompressionHelper.IsZlib(arr))
          {
            // ZLIB check (MSB is left)
            // CM 0-3 bits need to be 8
            // CMINFO 4-7 bits need to be 7
            decompressor = new ZLibStream(compressStream, CompressionMode.Decompress);
          }
          else if (CompressionHelper.IsGzip(arr))
          {
            // GZIP check (MSB is right)
            decompressor = new GZipStream(compressStream, CompressionMode.Decompress);
          }
          else
            decompressor = new DeflateStream(compressStream, CompressionMode.Decompress);

          MemoryStream stream = new MemoryStream();

          decompressor.CopyTo(stream);


          // dispose streams
          compressStream.Dispose();
          decompressor.Dispose();
          // write custom stream because this will copy, so we copy from decompressor
          // and then we have to copy again, i would preferably have one copy
          decoded = stream.ToArray();
          stream.Dispose();
          break;
        case PDF_Filter.RunLengthDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.CCITTFaxDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.JBIG2Decode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.DCTDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.JPXDecode:
          decoded = Array.Empty<byte>();
          break;
        case PDF_Filter.Crypt:
          decoded = Array.Empty<byte>();
          break;
        default:
          decoded = new byte[1];
          break;
      }

      return decoded;
    }
  }
}
