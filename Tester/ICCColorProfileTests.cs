using Converter.Parsers.ICC;
using Converter.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tester
{
  [TestClass]
  public class ICCColorProfileTests
  {

    [TestMethod]
    public void TestS15Fixed16Num1()
    {
      byte[] arr = new byte[4];
      int pos = 0;
      // 0x80000000
      Span<byte> buffer = arr.AsSpan();
      BufferWriter.WriteInt16BE(ref buffer, ref pos, -32768);
      BufferWriter.WriteUInt16BE(ref buffer, ref pos, 0);

      double expectedRes = -32768;
      ICCParser ICCParser = new ICCParser(Array.Empty<byte>(), true);
      pos = 0;
      ReadOnlySpan<byte> readOnly = arr.AsSpan();
      double actualRes = ICCParser.ParseS15Fixed16Number(ref readOnly, ref pos);
      Assert.AreEqual(expectedRes, actualRes);
    }

    [TestMethod]
    public void TestS15Fixed16Num2()
    {
      byte[] arr = new byte[4];
      int pos = 0;
      // 0x80000000
      Span<byte> buffer = arr.AsSpan();
      BufferWriter.WriteInt16BE(ref buffer, ref pos, 0);
      BufferWriter.WriteUInt16BE(ref buffer, ref pos, 0);

      double expectedRes = 0;
      ICCParser ICCParser = new ICCParser(Array.Empty<byte>(), true);
      pos = 0;
      ReadOnlySpan<byte> readOnly = arr.AsSpan();
      double actualRes = ICCParser.ParseS15Fixed16Number(ref readOnly, ref pos);
      Assert.AreEqual(expectedRes, actualRes);
    }
    [TestMethod]
    public void TestS15Fixed16Num3()
    {
      byte[] arr = new byte[4];
      int pos = 0;
      // 0x80000000
      Span<byte> buffer = arr.AsSpan();
      BufferWriter.WriteInt16BE(ref buffer, ref pos, 1);
      BufferWriter.WriteUInt16BE(ref buffer, ref pos, 0);

      double expectedRes = 1;
      ICCParser ICCParser = new ICCParser(Array.Empty<byte>(), true);
      pos = 0;
      ReadOnlySpan<byte> readOnly = arr.AsSpan();
      double actualRes = ICCParser.ParseS15Fixed16Number(ref readOnly, ref pos);
      Assert.AreEqual(expectedRes, actualRes);
    }
    [TestMethod]
    public void TestS15Fixed16Num4()
    {
      byte[] arr = new byte[4];
      int pos = 0;
      // 0x80000000
      Span<byte> buffer = arr.AsSpan();

      BufferWriter.WriteInt16BE(ref buffer, ref pos, 32767);
      BufferWriter.WriteUInt16BE(ref buffer, ref pos, 65535);

      double expectedRes = 32767 + (65535 / 65536d);
      ICCParser ICCParser = new ICCParser(Array.Empty<byte>(), true);
      pos = 0;
      ReadOnlySpan<byte> readOnly = arr.AsSpan();
      double actualRes = ICCParser.ParseS15Fixed16Number(ref readOnly, ref pos);
      Assert.AreEqual(expectedRes, actualRes);
    }
  }
}
