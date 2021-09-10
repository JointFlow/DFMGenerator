namespace DFNGenerator_Ocean
{
    partial class DFN_2UI
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.comboBox1 = new Slb.Ocean.Petrel.UI.Controls.ComboBox();
            this.basicButton1 = new Slb.Ocean.Petrel.UI.Controls.BasicButton();
            this.listBox1 = new Slb.Ocean.Petrel.UI.Controls.ListBox();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.Location = new System.Drawing.Point(342, 46);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(50, 23);
            this.comboBox1.TabIndex = 0;
            // 
            // basicButton1
            // 
            this.basicButton1.BackColor = System.Drawing.Color.White;
            this.basicButton1.Location = new System.Drawing.Point(317, 161);
            this.basicButton1.Name = "basicButton1";
            this.basicButton1.Size = new System.Drawing.Size(75, 23);
            this.basicButton1.TabIndex = 1;
            this.basicButton1.Text = "basicButton1";
            this.basicButton1.UseVisualStyleBackColor = false;
            // 
            // listBox1
            // 
            this.listBox1.Location = new System.Drawing.Point(78, 13);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(120, 96);
            this.listBox1.TabIndex = 2;
            // 
            // DFN_2UI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Aqua;
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.basicButton1);
            this.Controls.Add(this.comboBox1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "DFN_2UI";
            this.Size = new System.Drawing.Size(461, 258);
            this.Load += new System.EventHandler(this.DFN_2UI_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Slb.Ocean.Petrel.UI.Controls.ComboBox comboBox1;
        private Slb.Ocean.Petrel.UI.Controls.BasicButton basicButton1;
        private Slb.Ocean.Petrel.UI.Controls.ListBox listBox1;
    }
}
