namespace Converter.Parsers
{
  // NOTE: this is less efficient than span one because i will be reading char one by one for now
  public static class StreamParseHelper
  {
    private static byte CR = 0x0d;
    private static byte LF = 0x0a;
    // just retrives CONTENT of next line, so withopt CR and LF bytes, but bytes will skipped to next line
    public static byte[] GetNextLine(Stream stream)
    {
      long start = stream.Position;
      int iByte = stream.ReadByte();
      // End of stream
      while (iByte != -1)
      {
        if ((byte)iByte == LF)
          break;
        else if ((byte)iByte == CR)
          break;
          stream.ReadByte();

      }
      long end = stream.Position;
      long len = end - start;
      // TODO: maybe need to len is bigger than INT.MAX and read it other way, but for siple files its fine
      byte[] buffer = new byte[len];
      int readBytes = stream.Read(buffer, (int)start, (int)(len));
      if (readBytes != len)
        throw new InvalidDataException("Invalid data.");

      // make sure you move to next line, in case of CR next line can be followed by LF so we need to check if 
      // next byte is LF and skip again
      if (iByte == -1)
        throw new InvalidDataException("Unxpected end of stream!");
      iByte = stream.ReadByte();

      if (iByte == -1)
        throw new InvalidDataException("Unxpected end of stream!");

      if ((byte)iByte == LF)
        stream.ReadByte();

      return buffer;
    }

    public static bool IsEOL(char c)
    {
      // LINE FEED (LF) || CARRIAGE RETURN (CR)
      if (c == (byte)0x0a || c == (byte)0x0d)
        return true;
      return false;
    }
  }
}
