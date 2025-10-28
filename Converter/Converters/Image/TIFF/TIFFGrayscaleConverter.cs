using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.Rasterizers;
using Converter.Writers.TIFF;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
namespace Converter.Converters.Image.TIFF
{
  public class TIFFGrayscaleConverter : AConverter
  {
    private ITIFFWriter _writer;
    public TIFFGrayscaleConverter(List<PDF_FontData> fontDataRecords, PDF_ResourceDict rDict, PDF_PageInfo pInfo, SourceConversion source, TIFFWriterOptions options)
      : base(fontDataRecords, rDict, pInfo, source, options) { }

    public override void SetupConverter()
    {
      int width = (int)_pInfo.MediaBox.urX;
      int height = (int)_pInfo.MediaBox.urY;
      // make sure that buffer is big enough;
      if (_options.Width == 0 || _options.Width < width)
        _options.Width = width;

      if (_options.Height == 0 || _options.Height < height)
        _options.Height = height;

      _outputBuffer = new byte[_options.Height * _options.Width];
      _writer = new TIFFGrayscaleWriter("convertTest.tiff");
      TIFFWriterOptions tiffOptions = new TIFFWriterOptions()
      {
        Width = _options.Width,
        Height = _options.Height
      };
    }
    // I feel like i sohuld be calculating CTM inside of Interpreter?
    public override void PDF_DrawText(string fontKey, string textToWrite, PDFGI_DrawState state, int positionAdjustment = 0)
    {
      //workaround, use IRasterizer in covnerter
      PDF_FontData fd = GetFontDataFromKey(fontKey);
      IRasterizer activeParser = fd.Rasterizer;
      int[] activeWidths = fd.FontInfo.Widths;
      // skip for now 
      //if (fd.Key == "F2.0")
      //  return;
      // ascent and descent are defined in font descriptor, use those I think over getting i from  the font


      char c;
      int glyphIndex;
      int baseline = 0;
      state.TextObject.TextMatrix[2, 0] -= (positionAdjustment / 1000f) * state.TextObject.TextMatrix[0, 0] * state.TextObject.FontScaleFactor;
      // int res = ttfHelper.GetGlyphFromEncoding('S');
      for (int i = 0; i < textToWrite.Length; i++)
      {
        c = textToWrite[i];
        // TODO: use this instead of c, FIX 
        (int glyphIndex, string glyphName) glyph = activeParser.GetGlyphInfo(c);

        ComputeTextRenderingMatrix(state.TextObject, state.CTM, ref state.TextRenderingMatrix);

        // rounding makes it look a bit better?
        int X = (int)MathF.Round((float)state.TextRenderingMatrix[2, 0]);
        // because origin is bottom-left we have do bitmapHeight - , to get position on the top
        int Y = _options.Height - (int)(state.TextRenderingMatrix[2, 1]);

        int idx = (int)c - fd.FontInfo.FirstChar;
        float width = 0;
        if (idx < activeWidths.Length)
          width = activeWidths[idx] / 1000f;
        else
          width = fd.FontInfo.FontDescriptor.MissingWidth / 1000f;

        (float scaleX, float scaleY) s = activeParser.GetScale(glyph.glyphIndex, state.TextRenderingMatrix, width);
        #region asserts
        Debug.Assert(X > 0, $"X is negative at index {i}. Lit: {textToWrite}");
        Debug.Assert(Y > 0, $"Y is negative at index {i}. Lit: {textToWrite}");
        Debug.Assert(X < _options.Width, $"X must be within bounds.X: {X} - Width: {_options.Width}. Lit: {textToWrite}");
        Debug.Assert(Y < _options.Height, $"Y must be within bounds.Y: {Y} - Height: {_options.Height}. Lit: {textToWrite}");
        Debug.Assert(s.scaleX > 0, $"Scale factor X must be higher than 0! sfX: {s.scaleX}. Lit: {textToWrite}. Ind : {i}");
        Debug.Assert(s.scaleY > 0, $"Scale factor Y must be higher than 0! sfY: {s.scaleY}. Lit: {textToWrite}.Ind : {i}");
        #endregion asserts

        int ascent = fd.FontInfo.FontDescriptor.Ascent;
        int descent = fd.FontInfo.FontDescriptor.Ascent;
        int lineGap = 0;
        // missing data in font descriptor, read from font
        if (ascent == 0 || descent == 0)
          activeParser.GetFontVMetrics(ref ascent, ref descent, ref lineGap);
        ascent = (int)Math.Round(ascent * s.scaleY);
        descent = (int)Math.Round(descent * s.scaleY);
        lineGap = (int)Math.Round(lineGap * s.scaleY);
        int ax = 0; // charatcter width
        int lsb = 0; // left side bearing

        activeParser.GetCodepointHMetrics(c, ref ax, ref lsb);

        int c_x0 = 0;
        int c_y0 = 0;
        int c_x1 = 0;
        int c_y1 = 0;
        activeParser.GetCodepointBitmapBox(c, s.scaleX, s.scaleY, ref c_x0, ref c_y0, ref c_x1, ref c_y1);

        // char height - different than bounding box height
        int y = Y + c_y0;

        if (y < 0)
          y = 0;
        int glyphWidth = c_x1 - c_x0; // I think that this should be replaced from value in Widths array
        int glyphHeight = c_y1 - c_y0;

        int byteOffset = X + (y * _options.Width);
        activeParser.MakeCodepointBitmap(ref _outputBuffer, byteOffset, glyphWidth, glyphHeight, _options.Width, s.scaleX, s.scaleY, c);
        // kerning

        //int kern;
        //kern = parser.GetCodepointKernAdvance(textToTranslate[i], textToTranslate[i + 1]);
        //x += (int)Math.Round(kern * scaleFactor);


        double advanceX = width * state.TextObject.FontScaleFactor + state.TextObject.Tc;
        double advanceY = 0 + state.TextObject.FontScaleFactor; // when advance Y not 0? when fonts are vertical??
        if (c == ' ')
          advanceX += state.TextObject.Tw;
        advanceX *= state.TextObject.Th;
        // TODO: this really depends on what type of CTM it is. i.e is there shear, transaltion, rotation etc
        // I should detect this and save state somewhere
        // for now just support translate and scale
        // NOTE: actually I think I can just multiply matrix, and this is done to avoid matrix multiplciation
        state.TextObject.TextMatrix[2, 0] = advanceX * state.TextRenderingMatrix[0, 0] + state.TextObject.TextMatrix[2, 0];
        state.TextObject.TextMatrix[2, 1] = 0 * state.TextObject.TextMatrix[1, 1] + state.TextObject.TextMatrix[2, 1];
      }


    }

    public override void Save()
    {

      TIFFWriterOptions tiffOptions = new TIFFWriterOptions()
      {
        Width = _options.Width,
        Height = _options.Height
      };
      _writer.WriteImageWithBuffer(ref tiffOptions, _outputBuffer);
    }
  }
}

