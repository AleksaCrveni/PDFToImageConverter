using Converter;
using Converter.Converters;
using Converter.Converters.Image.TIFF;
using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.TIFF;
using Converter.Parsers.PDF;
using Converter.Rasterizers;
using Converter.Utils;
using Converter.Writers.TIFF;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text;
using static System.Windows.Forms.AxHost;

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
    bool _zoomChanged = false;
    readonly float MAX_ZOOM = 5f;
    readonly float MIN_ZOOM = 1f;
    string _lastFontRef;
    uint _totalStringLiteralCount;

    enum ZOOM { IN, OUT }
    string _fileFullPath;
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
      tview_fontInfo.NodeMouseDoubleClick += TreeNodeMouseClickEventHandler;
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
        _zoomChanged = true;
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
        _fileFullPath = _dialog.FileName;
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
        ReadNextData(true);
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
        panel2.Size = new Size(options.Width, options.Height);
        pb_mainImage.Size = new Size(options.Width, options.Height);
        pb_mainImage.Image = Image.FromStream(new MemoryStream(_imageData));


        _end = false;

        _imageBrush = new TextureBrush(pb_mainImage.Image);
        _transform = new Matrix();
        _zoomScale = 1.0f;
        _lastFontRef = _interpreter._debugState.FontRef;
        _currFontData = _interpreter.GetFontDataFromKey(_interpreter._debugState.FontRef);
        UpdateFontInfoTreeView();
        _totalStringLiteralCount = 0;
        lbl_literalNumber.Text = _totalStringLiteralCount.ToString();
        lbl_charValue.Text = "NULL";
        lbl_glyphIndex.Text = "NULL";
        lbl_glyphName.Text = "NULL";
        lbl_currentChar.Text = "NULL";
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
      if (_lastFontRef != _interpreter._debugState.FontRef)
      {
        UpdateFontInfoTreeView();
        _lastFontRef = _interpreter._debugState.FontRef;
      }
      _currRasterizer = _currFontData.Rasterizer;
      double[] widths = _currFontData.FontInfo.Widths;


      DrawGlyphAndUpdateGlyphInfo(c, ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex, true);

      UpdateImageDataAndPictureBox();
      UpdateLabels();
      _localState.charIndex++;

      if (_localState.charIndex >= _localState.currentText.Length)
      {
        _localState.textIndex++;
        if (_localState.textIndex >= _interpreter._debugState.Literals.Count)
        {
          // Load next from interpreer
          ReadNextData(true);
        }
        else
        {
          SetNextTextAndUpdateState(true);
        }
      }

    }

    public void ReadNextData(bool updateUI)
    {
      _interpreter.ConvertToPixelData();
      if (_interpreter._debugState.Literals.Count == 0)
      {
        MessageBox.Show("End of content!");
        _end = true;
        return;
      }
      _localState.textIndex = 0;
      SetNextTextAndUpdateState(updateUI);
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
      try
      {
        sw.Start();
        while (!_end)
        {
          ProcessText(false);
        }
        
      } catch(Exception ex)
      {

      }
      finally
      {
        long processTime = sw.ElapsedMilliseconds;
        UpdateImageDataAndPictureBox();
        UpdateLabels();
        lbl_charValue.Text = "END";
        lbl_glyphIndex.Text = "END";
        lbl_glyphName.Text = "END";
        lbl_literalNumber.Text = _totalStringLiteralCount.ToString();
        sw.Stop();
        MessageBox.Show($"Process time: {processTime}ms. Total: {sw.ElapsedMilliseconds}ms!");
      }
      
      

    }

    private void ProcessText(bool updateUI = true)
    {
      GlyphInfo gInfo = new GlyphInfo();

      _currFontData = _interpreter.GetFontDataFromKey(_interpreter._debugState.FontRef);

      _currRasterizer = _currFontData.Rasterizer;
      double[] widths = _currFontData.FontInfo.Widths;
      char? c;
      if (_currFontData.FontInfo.SubType == PDF_FontType.Type0)
      {
        char CID = (char)RasterHelper.ReadUintFromHex(_localState.currentText);
        c = _currRasterizer.FindCharFromCID(CID);
        if (c != null)
        {
          DrawGlyphAndUpdateGlyphInfo((char)CID, ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex, updateUI, CID);
        } 
        else
        {
          List<char> ligature = _currRasterizer.FindLigatureFromCID(CID);
          if (ligature.Count == 0)
          {
            DrawGlyphAndUpdateGlyphInfo((char)0, ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex, updateUI, CID);
          }
          else
          {
            for (int i = 0; i < ligature.Count; i++)
            {
              // thgis will need fixing
              DrawGlyphAndUpdateGlyphInfo((char)ligature[i], ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex, updateUI, CID);
            }
          }
        }  
      }
      else
      {
        for (int i = _localState.charIndex; i < _localState.currentText.Length; i++)
        {
          c = _localState.currentText[i];
          DrawGlyphAndUpdateGlyphInfo((char)c, ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex, updateUI);
        }
      }
      
      _localState.charIndex = _localState.currentText.Length - 1;
      if (updateUI)
      {
        UpdateImageDataAndPictureBox();
        UpdateLabels();
        if (_lastFontRef != _interpreter._debugState.FontRef)
        {
          UpdateFontInfoTreeView();
          _lastFontRef = _interpreter._debugState.FontRef;
        }
      }
      _localState.textIndex++;
      if (_localState.textIndex >= _interpreter._debugState.Literals.Count)
      {
        // Load next from interpreer
        ReadNextData(updateUI);
      }
      else
      {
        SetNextTextAndUpdateState(updateUI);
      }
    }


    public void SetNextTextAndUpdateState(bool updateUI)
    {
      // update local state
      _localState.charIndex = 0;
      _localState.currentText = _interpreter._debugState.Literals[_localState.textIndex].Literal;
      _totalStringLiteralCount++;
      if (updateUI)
        lbl_literalNumber.Text = _totalStringLiteralCount.ToString();
      _interpreter._debugState.State.TextObject.TextMatrix[2, 0] -=
        (_interpreter._debugState.Literals[_localState.textIndex].PositionAdjustment / 1000f) *
        _interpreter._debugState.State.TextObject.TextMatrix[0, 0]
        * _interpreter._debugState.State.TextObject.FontScaleFactor;
    }

    public void DrawGlyphAndUpdateGlyphInfo(char c, ref GlyphInfo glyphInfo, IRasterizer rasterizer, PDFGI_DrawState state, PDF_FontData fd, double[] widths, string literal, int index, bool updateUI, char CID = ' ')
    {
      _interpreter.PDF_DrawGlyph(c, ref glyphInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex, CID);
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
      if (_zoomChanged && _zoomScale == MIN_ZOOM)
      {
        x = 0;
        y = 0;
        _imageBrush = new TextureBrush(pb_mainImage.Image);
        pb_mainImage.Image = Image.FromStream(new MemoryStream(_imageData));
        _transform = new Matrix();
        pb_mainImage.Invalidate();
        _zoomChanged = false;
        return;
      }
      e.Graphics.FillRectangle(_imageBrush, x, y, w - 1, h - 1);
    }

    // not most efficient since we always recreate tree b ut w/e
    
    private void UpdateFontInfoTreeView()
    {
      tview_fontInfo.BeginUpdate();
      tview_fontInfo.Nodes.Clear();
      tview_fontInfo.Nodes.Add(_currFontData.Key);
      tview_fontInfo.Nodes[0].Nodes.Add($"Name: {_currFontData.FontInfo.Name}");
      tview_fontInfo.Nodes[0].Nodes.Add($"BaseFont: {_currFontData.FontInfo.BaseFont}");
      tview_fontInfo.Nodes[0].Nodes.Add($"Type: {_currFontData.FontInfo.SubType}");
      if (_currFontData.FontInfo.SubType == PDF_FontType.Type0)
      {
        CIDFontDictionary fontDict = _currFontData.FontInfo.DescendantFontsInfo[0].DescendantDict;
        tview_fontInfo.Nodes[0].Nodes.Add($"Widths");
        foreach (KeyValuePair<int, int> kvp in fontDict.W)
        {
          tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"CID: {kvp.Key} Width: {kvp.Value}");
        }
        
        tview_fontInfo.Nodes[0].Nodes.Add("FontDescriptor");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"SubType: {fontDict.Subtype}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontName: {fontDict.FontDescriptor.FontName}");
        if (fontDict.CIDToGIDMap == null)
        {
          tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"CIDToGIDMap: {fontDict.CIDToGIDMapName}");
        }
        else
        {
          tview_fontInfo.Nodes[0].LastNode.Nodes.Add("CIDToGIDMap");
          tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add(Encoding.Default.GetString(fontDict.CIDToGIDMap.RawStreamData));
        }

        PDF_CID_CMAP cmap = _currFontData.FontInfo.DescendantFontsInfo[0].Cmap;
        if (cmap != null)
        {
          tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"CMAP");
          foreach (KeyValuePair<char, char> kvp in cmap.Cmap)
          {
            tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"CID: {(int)kvp.Key} Index-Char: {(int)kvp.Value}-{kvp.Value}");
          }
          tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Ligatures");
          foreach (KeyValuePair<char, List<char>> kvp in cmap.LigatureCmap)
          {
            tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"CID: {(int)kvp.Key}");
            foreach (char c in kvp.Value)
            {
              tview_fontInfo.Nodes[0].LastNode.LastNode.LastNode.Nodes.Add($"Index-Char: {(int)c}-{c}");
            }
          }
        }

        // TODO: Add more info 
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"CIDSystemInfo Supplement: {fontDict.CIDSystemInfo.Supplement}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"CIDSystemInfo Registry: {fontDict.CIDSystemInfo.Registry}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"CIDSystemInfo Ordering: {fontDict.CIDSystemInfo.Ordering}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontFamily: {fontDict.FontDescriptor.FontFamily}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontStretch: {fontDict.FontDescriptor.FontStretch}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontWeight: {fontDict.FontDescriptor.FontWeight}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Flags: {fontDict.FontDescriptor.Flags.ToString()}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontBBox");
        tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"llX: {fontDict.FontDescriptor.FontBBox.llX}");
        tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"llY: {fontDict.FontDescriptor.FontBBox.llY}");
        tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"urX: {fontDict.FontDescriptor.FontBBox.urX}");
        tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"urY: {fontDict.FontDescriptor.FontBBox.urY}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"ItalicAngle: {fontDict.FontDescriptor.ItalicAngle}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Ascent: {fontDict.FontDescriptor.Ascent}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Descent: {fontDict.FontDescriptor.Descent}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Leading: {fontDict.FontDescriptor.Leading}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"CapHeight: {fontDict.FontDescriptor.CapHeight}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"XHeight: {fontDict.FontDescriptor.XHeight}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"StemV: {fontDict.FontDescriptor.StemV}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"StemH: {fontDict.FontDescriptor.StemH}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"AvgWidth: {fontDict.FontDescriptor.AvgWidth}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"MaxWidth: {fontDict.FontDescriptor.MaxWidth}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"MissingWidth: {fontDict.FontDescriptor.MissingWidth}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"DescendantFontsIR: {_currFontData.FontInfo.DescendantFontsIR[0].ojbIndex} {_currFontData.FontInfo.DescendantFontsIR[0].generation}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"ToUnicodeIR: {_currFontData.FontInfo.ToUnicodeIR.objIndex} {_currFontData.FontInfo.ToUnicodeIR.generation}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"BaseEncoding: {_currFontData.FontInfo.EncodingData.BaseEncoding}");

      }
      else
      {
        tview_fontInfo.Nodes[0].Nodes.Add($"FirstChar: {_currFontData.FontInfo.FirstChar}");
        tview_fontInfo.Nodes[0].Nodes.Add($"LastChar: {_currFontData.FontInfo.LastChar}");
        tview_fontInfo.Nodes[0].Nodes.Add($"Widths");
        for (int i = 0; i < _currFontData.FontInfo.Widths.Length; i++)
        {
          tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Index: {i} Width: {_currFontData.FontInfo.Widths[i]}");
        }
        tview_fontInfo.Nodes[0].Nodes.Add("FontDescriptor");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontName: {_currFontData.FontInfo.FontDescriptor.FontName}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontFamily: {_currFontData.FontInfo.FontDescriptor.FontFamily}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontStretch: {_currFontData.FontInfo.FontDescriptor.FontStretch}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontWeight: {_currFontData.FontInfo.FontDescriptor.FontWeight}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Flags: {_currFontData.FontInfo.FontDescriptor.Flags.ToString()}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"FontBBox");
        tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"llX: {_currFontData.FontInfo.FontDescriptor.FontBBox.llX}");
        tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"llY: {_currFontData.FontInfo.FontDescriptor.FontBBox.llY}");
        tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"urX: {_currFontData.FontInfo.FontDescriptor.FontBBox.urX}");
        tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"urY: {_currFontData.FontInfo.FontDescriptor.FontBBox.urY}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"ItalicAngle: {_currFontData.FontInfo.FontDescriptor.ItalicAngle}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Ascent: {_currFontData.FontInfo.FontDescriptor.Ascent}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Descent: {_currFontData.FontInfo.FontDescriptor.Descent}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Leading: {_currFontData.FontInfo.FontDescriptor.Leading}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"CapHeight: {_currFontData.FontInfo.FontDescriptor.CapHeight}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"XHeight: {_currFontData.FontInfo.FontDescriptor.XHeight}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"StemV: {_currFontData.FontInfo.FontDescriptor.StemV}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"StemH: {_currFontData.FontInfo.FontDescriptor.StemH}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"AvgWidth: {_currFontData.FontInfo.FontDescriptor.AvgWidth}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"MaxWidth: {_currFontData.FontInfo.FontDescriptor.MaxWidth}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"MissingWidth: {_currFontData.FontInfo.FontDescriptor.MissingWidth}");
        tview_fontInfo.Nodes[0].Nodes.Add("EncodingData");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"BaseEncoding: {_currFontData.FontInfo.EncodingData.BaseEncoding}");
        tview_fontInfo.Nodes[0].LastNode.Nodes.Add($"Differences");
        for (int i = 0; i < _currFontData.FontInfo.EncodingData.Differences.Count; i++)
        {
          tview_fontInfo.Nodes[0].LastNode.LastNode.Nodes.Add($"Codepoint: {_currFontData.FontInfo.EncodingData.Differences[i].code} Value: {_currFontData.FontInfo.EncodingData.Differences[i].val}");
        }
      }

      tview_fontInfo.Nodes[0].Expand();
      tview_fontInfo.EndUpdate();
    }

    private void btn_upTo_Click(object sender, EventArgs e)
    {
      // shorcut here just so we can create glyphinfo once if needed 
      if (_end)
      {
        MessageBox.Show("End of content!");
        return;
      }

      bool valid = UInt32.TryParse(txb_literalNumber.Text, out uint target);
      string err = string.Empty;

      if (txb_literalNumber.Text.Trim().Length == 0)
        err = "Value must be set!";

      if (txb_literalNumber.Text.Contains('-'))
        err = "Number can't be negative!";

      if (!valid)
        err = "Invalid value. Target must be positive integer!";

      if (target <= _totalStringLiteralCount && valid)
        err = "Target must be bigger than current position (count)!";

      if (err != string.Empty)
      {
        MessageBox.Show(err);
        return;
      }
      while (!_end && target > _totalStringLiteralCount)
      {
        ProcessText(false);
      }

      UpdateImageDataAndPictureBox();
      UpdateLabels();
      lbl_literalNumber.Text = _totalStringLiteralCount.ToString();
    }

    private void btn_fontPlayground_Click(object sender, EventArgs e)
    {
      Playground p;
      if (_file == null)
      {
        p = new Playground();
      } else
      {
        PdfParser parser = new PdfParser();
        PDF_Options options = new PDF_Options();
        PDFFile file = parser.Parse(_fileFullPath, ref options, true);
        p = new Playground(file);
        
      }
      p.Show();
    }

    private void TreeNodeMouseClickEventHandler(object? sender, TreeNodeMouseClickEventArgs e)
    {
      TreeView node = (TreeView)sender;
      DataViewer dw = new DataViewer(node.SelectedNode.Text);
      dw.Show();
    }

  }
}
