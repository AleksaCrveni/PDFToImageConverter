namespace RasterizeDebugger
{
  partial class Form1
  {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      pb_mainImage = new PictureBox();
      label1 = new Label();
      lbl_currentText = new Label();
      btn_fontInfo = new Button();
      btn_nextChar = new Button();
      btn_nextText = new Button();
      label2 = new Label();
      lbl_currentChar = new Label();
      btn_load = new Button();
      btn_processAll = new Button();
      lbl_currFilename = new Label();
      label3 = new Label();
      lbl_contentLength = new Label();
      label4 = new Label();
      lbl_currPosition = new Label();
      ((System.ComponentModel.ISupportInitialize)pb_mainImage).BeginInit();
      SuspendLayout();
      // 
      // pb_mainImage
      // 
      pb_mainImage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      pb_mainImage.Location = new Point(12, 83);
      pb_mainImage.Name = "pb_mainImage";
      pb_mainImage.Size = new Size(892, 483);
      pb_mainImage.TabIndex = 0;
      pb_mainImage.TabStop = false;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Font = new Font("Segoe UI", 11F);
      label1.Location = new Point(12, 9);
      label1.Name = "label1";
      label1.Size = new Size(95, 20);
      label1.TabIndex = 1;
      label1.Text = "Current Text :";
      // 
      // lbl_currentText
      // 
      lbl_currentText.AutoSize = true;
      lbl_currentText.Location = new Point(113, 13);
      lbl_currentText.Name = "lbl_currentText";
      lbl_currentText.Size = new Size(36, 15);
      lbl_currentText.TabIndex = 2;
      lbl_currentText.Text = "NULL";
      // 
      // btn_fontInfo
      // 
      btn_fontInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_fontInfo.Location = new Point(759, 9);
      btn_fontInfo.Name = "btn_fontInfo";
      btn_fontInfo.Size = new Size(75, 23);
      btn_fontInfo.TabIndex = 3;
      btn_fontInfo.Text = "Font Info";
      btn_fontInfo.UseVisualStyleBackColor = true;
      // 
      // btn_nextChar
      // 
      btn_nextChar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_nextChar.Location = new Point(600, 9);
      btn_nextChar.Name = "btn_nextChar";
      btn_nextChar.Size = new Size(75, 23);
      btn_nextChar.TabIndex = 4;
      btn_nextChar.Text = "Next Char";
      btn_nextChar.UseVisualStyleBackColor = true;
      // 
      // btn_nextText
      // 
      btn_nextText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_nextText.Location = new Point(681, 9);
      btn_nextText.Name = "btn_nextText";
      btn_nextText.Size = new Size(72, 23);
      btn_nextText.TabIndex = 5;
      btn_nextText.Text = "Next Text";
      btn_nextText.UseVisualStyleBackColor = true;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Font = new Font("Segoe UI", 11F);
      label2.Location = new Point(12, 38);
      label2.Name = "label2";
      label2.Size = new Size(98, 20);
      label2.TabIndex = 6;
      label2.Text = "Current Char :";
      // 
      // lbl_currentChar
      // 
      lbl_currentChar.AutoSize = true;
      lbl_currentChar.Location = new Point(113, 42);
      lbl_currentChar.Name = "lbl_currentChar";
      lbl_currentChar.Size = new Size(36, 15);
      lbl_currentChar.TabIndex = 7;
      lbl_currentChar.Text = "NULL";
      // 
      // btn_load
      // 
      btn_load.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_load.Location = new Point(759, 38);
      btn_load.Name = "btn_load";
      btn_load.Size = new Size(75, 23);
      btn_load.TabIndex = 8;
      btn_load.Text = "Load PDF:";
      btn_load.UseVisualStyleBackColor = true;
      // 
      // btn_processAll
      // 
      btn_processAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_processAll.Location = new Point(841, 10);
      btn_processAll.Name = "btn_processAll";
      btn_processAll.Size = new Size(63, 51);
      btn_processAll.TabIndex = 9;
      btn_processAll.Text = "Process All";
      btn_processAll.UseVisualStyleBackColor = true;
      // 
      // lbl_currFilename
      // 
      lbl_currFilename.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      lbl_currFilename.Location = new Point(600, 42);
      lbl_currFilename.Name = "lbl_currFilename";
      lbl_currFilename.Size = new Size(156, 19);
      lbl_currFilename.TabIndex = 10;
      lbl_currFilename.Text = "Filename";
      // 
      // label3
      // 
      label3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      label3.AutoSize = true;
      label3.Location = new Point(600, 65);
      label3.Name = "label3";
      label3.Size = new Size(93, 15);
      label3.TabIndex = 11;
      label3.Text = "Content length :";
      // 
      // lbl_contentLength
      // 
      lbl_contentLength.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      lbl_contentLength.AutoSize = true;
      lbl_contentLength.Location = new Point(690, 65);
      lbl_contentLength.Name = "lbl_contentLength";
      lbl_contentLength.Size = new Size(13, 15);
      lbl_contentLength.TabIndex = 12;
      lbl_contentLength.Text = "0";
      // 
      // label4
      // 
      label4.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      label4.AutoSize = true;
      label4.Location = new Point(854, 65);
      label4.Name = "label4";
      label4.Size = new Size(13, 15);
      label4.TabIndex = 14;
      label4.Text = "0";
      // 
      // lbl_currPosition
      // 
      lbl_currPosition.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      lbl_currPosition.AutoSize = true;
      lbl_currPosition.Location = new Point(759, 65);
      lbl_currPosition.Name = "lbl_currPosition";
      lbl_currPosition.Size = new Size(99, 15);
      lbl_currPosition.TabIndex = 13;
      lbl_currPosition.Text = "Current position :";
      // 
      // Form1
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(916, 578);
      Controls.Add(label4);
      Controls.Add(lbl_currPosition);
      Controls.Add(lbl_contentLength);
      Controls.Add(label3);
      Controls.Add(lbl_currFilename);
      Controls.Add(btn_processAll);
      Controls.Add(btn_load);
      Controls.Add(lbl_currentChar);
      Controls.Add(label2);
      Controls.Add(btn_nextText);
      Controls.Add(btn_nextChar);
      Controls.Add(btn_fontInfo);
      Controls.Add(lbl_currentText);
      Controls.Add(label1);
      Controls.Add(pb_mainImage);
      Name = "Form1";
      Text = "PDF Rasterizer Debugger";
      Load += Form1_Load;
      ((System.ComponentModel.ISupportInitialize)pb_mainImage).EndInit();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private PictureBox pb_mainImage;
    private Label label1;
    private Label lbl_currentText;
    private Button btn_fontInfo;
    private Button btn_nextChar;
    private Button btn_nextText;
    private Label label2;
    private Label lbl_currentChar;
    private Button btn_load;
    private Button btn_processAll;
    private Label lbl_currFilename;
    private Label label3;
    private Label lbl_contentLength;
    private Label label4;
    private Label lbl_currPosition;
  }
}
