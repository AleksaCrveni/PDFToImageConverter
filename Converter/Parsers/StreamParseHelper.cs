using System;

namespace Converter.Parsers
{
  // NOTE: this is less efficient than span one because i will be reading char one by one for now
  public static class StreamParseHelper
  {
    private static byte CR = 0x0d;
    private static byte LF = 0x0a;
    // just retrives CONTENT of next line, so withopt CR and LF bytes, but position will be moved to the END
    // of the line so next time its read normally
    public static byte[] GetNextLine(Stream stream)
    {
      // Make sure to move to another line first
      int iByte = SkipWhiteSpaceAndGetNextByte(stream);

      long start = stream.Position -1;
      if (iByte == -1)
        throw new InvalidDataException("Unxpected end of stream!");
      // End of stream
      while (iByte != -1)
      {
        if ((byte)iByte == LF)
          break;
        else if ((byte)iByte == CR)
          break;
        iByte = stream.ReadByte();

      }
      long end = stream.Position;
      long len = end - start;
      // TODO: maybe need to len is bigger than INT.MAX and read it other way, but for siple files its fine
      byte[] buffer = new byte[len];
      // go back to read entirely
      stream.Position = start;
      int readBytes = stream.Read(buffer, 0, (int)len);
      if (readBytes != len)
        throw new InvalidDataException("Invalid data.");

      // make sure you move to next line, in case of CR next line can be followed by LF so we need to check if 
      // next byte is LF and skip again
      if (iByte == -1)
        throw new InvalidDataException("Unxpected end of stream!");
      iByte = stream.ReadByte();

      if (iByte == -1)
        throw new InvalidDataException("Unxpected end of stream!");

      if ((byte)iByte != LF)
        stream.Position--;

      return buffer;
    }

    public static bool IsEOL(char c)
    {
      // LINE FEED (LF) || CARRIAGE RETURN (CR)
      if (c == (byte)0x0a || c == (byte)0x0d)
        return true;
      return false;
    }

    // SKip whitespace and return last value because its stream. So we dont have to go back and read again
    private static int SkipWhiteSpaceAndGetNextByte(Stream stream)
    {
      // NUL || HORIZONTAL TAB (HT) || LINE FEED (LF) || FORM FEED (FF) || CARRIAGE RETURN (CR) || SPACE (SP)
      int iByte = stream.ReadByte();
      // TODO: throw or return?
      if (iByte == -1)
        return iByte;
      while ((byte)iByte == (byte)0x00 || (byte)iByte == (byte)0x09 || (byte)iByte == (byte)0x0a || (byte)iByte == (byte)0x0c || (byte)iByte == (byte)0x0d || (byte)iByte == (byte)0x20)
      {
        iByte = stream.ReadByte();
        if (iByte == -1)
          return iByte;
      }

      return iByte;
    }
  }
}
