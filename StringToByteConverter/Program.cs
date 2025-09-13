using System.Runtime.CompilerServices;
using System.Text;

Stream _stdIn = Console.OpenStandardInput();
   
int offset = 0;
int size = 1024;
byte[] _buffer = new byte[size];
// Not sure if this is right approach

bool end = false;
string input = "";
Console.WriteLine("REPL started");
int ind = 0;
Span<byte> span = new Span<byte>();
span = _buffer.AsSpan();
StringBuilder sb = new StringBuilder(0);
Span<byte> fourByteSpan = new byte[4];
// create span out of character
int mode = 0;
// create unsigned int32 out of 4 byte string
mode = 1;
while (!end)
{
  _stdIn.Read(_buffer, offset, size);
    //ind = span.IndexOfNewLine();
    //if (ind != -1)
    //  input = Encoding.Default.GetString(span.Slice(0, ind));
    //else
  input = Encoding.Default.GetString(span);

  end = mode switch
  {
    0 => CreateSpan(ref input),
    1 => CreateUInt32From4ByteString(input.AsSpan().Slice(0, 4)),
    _ => throw new Exception("Invalid mode!")
  };
  
  
}

bool CreateUInt32From4ByteString(ReadOnlySpan<char> input)
{
  sb.Clear();
  Span<byte> bInput = new byte[4];
  for (int i =0; i < bInput.Length; i++)
  {
    bInput[i] = (byte)input[i];
  }

  uint res = BitConverter.ToUInt32(bInput);
  Console.WriteLine($"Unsigned int value is {res}");
  return false;
  }

bool CreateSpan(ref string input)
{
  if (input == "EXIT()")
  {
    return true;
  }
  sb.Clear();
  sb.AppendFormat("0x{0:x2}", (byte)input[0]);
  int i = 1;
  for (i = i; i < input.Length; i++)
  {
    if (input[i] == 12 || input[i] == 13)
      break;
    sb.Append(", ");
    sb.AppendFormat("0x{0:x2}", (byte)input[i]);
  }

  Console.WriteLine($"Span<byte> buffer = new stackalloc byte[{i}] {{ {sb.ToString()} }};");
  return false;
}
Console.WriteLine("REPL closed");
Console.ReadLine();
