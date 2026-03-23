namespace RasterizeDebugger
{
  partial class DataViewer
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
      txb_Data = new TextBox();
      btn_Copy = new Button();
      SuspendLayout();
      // 
      // txb_Data
      // 
      txb_Data.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      txb_Data.Location = new Point(12, 34);
      txb_Data.Multiline = true;
      txb_Data.Name = "txb_Data";
      txb_Data.ReadOnly = true;
      txb_Data.Size = new Size(776, 404);
      txb_Data.TabIndex = 0;
      // 
      // btn_Copy
      // 
      btn_Copy.Location = new Point(12, 5);
      btn_Copy.Name = "btn_Copy";
      btn_Copy.Size = new Size(776, 23);
      btn_Copy.TabIndex = 1;
      btn_Copy.Text = "Copy";
      btn_Copy.UseVisualStyleBackColor = true;
      btn_Copy.Click += btn_Copy_Click;
      // 
      // DataViewer
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(800, 450);
      Controls.Add(btn_Copy);
      Controls.Add(txb_Data);
      Name = "DataViewer";
      Text = "DataViewer";
      Load += DataViewer_Load;
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private TextBox txb_Data;
    private Button btn_Copy;
  }
}