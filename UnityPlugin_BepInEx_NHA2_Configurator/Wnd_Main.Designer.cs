namespace UnityPlugin_BepInEx_NHA2_Configurator
{
    partial class Wnd_Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wnd_Main));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.Cbox_Gun = new System.Windows.Forms.ComboBox();
            this.Cbox_Laser = new System.Windows.Forms.ComboBox();
            this.Cbox_Crosshair = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Cbox_Lang = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.Cbox_InputMode = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.Btn_Save = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Txt_Width = new System.Windows.Forms.TextBox();
            this.Cbox_Screenmode = new System.Windows.Forms.ComboBox();
            this.Txt_Height = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.Cbox_Gun);
            this.groupBox2.Controls.Add(this.Cbox_Laser);
            this.groupBox2.Controls.Add(this.Cbox_Crosshair);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.Cbox_Lang);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.Cbox_InputMode);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Location = new System.Drawing.Point(12, 225);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(292, 171);
            this.groupBox2.TabIndex = 40;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Plugin options :";
            // 
            // Cbox_Gun
            // 
            this.Cbox_Gun.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Gun.FormattingEnabled = true;
            this.Cbox_Gun.Items.AddRange(new object[] {
            "NO",
            "YES"});
            this.Cbox_Gun.Location = new System.Drawing.Point(167, 138);
            this.Cbox_Gun.Name = "Cbox_Gun";
            this.Cbox_Gun.Size = new System.Drawing.Size(98, 21);
            this.Cbox_Gun.TabIndex = 29;
            // 
            // Cbox_Laser
            // 
            this.Cbox_Laser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Laser.FormattingEnabled = true;
            this.Cbox_Laser.Items.AddRange(new object[] {
            "NO",
            "YES"});
            this.Cbox_Laser.Location = new System.Drawing.Point(167, 111);
            this.Cbox_Laser.Name = "Cbox_Laser";
            this.Cbox_Laser.Size = new System.Drawing.Size(98, 21);
            this.Cbox_Laser.TabIndex = 28;
            // 
            // Cbox_Crosshair
            // 
            this.Cbox_Crosshair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Crosshair.FormattingEnabled = true;
            this.Cbox_Crosshair.Items.AddRange(new object[] {
            "NO",
            "YES"});
            this.Cbox_Crosshair.Location = new System.Drawing.Point(167, 84);
            this.Cbox_Crosshair.Name = "Cbox_Crosshair";
            this.Cbox_Crosshair.Size = new System.Drawing.Size(98, 21);
            this.Cbox_Crosshair.TabIndex = 27;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Language";
            // 
            // Cbox_Lang
            // 
            this.Cbox_Lang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Lang.Enabled = false;
            this.Cbox_Lang.FormattingEnabled = true;
            this.Cbox_Lang.Items.AddRange(new object[] {
            "EN",
            "CH"});
            this.Cbox_Lang.Location = new System.Drawing.Point(115, 27);
            this.Cbox_Lang.Name = "Cbox_Lang";
            this.Cbox_Lang.Size = new System.Drawing.Size(150, 21);
            this.Cbox_Lang.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(93, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Remove Crosshair";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 114);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(103, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Remove Laser Sight";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Input Mode";
            // 
            // Cbox_InputMode
            // 
            this.Cbox_InputMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_InputMode.FormattingEnabled = true;
            this.Cbox_InputMode.Items.AddRange(new object[] {
            "MOUSE (1 PLAYER)",
            "DEMULSHOOTER"});
            this.Cbox_InputMode.Location = new System.Drawing.Point(115, 54);
            this.Cbox_InputMode.Name = "Cbox_InputMode";
            this.Cbox_InputMode.Size = new System.Drawing.Size(150, 21);
            this.Cbox_InputMode.TabIndex = 6;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 141);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(75, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Remove Guns";
            // 
            // Btn_Save
            // 
            this.Btn_Save.Location = new System.Drawing.Point(104, 412);
            this.Btn_Save.Name = "Btn_Save";
            this.Btn_Save.Size = new System.Drawing.Size(106, 42);
            this.Btn_Save.TabIndex = 21;
            this.Btn_Save.Text = "Save !";
            this.Btn_Save.UseVisualStyleBackColor = true;
            this.Btn_Save.Click += new System.EventHandler(this.Btn_Save_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Location = new System.Drawing.Point(12, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(292, 143);
            this.pictureBox1.TabIndex = 36;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Txt_Width);
            this.groupBox1.Controls.Add(this.Cbox_Screenmode);
            this.groupBox1.Controls.Add(this.Txt_Height);
            this.groupBox1.Controls.Add(this.label18);
            this.groupBox1.Location = new System.Drawing.Point(12, 153);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(292, 66);
            this.groupBox1.TabIndex = 39;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Display :";
            // 
            // Txt_Width
            // 
            this.Txt_Width.Location = new System.Drawing.Point(6, 27);
            this.Txt_Width.Name = "Txt_Width";
            this.Txt_Width.Size = new System.Drawing.Size(47, 20);
            this.Txt_Width.TabIndex = 1;
            // 
            // Cbox_Screenmode
            // 
            this.Cbox_Screenmode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbox_Screenmode.FormattingEnabled = true;
            this.Cbox_Screenmode.Items.AddRange(new object[] {
            "Windowed",
            "Fullscreen"});
            this.Cbox_Screenmode.Location = new System.Drawing.Point(149, 27);
            this.Cbox_Screenmode.Name = "Cbox_Screenmode";
            this.Cbox_Screenmode.Size = new System.Drawing.Size(116, 21);
            this.Cbox_Screenmode.TabIndex = 3;
            // 
            // Txt_Height
            // 
            this.Txt_Height.Location = new System.Drawing.Point(77, 27);
            this.Txt_Height.Name = "Txt_Height";
            this.Txt_Height.Size = new System.Drawing.Size(47, 20);
            this.Txt_Height.TabIndex = 2;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(59, 30);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(12, 13);
            this.label18.TabIndex = 38;
            this.label18.Text = "x";
            // 
            // Wnd_Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(317, 466);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.Btn_Save);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Wnd_Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "NHA2 - Input Plugin Configurator v1.0";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox Cbox_Lang;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button Btn_Save;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox Cbox_InputMode;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox Txt_Width;
        private System.Windows.Forms.ComboBox Cbox_Screenmode;
        private System.Windows.Forms.TextBox Txt_Height;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox Cbox_Gun;
        private System.Windows.Forms.ComboBox Cbox_Laser;
        private System.Windows.Forms.ComboBox Cbox_Crosshair;
    }
}

