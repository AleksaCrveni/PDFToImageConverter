using Converter.FileStructures.PDF;
using Converter.Parsers.PostScript;
using System;
using System.Collections.Generic;
using System.Text;

namespace RasterizeDebugger
{
  public static class Helper
  {
    public static void FillTreeWithFontInfo(TreeView tw, PDF_FontData fontData)
    {
      
      tw.BeginUpdate();
      tw.Nodes.Clear();
      tw.Nodes.Add(fontData.Key);
      tw.Nodes[0].Nodes.Add($"Name: {fontData.FontInfo.Name}");
      tw.Nodes[0].Nodes.Add($"BaseFont: {fontData.FontInfo.BaseFont}");
      tw.Nodes[0].Nodes.Add($"Type: {fontData.FontInfo.SubType}");
      if (fontData.FontInfo.SubType == PDF_FontType.Type0)
      {
        CIDFontDictionary fontDict = fontData.FontInfo.DescendantFontsInfo[0].DescendantDict;
        tw.Nodes[0].Nodes.Add($"Widths");
        foreach (KeyValuePair<int, int> kvp in fontDict.W)
        {
          tw.Nodes[0].LastNode.Nodes.Add($"CID: {kvp.Key} Width: {kvp.Value}");
        }

        tw.Nodes[0].Nodes.Add("FontDescriptor");
        tw.Nodes[0].LastNode.Nodes.Add($"SubType: {fontDict.Subtype}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontName: {fontDict.FontDescriptor.FontName}");
        if (fontDict.CIDToGIDMap == null)
        {
          tw.Nodes[0].LastNode.Nodes.Add($"CIDToGIDMap: {fontDict.CIDToGIDMapName}");
        }
        else
        {
          tw.Nodes[0].LastNode.Nodes.Add("CIDToGIDMap");
          tw.Nodes[0].LastNode.LastNode.Nodes.Add(Encoding.Default.GetString(fontDict.CIDToGIDMap.RawStreamData));
        }

        PDF_CID_CMAP cmap = fontData.FontInfo.DescendantFontsInfo[0].Cmap;
        if (cmap != null)
        {
          tw.Nodes[0].LastNode.Nodes.Add($"CMAP");
          foreach (KeyValuePair<char, char> kvp in cmap.Cmap)
          {
            tw.Nodes[0].LastNode.LastNode.Nodes.Add($"CID: {(int)kvp.Key} Index-Char: {(int)kvp.Value}-{kvp.Value}");
          }
          tw.Nodes[0].LastNode.Nodes.Add($"Ligatures");
          foreach (KeyValuePair<char, List<char>> kvp in cmap.LigatureCmap)
          {
            tw.Nodes[0].LastNode.LastNode.Nodes.Add($"CID: {(int)kvp.Key}");
            foreach (char c in kvp.Value)
            {
              tw.Nodes[0].LastNode.LastNode.LastNode.Nodes.Add($"Index-Char: {(int)c}-{c}");
            }
          }
        }

        // TODO: Add more info 
        tw.Nodes[0].LastNode.Nodes.Add($"CIDSystemInfo Supplement: {fontDict.CIDSystemInfo.Supplement}");
        tw.Nodes[0].LastNode.Nodes.Add($"CIDSystemInfo Registry: {fontDict.CIDSystemInfo.Registry}");
        tw.Nodes[0].LastNode.Nodes.Add($"CIDSystemInfo Ordering: {fontDict.CIDSystemInfo.Ordering}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontFamily: {fontDict.FontDescriptor.FontFamily}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontStretch: {fontDict.FontDescriptor.FontStretch}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontWeight: {fontDict.FontDescriptor.FontWeight}");
        tw.Nodes[0].LastNode.Nodes.Add($"Flags: {fontDict.FontDescriptor.Flags.ToString()}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontBBox");
        tw.Nodes[0].LastNode.LastNode.Nodes.Add($"llX: {fontDict.FontDescriptor.FontBBox.llX}");
        tw.Nodes[0].LastNode.LastNode.Nodes.Add($"llY: {fontDict.FontDescriptor.FontBBox.llY}");
        tw.Nodes[0].LastNode.LastNode.Nodes.Add($"urX: {fontDict.FontDescriptor.FontBBox.urX}");
        tw.Nodes[0].LastNode.LastNode.Nodes.Add($"urY: {fontDict.FontDescriptor.FontBBox.urY}");
        tw.Nodes[0].LastNode.Nodes.Add($"ItalicAngle: {fontDict.FontDescriptor.ItalicAngle}");
        tw.Nodes[0].LastNode.Nodes.Add($"Ascent: {fontDict.FontDescriptor.Ascent}");
        tw.Nodes[0].LastNode.Nodes.Add($"Descent: {fontDict.FontDescriptor.Descent}");
        tw.Nodes[0].LastNode.Nodes.Add($"Leading: {fontDict.FontDescriptor.Leading}");
        tw.Nodes[0].LastNode.Nodes.Add($"CapHeight: {fontDict.FontDescriptor.CapHeight}");
        tw.Nodes[0].LastNode.Nodes.Add($"XHeight: {fontDict.FontDescriptor.XHeight}");
        tw.Nodes[0].LastNode.Nodes.Add($"StemV: {fontDict.FontDescriptor.StemV}");
        tw.Nodes[0].LastNode.Nodes.Add($"StemH: {fontDict.FontDescriptor.StemH}");
        tw.Nodes[0].LastNode.Nodes.Add($"AvgWidth: {fontDict.FontDescriptor.AvgWidth}");
        tw.Nodes[0].LastNode.Nodes.Add($"MaxWidth: {fontDict.FontDescriptor.MaxWidth}");
        tw.Nodes[0].LastNode.Nodes.Add($"MissingWidth: {fontDict.FontDescriptor.MissingWidth}");
        tw.Nodes[0].LastNode.Nodes.Add($"DescendantFontsIR: {fontData.FontInfo.DescendantFontsIR[0].ojbIndex} {fontData.FontInfo.DescendantFontsIR[0].generation}");
        tw.Nodes[0].LastNode.Nodes.Add($"ToUnicodeIR: {fontData.FontInfo.ToUnicodeIR.objIndex} {fontData.FontInfo.ToUnicodeIR.generation}");
        tw.Nodes[0].LastNode.Nodes.Add($"BaseEncoding: {fontData.FontInfo.EncodingData.BaseEncoding}");

      }
      else
      {
        tw.Nodes[0].Nodes.Add($"FirstChar: {fontData.FontInfo.FirstChar}");
        tw.Nodes[0].Nodes.Add($"LastChar: {fontData.FontInfo.LastChar}");
        tw.Nodes[0].Nodes.Add($"Widths");
        for (int i = 0; i < fontData.FontInfo.Widths.Length; i++)
        {
          tw.Nodes[0].LastNode.Nodes.Add($"Index: {i} Width: {fontData.FontInfo.Widths[i]}");
        }
        tw.Nodes[0].Nodes.Add("FontDescriptor");
        tw.Nodes[0].LastNode.Nodes.Add($"FontName: {fontData.FontInfo.FontDescriptor.FontName}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontFamily: {fontData.FontInfo.FontDescriptor.FontFamily}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontStretch: {fontData.FontInfo.FontDescriptor.FontStretch}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontWeight: {fontData.FontInfo.FontDescriptor.FontWeight}");
        tw.Nodes[0].LastNode.Nodes.Add($"Flags: {fontData.FontInfo.FontDescriptor.Flags.ToString()}");
        tw.Nodes[0].LastNode.Nodes.Add($"FontBBox");
        tw.Nodes[0].LastNode.LastNode.Nodes.Add($"llX: {fontData.FontInfo.FontDescriptor.FontBBox.llX}");
        tw.Nodes[0].LastNode.LastNode.Nodes.Add($"llY: {fontData.FontInfo.FontDescriptor.FontBBox.llY}");
        tw.Nodes[0].LastNode.LastNode.Nodes.Add($"urX: {fontData.FontInfo.FontDescriptor.FontBBox.urX}");
        tw.Nodes[0].LastNode.LastNode.Nodes.Add($"urY: {fontData.FontInfo.FontDescriptor.FontBBox.urY}");
        tw.Nodes[0].LastNode.Nodes.Add($"ItalicAngle: {fontData.FontInfo.FontDescriptor.ItalicAngle}");
        tw.Nodes[0].LastNode.Nodes.Add($"Ascent: {fontData.FontInfo.FontDescriptor.Ascent}");
        tw.Nodes[0].LastNode.Nodes.Add($"Descent: {fontData.FontInfo.FontDescriptor.Descent}");
        tw.Nodes[0].LastNode.Nodes.Add($"Leading: {fontData.FontInfo.FontDescriptor.Leading}");
        tw.Nodes[0].LastNode.Nodes.Add($"CapHeight: {fontData.FontInfo.FontDescriptor.CapHeight}");
        tw.Nodes[0].LastNode.Nodes.Add($"XHeight: {fontData.FontInfo.FontDescriptor.XHeight}");
        tw.Nodes[0].LastNode.Nodes.Add($"StemV: {fontData.FontInfo.FontDescriptor.StemV}");
        tw.Nodes[0].LastNode.Nodes.Add($"StemH: {fontData.FontInfo.FontDescriptor.StemH}");
        tw.Nodes[0].LastNode.Nodes.Add($"AvgWidth: {fontData.FontInfo.FontDescriptor.AvgWidth}");
        tw.Nodes[0].LastNode.Nodes.Add($"MaxWidth: {fontData.FontInfo.FontDescriptor.MaxWidth}");
        tw.Nodes[0].LastNode.Nodes.Add($"MissingWidth: {fontData.FontInfo.FontDescriptor.MissingWidth}");
        tw.Nodes[0].Nodes.Add("EncodingData");
        tw.Nodes[0].LastNode.Nodes.Add($"BaseEncoding: {fontData.FontInfo.EncodingData.BaseEncoding}");
        tw.Nodes[0].LastNode.Nodes.Add($"Differences");
        for (int i = 0; i < fontData.FontInfo.EncodingData.Differences.Count; i++)
        {
          tw.Nodes[0].LastNode.LastNode.Nodes.Add($"Codepoint: {fontData.FontInfo.EncodingData.Differences[i].code} Value: {fontData.FontInfo.EncodingData.Differences[i].val}");
        }
      }

      tw.Nodes[0].Expand();
      tw.EndUpdate();
    }
  }
}
