using System.Formats.Asn1;

namespace Converter.Utils
{
  public static class DecryptionHelper
  {
    public static readonly ushort EEXEC_CONST_PWD = 55665;
    public static readonly ushort CHARSTRING_CONST_PWD = 4330;
    public static byte[] DecryptAdobeType1CharString(ReadOnlySpan<byte> encrypted)
    {
      return DecryptAdobeType1Encryption(encrypted, CHARSTRING_CONST_PWD, 4);
    }
    public static byte[] DecryptAdobeType1CharString(ReadOnlySpan<byte> encrypted, ushort lenIV)
    {
      return DecryptAdobeType1Encryption(encrypted, CHARSTRING_CONST_PWD, lenIV);
    }
    public static byte[] DecryptAdobeType1EEXEC(ReadOnlySpan<byte> encrypted)
    {
      return DecryptAdobeType1Encryption(encrypted, EEXEC_CONST_PWD, 4);
    }
    // this is 1 to 1 decryption/encryotion just need to ac count for lenIV
    // This can be calculated using Lenght2 and 1 so we dont have to read each character but can slice directly in the parent
    public static byte[] DecryptAdobeType1Encryption(ReadOnlySpan<byte> encrypted, ushort pwd, ushort lenIV)
    {
      if (encrypted.Length - lenIV < 0)
        lenIV = 0;

      ushort r  = pwd;
      ushort c1 = 52845;
      ushort c2 = 22719;
      byte[] decrypted = new byte[encrypted.Length - lenIV];
      for (int i = 0; i < encrypted.Length; i++)
      {
        // In case where lenIV is not 0 we have stil have to calculate r but we won't make it part of decrypted bytes because they are random fillers
        // Instead of having if here we can just split it in 2 loops but w/e
        if (i >= lenIV)
          decrypted[i - lenIV] = (byte)(encrypted[i] ^ (r >> 8));

        // eh , I really need to check if comp will expand to int automatically, but i need to make sure they wrap, so do this for now..
        r = (ushort)((ushort)((ushort)(encrypted[i] + r) * c1) + c2);
      }

      return decrypted;
    }
  }
}
