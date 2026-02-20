using Converter.FileStructures.General;
using System.IO.Compression;

namespace Converter.Utils
{
  public static class DecompressionHelper
  {
    public static byte[] DecodeFilter(ref ReadOnlySpan<byte> inputBuffer, ENCODING_FILTER filter)
    {
      // first just do single filter
      if (filter == ENCODING_FILTER.Null)
        return new byte[1];

      byte[] decoded;
      switch (filter)
      {
        case ENCODING_FILTER.Null:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.ASCIIHexDecode:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.ASCII85Decode:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.LZWDecode:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.FlateDecode:
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
        case ENCODING_FILTER.RunLengthDecode:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.CCITTFaxDecode:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.JBIG2Decode:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.DCTDecode:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.JPXDecode:
          decoded = Array.Empty<byte>();
          break;
        case ENCODING_FILTER.Crypt:
          decoded = Array.Empty<byte>();
          break;
        default:
          decoded = new byte[1];
          break;
      }

      return decoded;
    }

    // TODO: Expand this to work with multiple filters
    public static byte[] DecodeFilters(ref ReadOnlySpan<byte> inputBuffer, List<ENCODING_FILTER> filters)
    {
      // first just do single filter
      ENCODING_FILTER f = filters[0];
      return DecodeFilter(ref inputBuffer, f);
    }

    public static ZLibStream GetZLibStreamDecompress(byte[] input)
    {
      var compressStream = new MemoryStream(input);
      return new ZLibStream(compressStream, CompressionMode.Decompress);
    }
  }
}
