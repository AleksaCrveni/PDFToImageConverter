using Converter.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converter
{
  class PDFFile
  {
    public PDFVersion PdfVersion { get; set; } = PDFVersion.INVALID;
    public ulong LastCrossReferenceOffset { get; set; }
    public Trailer Trailer { get; set; }
  }
}
