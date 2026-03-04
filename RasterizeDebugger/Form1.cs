using Converter;
using Converter.Converters;
using Converter.Converters.Image.TIFF;
using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.Parsers.PDF;
using Converter.Rasterizers;
using Converter.Writers.TIFF;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace RasterizeDebugger
{
  /// <summary>
  /// THIS ONLY WORKS FOR 1 BYTE PER PIXEL WRITERS 
  /// </summary>
  public partial class form_main : Form
  {
    OpenFileDialog _dialog;
    PDFGOInterpreter _interpreter;
    PDFFile _file;
    PdfParser _parser;
    MemoryStream _outStream;
    byte[] _imageData;
    int _imageDataStartPos;
    LocalState _localState;
    PDF_FontData _currFontData;
    IRasterizer _currRasterizer;
    bool _end;
    Matrix _transform = new Matrix();
    float _zoomScale = 1.0f;
    readonly float _scrollValue = 0.1f;
    TextureBrush _imageBrush;
    ZOOM _zoomMode = ZOOM.IN;
    bool center = false;


    readonly float MAX_ZOOM = 5f;
    readonly float MIN_ZOOM = 1f;
    enum ZOOM { IN, OUT }

    class ZoomStatePosition
    {
      public PointF p;
      public float zoomScale;
    }
    class LocalState
    {
      public string currentText;
      public int textIndex; //index into Literals list
      public int charIndex;
    }

    public form_main()
    {
      InitializeComponent();
      _dialog = new OpenFileDialog();
      _dialog.Filter = "PDF (*.pdf)|*.pdf";
      _dialog.RestoreDirectory = true;
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
      pb_mainImage.Focus();
      if (pb_mainImage.Focused == true && e.Delta != 0)
      {
        // Map the Form-centric mouse location to the PictureBox client coordinate system
        Point pictureBoxPoint = pb_mainImage.PointToClient(this.PointToScreen(e.Location));
        ZoomScroll(pictureBoxPoint, e.Delta > 0);
      }
    }

    private void btn_load_Click(object sender, EventArgs e)
    {
      DialogResult result = _dialog.ShowDialog();
      if (result == DialogResult.OK)
      {
        if (_dialog.SafeFileNames.Length != 1)
        {
          MessageBox.Show("Can select only 1 file at the time!");
          return;
        }

        pb_mainImage.Image = null;
        lbl_currFilename.Text = $"Filename: {_dialog.SafeFileName}";
        _file = new PDFFile();
        _parser = new PdfParser();
        _outStream?.Dispose();
        _outStream = new MemoryStream();
        PDF_Options pdfOptions = new PDF_Options();
        _parser.Parse(_file, _dialog.OpenFile(), _outStream, ref pdfOptions, true);

        byte[] rawContent = _file.PageInformation[0].ContentDict.RawStreamData;
        PDF_ResourceDict rDict = _file.PageInformation[0].ResourceDict;

        // TODO: make this later based on some mode, to be to convert to other _file formats as well
        // TODO: save in conveter later
        IConverter converter = _file.Target switch
        {
          TargetConversion.TIFF_BILEVEL => throw new NotImplementedException(),
          TargetConversion.TIFF_GRAYSCALE => new TIFFGrayscaleConverter(rDict.Font, rDict, _file.PageInformation[0], SourceConversion.PDF, new TIFFWriterOptions(), _outStream),
          TargetConversion.TIFF_PALLETE => throw new NotImplementedException(),
          TargetConversion.TIFF_RGB => throw new NotImplementedException(),
        };

        //pdfGo.ConvertToPixelData();
        _interpreter = new PDFGOInterpreter(rawContent, rDict, converter, true);
        lbl_contentLength.Text = rawContent.Length.ToString();
        _localState = new LocalState();
        ReadNextData();
        UpdateLabels();
        MemoryStream memoryStream = new MemoryStream();
        TIFFGrayscaleWriter writer = new TIFFGrayscaleWriter(memoryStream);
        TIFFWriterOptions options = new TIFFWriterOptions()
        {
          Height = converter.GetHeight(),
          Width = converter.GetWidth()
        };
        writer.WriteEmptyImage(ref options);
        _imageData = memoryStream.ToArray();
        _imageDataStartPos = writer.data.InitialImageDataOffset;

        pb_mainImage.Image = Image.FromStream(new MemoryStream(_imageData));
        pb_mainImage.Size = new Size(options.Width, options.Height);
        _end = false;

        _imageBrush = new TextureBrush(pb_mainImage.Image);
        _transform = new Matrix();
        _zoomScale = 1.0f;
      }
    }

    private void btn_nextText_Click(object sender, EventArgs e)
    {
      if (_end)
      {
        MessageBox.Show("End of content!");
        return;
      }
      ProcessText();
    }
    private void btn_nextChar_Click(object sender, EventArgs e)
    {
      if (_end)
      {
        MessageBox.Show("End of content!");
        return;
      }

      char c = _localState.currentText[_localState.charIndex];
      GlyphInfo gInfo = new GlyphInfo();

      _currFontData = _interpreter.GetFontDataFromKey(_interpreter._debugState.FontRef);
      _currRasterizer = _currFontData.Rasterizer;
      double[] widths = _currFontData.FontInfo.Widths;
      DrawGlyphAndUpdateGlyphInfo(c, ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex);

      UpdateImageDataAndPictureBox();
      UpdateLabels();
      _localState.charIndex++;

      if (_localState.charIndex >= _localState.currentText.Length)
      {
        _localState.textIndex++;
        if (_localState.textIndex >= _interpreter._debugState.Literals.Count)
        {
          // Load next from interpreer
          ReadNextData();
        }
        else
        {
          SetNextTextAndUpdateState();
        }
      }

    }

    public void ReadNextData()
    {
      _interpreter.ConvertToPixelData();
      if (_interpreter._debugState.Literals.Count == 0)
      {
        MessageBox.Show("End of content!");
        _end = true;
        return;
      }
      _localState.textIndex = 0;
      SetNextTextAndUpdateState();
    }
    public void UpdateLabels()
    {
      lbl_currPosition.Text = _interpreter._pos.ToString();
      lbl_readPos.Text = _interpreter._readPos.ToString();
      lbl_currentText.Text = _localState.currentText;
      lbl_currentChar.Text = _localState.currentText[_localState.charIndex].ToString();
    }
    public void UpdateImageDataAndPictureBox()
    {
      Array.ConstrainedCopy(_interpreter._outputBuffer, 0, _imageData, _imageDataStartPos, _interpreter._outputBuffer.Length);
      pb_mainImage.Image = Image.FromStream(new MemoryStream(_imageData));
      _imageBrush = new TextureBrush(pb_mainImage.Image);
    }

    private void btn_processAll_Click(object sender, EventArgs e)
    {
      // shorcut here just so we can create glyphinfo once if needed 
      if (_end)
      {
        MessageBox.Show("End of content!");
        return;
      }
      Stopwatch sw = new Stopwatch();
      sw.Start();
      while (!_end)
      {
        ProcessText(false);
      }
      long processTime = sw.ElapsedMilliseconds;
      UpdateImageDataAndPictureBox();
      UpdateLabels();
      lbl_charValue.Text = "END";
      lbl_glyphIndex.Text = "END";
      lbl_glyphName.Text = "END";
      sw.Stop();
      MessageBox.Show($"Process time: {processTime}ms. Total: {sw.ElapsedMilliseconds}ms!");

    }

    private void ProcessText(bool updateUI = true)
    {
      GlyphInfo gInfo = new GlyphInfo();
      _currFontData = _interpreter.GetFontDataFromKey(_interpreter._debugState.FontRef);
      _currRasterizer = _currFontData.Rasterizer;
      double[] widths = _currFontData.FontInfo.Widths;
      char c;
      for (int i = _localState.charIndex; i < _localState.currentText.Length; i++)
      {
        c = _localState.currentText[i];
        DrawGlyphAndUpdateGlyphInfo(c, ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex, updateUI);
      }
      _localState.charIndex = _localState.currentText.Length - 1;
      if (updateUI)
      {
        UpdateImageDataAndPictureBox();
        UpdateLabels();
      }
      _localState.textIndex++;
      if (_localState.textIndex >= _interpreter._debugState.Literals.Count)
      {
        // Load next from interpreer
        ReadNextData();
      }
      else
      {
        SetNextTextAndUpdateState();
      }
    }


    public void SetNextTextAndUpdateState()
    {
      // update local state
      _localState.charIndex = 0;
      _localState.currentText = _interpreter._debugState.Literals[_localState.textIndex].Literal;
      _interpreter._debugState.State.TextObject.TextMatrix[2, 0] -=
        (_interpreter._debugState.Literals[_localState.textIndex].PositionAdjustment / 1000f) *
        _interpreter._debugState.State.TextObject.TextMatrix[0, 0]
        * _interpreter._debugState.State.TextObject.FontScaleFactor;
    }

    public void DrawGlyphAndUpdateGlyphInfo(char c, ref GlyphInfo glyphInfo, IRasterizer rasterizer, PDFGI_DrawState state, PDF_FontData fd, double[] widths, string literal, int index, bool updateUI = true)
    {
      _interpreter.PDF_DrawGlyph(c, ref glyphInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex);
      if (!updateUI)
        return;
      lbl_charValue.Text = ((int)c).ToString();
      lbl_glyphIndex.Text = glyphInfo.Index.ToString();
      lbl_glyphName.Text = glyphInfo.Name;

    }

    private void pb_mainImage_Click(object sender, EventArgs e)
    {

    }

    private void panel1_Paint(object sender, PaintEventArgs e)
    {

    }

    private void ZoomScroll(Point location, bool zoomIn)
    {
      // Figure out what the new scale will be. Ensure the scale factor remains between
      // 1% and 200%
      float newScale = Math.Min(Math.Max(_zoomScale + (zoomIn ? _scrollValue : -_scrollValue), MIN_ZOOM), MAX_ZOOM);

      if (newScale != _zoomScale)
      {
        float adjust = newScale / _zoomScale;
        _zoomMode = newScale < _zoomScale ? ZOOM.OUT : ZOOM.IN;

        _zoomScale = newScale;
        _transform.Translate(-location.X, -location.Y, MatrixOrder.Append);

        // Scale view
        _transform.Scale(adjust, adjust, MatrixOrder.Append);

        // Translate origin back to original mouse point.
        _transform.Translate(location.X, location.Y, MatrixOrder.Append);

        Debug.WriteLine($"{_zoomMode.ToString()} scale: {_zoomScale}");

        pb_mainImage.Invalidate();
      }
    }

    private void pb_mainImage_Paint(object sender, PaintEventArgs e)
    {
      if (pb_mainImage.Image == null)
        return;
      Graphics g = e.Graphics;
      g.Transform = _transform;
      Rectangle c = e.ClipRectangle;
      int x = 0;
      int y = 0;
      var w = (int)Math.Round(c.Width / _zoomScale);
      var h = (int)Math.Round(c.Height / _zoomScale);
      x = (int)Math.Round(c.X / _zoomScale - _transform.OffsetX / _zoomScale);
      y = (int)Math.Round(c.Y / _zoomScale - _transform.OffsetY / _zoomScale);
      // This is garbage workaorund to deal with non centered unzoom........
      if (center && _zoomScale == MIN_ZOOM)
      {
        x = 0;
        y = 0;
        _imageBrush = new TextureBrush(pb_mainImage.Image);
        pb_mainImage.Image = Image.FromStream(new MemoryStream(_imageData));
        center = false;
        _transform = new Matrix();
        pb_mainImage.Invalidate();
        return;
      }
      e.Graphics.FillRectangle(_imageBrush, x, y, w - 1, h - 1);
    }

    private void btn_centerImage_Click(object sender, EventArgs e)
    {
      center = true;
      pb_mainImage.Invalidate();

    }
  }
}
