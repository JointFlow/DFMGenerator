
namespace DFMGenerator_Ocean
{
    partial class ExtractFractureTraces
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
            this.unitTextBox_EFT_Depth = new Slb.Ocean.Petrel.UI.Controls.UnitTextBox();
            this.label_EFT_Depth = new System.Windows.Forms.Label();
            this.presentationBox_EFT_DFNToExtract = new Slb.Ocean.Petrel.UI.Controls.PresentationBox();
            this.dropTarget_EFT_DFNToExtract = new Slb.Ocean.Petrel.UI.DropTarget();
            this.label_EFT_DFNToExtract = new System.Windows.Forms.Label();
            this.btn_EFT_Cancel = new System.Windows.Forms.Button();
            this.btn_EFT_OK = new System.Windows.Forms.Button();
            this.label_EFT_Depth_Units = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // unitTextBox_EFT_Depth
            // 
            this.unitTextBox_EFT_Depth.Location = new System.Drawing.Point(143, 45);
            this.unitTextBox_EFT_Depth.Name = "unitTextBox_EFT_Depth";
            this.unitTextBox_EFT_Depth.Size = new System.Drawing.Size(123, 20);
            this.unitTextBox_EFT_Depth.TabIndex = 108;
            // 
            // label_EFT_Depth
            // 
            this.label_EFT_Depth.AutoSize = true;
            this.label_EFT_Depth.Location = new System.Drawing.Point(12, 48);
            this.label_EFT_Depth.Name = "label_EFT_Depth";
            this.label_EFT_Depth.Size = new System.Drawing.Size(128, 13);
            this.label_EFT_Depth.TabIndex = 107;
            this.label_EFT_Depth.Text = "Depth of horizontal plane:";
            // 
            // presentationBox_EFT_DFNToExtract
            // 
            this.presentationBox_EFT_DFNToExtract.Location = new System.Drawing.Point(229, 12);
            this.presentationBox_EFT_DFNToExtract.Name = "presentationBox_EFT_DFNToExtract";
            this.presentationBox_EFT_DFNToExtract.Size = new System.Drawing.Size(281, 22);
            this.presentationBox_EFT_DFNToExtract.TabIndex = 106;
            this.presentationBox_EFT_DFNToExtract.KeyDown += new System.Windows.Forms.KeyEventHandler(this.presentationBox_EFT_DFNToExtract_KeyDown);
            // 
            // dropTarget_EFT_DFNToExtract
            // 
            this.dropTarget_EFT_DFNToExtract.AllowDrop = true;
            this.dropTarget_EFT_DFNToExtract.Location = new System.Drawing.Point(197, 12);
            this.dropTarget_EFT_DFNToExtract.Name = "dropTarget_EFT_DFNToExtract";
            this.dropTarget_EFT_DFNToExtract.Size = new System.Drawing.Size(26, 23);
            this.dropTarget_EFT_DFNToExtract.TabIndex = 105;
            this.dropTarget_EFT_DFNToExtract.DragDrop += new System.Windows.Forms.DragEventHandler(this.dropTarget_EFT_DFNToExtract_DragDrop);
            // 
            // label_EFT_DFNToExtract
            // 
            this.label_EFT_DFNToExtract.AutoSize = true;
            this.label_EFT_DFNToExtract.Location = new System.Drawing.Point(12, 17);
            this.label_EFT_DFNToExtract.Name = "label_EFT_DFNToExtract";
            this.label_EFT_DFNToExtract.Size = new System.Drawing.Size(182, 13);
            this.label_EFT_DFNToExtract.TabIndex = 104;
            this.label_EFT_DFNToExtract.Text = "DFN to extract horizontal traces from:";
            // 
            // btn_EFT_Cancel
            // 
            this.btn_EFT_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_EFT_Cancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_EFT_Cancel.Location = new System.Drawing.Point(435, 43);
            this.btn_EFT_Cancel.Name = "btn_EFT_Cancel";
            this.btn_EFT_Cancel.Size = new System.Drawing.Size(75, 23);
            this.btn_EFT_Cancel.TabIndex = 110;
            this.btn_EFT_Cancel.Text = "Close";
            this.btn_EFT_Cancel.UseVisualStyleBackColor = true;
            this.btn_EFT_Cancel.Click += new System.EventHandler(this.btn_EFT_Cancel_Click);
            // 
            // btn_EFT_OK
            // 
            this.btn_EFT_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_EFT_OK.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_EFT_OK.Location = new System.Drawing.Point(354, 43);
            this.btn_EFT_OK.Name = "btn_EFT_OK";
            this.btn_EFT_OK.Size = new System.Drawing.Size(75, 23);
            this.btn_EFT_OK.TabIndex = 109;
            this.btn_EFT_OK.Text = "Extract";
            this.btn_EFT_OK.UseVisualStyleBackColor = true;
            this.btn_EFT_OK.Click += new System.EventHandler(this.btn_EFT_OK_Click);
            // 
            // label_EFT_Depth_Units
            // 
            this.label_EFT_Depth_Units.AutoSize = true;
            this.label_EFT_Depth_Units.Location = new System.Drawing.Point(272, 48);
            this.label_EFT_Depth_Units.Name = "label_EFT_Depth_Units";
            this.label_EFT_Depth_Units.Size = new System.Drawing.Size(15, 13);
            this.label_EFT_Depth_Units.TabIndex = 111;
            this.label_EFT_Depth_Units.Text = "m";
            // 
            // ExtractFractureTraces
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(522, 78);
            this.Controls.Add(this.label_EFT_Depth_Units);
            this.Controls.Add(this.btn_EFT_Cancel);
            this.Controls.Add(this.btn_EFT_OK);
            this.Controls.Add(this.unitTextBox_EFT_Depth);
            this.Controls.Add(this.label_EFT_Depth);
            this.Controls.Add(this.presentationBox_EFT_DFNToExtract);
            this.Controls.Add(this.dropTarget_EFT_DFNToExtract);
            this.Controls.Add(this.label_EFT_DFNToExtract);
            this.Name = "ExtractFractureTraces";
            this.Text = "ExtractFractureTraces";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ExtractFractureTraces_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Slb.Ocean.Petrel.UI.Controls.UnitTextBox unitTextBox_EFT_Depth;
        private System.Windows.Forms.Label label_EFT_Depth;
        private Slb.Ocean.Petrel.UI.Controls.PresentationBox presentationBox_EFT_DFNToExtract;
        private Slb.Ocean.Petrel.UI.DropTarget dropTarget_EFT_DFNToExtract;
        private System.Windows.Forms.Label label_EFT_DFNToExtract;
        private System.Windows.Forms.Button btn_EFT_Cancel;
        private System.Windows.Forms.Button btn_EFT_OK;
        private System.Windows.Forms.Label label_EFT_Depth_Units;
    }
}