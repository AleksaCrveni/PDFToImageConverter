namespace RasterizeDebugger
{
  partial class Playground
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
      cb_glyph = new ComboBox();
      cb_font = new ComboBox();
      label1 = new Label();
      label2 = new Label();
      btn_raster = new Button();
      label3 = new Label();
      txb_Scale = new TextBox();
      pb_main = new PictureBox();
      btn_loadShape = new Button();
      label4 = new Label();
      cb_page = new ComboBox();
      label5 = new Label();
      lbl_fontType = new Label();
      ((System.ComponentModel.ISupportInitialize)pb_main).BeginInit();
      SuspendLayout();
      // 
      // cb_glyph
      // 
      cb_glyph.FormattingEnabled = true;
      cb_glyph.Location = new Point(361, 39);
      cb_glyph.Name = "cb_glyph";
      cb_glyph.Size = new Size(222, 23);
      cb_glyph.TabIndex = 0;
      // 
      // cb_font
      // 
      cb_font.FormattingEnabled = true;
      cb_font.Location = new Point(52, 39);
      cb_font.Name = "cb_font";
      cb_font.Size = new Size(256, 23);
      cb_font.TabIndex = 1;
      cb_font.SelectedIndexChanged += cb_font_SelectedIndexChanged;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(12, 42);
      label1.Name = "label1";
      label1.Size = new Size(34, 15);
      label1.TabIndex = 2;
      label1.Text = "Font:";
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(314, 42);
      label2.Name = "label2";
      label2.Size = new Size(41, 15);
      label2.TabIndex = 3;
      label2.Text = "Glyph:";
      // 
      // btn_raster
      // 
      btn_raster.Location = new Point(589, 39);
      btn_raster.Name = "btn_raster";
      btn_raster.Size = new Size(75, 23);
      btn_raster.TabIndex = 4;
      btn_raster.Text = "Raster";
      btn_raster.UseVisualStyleBackColor = true;
      btn_raster.Click += btn_raster_Click;
      // 
      // label3
      // 
      label3.AutoSize = true;
      label3.Location = new Point(670, 42);
      label3.Name = "label3";
      label3.Size = new Size(37, 15);
      label3.TabIndex = 6;
      label3.Text = "Scale:";
      label3.Click += label3_Click;
      // 
      // txb_Scale
      // 
      txb_Scale.Location = new Point(713, 39);
      txb_Scale.Name = "txb_Scale";
      txb_Scale.Size = new Size(100, 23);
      txb_Scale.TabIndex = 7;
      txb_Scale.Text = "1.0";
      // 
      // pb_main
      // 
      pb_main.Location = new Point(12, 68);
      pb_main.Name = "pb_main";
      pb_main.Size = new Size(927, 591);
      pb_main.TabIndex = 8;
      pb_main.TabStop = false;
      // 
      // btn_loadShape
      // 
      btn_loadShape.Location = new Point(819, 39);
      btn_loadShape.Name = "btn_loadShape";
      btn_loadShape.Size = new Size(120, 23);
      btn_loadShape.TabIndex = 9;
      btn_loadShape.Text = "Load Shape";
      btn_loadShape.UseVisualStyleBackColor = true;
      btn_loadShape.Click += btn_loadShape_Click;
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Location = new Point(12, 15);
      label4.Name = "label4";
      label4.Size = new Size(36, 15);
      label4.TabIndex = 11;
      label4.Text = "Page:";
      // 
      // cb_page
      // 
      cb_page.FormattingEnabled = true;
      cb_page.Location = new Point(52, 12);
      cb_page.Name = "cb_page";
      cb_page.Size = new Size(256, 23);
      cb_page.TabIndex = 10;
      cb_page.SelectedIndexChanged += cb_page_SelectedIndexChanged;
      // 
      // label5
      // 
      label5.AutoSize = true;
      label5.Location = new Point(314, 15);
      label5.Name = "label5";
      label5.Size = new Size(61, 15);
      label5.TabIndex = 12;
      label5.Text = "FontType :";
      // 
      // lbl_fontType
      // 
      lbl_fontType.AutoSize = true;
      lbl_fontType.Location = new Point(378, 15);
      lbl_fontType.Name = "lbl_fontType";
      lbl_fontType.Size = new Size(0, 15);
      lbl_fontType.TabIndex = 13;
      // 
      // Playground
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(951, 671);
      Controls.Add(lbl_fontType);
      Controls.Add(label5);
      Controls.Add(label4);
      Controls.Add(cb_page);
      Controls.Add(btn_loadShape);
      Controls.Add(pb_main);
      Controls.Add(txb_Scale);
      Controls.Add(label3);
      Controls.Add(btn_raster);
      Controls.Add(label2);
      Controls.Add(label1);
      Controls.Add(cb_font);
      Controls.Add(cb_glyph);
      Name = "Playground";
      Text = "Playground";
      Load += Playground_Load;
      ((System.ComponentModel.ISupportInitialize)pb_main).EndInit();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private ComboBox cb_glyph;
    private ComboBox cb_font;
    private Label label1;
    private Label label2;
    private Button btn_raster;
    private Label label3;
    private TextBox txb_Scale;
    private PictureBox pb_main;
    private Button btn_loadShape;
    private Label label4;
    private ComboBox cb_page;
    private Label label5;
    private Label lbl_fontType;
  }
}