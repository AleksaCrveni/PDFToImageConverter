using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Converter
{
  public static class StreamHelper
  {
    public static byte CR = 0x0d;
    public static byte LF = 0x0a;
    public static int STACK_ALLOC_SIZE = 1024;

    // NOTE: HEAP
    // EOL marker can be iether CR or LF , or combination of CR followed by LF immedietly
    // TODO: refactor if checks later??
    public static Span<byte> GetNextLineAsSpan(Stream stream)
    {
      long startPosition = GetNextNewLinePosition(stream);
      if (startPosition == -1)
        return Span<byte>.Empty;
      long endPosition = GetNextNewLinePosition(stream);
      if (endPosition == -1)
        endPosition = stream.Position;

      byte[] arr = new byte[endPosition - startPosition];
      stream.Position = startPosition;
      int bytesRead = stream.Read(arr, 0, arr.Length);
      // This should always be same, if its not there is a bug
      if (bytesRead != arr.Length)
        throw new Exception("Exception durring next line position");

      return arr;
    }
    private static long GetNextNewLinePosition(Stream stream)
    {
      long startPosition = stream.Position;
      Span<byte> buffer = stackalloc byte[STACK_ALLOC_SIZE];
      int bytesRead = stream.Read(buffer);
      // EOF
      if (bytesRead == 0)
        return -1;

      byte iByte = buffer[0];
      int index = 0;
      int len = bytesRead;
      // NUL || HORIZONTAL TAB (HT) || LINE FEED (LF) || FORM FEED (FF) || CARRIAGE RETURN (CR) || SPACE (SP
      bool skipped = false;
      bool reloadedBuffer = false;

      // skip to start of nextline
      while (!skipped)
      {
        if (iByte == CR)
        {
          // next will be start of next line
          startPosition++;
          // if its last char in current stack arr we have to load another one to see if it ends with LF
          // otherwise we will be off by 1 byte
          if (index >= len - 1)
          {
            // if we reached end of array and its less than max buff size then we know its end of stream
            if (len < STACK_ALLOC_SIZE)
              return -1;
            bytesRead = stream.Read(buffer);
            // End of stream
            if (bytesRead == 0)
              return -1;
            iByte = buffer[0];
            len = bytesRead;
            index = 0;
          }
          else
          {
            iByte = buffer[++index];
          }
          // if LF is following 
          if (iByte == LF)
          {
            index++;
            startPosition++;
          }
          break;
        }
        else if (iByte == LF)
        {
          index++;
          startPosition++;
          break;
        }
        // check we are on end of buffer
        if (index >= len - 1)
        {
          // if we reached end of array and its less than max buff size then we know its end of stream
          if (len < STACK_ALLOC_SIZE)
            return -1;
          bytesRead = stream.Read(buffer);
          // End of stream
          if (bytesRead == 0)
            return -1;
          iByte = buffer[0];
          len = bytesRead;
          index = 0;
        }
        else
        {
          iByte = buffer[++index];
        }
        startPosition++;
      }

      stream.Position = startPosition;
      return startPosition;
    }
  }
}