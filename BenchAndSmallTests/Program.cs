using Converter;
using Converter.Writers;
//int count1 = 0b_0000_0001;
//int count2 = 0b_1110_0010;

//byte a = 0b1101_0001;
//byte a1 = (byte)(a << 0b0000_0101);
//int a2 = a << count2;
//Console.WriteLine($"{a} << {count1} is {a1}; {a} << {count2} is {a2}");
//// Output:
//// 1 << 1 is 2; 1 << 225 is 2

//int b = 0b_0100;
//int b1 = b >> count1;
//int b2 = b >> count2;
//Console.WriteLine($"{b} >> {count1} is {b1}; {b} >> {count2} is {b2}");
//// Output:
//// 4 >> 1 is 2; 4 >> 225 is 2

//int count = -31;
//int c = 0b_0001;
//int c1 = c << count;
//Console.WriteLine($"{c} << {count} is {c1}");
//int i = 0;


//var str = File.ReadAllBytes(@"W:\PDFToImageConverter\Files\buc.tif");
//Span<byte> buffer = new byte[str.Length];
//str.CopyTo(buffer);

//// big or small endian
//byte b0 = buffer[0];
//byte b1 = buffer[1];
//if ((b0 == (byte)'I' || b0 == (byte)'M') && b1 == b0)
//  Console.WriteLine("First two bytes are valid.");

//if (b0 == (byte)'I')
//  Console.WriteLine("Little endian");
//else
//  Console.WriteLine("Big endian");

//// arbitary nubmer to check
//byte b2 = buffer[2];
//byte b3 = buffer[3];
//if (b0 == 'I' && b2 == 42)
//  Console.WriteLine("Valid header");
//else if (b0 == 'M' && b3 == 42)
//  Console.WriteLine("Valid header");
//else
//  Console.WriteLine("Invalid tiff header");
//byte[] arr = buffer.Slice(4, 4).ToArray();
//int firstIDFOffset = BitConverter.ToInt32(arr);


//int i = 0;


//byte[] arr = File.ReadAllBytes(Files.BilevelTiff);
//byte b = arr[23880];
//if (b == 1)
//  arr[23880] = 0;
//File.WriteAllBytes("inverted.tiff", arr);

for (int i = 0; i < 10; i++)
{
  TIFFWriter.WriteRandomBilevelTIFF($"Files/Bilevel/test{i}.tif", new TIFFWriterOptions()
  {
    AllowStackAlloct = true,
  });
}

for (int i = 0; i < 10; i++)
{
  TIFFWriter.WriteRandomGrayscaleTIFF($"Files/Grayscale/test{i}.tif", new TIFFWriterOptions()
  {
    AllowStackAlloct = true,
  });
}

for (int i = 0; i < 10; i++)
{
  TIFFWriter.WriteRandomPaletteTiff($"Files/Palette/test{i}.tif", new TIFFWriterOptions()
  {
    AllowStackAlloct = true,
  });
}

//var runner = BenchmarkRunner.Run<MyBenches>();