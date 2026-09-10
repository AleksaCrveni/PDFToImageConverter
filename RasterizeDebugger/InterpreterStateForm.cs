using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Converter.DEBUG;
namespace RasterizeDebugger
{
  public partial class InterpreterStateForm : Form
  {
    InterpreterStateData _data;
    public InterpreterStateForm()
    {
      InitializeComponent();
    }

    private void InterpreterStateForm_Load(object sender, EventArgs e)
    {

    }

    public void UpdateUI(InterpreterStateData data)
    {
      txb_ScaleX.Text = $"ScaleX: {data.Scale.scaleX}";
      txb_ScaleY.Text = $"ScaleY: {data.Scale.scaleY}";
      txb_Font.Text = $"Font: {data.Font}";
      txb_TJCounter.Text = $"TJCount: {data.TJCount}";
      ShowWindings();
      ShowVertices();
      _data = data;
    }

    private void textBox1_TextChanged(object sender, EventArgs e)
    {

    }

    private void btn_Windings_Click(object sender, EventArgs e)
    {
      ShowWindings();
    }

    private void ShowWindings()
    {
      if (_data == null || _data.Windings == null)
        return;
      rtxb_Windings.Text = StringifyHelper.ConvertWindingsAndLengths(_data.Windings, _data.WindingLengths, _data.WindingCount);
    }

    private void ShowVertices()
    {
      if (_data == null || _data.Vertices == null)
        return;
      rtxb_Vertices.Text = StringifyHelper.ConvertVertices(_data.Vertices);
    }
  }
}
