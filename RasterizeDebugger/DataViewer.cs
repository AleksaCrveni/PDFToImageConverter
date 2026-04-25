using Converter.Parsers.PDF;

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
    private void DataViewer_Load(object sender, EventArgs e)
    {

    }

    private void btn_Copy_Click(object sender, EventArgs e)
    {
      Clipboard.SetText(txb_Data.Text);
    }

    private void btn_refreshLog_Click(object sender, EventArgs e)
    {
      txb_Data.Text = _interpreter._pathLogger.ToString();
    }
  }
}
