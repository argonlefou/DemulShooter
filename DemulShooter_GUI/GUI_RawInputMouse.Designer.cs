namespace DemulShooter_GUI
{
    partial class GUI_RawInputMouse
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.Gbox_GunOptions = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Txt_VirtualRightBtn = new System.Windows.Forms.TextBox();
            this.Lbl_MidButonEnable = new System.Windows.Forms.Label();
            this.Chk_VirtualMiddleBtn = new System.Windows.Forms.CheckBox();
            this.label36 = new System.Windows.Forms.Label();
            this.Txt_VirtualMiddleBtn = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Pnl_ButtonsViewer = new System.Windows.Forms.FlowLayoutPanel();
            this.Pnl_AxisViewer = new System.Windows.Forms.Panel();
            this.Gbox_GunOptions.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Gbox_GunOptions
            // 
            this.Gbox_GunOptions.Controls.Add(this.label1);
            this.Gbox_GunOptions.Controls.Add(this.Txt_VirtualRightBtn);
            this.Gbox_GunOptions.Controls.Add(this.Lbl_MidButonEnable);
            this.Gbox_GunOptions.Controls.Add(this.Chk_VirtualMiddleBtn);
            this.Gbox_GunOptions.Controls.Add(this.label36);
            this.Gbox_GunOptions.Controls.Add(this.Txt_VirtualMiddleBtn);
            this.Gbox_GunOptions.Location = new System.Drawing.Point(3, 3);
            this.Gbox_GunOptions.Name = "Gbox_GunOptions";
            this.Gbox_GunOptions.Size = new System.Drawing.Size(223, 189);
            this.Gbox_GunOptions.TabIndex = 44;
            this.Gbox_GunOptions.TabStop = false;
            this.Gbox_GunOptions.Text = "Device Options";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 133);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 13);
            this.label1.TabIndex = 56;
            this.label1.Text = "\"Right Mouse\"  Key :";
            // 
            // Txt_VirtualRightBtn
            // 
            this.Txt_VirtualRightBtn.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Txt_VirtualRightBtn.Enabled = false;
            this.Txt_VirtualRightBtn.Location = new System.Drawing.Point(7, 152);
            this.Txt_VirtualRightBtn.Name = "Txt_VirtualRightBtn";
            this.Txt_VirtualRightBtn.ReadOnly = true;
            this.Txt_VirtualRightBtn.Size = new System.Drawing.Size(132, 20);
            this.Txt_VirtualRightBtn.TabIndex = 57;
            this.Txt_VirtualRightBtn.MouseClick += new System.Windows.Forms.MouseEventHandler(this.TXT_DIK_MouseClick);
            // 
            // Lbl_MidButonEnable
            // 
            this.Lbl_MidButonEnable.AutoSize = true;
            this.Lbl_MidButonEnable.Location = new System.Drawing.Point(30, 37);
            this.Lbl_MidButonEnable.Name = "Lbl_MidButonEnable";
            this.Lbl_MidButonEnable.Size = new System.Drawing.Size(154, 13);
            this.Lbl_MidButonEnable.TabIndex = 13;
            this.Lbl_MidButonEnable.Text = "Enable \"Virtual\" mouse buttons";
            // 
            // Chk_VirtualMiddleBtn
            // 
            this.Chk_VirtualMiddleBtn.AutoSize = true;
            this.Chk_VirtualMiddleBtn.Location = new System.Drawing.Point(9, 37);
            this.Chk_VirtualMiddleBtn.Name = "Chk_VirtualMiddleBtn";
            this.Chk_VirtualMiddleBtn.Size = new System.Drawing.Size(15, 14);
            this.Chk_VirtualMiddleBtn.TabIndex = 55;
            this.Chk_VirtualMiddleBtn.UseVisualStyleBackColor = true;
            this.Chk_VirtualMiddleBtn.CheckedChanged += new System.EventHandler(this.Chk_VirtualMiddleBtn_CheckedChanged);
            // 
            // label36
            // 
            this.label36.AutoSize = true;
            this.label36.Location = new System.Drawing.Point(6, 75);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(110, 13);
            this.label36.TabIndex = 13;
            this.label36.Text = "\"Middle Mouse\" Key :";
            // 
            // Txt_VirtualMiddleBtn
            // 
            this.Txt_VirtualMiddleBtn.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Txt_VirtualMiddleBtn.Enabled = false;
            this.Txt_VirtualMiddleBtn.Location = new System.Drawing.Point(7, 94);
            this.Txt_VirtualMiddleBtn.Name = "Txt_VirtualMiddleBtn";
            this.Txt_VirtualMiddleBtn.ReadOnly = true;
            this.Txt_VirtualMiddleBtn.Size = new System.Drawing.Size(132, 20);
            this.Txt_VirtualMiddleBtn.TabIndex = 54;
            this.Txt_VirtualMiddleBtn.MouseClick += new System.Windows.Forms.MouseEventHandler(this.TXT_DIK_MouseClick);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Pnl_ButtonsViewer);
            this.groupBox1.Controls.Add(this.Pnl_AxisViewer);
            this.groupBox1.Location = new System.Drawing.Point(232, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(341, 189);
            this.groupBox1.TabIndex = 46;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Device Preview";
            // 
            // Pnl_ButtonsViewer
            // 
            this.Pnl_ButtonsViewer.Location = new System.Drawing.Point(199, 32);
            this.Pnl_ButtonsViewer.Name = "Pnl_ButtonsViewer";
            this.Pnl_ButtonsViewer.Size = new System.Drawing.Size(136, 142);
            this.Pnl_ButtonsViewer.TabIndex = 47;
            // 
            // Pnl_AxisViewer
            // 
            this.Pnl_AxisViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Pnl_AxisViewer.Location = new System.Drawing.Point(17, 32);
            this.Pnl_AxisViewer.Name = "Pnl_AxisViewer";
            this.Pnl_AxisViewer.Size = new System.Drawing.Size(146, 142);
            this.Pnl_AxisViewer.TabIndex = 44;
            this.Pnl_AxisViewer.Paint += new System.Windows.Forms.PaintEventHandler(this.Pnl_AxisViewer_Paint);
            // 
            // GUI_RawInputMouse
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Gbox_GunOptions);
            this.Name = "GUI_RawInputMouse";
            this.Size = new System.Drawing.Size(576, 202);
            this.Gbox_GunOptions.ResumeLayout(false);
            this.Gbox_GunOptions.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox Gbox_GunOptions;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox Txt_VirtualRightBtn;
        private System.Windows.Forms.Label Lbl_MidButonEnable;
        private System.Windows.Forms.CheckBox Chk_VirtualMiddleBtn;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.TextBox Txt_VirtualMiddleBtn;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Panel Pnl_AxisViewer;
        private System.Windows.Forms.FlowLayoutPanel Pnl_ButtonsViewer;
    }
}
