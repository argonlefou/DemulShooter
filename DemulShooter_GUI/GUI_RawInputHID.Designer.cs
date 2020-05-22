namespace DemulShooter_GUI
{
    partial class GUI_RawInputHID
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
            this.Gbox_HIDOptions = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Cbox_HID_YAxis = new System.Windows.Forms.ComboBox();
            this.label29 = new System.Windows.Forms.Label();
            this.Cbox_HID_XAxis = new System.Windows.Forms.ComboBox();
            this.Cbox_HID_OffScreenButton = new System.Windows.Forms.ComboBox();
            this.Cbox_HID_ActionButton = new System.Windows.Forms.ComboBox();
            this.Cbox_HID_OnScreenButton = new System.Windows.Forms.ComboBox();
            this.label30 = new System.Windows.Forms.Label();
            this.label31 = new System.Windows.Forms.Label();
            this.label32 = new System.Windows.Forms.Label();
            this.Pnl_AxisViewer = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Pnl_ButtonsViewer = new System.Windows.Forms.FlowLayoutPanel();
            this.Gbox_HIDOptions.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Gbox_HIDOptions
            // 
            this.Gbox_HIDOptions.Controls.Add(this.label1);
            this.Gbox_HIDOptions.Controls.Add(this.Cbox_HID_YAxis);
            this.Gbox_HIDOptions.Controls.Add(this.label29);
            this.Gbox_HIDOptions.Controls.Add(this.Cbox_HID_XAxis);
            this.Gbox_HIDOptions.Controls.Add(this.Cbox_HID_OffScreenButton);
            this.Gbox_HIDOptions.Controls.Add(this.Cbox_HID_ActionButton);
            this.Gbox_HIDOptions.Controls.Add(this.Cbox_HID_OnScreenButton);
            this.Gbox_HIDOptions.Controls.Add(this.label30);
            this.Gbox_HIDOptions.Controls.Add(this.label31);
            this.Gbox_HIDOptions.Controls.Add(this.label32);
            this.Gbox_HIDOptions.Location = new System.Drawing.Point(3, 3);
            this.Gbox_HIDOptions.Name = "Gbox_HIDOptions";
            this.Gbox_HIDOptions.Size = new System.Drawing.Size(223, 189);
            this.Gbox_HIDOptions.TabIndex = 43;
            this.Gbox_HIDOptions.TabStop = false;
            this.Gbox_HIDOptions.Text = "Device Options";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 156);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 13);
            this.label1.TabIndex = 14;
            this.label1.Text = "Y Axis :";
            // 
            // Cbox_HID_YAxis
            // 
            this.Cbox_HID_YAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_HID_YAxis.FormattingEnabled = true;
            this.Cbox_HID_YAxis.Items.AddRange(new object[] {
            "Left Stick",
            "Right Stick"});
            this.Cbox_HID_YAxis.Location = new System.Drawing.Point(109, 153);
            this.Cbox_HID_YAxis.Name = "Cbox_HID_YAxis";
            this.Cbox_HID_YAxis.Size = new System.Drawing.Size(101, 21);
            this.Cbox_HID_YAxis.TabIndex = 13;
            this.Cbox_HID_YAxis.SelectionChangeCommitted += new System.EventHandler(this.Cbox_HID_YAxis_SelectionChangeCommitted);
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(6, 129);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(42, 13);
            this.label29.TabIndex = 7;
            this.label29.Text = "X Axis :";
            // 
            // Cbox_HID_XAxis
            // 
            this.Cbox_HID_XAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_HID_XAxis.FormattingEnabled = true;
            this.Cbox_HID_XAxis.Items.AddRange(new object[] {
            "Left Stick",
            "Right Stick"});
            this.Cbox_HID_XAxis.Location = new System.Drawing.Point(109, 126);
            this.Cbox_HID_XAxis.Name = "Cbox_HID_XAxis";
            this.Cbox_HID_XAxis.Size = new System.Drawing.Size(101, 21);
            this.Cbox_HID_XAxis.TabIndex = 6;
            this.Cbox_HID_XAxis.SelectionChangeCommitted += new System.EventHandler(this.Cbox_HID_XAxis_SelectionChangeCommitted);
            // 
            // Cbox_HID_OffScreenButton
            // 
            this.Cbox_HID_OffScreenButton.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_HID_OffScreenButton.FormattingEnabled = true;
            this.Cbox_HID_OffScreenButton.Items.AddRange(new object[] {
            "A",
            "B",
            "X",
            "Y",
            "R Shoulder",
            "L Shoulder",
            "R Thumb",
            "L Thumb"});
            this.Cbox_HID_OffScreenButton.Location = new System.Drawing.Point(109, 86);
            this.Cbox_HID_OffScreenButton.Name = "Cbox_HID_OffScreenButton";
            this.Cbox_HID_OffScreenButton.Size = new System.Drawing.Size(101, 21);
            this.Cbox_HID_OffScreenButton.TabIndex = 5;
            this.Cbox_HID_OffScreenButton.SelectionChangeCommitted += new System.EventHandler(this.Cbox_HID_OffScreenButton_SelectionChangeCommitted);
            // 
            // Cbox_HID_ActionButton
            // 
            this.Cbox_HID_ActionButton.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_HID_ActionButton.FormattingEnabled = true;
            this.Cbox_HID_ActionButton.Items.AddRange(new object[] {
            "A",
            "B",
            "X",
            "Y",
            "R Shoulder",
            "L Shoulder",
            "R Thumb",
            "L Thumb"});
            this.Cbox_HID_ActionButton.Location = new System.Drawing.Point(109, 59);
            this.Cbox_HID_ActionButton.Name = "Cbox_HID_ActionButton";
            this.Cbox_HID_ActionButton.Size = new System.Drawing.Size(101, 21);
            this.Cbox_HID_ActionButton.TabIndex = 4;
            this.Cbox_HID_ActionButton.SelectionChangeCommitted += new System.EventHandler(this.Cbox_HID_ActionButton_SelectionChangeCommitted);
            // 
            // Cbox_HID_OnScreenButton
            // 
            this.Cbox_HID_OnScreenButton.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_HID_OnScreenButton.FormattingEnabled = true;
            this.Cbox_HID_OnScreenButton.Items.AddRange(new object[] {
            "A",
            "B",
            "X",
            "Y",
            "R Shoulder",
            "L Shoulder",
            "R Thumb",
            "L Thumb"});
            this.Cbox_HID_OnScreenButton.Location = new System.Drawing.Point(109, 32);
            this.Cbox_HID_OnScreenButton.Name = "Cbox_HID_OnScreenButton";
            this.Cbox_HID_OnScreenButton.Size = new System.Drawing.Size(101, 21);
            this.Cbox_HID_OnScreenButton.TabIndex = 3;
            this.Cbox_HID_OnScreenButton.SelectionChangeCommitted += new System.EventHandler(this.Cbox_HID_OnScreenButton_SelectionChangeCommitted);
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(4, 89);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(98, 13);
            this.label30.TabIndex = 2;
            this.label30.Text = "Off Screen  Button:";
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(4, 62);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(74, 13);
            this.label31.TabIndex = 1;
            this.label31.Text = "Action Button:";
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(4, 35);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(80, 13);
            this.label32.TabIndex = 0;
            this.label32.Text = "Trigger Button :";
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
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Pnl_ButtonsViewer);
            this.groupBox1.Controls.Add(this.Pnl_AxisViewer);
            this.groupBox1.Location = new System.Drawing.Point(232, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(341, 189);
            this.groupBox1.TabIndex = 45;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Device Preview";
            // 
            // Pnl_ButtonsViewer
            // 
            this.Pnl_ButtonsViewer.Location = new System.Drawing.Point(169, 32);
            this.Pnl_ButtonsViewer.Name = "Pnl_ButtonsViewer";
            this.Pnl_ButtonsViewer.Size = new System.Drawing.Size(166, 142);
            this.Pnl_ButtonsViewer.TabIndex = 46;
            // 
            // GUI_RawInputHID
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Gbox_HIDOptions);
            this.Name = "GUI_RawInputHID";
            this.Size = new System.Drawing.Size(576, 202);
            this.Gbox_HIDOptions.ResumeLayout(false);
            this.Gbox_HIDOptions.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox Gbox_HIDOptions;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.ComboBox Cbox_HID_XAxis;
        private System.Windows.Forms.ComboBox Cbox_HID_OffScreenButton;
        private System.Windows.Forms.ComboBox Cbox_HID_ActionButton;
        private System.Windows.Forms.ComboBox Cbox_HID_OnScreenButton;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox Cbox_HID_YAxis;
        private System.Windows.Forms.Panel Pnl_AxisViewer;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.FlowLayoutPanel Pnl_ButtonsViewer;
    }
}
