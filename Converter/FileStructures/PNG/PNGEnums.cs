namespace Converter.FileStructures.PNG
{
  public enum PNG_CHUNK_TYPE : uint
  {
    NULL = 0,
    // Critical
    IHDR = 1229472850,
    PLTE = 1347179589,
    IDAT = 1229209940,
    IEND = 1229278788,

    // Ancillary (Skippable)
    bKGD = 1649100612,
    cHRM = 1665684045,
    cICP = 1665745744,
    dSIG = 1683179847,
    eXIf = 1700284774,
    gAMA = 1732332865,
    hIST = 1749635924,
    iCCP = 1766015824,
    iTXt = 1767135348,
    pHYs = 1883789683,
    sBIT = 1933723988,
    sPLT = 1934642260,
    sRGB = 1934772034,
    sTER = 1934902610,
    tEXt = 1950701684,
    tIME = 1950960965,
    tRNS = 1951551059,
    zTXt = 2052348020
  }

  public enum PNG_COLOR_TYPE : byte
  {
    GRAYSCALE = 0,
    RGB = 2,
    PALLETE = 3,
    GRAYSCALE_ALPHA = 4,
    RGB_ALPHA = 6
  }
  public enum PNG_COMPRESSION : byte
  {
    DEFLATE
  }

  public enum PNG_FILTER : byte
  {
    ADAPTIVE
  }

  public enum PNG_INTERLANCE : byte
  {
    NONE,
    ADAM7
  }

}
