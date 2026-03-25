using Converter.FileStructures.PDF;
using Converter.FileStructures.PDF.GraphicsInterpreter;
using Converter.FileStructures.PostScript;
using Converter.FileStructures.TTF;
using Converter.FileStructures.Type1;
using Converter.Rasterizers;
using Converter.Utils;
using Converter.Writers.TIFF;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using static System.Windows.Forms.AxHost;


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

    public Playground()
    {
      InitializeComponent();
      _dialog = new OpenFileDialog();
      _dialog.Filter = "Shape (*.shape)|*.shape";
      _dialog.RestoreDirectory = true;
    }

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
      // bad workaround
      List<string> seen = new List<string>();

      if (_pdfFile != null) 
      {
        foreach (PDF_PageInfo pInfo in _pdfFile.PageInformation)
        {
          foreach (PDF_FontData font in pInfo.ResourceDict.Font)
          {
            if (seen.Contains(font.FontInfo.BaseFont))
              continue;
            cb_font.Items.Add(font.FontInfo.BaseFont);
            seen.Add(font.FontInfo.BaseFont);
          }
        }
        cb_font.SelectedIndex = 0;
      }
      

      
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
      
      if (_currFont.FontInfo.SubType == PDF_FontType.Type0)
      {
        PDF_CID_CMAP cmap = _currFont.FontInfo.DescendantFontsInfo[0].Cmap;
        if (cmap != null)
        {
          cb_glyph.BeginUpdate();
          cb_glyph.Items.Clear();
          foreach (KeyValuePair<char, char> kvp in cmap.Cmap)
          {
            cb_glyph.Items.Add($"{(int)kvp.Key}-{(int)kvp.Value}-{kvp.Value}"); // CID-Actual-Char
          }
          foreach (KeyValuePair<char, List<char>> kvp in cmap.LigatureCmap)
          {
            cb_glyph.Items.Add($"{(int)kvp.Key}-Ligature"); // CID-Actual-Char
          }
          cb_glyph.EndUpdate();
        } else
        {
          throw new NotImplementedException("CMAP EMPTY!");
        }
      }
      else if (_currFont.FontInfo.SubType == PDF_FontType.Type1)
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
      else
      {
        throw new NotImplementedException("Font type not supported in debugger!");
      }
    }

    private void label3_Click(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// TODO: support all fonts
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="InvalidDataException"></exception>
    private void btn_raster_Click(object sender, EventArgs e)
    {
      if (_pdfFile == null)
        return;
      Raster();
    }

    private void Raster()
    {
      char c = '0';
      char CID = '0';
      string name = ""; // have to do some reversal here as well
      GetCurrentChar(ref c, ref CID, ref name);
      Array.Clear(_data);
      ActualRaster(c, CID, name);
    }
    private void ActualRaster(char c, char CID, string name)
    {
      IRasterizer rasterizer = _currFont.Rasterizer;
      GlyphInfo glyphInfo = new GlyphInfo();
      double[] widths = _currFont.FontInfo.Widths;
      rasterizer.SetDefaultGlyphInfoValues(ref glyphInfo);
      // TODO: use this instead of c, FIX 
      if (name == "")
        rasterizer.GetGlyphInfo(c, ref glyphInfo);
      else
        glyphInfo.Name = name;

      int X = 20;
      int Y = 20;

      #region width calculation

      float width = 0;
      if (_currFont.FontInfo.SubType == PDF_FontType.Type0)
      {
        width = RasterHelper.GetCompositeWidth(CID, _currFont.FontInfo.DescendantFontsInfo![0].DescendantDict);
      }
      else
      {
        // Does this work for all charcaters
        int idx = (int)c - _currFont.FontInfo.FirstChar;

        if (idx < widths.Length)
          width = (float)widths[idx] / 1000f;
        else
          width = _currFont.FontInfo.FontDescriptor.MissingWidth / 1000f;
      }
      Debug.Assert(width != 0);
      #endregion
      bool valid = float.TryParse(txb_Scale.Text, out float res);
      if (!valid || res <= 0 || res > 100)
      {
        MessageBox.Show("Invalid scale value!");
        return;
      }
      _scale = res;
      (float scaleX, float scaleY) s = (_scale, _scale);

      #region asserts
      Debug.Assert(X > 0, $"X is negative at index;");
      Debug.Assert(Y > 0, $"Y is negative at index;");
      Debug.Assert(X < _width, $"X must be within bounds.X: {X} - Width: {_width}.");
      Debug.Assert(Y < _height, $"Y must be within bounds.Y: {Y} - Height: {_height}.");
      Debug.Assert(s.scaleX > 0, $"Scale factor X must be higher than 0! sfX: {s.scaleX}.");
      Debug.Assert(s.scaleY > 0, $"Scale factor Y must be higher than 0! sfY: {s.scaleY}.");
      #endregion asserts

      int ascent = 0;
      int descent = 0;
      if (_currFont.FontInfo.SubType == PDF_FontType.Type0)
      {
        ascent = (int)Math.Round(_currFont.FontInfo.DescendantFontsInfo![0].DescendantDict.FontDescriptor.Ascent * s.scaleY);
        descent = (int)Math.Round(_currFont.FontInfo.DescendantFontsInfo![0].DescendantDict.FontDescriptor.Descent * s.scaleY);
      }
      else
      {
        ascent = (int)Math.Round(_currFont.FontInfo.FontDescriptor.Ascent * s.scaleY);
        descent = (int)Math.Round(_currFont.FontInfo.FontDescriptor.Descent * s.scaleY);
      }

      #region glyph metrics

      int c_x0 = 0;
      int c_y0 = 0;
      int c_x1 = 0;
      int c_y1 = 0;
      rasterizer.GetGlyphBoundingBox(ref glyphInfo, s.scaleX, s.scaleY, ref c_x0, ref c_y0, ref c_x1, ref c_y1);

      Debug.Assert(c_x0 != int.MaxValue && c_x0 != int.MinValue);
      Debug.Assert(c_y0 != int.MaxValue && c_y0 != int.MinValue);
      Debug.Assert(c_x1 != int.MaxValue && c_x1 != int.MinValue);
      Debug.Assert(c_y1 != int.MaxValue && c_y1 != int.MinValue);

      // char height - different than bounding box height
      int y = Y + c_y0;
      // I think that this should be replaced from value in Widths array
      // NOTE: widths array wont work since this width is not in units but in pixels after its been scaled down
      int glyphWidth = c_x1 - c_x0;
      int glyphHeight = c_y1 - c_y0;

      //// Added when type1 interpreter had height 0 and caused issues, I didnt see impact on TTF files 
      //if (glyphHeight == 0)
      //  glyphHeight = 2;

      //if (glyphWidth == 0)
      //  glyphWidth = 2;
      //Debug.Assert(glyphWidth > 0);
      //Debug.Assert(glyphHeight > 0);

      #endregion

      int byteOffset = X + (20 * _width);
      int shiftX = 0;
      int shiftY = 0;

      rasterizer.RasterizeGlyph(_data, byteOffset, glyphWidth, glyphHeight, _width, s.scaleX, s.scaleY, shiftX, shiftY, ref glyphInfo);

      UpdateImage();
    }

    private void GetCurrentChar(ref char c, ref char CID, ref string name)
    {
      if (cb_glyph.SelectedItem == null)
      {
        MessageBox.Show("Glyph not must be selected!");
        return;
      }
      string cbValue = cb_glyph.SelectedItem.ToString();
      if (_currFont.FontInfo.SubType == PDF_FontType.Type0)
      {
        string[] vals = cbValue.Split('-');
        CID = Convert.ToChar(Convert.ToUInt16(vals[0]));
        if (vals.Length == 2)
          return;
        c = Convert.ToChar(Convert.ToUInt16(vals[0]));
      }
      else if (_currFont.FontInfo.SubType == PDF_FontType.Type1)
      {
        name = cbValue;
      }
      else if (_currFont.FontInfo.SubType == PDF_FontType.TrueType)
      {
        throw new NotImplementedException();
      }
      else
      {
        throw new NotImplementedException();
      }
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
    public PDF_FontData GetFontDataFromKey(string searchKey)
    {
      foreach (PDF_PageInfo pInfo in _pdfFile.PageInformation)
      {
        foreach (PDF_FontData font in pInfo.ResourceDict.Font)
        {
            if (font.Key == searchKey)
              return font;
        }
      }

      return new PDF_FontData();
    }
  }
}
