// See https://aka.ms/new-console-template for more information
int WIDTH = 800;
int HEIGHT = 600;

byte[] buff = new byte[HEIGHT * WIDTH * 4];
for (int i =0; i < buff.Length; i +=4)
{
  Random.Shared.NextBytes(buff.AsSpan(i, 3));
}

int x = 0;
File.WriteAllBytes("Single\\data.txt", buff);
