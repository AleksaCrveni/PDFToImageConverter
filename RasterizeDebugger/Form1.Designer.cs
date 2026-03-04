namespace RasterizeDebugger
{
  partial class form_main
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
      lbl_currPosition = new Label();
      label_5 = new Label();
      lbl_readPos = new Label();
      label_7 = new Label();
      label4 = new Label();
      lbl_charValue = new Label();
      label7 = new Label();
      panel1 = new Panel();
      treeView1 = new TreeView();
      lbl_glyphIndex = new Label();
      label6 = new Label();
      lbl_glyphName = new Label();
      panel2 = new Panel();
      btn_centerImage = new Button();
      ((System.ComponentModel.ISupportInitialize)pb_mainImage).BeginInit();
      panel1.SuspendLayout();
      panel2.SuspendLayout();
      SuspendLayout();
      // 
      // pb_mainImage
      // 
      pb_mainImage.Location = new Point(3, 3);
      pb_mainImage.Name = "pb_mainImage";
      pb_mainImage.Size = new Size(672, 574);
      pb_mainImage.TabIndex = 0;
      pb_mainImage.TabStop = false;
      pb_mainImage.Click += pb_mainImage_Click;
      pb_mainImage.Paint += pb_mainImage_Paint;
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
      btn_fontInfo.Location = new Point(855, 9);
      btn_fontInfo.Name = "btn_fontInfo";
      btn_fontInfo.Size = new Size(75, 23);
      btn_fontInfo.TabIndex = 3;
      btn_fontInfo.Text = "Font Info";
      btn_fontInfo.UseVisualStyleBackColor = true;
      // 
      // btn_nextChar
      // 
      btn_nextChar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_nextChar.Location = new Point(696, 9);
      btn_nextChar.Name = "btn_nextChar";
      btn_nextChar.Size = new Size(75, 23);
      btn_nextChar.TabIndex = 4;
      btn_nextChar.Text = "Next Char";
      btn_nextChar.UseVisualStyleBackColor = true;
      btn_nextChar.Click += btn_nextChar_Click;
      // 
      // btn_nextText
      // 
      btn_nextText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_nextText.Location = new Point(777, 9);
      btn_nextText.Name = "btn_nextText";
      btn_nextText.Size = new Size(72, 23);
      btn_nextText.TabIndex = 5;
      btn_nextText.Text = "Next Text";
      btn_nextText.UseVisualStyleBackColor = true;
      btn_nextText.Click += btn_nextText_Click;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
      label2.Location = new Point(0, 13);
      label2.Name = "label2";
      label2.Size = new Size(113, 20);
      label2.TabIndex = 6;
      label2.Text = "Current Glyph :";
      // 
      // lbl_currentChar
      // 
      lbl_currentChar.AutoSize = true;
      lbl_currentChar.Font = new Font("Segoe UI", 11F);
      lbl_currentChar.Location = new Point(119, 13);
      lbl_currentChar.Name = "lbl_currentChar";
      lbl_currentChar.Size = new Size(44, 20);
      lbl_currentChar.TabIndex = 7;
      lbl_currentChar.Text = "NULL";
      // 
      // btn_load
      // 
      btn_load.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_load.Location = new Point(855, 38);
      btn_load.Name = "btn_load";
      btn_load.Size = new Size(75, 23);
      btn_load.TabIndex = 8;
      btn_load.Text = "Load PDF";
      btn_load.UseVisualStyleBackColor = true;
      btn_load.Click += btn_load_Click;
      // 
      // btn_processAll
      // 
      btn_processAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_processAll.Location = new Point(937, 10);
      btn_processAll.Name = "btn_processAll";
      btn_processAll.Size = new Size(63, 51);
      btn_processAll.TabIndex = 9;
      btn_processAll.Text = "Process All";
      btn_processAll.UseVisualStyleBackColor = true;
      btn_processAll.Click += btn_processAll_Click;
      // 
      // lbl_currFilename
      // 
      lbl_currFilename.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      lbl_currFilename.Location = new Point(696, 42);
      lbl_currFilename.Name = "lbl_currFilename";
      lbl_currFilename.Size = new Size(156, 19);
      lbl_currFilename.TabIndex = 10;
      lbl_currFilename.Text = "Filename";
      // 
      // label3
      // 
      label3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      label3.AutoSize = true;
      label3.Location = new Point(696, 65);
      label3.Name = "label3";
      label3.Size = new Size(50, 15);
      label3.TabIndex = 11;
      label3.Text = "Length :";
      // 
      // lbl_contentLength
      // 
      lbl_contentLength.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      lbl_contentLength.AutoSize = true;
      lbl_contentLength.Location = new Point(743, 65);
      lbl_contentLength.Name = "lbl_contentLength";
      lbl_contentLength.Size = new Size(13, 15);
      lbl_contentLength.TabIndex = 12;
      lbl_contentLength.Text = "0";
      // 
      // lbl_currPosition
      // 
      lbl_currPosition.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      lbl_currPosition.AutoSize = true;
      lbl_currPosition.Location = new Point(806, 65);
      lbl_currPosition.Name = "lbl_currPosition";
      lbl_currPosition.Size = new Size(13, 15);
      lbl_currPosition.TabIndex = 14;
      lbl_currPosition.Text = "0";
      // 
      // label_5
      // 
      label_5.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      label_5.AutoSize = true;
      label_5.Location = new Point(777, 65);
      label_5.Name = "label_5";
      label_5.Size = new Size(32, 15);
      label_5.TabIndex = 13;
      label_5.Text = "Pos :";
      // 
      // lbl_readPos
      // 
      lbl_readPos.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      lbl_readPos.AutoSize = true;
      lbl_readPos.Location = new Point(917, 65);
      lbl_readPos.Name = "lbl_readPos";
      lbl_readPos.Size = new Size(13, 15);
      lbl_readPos.TabIndex = 16;
      lbl_readPos.Text = "0";
      // 
      // label_7
      // 
      label_7.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      label_7.AutoSize = true;
      label_7.Location = new Point(855, 65);
      label_7.Name = "label_7";
      label_7.Size = new Size(61, 15);
      label_7.TabIndex = 15;
      label_7.Text = "Read Pos :";
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
      label4.Location = new Point(0, 33);
      label4.Name = "label4";
      label4.Size = new Size(76, 20);
      label4.TabIndex = 17;
      label4.Text = "Num Val: ";
      // 
      // lbl_charValue
      // 
      lbl_charValue.AutoSize = true;
      lbl_charValue.Font = new Font("Segoe UI", 11F);
      lbl_charValue.Location = new Point(74, 33);
      lbl_charValue.Name = "lbl_charValue";
      lbl_charValue.Size = new Size(17, 20);
      lbl_charValue.TabIndex = 18;
      lbl_charValue.Text = "0";
      // 
      // label7
      // 
      label7.AutoSize = true;
      label7.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
      label7.Location = new Point(0, 53);
      label7.Name = "label7";
      label7.Size = new Size(98, 20);
      label7.TabIndex = 19;
      label7.Text = "Glyph Name:";
      // 
      // panel1
      // 
      panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
      panel1.Controls.Add(treeView1);
      panel1.Controls.Add(lbl_glyphIndex);
      panel1.Controls.Add(label6);
      panel1.Controls.Add(label2);
      panel1.Controls.Add(lbl_glyphName);
      panel1.Controls.Add(lbl_currentChar);
      panel1.Controls.Add(label7);
      panel1.Controls.Add(label4);
      panel1.Controls.Add(lbl_charValue);
      panel1.Location = new Point(696, 83);
      panel1.Name = "panel1";
      panel1.Size = new Size(304, 580);
      panel1.TabIndex = 21;
      panel1.Paint += panel1_Paint;
      // 
      // treeView1
      // 
      treeView1.Location = new Point(3, 96);
      treeView1.Name = "treeView1";
      treeView1.Size = new Size(294, 481);
      treeView1.TabIndex = 23;
      // 
      // lbl_glyphIndex
      // 
      lbl_glyphIndex.AutoSize = true;
      lbl_glyphIndex.Font = new Font("Segoe UI", 11F);
      lbl_glyphIndex.Location = new Point(96, 73);
      lbl_glyphIndex.Name = "lbl_glyphIndex";
      lbl_glyphIndex.Size = new Size(44, 20);
      lbl_glyphIndex.TabIndex = 22;
      lbl_glyphIndex.Text = "NULL";
      // 
      // label6
      // 
      label6.AutoSize = true;
      label6.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
      label6.Location = new Point(0, 73);
      label6.Name = "label6";
      label6.Size = new Size(95, 20);
      label6.TabIndex = 21;
      label6.Text = "Glyph Index:";
      // 
      // lbl_glyphName
      // 
      lbl_glyphName.AutoSize = true;
      lbl_glyphName.Font = new Font("Segoe UI", 11F);
      lbl_glyphName.Location = new Point(96, 53);
      lbl_glyphName.Name = "lbl_glyphName";
      lbl_glyphName.Size = new Size(44, 20);
      lbl_glyphName.TabIndex = 20;
      lbl_glyphName.Text = "NULL";
      // 
      // panel2
      // 
      panel2.Anchor = AnchorStyles.None;
      panel2.Controls.Add(pb_mainImage);
      panel2.Location = new Point(12, 83);
      panel2.Name = "panel2";
      panel2.Size = new Size(678, 580);
      panel2.TabIndex = 22;
      // 
      // btn_centerImage
      // 
      btn_centerImage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      btn_centerImage.Location = new Point(12, 54);
      btn_centerImage.Name = "btn_centerImage";
      btn_centerImage.Size = new Size(101, 23);
      btn_centerImage.TabIndex = 23;
      btn_centerImage.Text = "Center Image";
      btn_centerImage.UseVisualStyleBackColor = true;
      btn_centerImage.Click += btn_centerImage_Click;
      // 
      // form_main
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(1012, 675);
      Controls.Add(btn_centerImage);
      Controls.Add(panel2);
      Controls.Add(panel1);
      Controls.Add(lbl_readPos);
      Controls.Add(label_7);
      Controls.Add(lbl_currPosition);
      Controls.Add(label_5);
      Controls.Add(lbl_contentLength);
      Controls.Add(label3);
      Controls.Add(lbl_currFilename);
      Controls.Add(btn_processAll);
      Controls.Add(btn_load);
      Controls.Add(btn_nextText);
      Controls.Add(btn_nextChar);
      Controls.Add(btn_fontInfo);
      Controls.Add(lbl_currentText);
      Controls.Add(label1);
      Name = "form_main";
      Text = "PDF Rasterizer Debugger";
      Load += Form1_Load;
      ((System.ComponentModel.ISupportInitialize)pb_mainImage).EndInit();
      panel1.ResumeLayout(false);
      panel1.PerformLayout();
      panel2.ResumeLayout(false);
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
    private Label lbl_currPosition;
    private Label label_5;
    private Label lbl_readPos;
    private Label label_7;
    private Label label4;
    private Label lbl_charValue;
    private Label label7;
    private Panel panel1;
    private Label lbl_glyphIndex;
    private Label label6;
    private Label lbl_glyphName;
    private TreeView treeView1;
    private Panel panel2;
    private Button btn_centerImage;
  }
}
