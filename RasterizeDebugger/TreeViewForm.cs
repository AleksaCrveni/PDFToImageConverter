using Converter.FileStructures.PDF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RasterizeDebugger
{
  public partial class TreeViewForm : Form
  {
    public TreeViewForm()
    {
      InitializeComponent();
    }

    public TreeViewForm(PDF_FontData fd)
    {
      InitializeComponent();
      Helper.FillTreeWithFontInfo(treeView, fd);
    }
    private void TreeViewForm_Load(object sender, EventArgs e)
    {

    }
  }
}
