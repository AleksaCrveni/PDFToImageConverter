using Converter.FileStructures.DCT;
using Converter.FileStructures.Huffman;
using System.Diagnostics.Contracts;

namespace Converter.FileStructures.JPEG
{
  
  public class JPEGFile
  {
    public JPEG_HeaderData JFIF;
    public JPEG_QuantTable[] QuantTables;
    public JPEG_HuffmanData HuffmanData;
  }
  public class JPEG_HuffmanData
  {
    public JPEG_HuffmanTable[] HuffTablesDC;
    public JPEG_HuffmanTable[] HuffTablesAC;
  }

  public class JPEG_HeaderData
  {
    public int MajorVersion;
    public int MinorVersion;
    public JPEG_UNIT_TYPE Units;
    public ushort XDensity;
    public ushort YDensity;
    public byte XThumbnail;
    public byte YThumbnail;
    public byte[]? RGB;
  }
  public class JPEG_QuantTable
  {
    public int Precision;
    public byte Dest;
    public ushort[] Values;
  }

  public class JPEG_FrameHeader
  {
    public JPEG_MARKERS Type;
    public byte Precision; // P
    public ushort NumOfLines; //  Y
    public ushort NumOfSamplesPerLine; // X
    public byte NumOfImageComponentsInFrame; // Nf
    public JPEG_FrameComponentInfo[] ComponentInfo; // size Nf
  }

  public class JPEG_FrameComponentInfo
  {
    public byte ComponentIdentifier; // Ci
    public byte HorizontalSamplingFactor; // Hi
    public byte VerticalSamplingFactor; // Vi
    public byte QuantTableDest; // Tqi
  }

  public class JPEG_HuffmanTable
  {
    public JPEG_HUFFMAN_TABLE_TYPE Type;
    public byte Dest;
    public byte[] Values;
    public ushort[] MaxCode;
    /// Contains the largest code of length k (0 if none). MaxCode[17] is a sentinel to ensure the decoder terminates.Values[] offset for codes of length k  ValOffset[k] = Values[] index of 1st symbol of code length k, less the smallest code of length k; so given a code of length k, the corresponding symbol is Values[code + ValOffset[k]].
    public byte[] ValOffset;
    public JPEG_HuffmanTableEntry[] LookaheadTable;
  }

  public class JPEG_HuffmanTableEntry
  {
    public byte CodeSize;
    public byte CodeValue;
  }


  public class JPEG_ScanHeader
  {
    public byte NumOfImageComponents; // Ns
    public JPEG_ScanComponentSelectorData[] ComponentsUsed;
    public byte SpectralPredictorSelectionStart; // Ss
    public byte SpectralSelectionEnd; // Se
    public byte ApproximationBitPosHigh; // Ah
    public byte ApproximationBitPosLow; // Al
  }

  public class JPEG_ScanComponentSelectorData
  {
    public byte ComponentIndex; // Csj
    public byte DCEntropyTableIndex; // Tdj
    public byte ACEntropyTableIndex; // Taj
  }

  /// <summary>
  /// Holds combined data from frame header, scan header and few other properties needed 
  /// for easier decoding
  /// This class does NOT match any table 1/1 in JPEG spec
  /// </summary>
  public class JPEG_DecodeComponentsData
  {
    public int ComponentIndex;
    public byte HorizontalSamplingFactor;
    public byte VerticalSamplingFactor;
    public int DcPredictor;
    public JPEG_HuffmanTable? DCHuffmanTable;
    public JPEG_HuffmanTable? ACHuffmanTable;
    public JPEG_QuantTable QuantTable;
    public int HorizontalSubsamplingFactor;
    public int VerticalSubsamplingFactor;
  }

  public class JPEG_Block8x8
  {
    public short[] Data = new short[64];
  }
  public class JPEG_Block8x8F
  {
    public float[] Data = new float[64];
  }

  public interface JPEG_IDecoderState { }
  public class JPEG_BaselineDecoderState : JPEG_IDecoderState
  {
    public JPEG_DecodeComponentsData[] Components;
    public int MCUSPerLine;
    public int MCUSPerColumn;
    public int LevelShift;
    public int MaxHorizontalSampling;
    public int MaxVerticalSampling;
  }
    
}
