using Converter;
using Converter.Converters;
using Converter.Converters.Image.TIFF;
using Converter.FileStructures.General;
using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.Parsers.PDF;
using Converter.Rasterizers;
using Converter.Writers.TIFF;
using static System.Windows.Forms.AxHost;

namespace RasterizeDebugger
{
  /// <summary>
  /// THIS ONLY WORKS FOR 1 BYTE PER PIXEL WRITERS 
  /// </summary>
  public partial class Form1 : Form
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
    class LocalState
    {
      public string currentText;
      public int textIndex; //index into Literals list
      public int charIndex;
    }

    public Form1()
    {
      InitializeComponent();
      _dialog = new OpenFileDialog();
      _dialog.Filter = "PDF (*.pdf)|*.pdf";
      _dialog.RestoreDirectory = true;
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      //PdfParser parser = new();
      //PDFFile file = new PDFFile();
      //MemoryStream memoryStream = new MemoryStream();
      //PDF_Options options = new PDF_Options();
      //parser.Parse(file, File.OpenRead(Files.BaseDocFilePath), memoryStream, ref options);
      //Image image = Image.FromStream(memoryStream);
      //pb_mainImage.Image = image;
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
      _interpreter.PDF_DrawGlyph(c, ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex);

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
          // update local state
          _localState.charIndex = 0;
          _localState.currentText = _interpreter._debugState.Literals[_localState.textIndex].Literal;
          _interpreter._debugState.State.TextObject.TextMatrix[2, 0] -=
            (_interpreter._debugState.Literals[_localState.textIndex].PositionAdjustment / 1000f) *
            _interpreter._debugState.State.TextObject.TextMatrix[0, 0]
            * _interpreter._debugState.State.TextObject.FontScaleFactor;
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

      _localState.charIndex = 0;
      _localState.textIndex = 0;
      _localState.currentText = _interpreter._debugState.Literals[0].Literal;
      _interpreter._debugState.State.TextObject.TextMatrix[2, 0] -=
         (_interpreter._debugState.Literals[_localState.textIndex].PositionAdjustment / 1000f) *
         _interpreter._debugState.State.TextObject.TextMatrix[0, 0]
         * _interpreter._debugState.State.TextObject.FontScaleFactor;
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
    }

    private void btn_processAll_Click(object sender, EventArgs e)
    {
      // shorcut here just so we can create glyphinfo once if needed 
      if (_end)
      {
        MessageBox.Show("End of content!");
        return;
      }
      
      while (!_end)
      {
        ProcessText();
      }

      
    }

    private void ProcessText()
    {
      GlyphInfo gInfo = new GlyphInfo();
      _currFontData = _interpreter.GetFontDataFromKey(_interpreter._debugState.FontRef);
      _currRasterizer = _currFontData.Rasterizer;
      double[] widths = _currFontData.FontInfo.Widths;
      char c;
      for (int i = _localState.charIndex; i < _localState.currentText.Length; i++)
      {
        c = _localState.currentText[i];
        _interpreter.PDF_DrawGlyph(c, ref gInfo, _currRasterizer, _interpreter._debugState.State, _currFontData, widths, _localState.currentText, _localState.charIndex);
      }
      _localState.charIndex = _localState.currentText.Length - 1;
      UpdateImageDataAndPictureBox();
      UpdateLabels();
      _localState.textIndex++;
      if (_localState.textIndex >= _interpreter._debugState.Literals.Count)
      {
        // Load next from interpreer
        ReadNextData();
      }
      else
      {
        // update local state
        _localState.charIndex = 0;
        _localState.currentText = _interpreter._debugState.Literals[_localState.textIndex].Literal;
        _interpreter._debugState.State.TextObject.TextMatrix[2, 0] -=
          (_interpreter._debugState.Literals[_localState.textIndex].PositionAdjustment / 1000f) *
          _interpreter._debugState.State.TextObject.TextMatrix[0, 0]
          * _interpreter._debugState.State.TextObject.FontScaleFactor;
      }
    }
  }
}
