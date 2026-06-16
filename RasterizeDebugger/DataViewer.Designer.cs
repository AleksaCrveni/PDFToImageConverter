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
      btn_Copy = new Button();
      btn_refreshLog = new Button();
      txb_Data = new RichTextBox();
      cb_wordWrap = new CheckBox();
      SuspendLayout();
      // 
      // btn_Copy
      // 
      btn_Copy.Location = new Point(12, 4);
      btn_Copy.Name = "btn_Copy";
      btn_Copy.Size = new Size(574, 23);
      btn_Copy.TabIndex = 1;
      btn_Copy.Text = "Copy";
      btn_Copy.UseVisualStyleBackColor = true;
      btn_Copy.Click += btn_Copy_Click;
      // 
      // btn_refreshLog
      // 
      btn_refreshLog.Location = new Point(12, 33);
      btn_refreshLog.Name = "btn_refreshLog";
      btn_refreshLog.Size = new Size(574, 23);
      btn_refreshLog.TabIndex = 2;
      btn_refreshLog.Text = "Refresh";
      btn_refreshLog.UseVisualStyleBackColor = true;
      btn_refreshLog.Click += btn_refreshLog_Click;
      // 
      // txb_Data
      // 
      txb_Data.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      txb_Data.Location = new Point(12, 62);
      txb_Data.Name = "txb_Data";
      txb_Data.Size = new Size(813, 585);
      txb_Data.TabIndex = 3;
      txb_Data.Text = "";
      txb_Data.WordWrap = false;
      // 
      // cb_wordWrap
      // 
      cb_wordWrap.AutoSize = true;
      cb_wordWrap.Location = new Point(592, 4);
      cb_wordWrap.Name = "cb_wordWrap";
      cb_wordWrap.Size = new Size(83, 19);
      cb_wordWrap.TabIndex = 4;
      cb_wordWrap.Text = "WordWrap";
      cb_wordWrap.UseVisualStyleBackColor = true;
      cb_wordWrap.CheckedChanged += cb_wordWrap_CheckedChanged;
      // 
      // DataViewer
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(837, 659);
      Controls.Add(cb_wordWrap);
      Controls.Add(txb_Data);
      Controls.Add(btn_refreshLog);
      Controls.Add(btn_Copy);
      Name = "DataViewer";
      Text = "DataViewer";
      Load += DataViewer_Load;
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion
    private Button btn_Copy;
    private Button btn_refreshLog;
    private RichTextBox txb_Data;
    private CheckBox cb_wordWrap;
  }
}