using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converter.FileStructures.TIFF
{
  public struct BilevelData
  {
    public int StripOffsetsPointer;
    public int StripByteCounterOffsets;
    public int ImageDataOffset;
    public int StripCount;
    public int RowsPerStrip;
  }
}
