using Converter;

Stream stream = File.OpenRead(Files.SmallTest);

Span<byte> buffer = stackalloc byte[1024];
int bytesRead = stream.Read(buffer);

int i = 0;