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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
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
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.scriptbox);
            this.groupBox2.Controls.Add(this.textBox2);
            this.groupBox2.Controls.Add(this.execute);
            this.groupBox2.Controls.Add(this.button2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(12, 144);
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
            this.Controls.Add(this.MsgViewBox);
            this.Name = "Form1";
            this.Text = "Form1";
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
        private System.Windows.Forms.GroupBox groupBox2;
    }
}

