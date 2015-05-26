namespace MotionGestureCapture
{
    partial class MainGUI
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
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.initButton = new System.Windows.Forms.Button();
            this.mainLiveFeed = new System.Windows.Forms.PictureBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.mainAlteredFeed = new System.Windows.Forms.PictureBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.capButton = new System.Windows.Forms.Button();
            this.testingPic = new System.Windows.Forms.PictureBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.measuredGest = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.identifiedGest = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.oriDifference = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.measuredOri = new System.Windows.Forms.TextBox();
            this.identifiedOri = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.posMoE = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.measuredY = new System.Windows.Forms.TextBox();
            this.measuredX = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.identifiedY = new System.Windows.Forms.TextBox();
            this.identifiedX = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.mainLiveFeed)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainAlteredFeed)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.testingPic)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            // 
            // initButton
            // 
            this.initButton.Location = new System.Drawing.Point(6, 339);
            this.initButton.Name = "initButton";
            this.initButton.Size = new System.Drawing.Size(89, 36);
            this.initButton.TabIndex = 0;
            this.initButton.Text = "&Initialize";
            this.initButton.UseVisualStyleBackColor = true;
            this.initButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // mainLiveFeed
            // 
            this.mainLiveFeed.Location = new System.Drawing.Point(6, 6);
            this.mainLiveFeed.Name = "mainLiveFeed";
            this.mainLiveFeed.Size = new System.Drawing.Size(401, 327);
            this.mainLiveFeed.TabIndex = 1;
            this.mainLiveFeed.TabStop = false;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(22, 453);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(401, 24);
            this.comboBox1.TabIndex = 2;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(837, 411);
            this.tabControl1.TabIndex = 3;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.mainAlteredFeed);
            this.tabPage1.Controls.Add(this.mainLiveFeed);
            this.tabPage1.Controls.Add(this.initButton);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(829, 382);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "MainGUI";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // mainAlteredFeed
            // 
            this.mainAlteredFeed.Location = new System.Drawing.Point(418, 6);
            this.mainAlteredFeed.Name = "mainAlteredFeed";
            this.mainAlteredFeed.Size = new System.Drawing.Size(401, 327);
            this.mainAlteredFeed.TabIndex = 3;
            this.mainAlteredFeed.TabStop = false;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.capButton);
            this.tabPage2.Controls.Add(this.testingPic);
            this.tabPage2.Controls.Add(this.groupBox3);
            this.tabPage2.Controls.Add(this.groupBox2);
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(829, 382);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Testing";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // capButton
            // 
            this.capButton.Location = new System.Drawing.Point(7, 339);
            this.capButton.Name = "capButton";
            this.capButton.Size = new System.Drawing.Size(101, 37);
            this.capButton.TabIndex = 9;
            this.capButton.Text = "Capture";
            this.capButton.UseVisualStyleBackColor = true;
            // 
            // testingPic
            // 
            this.testingPic.Location = new System.Drawing.Point(7, 6);
            this.testingPic.Name = "testingPic";
            this.testingPic.Size = new System.Drawing.Size(401, 327);
            this.testingPic.TabIndex = 8;
            this.testingPic.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.comboBox2);
            this.groupBox3.Controls.Add(this.label12);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.measuredGest);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.identifiedGest);
            this.groupBox3.Location = new System.Drawing.Point(417, 217);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(405, 116);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Gesture";
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(299, 38);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(100, 24);
            this.comboBox2.TabIndex = 5;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(303, 20);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(78, 17);
            this.label12.TabIndex = 4;
            this.label12.Text = "Are Equal?";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(178, 20);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(71, 17);
            this.label11.TabIndex = 3;
            this.label11.Text = "Measured";
            // 
            // measuredGest
            // 
            this.measuredGest.Enabled = false;
            this.measuredGest.FormattingEnabled = true;
            this.measuredGest.Location = new System.Drawing.Point(165, 38);
            this.measuredGest.Name = "measuredGest";
            this.measuredGest.Size = new System.Drawing.Size(121, 24);
            this.measuredGest.TabIndex = 2;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(54, 18);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(65, 17);
            this.label10.TabIndex = 1;
            this.label10.Text = "Identified";
            // 
            // identifiedGest
            // 
            this.identifiedGest.FormattingEnabled = true;
            this.identifiedGest.Location = new System.Drawing.Point(37, 38);
            this.identifiedGest.Name = "identifiedGest";
            this.identifiedGest.Size = new System.Drawing.Size(121, 24);
            this.identifiedGest.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.oriDifference);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.measuredOri);
            this.groupBox2.Controls.Add(this.identifiedOri);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Location = new System.Drawing.Point(417, 131);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(406, 74);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Orientation";
            // 
            // oriDifference
            // 
            this.oriDifference.Enabled = false;
            this.oriDifference.Location = new System.Drawing.Point(299, 38);
            this.oriDifference.Name = "oriDifference";
            this.oriDifference.Size = new System.Drawing.Size(100, 22);
            this.oriDifference.TabIndex = 6;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(303, 18);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(73, 17);
            this.label9.TabIndex = 5;
            this.label9.Text = "Difference";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(194, 18);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 17);
            this.label8.TabIndex = 4;
            this.label8.Text = "Measured";
            // 
            // measuredOri
            // 
            this.measuredOri.Enabled = false;
            this.measuredOri.Location = new System.Drawing.Point(186, 38);
            this.measuredOri.Name = "measuredOri";
            this.measuredOri.Size = new System.Drawing.Size(100, 22);
            this.measuredOri.TabIndex = 3;
            // 
            // identifiedOri
            // 
            this.identifiedOri.Enabled = false;
            this.identifiedOri.Location = new System.Drawing.Point(80, 38);
            this.identifiedOri.Name = "identifiedOri";
            this.identifiedOri.Size = new System.Drawing.Size(100, 22);
            this.identifiedOri.TabIndex = 2;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 41);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 17);
            this.label7.TabIndex = 1;
            this.label7.Text = "Degrees";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(99, 18);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 17);
            this.label6.TabIndex = 0;
            this.label6.Text = "Identified";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.posMoE);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.measuredY);
            this.groupBox1.Controls.Add(this.measuredX);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.identifiedY);
            this.groupBox1.Controls.Add(this.identifiedX);
            this.groupBox1.Location = new System.Drawing.Point(417, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(406, 108);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Position";
            // 
            // textBox1
            // 
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(262, 41);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(116, 22);
            this.textBox1.TabIndex = 10;
            // 
            // posMoE
            // 
            this.posMoE.AutoSize = true;
            this.posMoE.Location = new System.Drawing.Point(268, 24);
            this.posMoE.Name = "posMoE";
            this.posMoE.Size = new System.Drawing.Size(103, 17);
            this.posMoE.TabIndex = 9;
            this.posMoE.Text = "Margin of Error";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(157, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(71, 17);
            this.label5.TabIndex = 8;
            this.label5.Text = "Measured";
            // 
            // measuredY
            // 
            this.measuredY.Enabled = false;
            this.measuredY.Location = new System.Drawing.Point(143, 69);
            this.measuredY.Name = "measuredY";
            this.measuredY.Size = new System.Drawing.Size(100, 22);
            this.measuredY.TabIndex = 7;
            // 
            // measuredX
            // 
            this.measuredX.Enabled = false;
            this.measuredX.Location = new System.Drawing.Point(143, 41);
            this.measuredX.Name = "measuredX";
            this.measuredX.Size = new System.Drawing.Size(100, 22);
            this.measuredX.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(54, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 17);
            this.label3.TabIndex = 5;
            this.label3.Text = "Identified";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 44);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(17, 17);
            this.label4.TabIndex = 5;
            this.label4.Text = "X";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(17, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "Y";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 17);
            this.label1.TabIndex = 3;
            // 
            // identifiedY
            // 
            this.identifiedY.Enabled = false;
            this.identifiedY.Location = new System.Drawing.Point(37, 69);
            this.identifiedY.Name = "identifiedY";
            this.identifiedY.Size = new System.Drawing.Size(100, 22);
            this.identifiedY.TabIndex = 2;
            // 
            // identifiedX
            // 
            this.identifiedX.Enabled = false;
            this.identifiedX.Location = new System.Drawing.Point(37, 41);
            this.identifiedX.Name = "identifiedX";
            this.identifiedX.Size = new System.Drawing.Size(100, 22);
            this.identifiedX.TabIndex = 1;
            this.identifiedX.Tag = "";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(19, 433);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(105, 17);
            this.label13.TabIndex = 4;
            this.label13.Text = "Capture Device";
            // 
            // MainGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(861, 490);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.comboBox1);
            this.Name = "MainGUI";
            this.Text = "MainGUI";
            ((System.ComponentModel.ISupportInitialize)(this.mainLiveFeed)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainAlteredFeed)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.testingPic)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Button initButton;
        private System.Windows.Forms.PictureBox mainLiveFeed;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox identifiedX;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox identifiedY;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox measuredY;
        private System.Windows.Forms.TextBox measuredX;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox oriDifference;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox measuredOri;
        private System.Windows.Forms.TextBox identifiedOri;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label posMoE;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox measuredGest;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox identifiedGest;
        private System.Windows.Forms.PictureBox mainAlteredFeed;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button capButton;
        private System.Windows.Forms.PictureBox testingPic;
    }
}

