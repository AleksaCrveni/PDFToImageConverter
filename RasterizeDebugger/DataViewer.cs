using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RasterizeDebugger
{
  public partial class DataViewer : Form
  {
    public DataViewer()
    {
      InitializeComponent();
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
  }
}
