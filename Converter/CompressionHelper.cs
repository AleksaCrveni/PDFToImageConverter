namespace Converter
{
  public static class CompressionHelper
  {
    public static bool IsZlib(byte[] bytes)
    {
      if (bytes[0] == 72)
      {
        int i = 0;
      }
      byte CMF = bytes[0];
      byte FLG = bytes[1];
      byte CM = (byte)(CMF & 15);
      byte CINFO = (byte)(CMF >> 4 & 15);

      // Fixed
      if ((CM == 8 && CINFO <= 7 && CINFO > 0) &&
        ((ushort)(CMF * 256) + FLG) % 31 == 0)
          return true;
      
      return false;
    }

    public static bool IsGzip(byte[] bytes)
    {
      return bytes[0] == 31 && bytes[1] == 139;
    }

  }
}
