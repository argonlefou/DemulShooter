namespace Ds_Diag
{
    partial class Ds_Diag
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

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Ds_Diag));
            this.Cbo_Dev = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label54 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.Cbox_MouseRight = new System.Windows.Forms.CheckBox();
            this.Cbox_MouseMiddle = new System.Windows.Forms.CheckBox();
            this.Cbox_MouseLeft = new System.Windows.Forms.CheckBox();
            this.Lbl_OnClient = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.Lbl_ClientSize = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.Lbl_ScreenSize = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.Lbl_OnScreen = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.Lbl_RawInput = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Pbox_Target = new System.Windows.Forms.PictureBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Pbox_Target)).BeginInit();
            this.SuspendLayout();
            // 
            // Cbo_Dev
            // 
            this.Cbo_Dev.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.Cbo_Dev.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbo_Dev.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Cbo_Dev.FormattingEnabled = true;
            this.Cbo_Dev.Location = new System.Drawing.Point(17, 36);
            this.Cbo_Dev.Margin = new System.Windows.Forms.Padding(5);
            this.Cbo_Dev.Name = "Cbo_Dev";
            this.Cbo_Dev.Size = new System.Drawing.Size(813, 28);
            this.Cbo_Dev.TabIndex = 21;
            this.Cbo_Dev.SelectionChangeCommitted += new System.EventHandler(this.Cbo_Dev_SelectionChangeCommitted);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(16, 11);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(124, 20);
            this.label1.TabIndex = 22;
            this.label1.Text = "Select a device :";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.label54);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.Cbox_MouseRight);
            this.groupBox1.Controls.Add(this.Cbox_MouseMiddle);
            this.groupBox1.Controls.Add(this.Cbox_MouseLeft);
            this.groupBox1.Controls.Add(this.Lbl_OnClient);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.Lbl_ClientSize);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.Lbl_ScreenSize);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.Lbl_OnScreen);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.Lbl_RawInput);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.ForeColor = System.Drawing.Color.White;
            this.groupBox1.Location = new System.Drawing.Point(17, 92);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(374, 246);
            this.groupBox1.TabIndex = 23;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Device Data [x, y]:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(195, 205);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(21, 20);
            this.label14.TabIndex = 40;
            this.label14.Text = "R";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(149, 205);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(22, 20);
            this.label13.TabIndex = 39;
            this.label13.Text = "M";
            // 
            // label54
            // 
            this.label54.AutoSize = true;
            this.label54.Location = new System.Drawing.Point(107, 205);
            this.label54.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label54.Name = "label54";
            this.label54.Size = new System.Drawing.Size(18, 20);
            this.label54.TabIndex = 38;
            this.label54.Text = "L";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(7, 184);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(73, 20);
            this.label12.TabIndex = 37;
            this.label12.Text = "Buttons :";
            // 
            // Cbox_MouseRight
            // 
            this.Cbox_MouseRight.AutoSize = true;
            this.Cbox_MouseRight.Location = new System.Drawing.Point(198, 188);
            this.Cbox_MouseRight.Name = "Cbox_MouseRight";
            this.Cbox_MouseRight.Size = new System.Drawing.Size(15, 14);
            this.Cbox_MouseRight.TabIndex = 36;
            this.Cbox_MouseRight.UseVisualStyleBackColor = true;
            // 
            // Cbox_MouseMiddle
            // 
            this.Cbox_MouseMiddle.AutoSize = true;
            this.Cbox_MouseMiddle.Location = new System.Drawing.Point(153, 188);
            this.Cbox_MouseMiddle.Name = "Cbox_MouseMiddle";
            this.Cbox_MouseMiddle.Size = new System.Drawing.Size(15, 14);
            this.Cbox_MouseMiddle.TabIndex = 35;
            this.Cbox_MouseMiddle.UseVisualStyleBackColor = true;
            // 
            // Cbox_MouseLeft
            // 
            this.Cbox_MouseLeft.AutoSize = true;
            this.Cbox_MouseLeft.Location = new System.Drawing.Point(108, 188);
            this.Cbox_MouseLeft.Name = "Cbox_MouseLeft";
            this.Cbox_MouseLeft.Size = new System.Drawing.Size(15, 14);
            this.Cbox_MouseLeft.TabIndex = 34;
            this.Cbox_MouseLeft.UseVisualStyleBackColor = true;
            // 
            // Lbl_OnClient
            // 
            this.Lbl_OnClient.AutoSize = true;
            this.Lbl_OnClient.Location = new System.Drawing.Point(107, 137);
            this.Lbl_OnClient.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_OnClient.Name = "Lbl_OnClient";
            this.Lbl_OnClient.Size = new System.Drawing.Size(83, 20);
            this.Lbl_OnClient.TabIndex = 33;
            this.Lbl_OnClient.Text = "[ 950, 492]";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(7, 137);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(78, 20);
            this.label10.TabIndex = 32;
            this.label10.Text = "OnClient :";
            // 
            // Lbl_ClientSize
            // 
            this.Lbl_ClientSize.AutoSize = true;
            this.Lbl_ClientSize.Location = new System.Drawing.Point(107, 49);
            this.Lbl_ClientSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_ClientSize.Name = "Lbl_ClientSize";
            this.Lbl_ClientSize.Size = new System.Drawing.Size(79, 20);
            this.Lbl_ClientSize.TabIndex = 31;
            this.Lbl_ClientSize.Text = "1024x768";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(7, 49);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(92, 20);
            this.label8.TabIndex = 30;
            this.label8.Text = "Client Size :";
            // 
            // Lbl_ScreenSize
            // 
            this.Lbl_ScreenSize.AutoSize = true;
            this.Lbl_ScreenSize.Location = new System.Drawing.Point(107, 29);
            this.Lbl_ScreenSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_ScreenSize.Name = "Lbl_ScreenSize";
            this.Lbl_ScreenSize.Size = new System.Drawing.Size(79, 20);
            this.Lbl_ScreenSize.TabIndex = 29;
            this.Lbl_ScreenSize.Text = "1024x768";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 29);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(103, 20);
            this.label6.TabIndex = 28;
            this.label6.Text = "Screen Size :";
            // 
            // Lbl_OnScreen
            // 
            this.Lbl_OnScreen.AutoSize = true;
            this.Lbl_OnScreen.Location = new System.Drawing.Point(107, 117);
            this.Lbl_OnScreen.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_OnScreen.Name = "Lbl_OnScreen";
            this.Lbl_OnScreen.Size = new System.Drawing.Size(83, 20);
            this.Lbl_OnScreen.TabIndex = 27;
            this.Lbl_OnScreen.Text = "[ 968, 492]";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 117);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(89, 20);
            this.label5.TabIndex = 26;
            this.label5.Text = "OnScreen :";
            // 
            // Lbl_RawInput
            // 
            this.Lbl_RawInput.AutoSize = true;
            this.Lbl_RawInput.Location = new System.Drawing.Point(107, 97);
            this.Lbl_RawInput.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_RawInput.Name = "Lbl_RawInput";
            this.Lbl_RawInput.Size = new System.Drawing.Size(139, 20);
            this.Lbl_RawInput.TabIndex = 25;
            this.Lbl_RawInput.Text = "[ 0x06B6, 0xFFFF]";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 97);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 20);
            this.label2.TabIndex = 24;
            this.label2.Text = "RawInput :";
            // 
            // Pbox_Target
            // 
            this.Pbox_Target.BackColor = System.Drawing.Color.Transparent;
            this.Pbox_Target.BackgroundImage = global::Ds_Diag.Properties.Resources.sharps1;
            this.Pbox_Target.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Pbox_Target.Location = new System.Drawing.Point(475, 160);
            this.Pbox_Target.Name = "Pbox_Target";
            this.Pbox_Target.Size = new System.Drawing.Size(80, 80);
            this.Pbox_Target.TabIndex = 24;
            this.Pbox_Target.TabStop = false;
            // 
            // Ds_Diag
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(844, 416);
            this.Controls.Add(this.Pbox_Target);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Cbo_Dev);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Ds_Diag";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DemulShooter Devices Diagnostic";
            this.TopMost = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Ds_Diag_Paint);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Pbox_Target)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox Cbo_Dev;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label54;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.CheckBox Cbox_MouseRight;
        private System.Windows.Forms.CheckBox Cbox_MouseMiddle;
        private System.Windows.Forms.CheckBox Cbox_MouseLeft;
        private System.Windows.Forms.Label Lbl_OnClient;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label Lbl_ClientSize;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label Lbl_ScreenSize;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label Lbl_OnScreen;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label Lbl_RawInput;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox Pbox_Target;

    }
}

