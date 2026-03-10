using Converter.FileStructures.PDF;
using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Rasterizers;
using Converter.Utils;
using Converter.Writers.TIFF;
using System.Diagnostics;
using System.Text;


namespace RasterizeDebugger
{
  public partial class Playground : Form
  {
    PDFFile _pdfFile;
    PDF_FontData _currFont;
    float _scale = 0.5f;
    int _width = 800;
    int _height = 800;
    byte[] _data;
    int _imageDataStartPos = 0;
    byte[] _imageData;
    OpenFileDialog _dialog;
    PSShape _shape;

    public Playground(PDFFile file)
    {
      InitializeComponent();
      _pdfFile = file;
      _dialog = new OpenFileDialog();
      _dialog.Filter = "Shape (*.shape)|*.shape";
      _dialog.RestoreDirectory = true;
    }
    private void Playground_Load(object sender, EventArgs e)
    {
      foreach (PDF_PageInfo pInfo in _pdfFile.PageInformation)
      {
        foreach (PDF_FontData font in pInfo.ResourceDict.Font)
        {
          cb_font.Items.Add(font.FontInfo.BaseFont);
        }
      }

      cb_font.SelectedIndex = 0;
      txb_Scale.Text = _scale.ToString();
      _scale = 1f;
      _data = new byte[_height * _width];
      pb_main.Size = new Size(_width, _height);
      MemoryStream memoryStream = new MemoryStream();
      TIFFGrayscaleWriter writer = new TIFFGrayscaleWriter(memoryStream);
      TIFFWriterOptions options = new TIFFWriterOptions()
      {
        Height = _height,
        Width = _width
      };
      writer.WriteEmptyImage(ref options);
      _imageDataStartPos = writer.data.InitialImageDataOffset;
      _imageData = memoryStream.ToArray();
      pb_main.Image = Image.FromStream(new MemoryStream(_imageData));

    }

    private void cb_font_SelectedIndexChanged(object sender, EventArgs e)
    {
      string name = cb_font.SelectedItem.ToString();
      bool found = false;
      foreach (PDF_PageInfo pInfo in _pdfFile.PageInformation)
      {
        foreach (PDF_FontData fontData in pInfo.ResourceDict.Font)
        {
          if (fontData.FontInfo.BaseFont == name)
          {
            _currFont = fontData;
            found = true;
            break;
          }
        }
        if (found)
          break;
      }

      // some font types like ttf do not have charset, we have to get data from font reader
      if (_currFont.FontInfo.FontDescriptor.CharSet.Length == 0)
      {
        throw new NotImplementedException("Only type1 fonts supported atm");
      }
      else
      {
        // this can happen also to be 0 len since font can use other tag , its specified in PDF docs
        // we ignore first char becuase string starts with /
        string[] chars = _currFont.FontInfo.FontDescriptor.CharSet.AsSpan().Slice(1).ToString().Split('/');
        Debug.Assert(chars.Length > 0);
        cb_glyph.Items.Clear();
        cb_glyph.BeginUpdate();
        cb_glyph.Items.AddRange(chars);
        cb_glyph.EndUpdate();
        cb_glyph.SelectedIndex = 0;
      }
    }

    private void label3_Click(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// TODO: MAKE IT GENERIC IT ONLY WORKS FOR TYPE1 ATM
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="InvalidDataException"></exception>
    private void btn_raster_Click(object sender, EventArgs e)
    {
      Array.Clear(_data);
      bool valid = float.TryParse(txb_Scale.Text, out float res);
      if (!valid || res <= 0 || res > 100)
      {
        MessageBox.Show("Invalid scale value!");
        return;
      }
      _scale = res;
      Type1Rasterizer r = (Type1Rasterizer)_currFont.Rasterizer;
      //r.GetGlyphInfo();
      string name = cb_glyph.SelectedItem.ToString();
      TYPE1_Point2D width = new TYPE1_Point2D();
      PSShape? shape = r.InterpretByName(name);
      Debug.Assert(shape != null);

      // already scaled
      List<TTFVertex> vertices = RasterHelper.ConvertToTTFVertexFormat(shape);
      StringBuilder sb = new StringBuilder();
      #region log
      int j = 0;
      for (int i = 0; i < shape._moves.Count; i++)
      {
        PS_COMMAND v = shape._moves[i];
        if (v == PS_COMMAND.MOVE_TO)
        {
          sb.Append($"{vertices[i].x} {vertices[i].y} ");
          sb.Append("MOVE_TO ");
        }
        else if (v == PS_COMMAND.LINE_TO)
        {
          sb.Append($"{vertices[i].x} {vertices[i].y} ");
          sb.Append("LINE_TO ");
        }
        else if (v == PS_COMMAND.CURVE_TO)
        {
          sb.Append($"{vertices[i].cx} {vertices[i].cy} {vertices[i].cx1} {vertices[i].cy1} {vertices[i].x} {vertices[i].y} ");
          sb.Append("CURVE_TO ");
        }
        else
        {
          throw new InvalidDataException("Invalid PS_COMMAND!");
        }

      }

      //File.WriteAllText($"TTF_VERTEX_FROM_TYPE1__{name}__{scale.ToString()}.txt", sb.ToString());
      #endregion log

      List<int> windingLengths = new List<int>();
      int windingCount = 0;

      List<Converter.FileStructures.TTF.PointF> windings = r.STB_FlattenCurves(ref vertices, vertices.Count, 0.35f / _scale, ref windingLengths, ref windingCount);
      int ix0 = 0;
      int iy0 = 0;
      int ix1 = 0;
      int iy1 = 0;
      int height = 0;
      RasterHelper.GetFakeBoundingBoxFromPoints(windings, ref ix0, ref iy0, ref ix1, ref iy1, _scale);

      height = iy1 - iy0;
      if (width.Y > 0)
        height = (int)(width.Y * _scale);
      BmpS result = new BmpS();
      result.H = height;
      result.W = (int)(shape._width.X * _scale);
      result.Offset = 20 * _height + 20; // draw at 20,20
      result.Pixels = _data;
      result.Stride = _width;
      r.STB_InternalRasterize(ref result, ref windings, ref windingLengths, windingCount, _scale, _scale, 0, 0, ix0, iy0, true);

      UpdateImage();
    }

    private void btn_loadShape_Click(object sender, EventArgs e)
    {
      DialogResult dialogResult = _dialog.ShowDialog();
      if (dialogResult == DialogResult.OK)
      {
        _shape = new PSShape();
        _shape.LoadData(File.ReadAllBytes(_dialog.FileName));
        PathRasterizer shapeRasterizer = new PathRasterizer(Array.Empty<byte>(), "");
        
          
        Array.Clear(_data);
        bool valid = float.TryParse(txb_Scale.Text, out float res);
        
        if (!valid || res <= 0 || res > 100)
        {
          MessageBox.Show("Invalid scale value!");
          return;
        }
        _scale = res;
        //_shape.ScaleAll(_scale);

        Debug.Assert(_shape != null);

        // already scaled
        List<TTFVertex> vertices = RasterHelper.ConvertToTTFVertexFormat(_shape);
        StringBuilder sb = new StringBuilder();
        #region log
        int j = 0;
        for (int i = 0; i < _shape._moves.Count; i++)
        {
          PS_COMMAND v = _shape._moves[i];
          if (v == PS_COMMAND.MOVE_TO)
          {
            sb.Append($"{vertices[i].x} {vertices[i].y} ");
            sb.Append("MOVE_TO ");
          }
          else if (v == PS_COMMAND.LINE_TO)
          {
            sb.Append($"{vertices[i].x} {vertices[i].y} ");
            sb.Append("LINE_TO ");
          }
          else if (v == PS_COMMAND.CURVE_TO)
          {
            sb.Append($"{vertices[i].cx} {vertices[i].cy} {vertices[i].cx1} {vertices[i].cy1} {vertices[i].x} {vertices[i].y} ");
            sb.Append("CURVE_TO ");
          }
          else
          {
            throw new InvalidDataException("Invalid PS_COMMAND!");
          }

        }

        //File.WriteAllText($"TTF_VERTEX_FROM_TYPE1__{name}__{scale.ToString()}.txt", sb.ToString());
        #endregion log

        List<int> windingLengths = new List<int>();
        int windingCount = 0;

        List<Converter.FileStructures.TTF.PointF> windings = shapeRasterizer.STB_FlattenCurves(ref vertices, vertices.Count, 0.35f / _scale, ref windingLengths, ref windingCount);
        int c_x0 = 0;
        int c_y0 = 0;
        int c_x1 = 0;
        int c_y1 = 0;
        RasterHelper.GetFakeBoundingBoxFromPoints(windings, ref c_x0, ref c_y0, ref c_x1, ref c_y1, _scale);
        int glyphWidth = c_x1 - c_x0;
        int glyphHeight = c_y1 - c_y0;
        if (_shape._width.Y > 0)
          glyphHeight = (int)(_shape._width.Y * _scale);

        if (glyphHeight == 0)
        {
          var w = windings.Last();
          w.Y = 1;
          windings[windings.Count - 1] = w;
          glyphHeight = 1;
        }
          
        if (glyphWidth == 0)
        {
          glyphWidth = 1;
        }
          
        BmpS result = new BmpS();
        result.H = glyphHeight;
        result.W = (int)(glyphWidth);
        result.Offset = 20 * _height + 20; // draw at 20,20
        result.Pixels = _data;
        result.Stride = _width;
        //shapeRasterizer.STB_InternalRasterize(ref result, ref windings, ref windingLengths, windingCount, _scale, _scale, 0, 0, 0, 0, false);
        shapeRasterizer.RasterizeShape(_data, result.Offset, _width, _shape, _scale);
        bool isEmpty = true;
        foreach (byte b in _data)
        {
          if (b > 0)
          {
            isEmpty = false;
            break;
          }
        }
        if (isEmpty)
          throw new Exception("NE RADI");
        UpdateImage();

      }
    }
    private void UpdateImage()
    {
      Array.ConstrainedCopy(_data, 0, _imageData, _imageDataStartPos, _data.Length);
      pb_main.Image = Image.FromStream(new MemoryStream(_imageData));
    }
  }
}
