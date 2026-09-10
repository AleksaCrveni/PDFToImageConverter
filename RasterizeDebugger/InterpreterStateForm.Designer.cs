namespace RasterizeDebugger
{
  partial class InterpreterStateForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      txb_ScaleX = new TextBox();
      txb_Font = new TextBox();
      txb_ScaleY = new TextBox();
      txb_TJCounter = new TextBox();
      rtxb_Windings = new RichTextBox();
      rtxb_Vertices = new RichTextBox();
      SuspendLayout();
      // 
      // txb_ScaleX
      // 
      txb_ScaleX.Location = new Point(12, 12);
      txb_ScaleX.Name = "txb_ScaleX";
      txb_ScaleX.ReadOnly = true;
      txb_ScaleX.Size = new Size(136, 23);
      txb_ScaleX.TabIndex = 0;
      txb_ScaleX.Text = "ScaleX: %";
      txb_ScaleX.TextChanged += textBox1_TextChanged;
      // 
      // txb_Font
      // 
      txb_Font.Location = new Point(12, 69);
      txb_Font.Name = "txb_Font";
      txb_Font.ReadOnly = true;
      txb_Font.Size = new Size(136, 23);
      txb_Font.TabIndex = 1;
      txb_Font.Text = "Font: %";
      // 
      // txb_ScaleY
      // 
      txb_ScaleY.Location = new Point(12, 41);
      txb_ScaleY.Name = "txb_ScaleY";
      txb_ScaleY.ReadOnly = true;
      txb_ScaleY.Size = new Size(136, 23);
      txb_ScaleY.TabIndex = 2;
      txb_ScaleY.Text = "ScaleY: %";
      // 
      // txb_TJCounter
      // 
      txb_TJCounter.Location = new Point(154, 12);
      txb_TJCounter.Name = "txb_TJCounter";
      txb_TJCounter.ReadOnly = true;
      txb_TJCounter.Size = new Size(136, 23);
      txb_TJCounter.TabIndex = 3;
      txb_TJCounter.Text = "TJCount: %";
      // 
      // rtxb_Windings
      // 
      rtxb_Windings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
      rtxb_Windings.Location = new Point(12, 98);
      rtxb_Windings.Name = "rtxb_Windings";
      rtxb_Windings.ReadOnly = true;
      rtxb_Windings.Size = new Size(348, 355);
      rtxb_Windings.TabIndex = 7;
      rtxb_Windings.Text = "";
      // 
      // rtxb_Vertices
      // 
      rtxb_Vertices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      rtxb_Vertices.Location = new Point(366, 98);
      rtxb_Vertices.Name = "rtxb_Vertices";
      rtxb_Vertices.ReadOnly = true;
      rtxb_Vertices.Size = new Size(348, 355);
      rtxb_Vertices.TabIndex = 8;
      rtxb_Vertices.Text = "";
      // 
      // InterpreterStateForm
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(723, 477);
      Controls.Add(rtxb_Vertices);
      Controls.Add(rtxb_Windings);
      Controls.Add(txb_TJCounter);
      Controls.Add(txb_ScaleY);
      Controls.Add(txb_Font);
      Controls.Add(txb_ScaleX);
      Name = "InterpreterStateForm";
      Text = "InterpreterStateForm";
      Load += InterpreterStateForm_Load;
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private TextBox txb_ScaleX;
    private TextBox txb_Font;
    private TextBox txb_ScaleY;
    private TextBox txb_TJCounter;
    private RichTextBox rtxb_Windings;
    private RichTextBox rtxb_Vertices;
  }
}