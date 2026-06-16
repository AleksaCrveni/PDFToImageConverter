using Converter.Parsers.PDF;
using System.Text;

namespace RasterizeDebugger
{
  public partial class DataViewer : Form
  {
    private PDFGOInterpreter _interpreter;
    public DataViewer()
    {
      InitializeComponent();
    }

    public DataViewer(PDFGOInterpreter interpreter)
    {
      InitializeComponent();
      _interpreter = interpreter;
      txb_Data.Text = _interpreter._pathLogger.ToString();
    }
    public DataViewer(string data)
    {
      InitializeComponent();
      txb_Data.Text = data;
    }
    public DataViewer(string[] lines)
    {
      InitializeComponent();
      txb_Data.Lines = lines;
    }

    private void DataViewer_Load(object sender, EventArgs e)
    {
      cb_wordWrap.Checked = txb_Data.WordWrap;
    }

    private void btn_Copy_Click(object sender, EventArgs e)
    {
      Clipboard.SetText(txb_Data.Text);
    }

    private void btn_refreshLog_Click(object sender, EventArgs e)
    {
      txb_Data.Text = _interpreter?._pathLogger?.ToString();
    }

    public void HighlightPos(int pos)
    {
      txb_Data.SelectionBackColor = txb_Data.BackColor;
      txb_Data.DeselectAll();
      txb_Data.Select(pos, 1);
      txb_Data.SelectionBackColor = Color.Red;
    }

    private void cb_wordWrap_CheckedChanged(object sender, EventArgs e)
    {
      txb_Data.WordWrap = cb_wordWrap.Checked;
    }
  }
}
