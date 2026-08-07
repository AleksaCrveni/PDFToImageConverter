using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Converter;
using Converter.FileStructures.PDF;
using Converter.Parsers.Fonts;
using Converter.Parsers.PDF;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BenchAndSmallTests
{
  [MemoryDiagnoser]
  public class MyBenches
  {
    private byte[] data;
    [Params(1000, 10000, 100_000)]
    public int N;
    byte[] samplePDF = Array.Empty<byte>();
    [GlobalSetup]
    public void Setup()
    {
      data = new byte[N];
      new Random(new Random().Next()).NextBytes(data);
      samplePDF = File.ReadAllBytes(@"W:\PDFToImageConverter\Files\sample.pdf");
    }

    //[Benchmark]
    //fastests
    public int[] BitConverterBench()
    {
      Span<byte> buffer = data.AsSpan();
      int[] res = new int[buffer.Length / 4];
      for (int i= 0; i < buffer.Length; i += 4)
      {
        res[i % 4] = BitConverter.ToInt32(buffer.Slice(i, 4));
      }

      return res;
    }

   // [Benchmark]
    // about same as bitconverter
    public int[] UnsafeAsBench()
    {
      Span<byte> buffer = data.AsSpan();
      int[] res = new int[buffer.Length / 4];
      for (int i = 0; i < buffer.Length; i += 4)
      {
        res[i % 4] = Unsafe.As<byte, int>(ref Unsafe.AsRef(in (MemoryMarshal.GetReference(buffer.Slice(i, 4)))));
      }

      return res;
    }
   // [Benchmark]
    //slowest
    public int[] ShiftingBench()
    {
      Span<byte> buffer = data.AsSpan();
      int[] res = new int[buffer.Length / 4];
      for (int i = 0; i < buffer.Length; i += 4)
      {
        res[i % 4] = (buffer[i + 3] | (buffer[i + 2] << 8) | (buffer[i + 1] << 16) | (buffer[i] << 24));
      }

      return res;
    }


    //[Benchmark]
    public int[] BinaryPrimitivesTest()
    {
      Span<byte> buffer = data.AsSpan();
      int[] res = new int[buffer.Length / 4];
      for (int i = 0; i < buffer.Length; i += 4)
      {
        res[i % 4] = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(i, 4));

      }

      return res;
    }
    [Benchmark]
    public int BenchPDFSampleConversion()
    {
      PDFFile file = new PDFFile();
      PdfParser p = new PdfParser();
      MemoryStream m = new MemoryStream();
      MemoryStream i = new MemoryStream(samplePDF);
      PDF_Options o = new PDF_Options();
      p.Parse(file, i, m, ref o);
      return file.CrossReferenceEntries.Count;
    }
  }
}
