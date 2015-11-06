namespace PuppetMaster
{
    partial class Form1
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
            this.scriptbox = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.execute = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.MsgViewBox = new System.Windows.Forms.ListBox();
            this.typePM = new System.Windows.Forms.TextBox();
            this.PuppetMURL = new System.Windows.Forms.TextBox();
            this.Type = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.siteBox = new System.Windows.Forms.TextBox();
            this.addSlave = new System.Windows.Forms.Button();
            this.SlaveURL = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.PMConfig = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // scriptbox
            // 
            this.scriptbox.Location = new System.Drawing.Point(82, 34);
            this.scriptbox.Name = "scriptbox";
            this.scriptbox.Size = new System.Drawing.Size(269, 20);
            this.scriptbox.TabIndex = 0;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(82, 79);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(268, 20);
            this.textBox2.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Source:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Command:";
            // 
            // execute
            // 
            this.execute.Location = new System.Drawing.Point(383, 34);
            this.execute.Name = "execute";
            this.execute.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.execute.Size = new System.Drawing.Size(107, 25);
            this.execute.TabIndex = 4;
            this.execute.Text = "Execute";
            this.execute.UseVisualStyleBackColor = true;
            this.execute.Click += new System.EventHandler(this.execute_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(383, 79);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(106, 26);
            this.button2.TabIndex = 5;
            this.button2.Text = "Send";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // MsgViewBox
            // 
            this.MsgViewBox.BackColor = System.Drawing.SystemColors.WindowText;
            this.MsgViewBox.ForeColor = System.Drawing.Color.Lime;
            this.MsgViewBox.FormattingEnabled = true;
            this.MsgViewBox.Location = new System.Drawing.Point(607, 12);
            this.MsgViewBox.Name = "MsgViewBox";
            this.MsgViewBox.Size = new System.Drawing.Size(392, 420);
            this.MsgViewBox.TabIndex = 6;
            this.MsgViewBox.SelectedIndexChanged += new System.EventHandler(this.MsgViewBox_SelectedIndexChanged);
            // 
            // typePM
            // 
            this.typePM.Location = new System.Drawing.Point(82, 25);
            this.typePM.Name = "typePM";
            this.typePM.Size = new System.Drawing.Size(100, 20);
            this.typePM.TabIndex = 7;
            // 
            // PuppetMURL
            // 
            this.PuppetMURL.Location = new System.Drawing.Point(82, 51);
            this.PuppetMURL.Name = "PuppetMURL";
            this.PuppetMURL.Size = new System.Drawing.Size(268, 20);
            this.PuppetMURL.TabIndex = 8;
            // 
            // Type
            // 
            this.Type.AutoSize = true;
            this.Type.Location = new System.Drawing.Point(6, 25);
            this.Type.Name = "Type";
            this.Type.Size = new System.Drawing.Size(31, 13);
            this.Type.TabIndex = 9;
            this.Type.Text = "Type";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 51);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "PM URL";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.siteBox);
            this.groupBox1.Controls.Add(this.addSlave);
            this.groupBox1.Controls.Add(this.SlaveURL);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.PMConfig);
            this.groupBox1.Controls.Add(this.Type);
            this.groupBox1.Controls.Add(this.typePM);
            this.groupBox1.Controls.Add(this.PuppetMURL);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(2, 48);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(466, 117);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "PuppetMaster";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(219, 25);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(25, 13);
            this.label6.TabIndex = 16;
            this.label6.Text = "Site";
            // 
            // siteBox
            // 
            this.siteBox.Location = new System.Drawing.Point(250, 25);
            this.siteBox.Name = "siteBox";
            this.siteBox.Size = new System.Drawing.Size(100, 20);
            this.siteBox.TabIndex = 15;
            // 
            // addSlave
            // 
            this.addSlave.Location = new System.Drawing.Point(372, 87);
            this.addSlave.Name = "addSlave";
            this.addSlave.Size = new System.Drawing.Size(75, 23);
            this.addSlave.TabIndex = 14;
            this.addSlave.Text = "Add";
            this.addSlave.UseVisualStyleBackColor = true;
            // 
            // SlaveURL
            // 
            this.SlaveURL.Location = new System.Drawing.Point(82, 87);
            this.SlaveURL.Name = "SlaveURL";
            this.SlaveURL.Size = new System.Drawing.Size(268, 20);
            this.SlaveURL.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Slave URL";
            // 
            // PMConfig
            // 
            this.PMConfig.Location = new System.Drawing.Point(372, 51);
            this.PMConfig.Name = "PMConfig";
            this.PMConfig.Size = new System.Drawing.Size(75, 23);
            this.PMConfig.TabIndex = 11;
            this.PMConfig.Text = "Configure";
            this.PMConfig.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.scriptbox);
            this.groupBox2.Controls.Add(this.textBox2);
            this.groupBox2.Controls.Add(this.execute);
            this.groupBox2.Controls.Add(this.button2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(2, 182);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(518, 125);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Scripts";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1009, 445);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.MsgViewBox);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        private void MsgViewBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
        
        }

        #endregion

        private System.Windows.Forms.TextBox scriptbox;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button execute;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ListBox MsgViewBox;
        private System.Windows.Forms.TextBox typePM;
        private System.Windows.Forms.TextBox PuppetMURL;
        private System.Windows.Forms.Label Type;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button PMConfig;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox SlaveURL;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button addSlave;
        private System.Windows.Forms.TextBox siteBox;
        private System.Windows.Forms.Label label6;
    }
}

