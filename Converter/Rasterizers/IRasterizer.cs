using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.TTF;

namespace Converter.Rasterizers
{
  public interface IRasterizer
  {
    public void GetGlyphInfo(int codepoint, ref GlyphInfo glyphInfo);
    public (float scaleX, float scaleY) GetScale(int glyphIndex, double[,] textRenderingMatrix, float width);
    public void RasterizeGlyph(byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, ref GlyphInfo glyphInfo);
    void SetDefaultGlyphInfoValues(ref GlyphInfo glyphInfo);
    void STB_AddPoint(List<PointF>? points, int n, float x, float y);
    int STB_CloseShape(ref List<TTFVertex> vertices, int numOfVertices, bool wasOff, bool startOff, int sx, int sy, int scx, int scy, int cx, int cy);
    bool STB_CompareEdge(TTFEdge a, TTFEdge b);
    void STB_FillActiveEdgesNewV2(Span<float> scanline, Span<float> scanline2, int len, ref List<ActiveEdgeV2> activeEdges, float yTop);
    int STB_FindGlyphIndex(int unicodeCodepoint);
    List<PointF> STB_FlattenCurves(ref List<TTFVertex> vertices, int numOfVerts, float objspaceFlatness, ref List<int> windingLengths, ref int windingCount);
    void STB_GetCodepointBitmapBox(int unicodeCodepoint, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1);
    void STB_GetCodepointBitmapBoxSubpixel(int unicodeCodepoint, float scaleX, float scaleY, float shiftX, float shiftY, ref int ix0, ref int iy0, ref int ix1, ref int iy1);
    void STB_GetCodepointHMetrics(int unicodeCodepoint, ref int advanceWidth, ref int leftSideBearing);
    int STB_GetCodepointKernAdvance(int ch1, int ch2);
    void STB_GetFontBoundingBox(ref int x0, ref int y0, ref int x1, ref int y1);
    void STB_GetFontVMetrics(ref int ascent, ref int descent, ref int lineGap);
    void STB_GetGlyphBitmapBox(int glyphIndex, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1);
    void STB_GetGlyphBitmapBoxSubpixel(int glyphIndex, float scaleX, float scaleY, float shiftX, float shiftY, ref int ix0, ref int iy0, ref int ix1, ref int iy1);
    bool STB_GetGlyphBox(int glyphIndex, ref int xMin, ref int yMin, ref int xMax, ref int yMax);
    int STB_GetGlyphGPOSInfoAdvance(int glyphIndex1, int glyphIndex2);
    void STB_GetGlyphHMetrics(int glyphIndex, ref int advanceWidth, ref int leftSideBearing);
    int STB_GetGlyphKernAdvance(int glyphIndex1, int glyphIndex2);
    int STB_GetGlyphKernInfoAdvance(int glyphIndex1, int glyphIndex2);
    int STB_GetGlyphOffset(ref ReadOnlySpan<byte> buffer, int glyphIndex);
    int STB_GetGlyphShape(int glyphIndex, ref List<TTFVertex> vertices);
    int STB_GetGlyphShapeTT(int glyphIndex, ref List<TTFVertex> vertices);
    void STB_HandleClippedEdgeV2(Span<float> scanline, int x, ref ActiveEdgeV2 edge, float x0, float y0, float x1, float y1);
    void STB_InternalRasterize(ref BmpS result, ref List<PointF> points, ref List<int> wCount, int windings, float scaleX, float scaleY, float shiftX, float shiftY, int offX, int offY, bool invert);
    void STB_MakeCodepointBitmap(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, int unicodeCodepoint);
    void STB_MakeCodepointBitmapSubpixel(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, int unicodeCodepoint);
    void STB_MakeGlyphBitmapSubpixel(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, int glyphIndex);
    ActiveEdgeV2 STB_NewActiveEdgeV2(ref TTFEdge edge, int offX, float startPoint);
    float STB_PositionTrapezoidArea(float height, float tx0, float tx1, float bx0, float bx1);
    void STB_Rasterize(ref BmpS result, float flatnessInPixels, ref List<TTFVertex> vertices, int numOfVerts, float scaleX, float scaleY, float shiftX, float shiftY, int xOff, int yOff, bool invert);
    void STB_RasterizeSortedEdgesV2(ref BmpS result, List<TTFEdge> edges, int n, int vSubSample, int offX, int offY);
    float STB_ScaleForPixelHeight(double size);
    float STB_ScaleForPixelHeight(float size);
    void STB_SetVertex(ref TTFVertex vertex, byte type, int x, int y, int cx, int cy);
    float STB_SizedTrapezoidArea(float height, float topWidth, float bottomWidth);
    float STB_SizedTriangleArea(float height, float width);
    void STB_SortEdges(ref List<TTFEdge> edges, int n);
    void STB_SortEdgesInsSort(ref Span<TTFEdge> edges, int n);
    void STB_SortEdgesQuickSort(ref Span<TTFEdge> oRefEdges, int n, int prevIndex = 0);
    void STB_TesselateCubic(List<PointF> points, ref int numOfPoints, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float objspaceFlatnessSquared, int n);
    int STB_TesselateCurve(List<PointF> points, ref int numOfPoints, float x0, float y0, float x1, float y1, float x2, float y2, float objspaceFlatnessSquared, int n);
  }
}
