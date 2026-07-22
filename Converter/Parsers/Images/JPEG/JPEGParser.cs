using Converter.FileStructures.JPEG;
using Converter.Utils;
using Converter.Utils.JPEG;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Transactions;

namespace Converter.Parsers.Images.JPEG
{
  /*
   * Parts of this implementation of JPEGPArser (mostly constructing huffmantables, computing MCUs and processing compneents)
   * is inspired by/altered/simplified and slower version of
   * https://github.com/yigolden-oss/JpegLibrary
    which has MIT licence that is included bellow
  */
  //  IT License

  //Copyright(c) 2019-2021 yigolden

  //Permission is hereby granted, free of charge, to any person obtaining a copy
  //of this software and associated documentation files(the "Software"), to deal
  //in the Software without restriction, including without limitation the rights
  //to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  //copies of the Software, and to permit persons to whom the Software is
  //furnished to do so, subject to the following conditions:

  //The above copyright notice and this permission notice shall be included in all
  //copies or substantial portions of the Software.

  //THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  //IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  //FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
  //AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  //LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  //OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  //SOFTWARE.

  /// <summary>
  /// CCITT T.81 Parser version (1992) + whatever bits of extra information I could find from other blogs, open source code etc
  /// </summary>
  public static class JPEGParser
  {
    /// <summary>
    /// This api is pretty much used for testing
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static List<int> Code16List = new List<int>();
    public static double[] IDCT_TABLE = [
      0.707107,  0.707107,  0.707107,  0.707107,  0.707107,  0.707107,  0.707107,  0.707107,
      0.980785,  0.831470,  0.555570,  0.195090, -0.195090, -0.555570, -0.831470, -0.980785,
      0.923880,  0.382683, -0.382683, -0.923880, -0.923880, -0.382683,  0.382683,  0.923880,
      0.831470, -0.195090, -0.980785, -0.555570,  0.555570,  0.980785,  0.195090, -0.831470,
      0.707107, -0.707107, -0.707107,  0.707107,  0.707107, -0.707107, -0.707107,  0.707107,
      0.555570, -0.980785,  0.195090,  0.831470, -0.831470, -0.195090,  0.980785, -0.555570,
      0.382683, -0.923880,  0.923880, -0.382683, -0.382683,  0.923880, -0.923880,  0.382683,
      0.195090, -0.555570,  0.831470, -0.980785,  0.980785, -0.831470,  0.555570, -0.195090
      ];
    public static JPEGFile Parse(string filename)
    {
      Stream stream = File.OpenRead(filename);
      if (stream.Length > Int32.MaxValue)
        throw new NotSupportedException("Only supported up to 4GB files");
      byte[] arr = ArrayPool<byte>.Shared.Rent((int)stream.Length);
      stream.Read(arr);
      JPEGFile f = Parse(arr.AsSpan());
      ArrayPool<byte>.Shared.Return(arr);
      return f;
    }
    public static JPEGFile Parse(Span<byte> buffer)
    {
      JPEGFile file = new JPEGFile();
      ByteReader r = new ByteReader(buffer);
      bool EOF = false;
      JPEG_MARKERS marker = (JPEG_MARKERS)r.ReadUInt16BE();
      if (marker != JPEG_MARKERS.SOI)
        throw new InvalidDataException($"Expected SOI marker got {(int)marker}");

      file.HuffmanData = new JPEG_HuffmanData();
      file.HuffmanData.HuffTablesAC = new JPEG_HuffmanTable[4];
      file.HuffmanData.HuffTablesDC = new JPEG_HuffmanTable[4];

      file.QuantTables = new JPEG_QuantTable[4];
      
      for (int i = 0; i < 4; i++)
      {
        JPEG_HuffmanTable hAC = new JPEG_HuffmanTable();
        file.HuffmanData.HuffTablesAC[i] = hAC;

        JPEG_HuffmanTable tDC = new JPEG_HuffmanTable();
        file.HuffmanData.HuffTablesDC[i] = tDC;

        JPEG_QuantTable q = new JPEG_QuantTable();
        file.QuantTables[i] = q;
      }

      byte[] outputBuffer = Array.Empty<byte>();

      JPEG_FrameHeader currentFrameHeader = new JPEG_FrameHeader();
      int byteSizeOfMarkerSizeField;
      JPEG_IDecoderState state = null;
      int restartInterval = 0;
      uint size = 0;
      while (!EOF)
      {
        marker = (JPEG_MARKERS)r.ReadUInt16BE();
        byteSizeOfMarkerSizeField = GetLengthFieldByteSize(marker);
        if (byteSizeOfMarkerSizeField == 1)
          size = r.ReadByte();
        else if (byteSizeOfMarkerSizeField == 2)
          size = r.ReadUInt16BE();
        else if (byteSizeOfMarkerSizeField == 4)
          size = r.ReadUInt32BE();
        // ideally we want to do switch on marker just once and read size but this is a bit easier to work with for now
        switch (marker)
        {
          case JPEG_MARKERS.SOF0:
            currentFrameHeader.Type = JPEG_MARKERS.SOF0;
            ParseBaselineDCTFrameHeaderData(ref r, currentFrameHeader);
            state = InitBaselineDecoderState(currentFrameHeader);
            // we will also just support one frame per file
            if (outputBuffer.Length > 0)
            {
              throw new InvalidDataException("One frame per file supported!");
            }
            else
            { 
              currentFrameHeader.Height = currentFrameHeader.NumOfLines;
              currentFrameHeader.Width = currentFrameHeader.NumOfSamplesPerLine;
              outputBuffer = new byte[currentFrameHeader.Height * currentFrameHeader.Width * currentFrameHeader.NumOfImageComponentsInFrame];
            }
            break;
          case JPEG_MARKERS.DHT:
            ParseDHT(file.HuffmanData, ref r, size);
            break;
          case JPEG_MARKERS.JPG:
            break;
          case JPEG_MARKERS.DAC:
            break;
          case JPEG_MARKERS.SOI:
            break;
          case JPEG_MARKERS.EOI:
            EOF = true;
            break;
          case JPEG_MARKERS.SOS:
            JPEG_ScanHeader scanHeader = ParseScanHeader(ref r, size);
            DecodeScan(ref r, state, scanHeader, currentFrameHeader, file, restartInterval, outputBuffer);
            break;
          case JPEG_MARKERS.DQT:
            ParseDQT(ref r, file.QuantTables, size);
            break;
          case JPEG_MARKERS.DRI:
            restartInterval = ParseDRI(ref r);
            break;
          case JPEG_MARKERS.APP0:
            file.JFIF = ParseJFIF(ref r);
            break;
          default:
            throw new NotSupportedException($"Marker {marker.ToString()} not supported!");
            break;
        }

      }

      file.Height = currentFrameHeader.Height;
      file.Width = currentFrameHeader.Width;
      file.Buffer = outputBuffer;
      return file;
    }

    public static JPEG_HeaderData ParseJFIF(ref ByteReader r)
    {
      JPEG_HeaderData h = new JPEG_HeaderData();
      string key = Encoding.ASCII.GetString(r.ReadNextNBytes(4));
      if (key != "JFIF")
        throw new InvalidDataException("Invalid JFIF segment!");
      r.ReadByte();
      h.MajorVersion = r.ReadByte();
      h.MinorVersion = r.ReadByte();
      byte u = r.ReadByte();
      if (u < 0 || u > 2)
        throw new InvalidDataException("Invalid Unit value!");
      h.Units = (JPEG_UNIT_TYPE)u;
      h.XDensity = r.ReadUInt16BE();
      h.YDensity = r.ReadUInt16BE();
      if (h.XDensity == 0 || h.YDensity == 0)
        throw new InvalidDataException("Invalid Density values!");
      h.XThumbnail = r.ReadByte();
      h.YThumbnail = r.ReadByte();
      int rgbSize = h.XThumbnail * h.YThumbnail;
      if (rgbSize > 0)
      {
        h.RGB = r.ReadNextNBytes(rgbSize * 3);
      }
      return h;
    }

    public static void DecodeScan(ref ByteReader r, JPEG_IDecoderState state, JPEG_ScanHeader scanHeader, JPEG_FrameHeader frameHeader, JPEGFile file, int restartInterval, byte[] outputBuffer)
    {
      switch (frameHeader.Type)
      {
        case JPEG_MARKERS.SOF0:
          JPEG_BaselineDecoderState baselineState = (JPEG_BaselineDecoderState)state;
          UpdateBaselineStateWithScanHeader(baselineState, frameHeader, scanHeader, file.HuffmanData, file.QuantTables);
          DecodeBaselineDCT(ref r, baselineState, restartInterval, outputBuffer, frameHeader.Width, frameHeader.Height);
          break;
        default:
          throw new NotImplementedException("Frameheader of this type not supported!");
      }
    }

    /// <summary>
    /// Just comment out loggs for now and remove them later when more images are tested accross different files
    /// </summary>
    /// <param name="r"></param>
    /// <param name="state"></param>
    /// <param name="restartInterval"></param>
    /// <param name="outputBuffer"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <exception cref="InvalidDataException"></exception>
    public static void DecodeBaselineDCT(ref ByteReader r, JPEG_BaselineDecoderState state, int restartInterval, byte[] outputBuffer, int width, int height)
    {
      JPEGBitReader bitReader = new JPEGBitReader();
      Span<short> outputBlock = stackalloc short[64];
      Span<float> outputFBlock = stackalloc float[64];
      Span<short> tempBlock = stackalloc short[64];
      Span<float> blockF = stackalloc float[64];

      int MCUSBeforeRestart = restartInterval;
      List<byte> decoded = new List<byte>();
      List<float> floats = new List<float>();
      List<float> idctfloats = new List<float>();
      List<byte> orig = new List<byte>();

      int mcuCounter = 0;
      for (int rowMCU = 0; rowMCU < state.MCUSPerColumn; rowMCU++)
      {
        int offsetY = rowMCU * state.MaxVerticalSampling;
        for (int colMCU = 0; colMCU < state.MCUSPerLine; colMCU++)
        {
          int offsetX = colMCU * state.MaxHorizontalSampling;

          foreach (JPEG_DecodeComponentsData component in state.Components)
          {
            int index = component.ComponentIndex;
            int h = component.HorizontalSamplingFactor;
            int v = component.VerticalSamplingFactor;
            int hs = component.HorizontalSubsamplingFactor;
            int vs = component.VerticalSubsamplingFactor;

            for (int y = 0; y < v; y++)
            {
              int blockOffsetY = (offsetY + y) * 8;
              for (int x = 0; x < h; x++)
              {
               
                mcuCounter++;
                // ideally we would want this to be in separate fucntions but we can't pass multile ref structs by reference of same scope (?) due to life time erros from
                // compiler without using unsafe, we wil just put them in separate scope and fold in editor
                outputBlock.Clear();
                {
                  int t = DecodeHuffmanCode(ref bitReader, ref r, component.DCHuffmanTable!);
                  if (t != 0)
                  {
                    t = ReceiveAndExtend(ref bitReader, ref r, t);
                  }

                  t += component.DcPredictor;
                  component.DcPredictor = t;
                  outputBlock[0] = (short)t;
                  // rest are AC
                  // Figure F.13 – Huffman decoding procedure for AC coefficients
                  JPEG_HuffmanTable acTable = component.ACHuffmanTable!;
                  for (int i = 1; i < 64;)
                  {
                    int s = DecodeHuffmanCode(ref bitReader, ref r, acTable);
                    int rr = s >> 4;
                    s &= 15;
                    if (s != 0)
                    {
                      i += rr;
                      s = ReceiveAndExtend(ref bitReader, ref r, s);
                      outputBlock[Math.Min(i++, 63)] = (short)s;
                    }
                    else
                    {
                      if (rr == 0)
                      {
                        break;
                      }

                      i += 16;
                    }
                  }
                }
                
                
                //for (int i =0; i < outputBlock.Length; i++)
                //{
                //  short b = outputBlock[i];
                //  decoded.Add((byte)(b >> 8));
                //  decoded.Add((byte)(b & 255));
                //}
                  
                DequantizeBlockAndUnZigZag(component.QuantTable, ref outputBlock, ref blockF);
                //for (int i = 0; i < 64; i++)
                //{
                //  floats.Add(blockF[i]);
                //}
                IDCTTable(ref blockF, ref outputFBlock);
                //for (int i = 0; i < 64; i++)
                //{
                //  idctfloats.Add(outputFBlock[i]);
                //}


                ShiftBlockLevel(ref outputFBlock, ref outputBlock, state.LevelShift);
                TransposeBlock(ref outputBlock, ref tempBlock);
                //for (int i = 0; i < outputBlock.Length; i++)
                //{
                //  short b = original[i];
                //  orig.Add((byte)(b >> 8));
                //  orig.Add((byte)(b & 255));
                //}
                WriteBlock8Bit(outputBuffer, state.Components.Length, width, height, ref outputBlock, ref tempBlock, index, (offsetX + x) * 8, blockOffsetY, hs, vs);
              }
            }
          }

          // This is done only if there is table before that defines restartInterval
          if (restartInterval > 0 && (--MCUSBeforeRestart) == 0)
          {
            bitReader.AdvanceAlignByte(ref r);
            JPEG_MARKERS marker = bitReader.ReadMarker();
            if (marker == JPEG_MARKERS.EOI)
            {
              return;
            }
            if (!IsRestartMarker(marker))
            {
              throw new InvalidDataException("Expected Restart Marker!");
            }

            MCUSBeforeRestart = restartInterval;
            foreach (JPEG_DecodeComponentsData c in state.Components)
              c.DcPredictor = 0;
          }
        }
      }


      //File.WriteAllBytes("decodedRawMy.bin", decoded.ToArray());
      //StringBuilder sb = new StringBuilder();
      //int blockCount = floats.Count / 64;
      //for (int i = 0; i < blockCount; i++)
      //{
      //  for (int j = 0; j < 64; j++)
      //    sb.Append(floats[i * 64 + j]);
      //  sb.Append('\n');
      //  sb.Append($"END OF BLOCK {i}");
      //  sb.Append('\n');
      //}
      //File.WriteAllText("dequantRawMy.bin", sb.ToString());
      //sb.Clear();
      //for (int i = 0; i < blockCount; i++)
      //{
      //  for (int j = 0; j < 64; j++)
      //    sb.Append(idctfloats[i * 64 + j]);
      //  sb.Append('\n');
      //  sb.Append($"END OF BLOCK {i}");
      //  sb.Append('\n');
      //}
      //File.WriteAllText("idctRawMy.bin", sb.ToString());
      //File.WriteAllBytes("origRawMy.bin", orig.ToArray());
      bitReader.AdvanceAlignByte(ref r);
      JPEG_MARKERS m = bitReader.PeekMarker();
      if (!IsRestartMarker(m))
      {
        r.SetPos(r.GetPos() - 2); // eww
      }
    }
    public static bool IsRestartMarker(JPEG_MARKERS m) => m >= JPEG_MARKERS.RST0 && m <= JPEG_MARKERS.RST7;
    
    public static void WriteBlock8Bit(byte[] buffer, int componentCount, int width, int height, ref Span<short> block, ref Span<short> tempBlock, int componentIndex, int x, int y, int horizontalSubsamplingFactor, int verticalSubsamplingFactor)
    {
      // data is sequentical, not veriten into blocks
      if (horizontalSubsamplingFactor == 1 && verticalSubsamplingFactor == 1)
      {
        WriteBlock8BitFast(buffer, componentCount, width, height, componentIndex, ref block, x, y);
      }
      else
      {
        WriteBlock8BitSlow(buffer, componentCount, width, height, ref block, ref tempBlock, componentIndex, x, y, horizontalSubsamplingFactor, verticalSubsamplingFactor);
      }

    }
    public static void WriteBlock8BitFast(byte[] buffer, int componentCount, int width, int height, int componentIndex, ref Span<short> block, int x, int y)
    {
      if (x > width || y > height)
        return;

      // this for like edge blocks that arent 8 byte aligned
      int writeWidth = Math.Min(width - x, 8); 
      int writeHeight = Math.Min(height - y, 8);

      // offset to block
      int offset = y * width * componentCount + x * componentCount + componentIndex; 
      for (int destY = 0; destY < writeHeight; destY++)
      {
        int rowOffset = offset + destY * width * componentCount;
        for (int destX = 0; destX < writeWidth; destX++)
        {
          buffer[rowOffset + destX * componentCount] = MyMath.ClampTo8Bit(block[destY * 8 + destX]);
        }
      }
    }
    
    public static void WriteBlock8BitSlow(byte[] buffer, int componentCount, int width, int height, ref Span<short> block, ref Span<short> tempBlock, int componentIndex, int x, int y, int horizontalSubsamplingFactor, int verticalSubsamplingFactor)
    {
      int hShift = (int)Math.Floor(Math.Log2((uint)horizontalSubsamplingFactor)); // wtf is this
      int vShift = (int)Math.Floor(Math.Log2((uint)verticalSubsamplingFactor)); // wtf is this

      for (int v = 0; v < verticalSubsamplingFactor; v++)
      {
        for (int h = 0; h < horizontalSubsamplingFactor; h++)
        {
          int vBlock = 8 * v;
          int hBlock = 8 * h;

          // fill tempblock
          for (int i = 0; i < 8; i++)
          {
            int tempRowOffset = i * 8;
            int blockRowOffset = ((vBlock + i) >> vShift) * 8;
            for (int j = 0; j < 8; j++)
            {
              tempBlock[tempRowOffset + j] = block[(blockRowOffset + ((hBlock + j) >> hShift))];
            }
          }

          WriteBlock8BitFast(buffer, componentCount, width, height, componentIndex, ref tempBlock, x + 8 * h, y + 8 * v);
        }
      }
    }

    public static JPEG_BaselineDecoderState InitBaselineDecoderState(JPEG_FrameHeader frameHeader)
    {
      JPEG_BaselineDecoderState state = new JPEG_BaselineDecoderState();
      int maxHorizontalSampling = 1; // HMax
      int maxVerticalSampling = 1; // VMax

      foreach (JPEG_FrameComponentInfo cInfo in frameHeader.ComponentInfo)
      {
        if (cInfo.HorizontalSamplingFactor > maxHorizontalSampling)
          maxHorizontalSampling = cInfo.HorizontalSamplingFactor;

        if (cInfo.VerticalSamplingFactor > maxVerticalSampling)
          maxVerticalSampling = cInfo.VerticalSamplingFactor;
      }

      state.MaxHorizontalSampling = maxHorizontalSampling;
      state.MaxVerticalSampling = maxHorizontalSampling;
      // A.1.1 Dimensions and sampling factorsA.1.1 Dimensions and sampling factors
      // Even if mcus can be different size, they still have to be bounded by 8x8 grid
      state.MCUSPerLine = (frameHeader.NumOfSamplesPerLine + 8 * maxHorizontalSampling - 1) / (8 * maxHorizontalSampling);
      state.MCUSPerColumn = (frameHeader.NumOfLines + 8 * maxVerticalSampling - 1) / (8 * maxVerticalSampling);
      state.LevelShift = 1 << (frameHeader.Precision - 1); // this is pretty much 2^(p - 1) from A.3.1;
      state.Components = new JPEG_DecodeComponentsData[frameHeader.NumOfImageComponentsInFrame];
      for (int i = 0; i < state.Components.Length; i++)
      {
        state.Components[i] = new JPEG_DecodeComponentsData();
      }
      return state;
    }
    public static void UpdateBaselineStateWithScanHeader(JPEG_BaselineDecoderState state, JPEG_FrameHeader frameHeader, JPEG_ScanHeader scanHeader, JPEG_HuffmanData huffmanData, JPEG_QuantTable[] quantTables)
    {
      PrepareComponentsForDecoding(state.Components, frameHeader, scanHeader, huffmanData, quantTables);
    }

    public static void IDCTTable(ref Span<float> DCT, ref Span<float> IDCT)
    {
      for (int x = 0; x < 8; x++)
      {
        for (int y = 0; y < 8; y++)
        {
          IDCT[x * 8 + y] = (float)ShortIDCT(ref DCT, x, y);
        }
      }
    }

    public static double ShortIDCT(ref Span<float> DCT, int x, int y)
    {
      double sum = 0;
      for (int u = 0; u < 8; u++)
      {
        for (int v = 0; v < 8; v++)
        {
          sum += DCT[v * 8 + u] * IDCT_TABLE[u * 8 + x] * IDCT_TABLE[v * 8 + y];
        }
      }
      return sum / 4;
    }
 
    public static void ShiftBlockLevel(ref Span<float> inputBlock, ref Span<short> outputBlock, int levelShift)
    {
      for (int i = 0; i < 64; i++)
      {
        outputBlock[i] = (short)(Math.Round(inputBlock[i]) + levelShift);
      }
    }
    public static void DequantizeBlockAndUnZigZag(JPEG_QuantTable t, ref Span<short> inputBlock, ref Span<float> outputBlock)
    {
      for (int i = 0; i < 64; i++)
      {
        outputBlock[JPEGZigZag.BufferToBlockMap[i]] = inputBlock[i] * t.Values[i];
      }
    }

    public static void ReadBlockBaseline(ref JPEGBitReader reader, ref ByteReader br, JPEG_DecodeComponentsData component, ref Span<short> destBlock)
    {
      // F.2.2.1 Huffman decoding of DC coefficients
      // first element is always DC
      int t = DecodeHuffmanCode(ref reader, ref br, component.DCHuffmanTable!);
      if (t != 0)
      {
        t = ReceiveAndExtend(ref reader, ref br, t);
      }

      t += component.DcPredictor;
      component.DcPredictor = t;
      destBlock[0] = (short)t;
      // rest are AC
      // Figure F.13 – Huffman decoding procedure for AC coefficients
      JPEG_HuffmanTable acTable = component.ACHuffmanTable!;
      for (int i = 1; i < 64;)
      {
        int s = DecodeHuffmanCode(ref reader, ref br, acTable);
        int r = s >> 4;
        s &= 15;
        if (s != 0)
        {
          i += r;
          s = ReceiveAndExtend(ref reader, ref br, s);
          destBlock[Math.Min(i++, 63)] = (short)s;
        }
        else
        {
          if (r == 0)
          {
            break;
          }

          i += 16;
        }
      }

    }
    public static int DecodeHuffmanCode(ref JPEGBitReader reader, ref ByteReader r, JPEG_HuffmanTable t)
    {
      int bits = reader.PeekBits(16, out int bitsRead, ref r);
      Code16List.Add(bits);
      JPEG_HuffmanTableEntry entry = LookupBitsInHuffmanTable(t, bits);
      bitsRead = Math.Min(entry.CodeSize, bitsRead);
      reader.SkipBits(bitsRead, out _, ref r);
      return entry.CodeValue;
    }

    // Figure F.12 – Extending the sign bit of a decoded value in V
    public static int ReceiveAndExtend(ref JPEGBitReader reader, ref ByteReader r, int length)
    {
      int value = reader.ReadBits(length, out bool isMarkerEncountered, ref r);
      if (isMarkerEncountered)
        throw new InvalidDataException("Expect raw data from bit stream. Yet a marker is encountered.");
      return (value - ((((value + value) >> length) - 1) & ((1 << length) - 1)));
    }
    public static JPEG_HuffmanTableEntry LookupBitsInHuffmanTable(JPEG_HuffmanTable t, int code16bit)
    {
      // check if its high 8
      int high8 = code16bit >> 8;
      JPEG_HuffmanTableEntry entry = t.LookaheadTable[high8];
      if (entry.CodeSize != 0)
        return entry;

      // if not do full code lookup
      int size = 9;
      while (code16bit > t.MaxCode[size])
        size++;

      if (size > 16)
        throw new InvalidDataException("Invalid huffman code encoutnered!");

      code16bit >>= (16 - size);
      JPEG_HuffmanTableEntry e = new JPEG_HuffmanTableEntry();
      e.CodeSize = (byte)size;
      e.CodeValue = t.Values[(t.ValOffset[size] + code16bit) & 0xFF];
      return e;
    }

    public static void PrepareComponentsForDecoding(JPEG_DecodeComponentsData[] components, JPEG_FrameHeader frameHeader, JPEG_ScanHeader scanHeader, JPEG_HuffmanData huffmanData, JPEG_QuantTable[] quantTables)
    {

      int maxHorizontalSampling = 1; // HMax
      int maxVerticalSampling = 1; // VMax

      foreach (JPEG_FrameComponentInfo cInfo in frameHeader.ComponentInfo)
      {
        if (cInfo.HorizontalSamplingFactor > maxHorizontalSampling)
          maxHorizontalSampling = cInfo.HorizontalSamplingFactor;

        if (cInfo.VerticalSamplingFactor > maxVerticalSampling)
          maxVerticalSampling = cInfo.VerticalSamplingFactor;
      }

      for (int i = 0; i < scanHeader.NumOfImageComponents; i++)
      {
        JPEG_ScanComponentSelectorData scanComponent = scanHeader.ComponentsUsed[i];
        int componentIndex = 0;
        JPEG_FrameComponentInfo? frameComponent = null;
        for (int j = 0; j < frameHeader.NumOfImageComponentsInFrame; j++)
        {
          JPEG_FrameComponentInfo fci = frameHeader.ComponentInfo[j];
          if (scanComponent.ComponentIndex == fci.ComponentIdentifier)
          {
            componentIndex = j;
            frameComponent = fci;
          }
        }

        if (frameComponent == null)
          throw new InvalidDataException("Matching frame component not found!");

        components[i] = new JPEG_DecodeComponentsData();

        components[i].ComponentIndex = componentIndex;
        components[i].VerticalSamplingFactor = frameComponent.VerticalSamplingFactor;
        components[i].HorizontalSamplingFactor = frameComponent.HorizontalSamplingFactor;
        components[i].DCHuffmanTable = huffmanData.HuffTablesDC[scanComponent.DCEntropyTableIndex];
        components[i].ACHuffmanTable = huffmanData.HuffTablesAC[scanComponent.ACEntropyTableIndex];
        components[i].QuantTable = quantTables[frameComponent.QuantTableDest];
        components[i].HorizontalSubsamplingFactor = maxHorizontalSampling / frameComponent.HorizontalSamplingFactor;
        components[i].VerticalSubsamplingFactor = maxVerticalSampling / frameComponent.VerticalSamplingFactor;
        components[i].DcPredictor = 0;
      }
    }

    /// <summary>
    /// Figure C.2 – Generation of table of Huffman codes
    /// </summary>
    public static int GenerateHuffmanSizeTable(ReadOnlySpan<byte> bits, Span<byte> huffSize)
    {
      int k = 0;
      int i = 1;
      int j = 1;
      for (; i <= 16; i++)
      {
        while (j <= bits[i - 1])
        {
          huffSize[k] = (byte)i;
          k++;
          j++;
        }
        j = 1;
      }

      huffSize[k] = 0;
      return k; // LASTK = k
    }

    public static void GenerateHuffmanCodeTable(ReadOnlySpan<byte> huffSize, Span<ushort> huffCode)
    {
      int k = 0;
      int code = 0;
      int SI = huffSize[0];
      while (true)
      {
        do
        {
          huffCode[k] = (ushort)code;
          code++;
          k++;
        } while (huffSize[k] == SI);

        if (huffSize[k] == 0)
          break;

        do
        {
          code <<= 1;
          SI++;
        } while (huffSize[k] != SI);
      }
    }

    public static JPEG_ScanHeader ParseScanHeader(ref ByteReader r, uint ls)
    {
      JPEG_ScanHeader h = new JPEG_ScanHeader();
      h.NumOfImageComponents = r.ReadByte();
      if (6 + 2 * h.NumOfImageComponents != ls)
        throw new InvalidDataException("Invalid Number of image components in the scan!");
      h.ComponentsUsed = new JPEG_ScanComponentSelectorData[h.NumOfImageComponents];
      for (int i = 0; i < h.NumOfImageComponents; i++)
      {
        JPEG_ScanComponentSelectorData sd = new JPEG_ScanComponentSelectorData();
        sd.ComponentIndex = r.ReadByte();
        byte TdTa = r.ReadByte();
        sd.DCEntropyTableIndex = (byte)(TdTa >> 4);
        sd.ACEntropyTableIndex = (byte)(TdTa & 15);
        h.ComponentsUsed[i] = sd;
      }

      h.SpectralPredictorSelectionStart = r.ReadByte();
      h.SpectralSelectionEnd = r.ReadByte();

      byte AhAl = r.ReadByte();
      h.ApproximationBitPosHigh = (byte)(AhAl >> 4);
      h.ApproximationBitPosLow = (byte)(AhAl & 15);

      return h;
    }
    // Define Huffman Table
    public static void ParseDHT(JPEG_HuffmanData hData, ref ByteReader r, uint lh)
    {
      byte TcTh = r.ReadByte();
      byte tc = (byte)(TcTh >> 4);
      byte dest = (byte)(TcTh & 15);
      if (tc != 0 && tc != 1)
        throw new InvalidDataException("Invalid data exception!");

      JPEG_HuffmanTable t;
      JPEG_HUFFMAN_TABLE_TYPE tableClass = (JPEG_HUFFMAN_TABLE_TYPE)tc;

      if (tc == (byte)JPEG_HUFFMAN_TABLE_TYPE.AC)
        t = hData.HuffTablesAC[dest];
      else
        t = hData.HuffTablesDC[dest];

      t.Dest = dest;
      t.Type = (JPEG_HUFFMAN_TABLE_TYPE)tc;
      Span<byte> lengths = stackalloc byte[16];
      int tm = 0;

      for (int i = 0; i < 16; i++)
      {
        lengths[i] = r.ReadByte();
        tm += lengths[i];
      }

      if (tm > 256)
        throw new InvalidDataException("Huffman table Tm too large!");

      Span<byte> huffSize = stackalloc byte[256 + 1];
      //Figure C.1 – Generation of table of Huffman code sizes
      GenerateHuffmanSizeTable(lengths, huffSize);

      Span<ushort> huffCode = stackalloc ushort[256 + 1];
      //Figure C.2 – Generation of table of Huffman codes
      GenerateHuffmanCodeTable(huffSize, huffCode);
      ReadOnlySpan<byte> codeValues = r.SliceBuffer(tm);
      ComputeHuffmanLookaheadTable(t, lengths, huffCode, codeValues);
    }

    // I am not really sure what is happening here outside of obvious "Computing lookahead table"
    public static void ComputeHuffmanLookaheadTable(JPEG_HuffmanTable table, ReadOnlySpan<byte> codeLengths, ReadOnlySpan<ushort> huffCode, ReadOnlySpan<byte> values)
    {
      if (table.Values == null)
      {
        table.Values = new byte[256]; //Vi,j
        table.MaxCode = new ushort[18];
        table.ValOffset = new byte[19];
        table.LookaheadTable = new JPEG_HuffmanTableEntry[256];
        for (int i = 0; i < table.LookaheadTable.Length; i++)
        {
          JPEG_HuffmanTableEntry e = new JPEG_HuffmanTableEntry();
          table.LookaheadTable[i] = e;
        }
      }

      values.CopyTo(table.Values);
      int p = 0;
      for (int l = 1; l <= 16; l++)
      {
        if (codeLengths[l - 1] != 0)
        {
          int offset = p - huffCode[p];
          table.ValOffset[l] = (byte)offset;
          p += codeLengths[l - 1];
          table.MaxCode[l] = huffCode[p - 1];
          table.MaxCode[l] <<= 16 - l;
          table.MaxCode[l] = (ushort)(table.MaxCode[l] | (uint)((1 << (16 - l)) - 1));
        }
        else
        {
          table.MaxCode[l] = 0;
        }
      }

      table.ValOffset[18] = 0;
      table.MaxCode[17] = ushort.MaxValue;
      p = 0;
      for (int l = 1; l <= 8; l++)
      {
        for (int i = 0; i < codeLengths[l - 1]; i++, p++)
        {
          FillByteLookupTable(huffCode[p], (byte)l, table.Values[p], table.LookaheadTable);
        }
      }
    }

    public static void FillByteLookupTable(int code, byte codeSize, byte value, JPEG_HuffmanTableEntry[] entries)
    {
      if (codeSize > 8)
        throw new InvalidDataException("CodeSize too large!");

      int freeBitCount = 8 - codeSize;
      code = (byte)(code << freeBitCount);
      for (int i = 0; i < (1 << freeBitCount); i++)
      {
        //JPEG_HuffmanTableEntry e = new JPEG_HuffmanTableEntry();
        //e.CodeSize = codeSize;
        //e.CodeValue = value;
        entries[code + i].CodeSize = codeSize;
        entries[code + i].CodeValue = value;
      }
    }

    public static ushort ParseDRI(ref ByteReader r) => r.ReadUInt16BE();
    
    // Define Quantization Table
    public static void ParseDQT(ref ByteReader r, JPEG_QuantTable[] tables , uint lq)
    { 
      byte PqTq = r.ReadByte();
      int precision = (PqTq & 16) == 1 ? 16 : 8;
      byte dest = (byte)(PqTq & 15);
      if (dest < 0 || dest > 3)
        throw new InvalidDataException("Invalid DQT destionation!");
      byte Dest = dest;
      JPEG_QuantTable t = tables[dest];
      t.Dest = dest;
      t.Precision = precision;

      int numOfQuantTables = ((int)lq - 2) / (65 + 64 * (PqTq & 16));
      // for now only support one quant talbe in the DQT segment
      if (numOfQuantTables != 1)
        throw new InvalidDataException("Currently supporting only 1 DQT table per segment!");

      // we will preserve zigzag order since we can use it easy when doing deq process
      ushort[] dqt = new ushort[64];
      if (precision == 8)
      {
        for (int i = 0; i < 64; i++)
        {
          dqt[i] = r.ReadByte();
        }
      }
      else
      {
        for (int i = 0; i < 64; i++)
        {
          dqt[i] = r.ReadUInt16BE();
        }
      }
      t.Values = dqt;
    }
    /// <summary>
    /// SOF0
    /// </summary>
    /// <param name="r"></param>
    public static void ParseBaselineDCTFrameHeaderData(ref ByteReader r, JPEG_FrameHeader h)
    {
      h.Precision = r.ReadByte();
      h.NumOfLines = r.ReadUInt16BE();
      h.NumOfSamplesPerLine = r.ReadUInt16BE();
      if (h.NumOfSamplesPerLine == 0)
        throw new InvalidDataException("NumOfSamplesPerLine can't be 0");
      h.NumOfImageComponentsInFrame = r.ReadByte();
      h.ComponentInfo = new JPEG_FrameComponentInfo[h.NumOfImageComponentsInFrame];
      for (int i = 0; i < h.NumOfImageComponentsInFrame; i++)
      {
        JPEG_FrameComponentInfo ci = new JPEG_FrameComponentInfo();
        ci.ComponentIdentifier = r.ReadByte();
        byte b = r.ReadByte();
        ci.HorizontalSamplingFactor = (byte)(b >> 4);
        ci.VerticalSamplingFactor = (byte)(b & 15);
        if (ci.HorizontalSamplingFactor < 1 || ci.HorizontalSamplingFactor > 4)
          throw new InvalidDataException("Invalid HorizontalSamplingFactor value!");
        if (ci.VerticalSamplingFactor < 1 || ci.VerticalSamplingFactor > 4)
          throw new InvalidDataException("Invalid VerticalSamplingFactor value!");
        ci.QuantTableDest = r.ReadByte();
        if (ci.QuantTableDest < 0 || ci.QuantTableDest > 3)
          throw new InvalidDataException("Invalid QuantTableDest value!");
        h.ComponentInfo[i] = ci;
      }
    }
  

    public static void TransposeBlock(ref Span<short> inputBlock, ref Span<short> temp)
    {
      for (int i = 0; i < 8; i++)
      {
        for (int j = 0; j < 8; j++)
        {
          temp[j * 8 + i] = inputBlock[i * 8 + j];
        }
      }

      // do MemCopy?
      for (int i = 0; i < 64; i++)
        inputBlock[i] = temp[i];
    }

    public static int GetLengthFieldByteSize(JPEG_MARKERS m) => m switch
    {
      JPEG_MARKERS.SOF0  => 2,
      JPEG_MARKERS.SOF1  => 2,
      JPEG_MARKERS.SOF2  => 2,
      JPEG_MARKERS.SOF3  => 2,
      JPEG_MARKERS.DHT   => 2,
      JPEG_MARKERS.SOF5  => 2,
      JPEG_MARKERS.SOF6  => 2,
      JPEG_MARKERS.SOF7  => 2,
      JPEG_MARKERS.JPG   => throw new NotImplementedException(),
      JPEG_MARKERS.SOF9  => 2,
      JPEG_MARKERS.SOF10 => 2,
      JPEG_MARKERS.SOF11 => 2,
      JPEG_MARKERS.DAC   => 2,
      JPEG_MARKERS.SOF13 => 2,
      JPEG_MARKERS.SOF14 => 2,
      JPEG_MARKERS.SOF15 => 2,
      JPEG_MARKERS.RST0  => 0,
      JPEG_MARKERS.RST1  => 0,
      JPEG_MARKERS.RST2  => 0,
      JPEG_MARKERS.RST3  => 0,
      JPEG_MARKERS.RST4  => 0,
      JPEG_MARKERS.RST5  => 0,
      JPEG_MARKERS.RST6  => 0,
      JPEG_MARKERS.RST7  => 0,
      JPEG_MARKERS.EOI   => 0,
      JPEG_MARKERS.SOI   => 0,
      JPEG_MARKERS.SOS   => 2,
      JPEG_MARKERS.DQT   => 2,
      JPEG_MARKERS.DNL   => 2,
      JPEG_MARKERS.DRI   => 2,
      JPEG_MARKERS.DHP   => 2,
      JPEG_MARKERS.EXP   => 2,
      JPEG_MARKERS.APP0  => 2,
      JPEG_MARKERS.APP1  => 2,
      JPEG_MARKERS.APP2  => 2,
      JPEG_MARKERS.APP3  => 2,
      JPEG_MARKERS.APP4  => 2,
      JPEG_MARKERS.APP5  => 2,
      JPEG_MARKERS.APP6  => 2,
      JPEG_MARKERS.APP7  => 2,
      JPEG_MARKERS.APP8  => 2,
      JPEG_MARKERS.APP9  => 2,
      JPEG_MARKERS.APP10 => 2,
      JPEG_MARKERS.APP11 => 2,
      JPEG_MARKERS.APP12 => 2,
      JPEG_MARKERS.APP13 => 2,
      JPEG_MARKERS.APP14 => 2,
      JPEG_MARKERS.APP15 => 2,
      JPEG_MARKERS.JPG0  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG1  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG2  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG3  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG4  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG5  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG6  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG7  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG8  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG9  => throw new NotImplementedException(),
      JPEG_MARKERS.JPG10 => throw new NotImplementedException(),
      JPEG_MARKERS.JPG11 => throw new NotImplementedException(),
      JPEG_MARKERS.JPG12 => throw new NotImplementedException(),
      JPEG_MARKERS.JPG13 => throw new NotImplementedException(),
      JPEG_MARKERS.COM   => 2,
      _ => throw new Exception("Unknown marker!")
    };

  }
}