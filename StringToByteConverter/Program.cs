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

while (!end)
{
  _stdIn.Read(_buffer, offset, size);
    //ind = span.IndexOfNewLine();
    //if (ind != -1)
    //  input = Encoding.Default.GetString(span.Slice(0, ind));
    //else
  input = Encoding.Default.GetString(span);

  if (input == "EXIT()")
  {
    end = true;
    break;
  }
  sb.Clear();
  sb.AppendFormat("0x{0:x2}", (byte)input[0]);
  int i = 1;
  for (i =i; i < input.Length; i++)
  {
    if (input[i] == 12 || input[i] == 13)
      break;
    sb.Append(", ");
    sb.AppendFormat("0x{0:x2}", (byte)input[i]);
  }

  Console.WriteLine($"Span<byte> buffer = new stackalloc byte[{i}] {{ {sb.ToString()} }};");
}
Console.WriteLine("REPL closed");
Console.ReadLine();
