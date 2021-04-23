namespace DemulShooter_GUI
{
    partial class GUI_AnalogCalibration
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
            this.Cbox_Player = new System.Windows.Forms.CheckBox();
            this.Gbox_P1_Calib = new System.Windows.Forms.GroupBox();
            this.Btn_Init_Calib = new System.Windows.Forms.Button();
            this.Btn_Stop_Calib = new System.Windows.Forms.Button();
            this.Btn_Start_Calib = new System.Windows.Forms.Button();
            this.label28 = new System.Windows.Forms.Label();
            this.Txt_Calib_Ymax = new System.Windows.Forms.TextBox();
            this.label29 = new System.Windows.Forms.Label();
            this.Txt_Calib_Ymin = new System.Windows.Forms.TextBox();
            this.label32 = new System.Windows.Forms.Label();
            this.Txt_Calib_Xmax = new System.Windows.Forms.TextBox();
            this.label36 = new System.Windows.Forms.Label();
            this.Txt_Calib_Xmin = new System.Windows.Forms.TextBox();
            this.Gbox_P1_Calib.SuspendLayout();
            this.SuspendLayout();
            // 
            // Cbox_Player
            // 
            this.Cbox_Player.AutoSize = true;
            this.Cbox_Player.Location = new System.Drawing.Point(17, 0);
            this.Cbox_Player.Margin = new System.Windows.Forms.Padding(4);
            this.Cbox_Player.Name = "Cbox_Player";
            this.Cbox_Player.Size = new System.Drawing.Size(126, 20);
            this.Cbox_Player.TabIndex = 0;
            this.Cbox_Player.Text = "Override P1 Axis";
            this.Cbox_Player.UseVisualStyleBackColor = true;
            this.Cbox_Player.CheckedChanged += new System.EventHandler(this.Cbox_Player_CheckedChanged);
            // 
            // Gbox_P1_Calib
            // 
            this.Gbox_P1_Calib.Controls.Add(this.Btn_Init_Calib);
            this.Gbox_P1_Calib.Controls.Add(this.Btn_Stop_Calib);
            this.Gbox_P1_Calib.Controls.Add(this.Btn_Start_Calib);
            this.Gbox_P1_Calib.Controls.Add(this.label28);
            this.Gbox_P1_Calib.Controls.Add(this.Txt_Calib_Ymax);
            this.Gbox_P1_Calib.Controls.Add(this.label29);
            this.Gbox_P1_Calib.Controls.Add(this.Txt_Calib_Ymin);
            this.Gbox_P1_Calib.Controls.Add(this.label32);
            this.Gbox_P1_Calib.Controls.Add(this.Txt_Calib_Xmax);
            this.Gbox_P1_Calib.Controls.Add(this.label36);
            this.Gbox_P1_Calib.Controls.Add(this.Txt_Calib_Xmin);
            this.Gbox_P1_Calib.Controls.Add(this.Cbox_Player);
            this.Gbox_P1_Calib.Location = new System.Drawing.Point(4, 4);
            this.Gbox_P1_Calib.Margin = new System.Windows.Forms.Padding(4);
            this.Gbox_P1_Calib.Name = "Gbox_P1_Calib";
            this.Gbox_P1_Calib.Padding = new System.Windows.Forms.Padding(4);
            this.Gbox_P1_Calib.Size = new System.Drawing.Size(266, 120);
            this.Gbox_P1_Calib.TabIndex = 56;
            this.Gbox_P1_Calib.TabStop = false;
            this.Gbox_P1_Calib.Text = "                                          ";
            // 
            // Btn_Init_Calib
            // 
            this.Btn_Init_Calib.Enabled = false;
            this.Btn_Init_Calib.Location = new System.Drawing.Point(8, 87);
            this.Btn_Init_Calib.Margin = new System.Windows.Forms.Padding(4);
            this.Btn_Init_Calib.Name = "Btn_Init_Calib";
            this.Btn_Init_Calib.Size = new System.Drawing.Size(74, 25);
            this.Btn_Init_Calib.TabIndex = 66;
            this.Btn_Init_Calib.Text = "Default";
            this.Btn_Init_Calib.UseVisualStyleBackColor = true;
            this.Btn_Init_Calib.Click += new System.EventHandler(this.Btn_Init_Calib_Click);
            // 
            // Btn_Stop_Calib
            // 
            this.Btn_Stop_Calib.Enabled = false;
            this.Btn_Stop_Calib.Location = new System.Drawing.Point(185, 88);
            this.Btn_Stop_Calib.Margin = new System.Windows.Forms.Padding(4);
            this.Btn_Stop_Calib.Name = "Btn_Stop_Calib";
            this.Btn_Stop_Calib.Size = new System.Drawing.Size(74, 25);
            this.Btn_Stop_Calib.TabIndex = 65;
            this.Btn_Stop_Calib.Text = "Stop";
            this.Btn_Stop_Calib.UseVisualStyleBackColor = true;
            this.Btn_Stop_Calib.Click += new System.EventHandler(this.Btn_Stop_Calib_Click);
            // 
            // Btn_Start_Calib
            // 
            this.Btn_Start_Calib.Enabled = false;
            this.Btn_Start_Calib.Location = new System.Drawing.Point(103, 88);
            this.Btn_Start_Calib.Margin = new System.Windows.Forms.Padding(4);
            this.Btn_Start_Calib.Name = "Btn_Start_Calib";
            this.Btn_Start_Calib.Size = new System.Drawing.Size(74, 25);
            this.Btn_Start_Calib.TabIndex = 64;
            this.Btn_Start_Calib.Text = "Start";
            this.Btn_Start_Calib.UseVisualStyleBackColor = true;
            this.Btn_Start_Calib.Click += new System.EventHandler(this.Btn_Start_Calib_Click);
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(151, 52);
            this.label28.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(51, 16);
            this.label28.TabIndex = 63;
            this.label28.Text = "Y Max :";
            // 
            // Txt_Calib_Ymax
            // 
            this.Txt_Calib_Ymax.Enabled = false;
            this.Txt_Calib_Ymax.Location = new System.Drawing.Point(205, 49);
            this.Txt_Calib_Ymax.Margin = new System.Windows.Forms.Padding(4);
            this.Txt_Calib_Ymax.Name = "Txt_Calib_Ymax";
            this.Txt_Calib_Ymax.ReadOnly = true;
            this.Txt_Calib_Ymax.Size = new System.Drawing.Size(54, 22);
            this.Txt_Calib_Ymax.TabIndex = 62;
            this.Txt_Calib_Ymax.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_Calib_Ymax.TextChanged += new System.EventHandler(this.Txt_Calib_Ymax_TextChanged);
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(151, 28);
            this.label29.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(47, 16);
            this.label29.TabIndex = 61;
            this.label29.Text = "Y Min :";
            // 
            // Txt_Calib_Ymin
            // 
            this.Txt_Calib_Ymin.Enabled = false;
            this.Txt_Calib_Ymin.Location = new System.Drawing.Point(205, 25);
            this.Txt_Calib_Ymin.Margin = new System.Windows.Forms.Padding(4);
            this.Txt_Calib_Ymin.Name = "Txt_Calib_Ymin";
            this.Txt_Calib_Ymin.ReadOnly = true;
            this.Txt_Calib_Ymin.Size = new System.Drawing.Size(54, 22);
            this.Txt_Calib_Ymin.TabIndex = 60;
            this.Txt_Calib_Ymin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_Calib_Ymin.TextChanged += new System.EventHandler(this.Txt_Calib_Ymin_TextChanged);
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(10, 52);
            this.label32.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(50, 16);
            this.label32.TabIndex = 4;
            this.label32.Text = "X Max :";
            // 
            // Txt_Calib_Xmax
            // 
            this.Txt_Calib_Xmax.Enabled = false;
            this.Txt_Calib_Xmax.Location = new System.Drawing.Point(64, 49);
            this.Txt_Calib_Xmax.Margin = new System.Windows.Forms.Padding(4);
            this.Txt_Calib_Xmax.Name = "Txt_Calib_Xmax";
            this.Txt_Calib_Xmax.ReadOnly = true;
            this.Txt_Calib_Xmax.Size = new System.Drawing.Size(54, 22);
            this.Txt_Calib_Xmax.TabIndex = 3;
            this.Txt_Calib_Xmax.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_Calib_Xmax.TextChanged += new System.EventHandler(this.Txt_Calib_Xmax_TextChanged);
            // 
            // label36
            // 
            this.label36.AutoSize = true;
            this.label36.Location = new System.Drawing.Point(10, 28);
            this.label36.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(46, 16);
            this.label36.TabIndex = 2;
            this.label36.Text = "X Min :";
            // 
            // Txt_Calib_Xmin
            // 
            this.Txt_Calib_Xmin.Enabled = false;
            this.Txt_Calib_Xmin.Location = new System.Drawing.Point(64, 25);
            this.Txt_Calib_Xmin.Margin = new System.Windows.Forms.Padding(4);
            this.Txt_Calib_Xmin.Name = "Txt_Calib_Xmin";
            this.Txt_Calib_Xmin.ReadOnly = true;
            this.Txt_Calib_Xmin.Size = new System.Drawing.Size(54, 22);
            this.Txt_Calib_Xmin.TabIndex = 1;
            this.Txt_Calib_Xmin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.Txt_Calib_Xmin.TextChanged += new System.EventHandler(this.Txt_Calib_Xmin_TextChanged);
            // 
            // GUI_AnalogCalibration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Gbox_P1_Calib);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "GUI_AnalogCalibration";
            this.Size = new System.Drawing.Size(277, 135);
            this.Gbox_P1_Calib.ResumeLayout(false);
            this.Gbox_P1_Calib.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox Cbox_Player;
        private System.Windows.Forms.GroupBox Gbox_P1_Calib;
        private System.Windows.Forms.Button Btn_Start_Calib;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.TextBox Txt_Calib_Ymax;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.TextBox Txt_Calib_Ymin;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.TextBox Txt_Calib_Xmax;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.TextBox Txt_Calib_Xmin;
        private System.Windows.Forms.Button Btn_Init_Calib;
        private System.Windows.Forms.Button Btn_Stop_Calib;
    }
}
