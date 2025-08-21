using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BenchAndSmallTests
{
  [SimpleJob(RuntimeMoniker.Net80)]
  [MemoryDiagnoser]
  public class MyBenches
  {
    private byte[] data;
    [Params(1000, 10000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
      data = new byte[N];
      new Random(new Random().Next()).NextBytes(data);
    }

    [Benchmark]
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

    [Benchmark]
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
    [Benchmark]
    //slowest
    public int[] ShiftingBench()
    {
      Span<byte> buffer = data.AsSpan();
      int[] res = new int[buffer.Length / 4];
      int number0 = 0;
      int number1 = 0;
      int number2 = 0;
      int number3 = 0;
      for (int i = 0; i < buffer.Length; i += 4)
      {
        number0 |= buffer[i + 3];
        number0 = (number0 << 8) | buffer[i + 2];
        number0 = (number0 << 8) | buffer[i + 1];
        number0 = (number0 << 8) | buffer[i];

        res[i % 4] = number0;
       
      }

      return res;
    }
  }
}
