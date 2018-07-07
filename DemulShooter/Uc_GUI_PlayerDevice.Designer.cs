namespace DemulShooter
{
    partial class Uc_GUI_PlayerDevice
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
            this.Lbl_MidButonEnable = new System.Windows.Forms.Label();
            this.Chk_VirtualMiddleBtn = new System.Windows.Forms.CheckBox();
            this.label36 = new System.Windows.Forms.Label();
            this.Txt_VirtualMiddleBtn = new System.Windows.Forms.TextBox();
            this.Gbox_PadOptions = new System.Windows.Forms.GroupBox();
            this.Chk_VibrationEnable = new System.Windows.Forms.CheckBox();
            this.Tbar_VibrationStrength = new System.Windows.Forms.TrackBar();
            this.Tbar_VibrationLength = new System.Windows.Forms.TrackBar();
            this.label27 = new System.Windows.Forms.Label();
            this.label28 = new System.Windows.Forms.Label();
            this.label29 = new System.Windows.Forms.Label();
            this.Cbox_Pad_Axis = new System.Windows.Forms.ComboBox();
            this.Cbox_Pad_MouseRight = new System.Windows.Forms.ComboBox();
            this.Cbox_Pad_MouseMiddle = new System.Windows.Forms.ComboBox();
            this.Cbox_Pad_MouseLeft = new System.Windows.Forms.ComboBox();
            this.label30 = new System.Windows.Forms.Label();
            this.label31 = new System.Windows.Forms.Label();
            this.label32 = new System.Windows.Forms.Label();
            this.Cbo_Device = new System.Windows.Forms.ComboBox();
            this.Lbl_Player = new System.Windows.Forms.Label();
            this.Gbox_GunOptions.SuspendLayout();
            this.Gbox_PadOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Tbar_VibrationStrength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Tbar_VibrationLength)).BeginInit();
            this.SuspendLayout();
            // 
            // Gbox_GunOptions
            // 
            this.Gbox_GunOptions.Controls.Add(this.Lbl_MidButonEnable);
            this.Gbox_GunOptions.Controls.Add(this.Chk_VirtualMiddleBtn);
            this.Gbox_GunOptions.Controls.Add(this.label36);
            this.Gbox_GunOptions.Controls.Add(this.Txt_VirtualMiddleBtn);
            this.Gbox_GunOptions.Enabled = false;
            this.Gbox_GunOptions.Location = new System.Drawing.Point(4, 65);
            this.Gbox_GunOptions.Name = "Gbox_GunOptions";
            this.Gbox_GunOptions.Size = new System.Drawing.Size(145, 131);
            this.Gbox_GunOptions.TabIndex = 43;
            this.Gbox_GunOptions.TabStop = false;
            this.Gbox_GunOptions.Text = "Gun Options";
            // 
            // Lbl_MidButonEnable
            // 
            this.Lbl_MidButonEnable.AutoSize = true;
            this.Lbl_MidButonEnable.Location = new System.Drawing.Point(30, 28);
            this.Lbl_MidButonEnable.Name = "Lbl_MidButonEnable";
            this.Lbl_MidButonEnable.Size = new System.Drawing.Size(82, 26);
            this.Lbl_MidButonEnable.TabIndex = 13;
            this.Lbl_MidButonEnable.Text = "Enable \"Virtual\"\r\nmiddle button";
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
            this.label36.Size = new System.Drawing.Size(79, 13);
            this.label36.TabIndex = 13;
            this.label36.Text = "Keyboard Key :";
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
            this.Txt_VirtualMiddleBtn.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Txt_VirtualMiddleBtn_Click);
            // 
            // Gbox_PadOptions
            // 
            this.Gbox_PadOptions.Controls.Add(this.Chk_VibrationEnable);
            this.Gbox_PadOptions.Controls.Add(this.Tbar_VibrationStrength);
            this.Gbox_PadOptions.Controls.Add(this.Tbar_VibrationLength);
            this.Gbox_PadOptions.Controls.Add(this.label27);
            this.Gbox_PadOptions.Controls.Add(this.label28);
            this.Gbox_PadOptions.Controls.Add(this.label29);
            this.Gbox_PadOptions.Controls.Add(this.Cbox_Pad_Axis);
            this.Gbox_PadOptions.Controls.Add(this.Cbox_Pad_MouseRight);
            this.Gbox_PadOptions.Controls.Add(this.Cbox_Pad_MouseMiddle);
            this.Gbox_PadOptions.Controls.Add(this.Cbox_Pad_MouseLeft);
            this.Gbox_PadOptions.Controls.Add(this.label30);
            this.Gbox_PadOptions.Controls.Add(this.label31);
            this.Gbox_PadOptions.Controls.Add(this.label32);
            this.Gbox_PadOptions.Enabled = false;
            this.Gbox_PadOptions.Location = new System.Drawing.Point(155, 65);
            this.Gbox_PadOptions.Name = "Gbox_PadOptions";
            this.Gbox_PadOptions.Size = new System.Drawing.Size(414, 189);
            this.Gbox_PadOptions.TabIndex = 42;
            this.Gbox_PadOptions.TabStop = false;
            this.Gbox_PadOptions.Text = "GamePad Options";
            // 
            // Chk_VibrationEnable
            // 
            this.Chk_VibrationEnable.AutoSize = true;
            this.Chk_VibrationEnable.Location = new System.Drawing.Point(242, 21);
            this.Chk_VibrationEnable.Name = "Chk_VibrationEnable";
            this.Chk_VibrationEnable.Size = new System.Drawing.Size(111, 17);
            this.Chk_VibrationEnable.TabIndex = 12;
            this.Chk_VibrationEnable.Text = "Vibration on shoot";
            this.Chk_VibrationEnable.UseVisualStyleBackColor = true;
            this.Chk_VibrationEnable.CheckedChanged += new System.EventHandler(this.Chk_VibrationEnable_CheckedChanged);
            // 
            // Tbar_VibrationStrength
            // 
            this.Tbar_VibrationStrength.LargeChange = 10000;
            this.Tbar_VibrationStrength.Location = new System.Drawing.Point(233, 139);
            this.Tbar_VibrationStrength.Maximum = 65535;
            this.Tbar_VibrationStrength.Name = "Tbar_VibrationStrength";
            this.Tbar_VibrationStrength.Size = new System.Drawing.Size(180, 45);
            this.Tbar_VibrationStrength.SmallChange = 1000;
            this.Tbar_VibrationStrength.TabIndex = 11;
            this.Tbar_VibrationStrength.TickFrequency = 10000;
            this.Tbar_VibrationStrength.ValueChanged += new System.EventHandler(this.Tbar_VibrationStrength_ValueChanged);
            // 
            // Tbar_VibrationLength
            // 
            this.Tbar_VibrationLength.LargeChange = 50;
            this.Tbar_VibrationLength.Location = new System.Drawing.Point(233, 74);
            this.Tbar_VibrationLength.Maximum = 200;
            this.Tbar_VibrationLength.Minimum = 50;
            this.Tbar_VibrationLength.Name = "Tbar_VibrationLength";
            this.Tbar_VibrationLength.Size = new System.Drawing.Size(180, 45);
            this.Tbar_VibrationLength.SmallChange = 10;
            this.Tbar_VibrationLength.TabIndex = 10;
            this.Tbar_VibrationLength.TickFrequency = 50;
            this.Tbar_VibrationLength.Value = 50;
            this.Tbar_VibrationLength.ValueChanged += new System.EventHandler(this.Tbar_VibrationLength_ValueChanged);
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(239, 120);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(95, 13);
            this.label27.TabIndex = 9;
            this.label27.Text = "Vibration strength :";
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(239, 55);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(86, 13);
            this.label28.TabIndex = 8;
            this.label28.Text = "Vibration length :";
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(4, 155);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(52, 13);
            this.label29.TabIndex = 7;
            this.label29.Text = "X-Y Axis :";
            // 
            // Cbox_Pad_Axis
            // 
            this.Cbox_Pad_Axis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Pad_Axis.FormattingEnabled = true;
            this.Cbox_Pad_Axis.Items.AddRange(new object[] {
            "Left Stick",
            "Right Stick"});
            this.Cbox_Pad_Axis.Location = new System.Drawing.Point(109, 152);
            this.Cbox_Pad_Axis.Name = "Cbox_Pad_Axis";
            this.Cbox_Pad_Axis.Size = new System.Drawing.Size(101, 21);
            this.Cbox_Pad_Axis.TabIndex = 6;
            this.Cbox_Pad_Axis.SelectionChangeCommitted += new System.EventHandler(this.Cbox_Pad_Axis_SelectionChangeCommitted);
            // 
            // Cbox_Pad_MouseRight
            // 
            this.Cbox_Pad_MouseRight.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Pad_MouseRight.FormattingEnabled = true;
            this.Cbox_Pad_MouseRight.Items.AddRange(new object[] {
            "A",
            "B",
            "X",
            "Y",
            "R Shoulder",
            "L Shoulder",
            "R Thumb",
            "L Thumb"});
            this.Cbox_Pad_MouseRight.Location = new System.Drawing.Point(109, 112);
            this.Cbox_Pad_MouseRight.Name = "Cbox_Pad_MouseRight";
            this.Cbox_Pad_MouseRight.Size = new System.Drawing.Size(101, 21);
            this.Cbox_Pad_MouseRight.TabIndex = 5;
            this.Cbox_Pad_MouseRight.SelectionChangeCommitted += new System.EventHandler(this.Cbox_Pad_MouseRight_SelectionChangeCommitted);
            // 
            // Cbox_Pad_MouseMiddle
            // 
            this.Cbox_Pad_MouseMiddle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Pad_MouseMiddle.FormattingEnabled = true;
            this.Cbox_Pad_MouseMiddle.Items.AddRange(new object[] {
            "A",
            "B",
            "X",
            "Y",
            "R Shoulder",
            "L Shoulder",
            "R Thumb",
            "L Thumb"});
            this.Cbox_Pad_MouseMiddle.Location = new System.Drawing.Point(109, 72);
            this.Cbox_Pad_MouseMiddle.Name = "Cbox_Pad_MouseMiddle";
            this.Cbox_Pad_MouseMiddle.Size = new System.Drawing.Size(101, 21);
            this.Cbox_Pad_MouseMiddle.TabIndex = 4;
            this.Cbox_Pad_MouseMiddle.SelectionChangeCommitted += new System.EventHandler(this.Cbox_Pad_MouseMiddle_SelectionChangeCommitted);
            // 
            // Cbox_Pad_MouseLeft
            // 
            this.Cbox_Pad_MouseLeft.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Pad_MouseLeft.FormattingEnabled = true;
            this.Cbox_Pad_MouseLeft.Items.AddRange(new object[] {
            "A",
            "B",
            "X",
            "Y",
            "R Shoulder",
            "L Shoulder",
            "R Thumb",
            "L Thumb"});
            this.Cbox_Pad_MouseLeft.Location = new System.Drawing.Point(109, 32);
            this.Cbox_Pad_MouseLeft.Name = "Cbox_Pad_MouseLeft";
            this.Cbox_Pad_MouseLeft.Size = new System.Drawing.Size(101, 21);
            this.Cbox_Pad_MouseLeft.TabIndex = 3;
            this.Cbox_Pad_MouseLeft.SelectionChangeCommitted += new System.EventHandler(this.Cbox_Pad_MouseLeft_SelectionChangeCommitted);
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(4, 115);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(73, 13);
            this.label30.TabIndex = 2;
            this.label30.Text = "Mouse Right :";
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(4, 75);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(79, 13);
            this.label31.TabIndex = 1;
            this.label31.Text = "Mouse Middle :";
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(4, 35);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(66, 13);
            this.label32.TabIndex = 0;
            this.label32.Text = "Mouse Left :";
            // 
            // Cbo_Device
            // 
            this.Cbo_Device.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.Cbo_Device.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbo_Device.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Cbo_Device.FormattingEnabled = true;
            this.Cbo_Device.Location = new System.Drawing.Point(13, 27);
            this.Cbo_Device.Margin = new System.Windows.Forms.Padding(4);
            this.Cbo_Device.Name = "Cbo_Device";
            this.Cbo_Device.Size = new System.Drawing.Size(554, 23);
            this.Cbo_Device.TabIndex = 40;
            this.Cbo_Device.SelectionChangeCommitted += new System.EventHandler(this.Cbo_Device_SelectionChangeCommitted);
            // 
            // Lbl_Player
            // 
            this.Lbl_Player.AutoSize = true;
            this.Lbl_Player.Location = new System.Drawing.Point(10, 7);
            this.Lbl_Player.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Player.Name = "Lbl_Player";
            this.Lbl_Player.Size = new System.Drawing.Size(63, 13);
            this.Lbl_Player.TabIndex = 41;
            this.Lbl_Player.Text = "P1 Device :";
            // 
            // Uc_GUI_PlayerDevice
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.Gbox_GunOptions);
            this.Controls.Add(this.Gbox_PadOptions);
            this.Controls.Add(this.Cbo_Device);
            this.Controls.Add(this.Lbl_Player);
            this.Name = "Uc_GUI_PlayerDevice";
            this.Size = new System.Drawing.Size(576, 259);
            this.Gbox_GunOptions.ResumeLayout(false);
            this.Gbox_GunOptions.PerformLayout();
            this.Gbox_PadOptions.ResumeLayout(false);
            this.Gbox_PadOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Tbar_VibrationStrength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Tbar_VibrationLength)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox Gbox_GunOptions;
        private System.Windows.Forms.Label Lbl_MidButonEnable;
        private System.Windows.Forms.CheckBox Chk_VirtualMiddleBtn;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.TextBox Txt_VirtualMiddleBtn;
        private System.Windows.Forms.GroupBox Gbox_PadOptions;
        private System.Windows.Forms.CheckBox Chk_VibrationEnable;
        private System.Windows.Forms.TrackBar Tbar_VibrationStrength;
        private System.Windows.Forms.TrackBar Tbar_VibrationLength;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.ComboBox Cbox_Pad_Axis;
        private System.Windows.Forms.ComboBox Cbox_Pad_MouseRight;
        private System.Windows.Forms.ComboBox Cbox_Pad_MouseMiddle;
        private System.Windows.Forms.ComboBox Cbox_Pad_MouseLeft;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.ComboBox Cbo_Device;
        private System.Windows.Forms.Label Lbl_Player;
    }
}
