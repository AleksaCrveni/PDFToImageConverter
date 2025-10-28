using Converter.FileStructures.TTF;

namespace Converter.Rasterizers
{
  public interface IRasterizer
  {
    public (int glyphIndex, string glyphName) GetGlyphInfo(char c);
    public (float scaleX, float scaleY) GetScale(int glyphName, double[,] textRenderingMatrix, float width);



    void AddPoint(List<PointF>? points, int n, float x, float y);
    int CloseShape(ref List<TTFVertex> vertices, int numOfVertices, bool wasOff, bool startOff, int sx, int sy, int scx, int scy, int cx, int cy);
    bool CompareEdge(TTFEdge a, TTFEdge b);
    void FillActiveEdgesNewV2(Span<float> scanline, Span<float> scanline2, int len, ref List<ActiveEdgeV2> activeEdges, float yTop);
    int FindGlyphIndex(int unicodeCodepoint);
    List<PointF> FlattenCurves(ref List<TTFVertex> vertices, int numOfVerts, float objspaceFlatness, ref List<int> windingLengths, ref int windingCount);
    void GetCodepointBitmapBox(int unicodeCodepoint, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1);
    void GetCodepointBitmapBoxSubpixel(int unicodeCodepoint, float scaleX, float scaleY, float shiftX, float shiftY, ref int ix0, ref int iy0, ref int ix1, ref int iy1);
    void GetCodepointHMetrics(int unicodeCodepoint, ref int advanceWidth, ref int leftSideBearing);
    int GetCodepointKernAdvance(int ch1, int ch2);
    void GetFontBoundingBox(ref int x0, ref int y0, ref int x1, ref int y1);
    void GetFontVMetrics(ref int ascent, ref int descent, ref int lineGap);
    void GetGlyphBitmapBox(int glyphIndex, float scaleX, float scaleY, ref int ix0, ref int iy0, ref int ix1, ref int iy1);
    void GetGlyphBitmapBoxSubpixel(int glyphIndex, float scaleX, float scaleY, float shiftX, float shiftY, ref int ix0, ref int iy0, ref int ix1, ref int iy1);
    bool GetGlyphBox(int glyphIndex, ref int xMin, ref int yMin, ref int xMax, ref int yMax);
    int GetGlyphGPOSInfoAdvance(int glyphIndex1, int glyphIndex2);
    void GetGlyphHMetrics(int glyphIndex, ref int advanceWidth, ref int leftSideBearing);
    int GetGlyphKernAdvance(int glyphIndex1, int glyphIndex2);
    int GetGlyphKernInfoAdvance(int glyphIndex1, int glyphIndex2);
    int GetGlyphOffset(ref ReadOnlySpan<byte> buffer, int glyphIndex);
    int GetGlyphShape(int glyphIndex, ref List<TTFVertex> vertices);
    int GetGlyphShapeTT(int glyphIndex, ref List<TTFVertex> vertices);
    void HandleClippedEdgeV2(Span<float> scanline, int x, ref ActiveEdgeV2 edge, float x0, float y0, float x1, float y1);
    void InternalRasterize(ref BmpS result, ref List<PointF> points, ref List<int> wCount, int windings, float scaleX, float scaleY, float shiftX, float shiftY, int offX, int offY, bool invert);
    void MakeCodepointBitmap(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, int unicodeCodepoint);
    void MakeCodepointBitmapSubpixel(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, int unicodeCodepoint);
    void MakeGlyphBitmapSubpixel(ref byte[] bitmapArr, int byteOffset, int glyphWidth, int glyphHeight, int glyphStride, float scaleX, float scaleY, float shiftX, float shiftY, int glyphIndex);
    ActiveEdgeV2 NewActiveEdgeV2(ref TTFEdge edge, int offX, float startPoint);
    float PositionTrapezoidArea(float height, float tx0, float tx1, float bx0, float bx1);
    void Rasterize(ref BmpS result, float flatnessInPixels, ref List<TTFVertex> vertices, int numOfVerts, float scaleX, float scaleY, float shiftX, float shiftY, int xOff, int yOff, bool invert);
    void RasterizeSortedEdgesV2(ref BmpS result, List<TTFEdge> edges, int n, int vSubSample, int offX, int offY);
    float ScaleForPixelHeight(double size);
    float ScaleForPixelHeight(float size);
    void SetVertex(ref TTFVertex vertex, byte type, int x, int y, int cx, int cy);
    float SizedTrapezoidArea(float height, float topWidth, float bottomWidth);
    float SizedTriangleArea(float height, float width);
    void SortEdges(ref List<TTFEdge> edges, int n);
    void SortEdgesInsSort(ref Span<TTFEdge> edges, int n);
    void SortEdgesQuickSort(ref Span<TTFEdge> oRefEdges, int n, int prevIndex = 0);
    void TesselateCubic(List<PointF> points, ref int numOfPoints, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float objspaceFlatnessSquared, int n);
    int TesselateCurve(List<PointF> points, ref int numOfPoints, float x0, float y0, float x1, float y1, float x2, float y2, float objspaceFlatnessSquared, int n);
  }
}
