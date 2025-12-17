namespace Converter.Utils
{
  public static class DecryptionHelper
  {
    public static byte[] DecryptAdobeType1Encryption(ReadOnlySpan<byte> encrypted)
    {
      ushort r  = 55665;
      ushort c1 = 52845;
      ushort c2 = 22719;
      byte[] decrypted = new byte[encrypted.Length];
      for (int i = 0; i < encrypted.Length; i++)
      {
        decrypted[i] = (byte)(encrypted[i] ^ (r >> 8));
        // eh , I really need to check if comp will expand to int automatically, but i need to make sure they wrap, so do this for now..
        r = (ushort)((ushort)((ushort)(encrypted[i] + r) * c1) + c2);
      };
      return decrypted;
    }
  }
}
