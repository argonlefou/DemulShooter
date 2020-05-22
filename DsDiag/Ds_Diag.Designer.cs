namespace DsDiag
{
    partial class DsDiag
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DsDiag));
            this.Cbo_Dev = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.Lbl_AxisYMax = new System.Windows.Forms.Label();
            this.Lbl_AxisYMin = new System.Windows.Forms.Label();
            this.lbl14 = new System.Windows.Forms.Label();
            this.lbl13 = new System.Windows.Forms.Label();
            this.Lbl_AxisXMax = new System.Windows.Forms.Label();
            this.Lbl_AxisXMin = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.Cbo_AxisY = new System.Windows.Forms.ComboBox();
            this.Cbo_AxisX = new System.Windows.Forms.ComboBox();
            this.Lbl_AxisX_Txt = new System.Windows.Forms.Label();
            this.FlowPanelButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.Lbl_Nbr_Buttons = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.Lbl_AxisY_Txt = new System.Windows.Forms.Label();
            this.Lbl_Product = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.Lbl_Nbr_Axis = new System.Windows.Forms.Label();
            this.Lbl_Nbr_Axis_Txt = new System.Windows.Forms.Label();
            this.Lbl_Manufacturer = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.Lbl_dwType = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.Lbl_VID = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.Lbl_PID = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.Txt_Log = new System.Windows.Forms.RichTextBox();
            this.Btn_Export = new System.Windows.Forms.Button();
            this.Tmr_RefreshGui = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Pbox_Target)).BeginInit();
            this.groupBox2.SuspendLayout();
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
            this.Cbo_Dev.Size = new System.Drawing.Size(630, 28);
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
            this.groupBox1.Location = new System.Drawing.Point(20, 81);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(371, 168);
            this.groupBox1.TabIndex = 23;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Device Data [x, y]:";
            // 
            // Lbl_OnClient
            // 
            this.Lbl_OnClient.AutoSize = true;
            this.Lbl_OnClient.Location = new System.Drawing.Point(107, 137);
            this.Lbl_OnClient.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_OnClient.Name = "Lbl_OnClient";
            this.Lbl_OnClient.Size = new System.Drawing.Size(14, 20);
            this.Lbl_OnClient.TabIndex = 33;
            this.Lbl_OnClient.Text = "-";
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
            this.Lbl_OnScreen.Size = new System.Drawing.Size(14, 20);
            this.Lbl_OnScreen.TabIndex = 27;
            this.Lbl_OnScreen.Text = "-";
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
            this.Lbl_RawInput.Size = new System.Drawing.Size(14, 20);
            this.Lbl_RawInput.TabIndex = 25;
            this.Lbl_RawInput.Text = "-";
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
            this.Pbox_Target.BackgroundImage = global::DsDiag.Properties.Resources.sharps1;
            this.Pbox_Target.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Pbox_Target.Location = new System.Drawing.Point(750, 81);
            this.Pbox_Target.Name = "Pbox_Target";
            this.Pbox_Target.Size = new System.Drawing.Size(80, 80);
            this.Pbox_Target.TabIndex = 24;
            this.Pbox_Target.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.BackColor = System.Drawing.Color.Black;
            this.groupBox2.Controls.Add(this.Lbl_AxisYMax);
            this.groupBox2.Controls.Add(this.Lbl_AxisYMin);
            this.groupBox2.Controls.Add(this.lbl14);
            this.groupBox2.Controls.Add(this.lbl13);
            this.groupBox2.Controls.Add(this.Lbl_AxisXMax);
            this.groupBox2.Controls.Add(this.Lbl_AxisXMin);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.Cbo_AxisY);
            this.groupBox2.Controls.Add(this.Cbo_AxisX);
            this.groupBox2.Controls.Add(this.Lbl_AxisX_Txt);
            this.groupBox2.Controls.Add(this.FlowPanelButtons);
            this.groupBox2.Controls.Add(this.Lbl_Nbr_Buttons);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.Lbl_AxisY_Txt);
            this.groupBox2.Controls.Add(this.Lbl_Product);
            this.groupBox2.Controls.Add(this.label24);
            this.groupBox2.Controls.Add(this.Lbl_Nbr_Axis);
            this.groupBox2.Controls.Add(this.Lbl_Nbr_Axis_Txt);
            this.groupBox2.Controls.Add(this.Lbl_Manufacturer);
            this.groupBox2.Controls.Add(this.label17);
            this.groupBox2.Controls.Add(this.Lbl_dwType);
            this.groupBox2.Controls.Add(this.label19);
            this.groupBox2.Controls.Add(this.Lbl_VID);
            this.groupBox2.Controls.Add(this.label21);
            this.groupBox2.Controls.Add(this.Lbl_PID);
            this.groupBox2.Controls.Add(this.label23);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.ForeColor = System.Drawing.Color.White;
            this.groupBox2.Location = new System.Drawing.Point(17, 310);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(455, 357);
            this.groupBox2.TabIndex = 41;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Device Informations:";
            // 
            // Lbl_AxisYMax
            // 
            this.Lbl_AxisYMax.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Lbl_AxisYMax.AutoSize = true;
            this.Lbl_AxisYMax.Location = new System.Drawing.Point(276, 211);
            this.Lbl_AxisYMax.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_AxisYMax.Name = "Lbl_AxisYMax";
            this.Lbl_AxisYMax.Size = new System.Drawing.Size(14, 20);
            this.Lbl_AxisYMax.TabIndex = 57;
            this.Lbl_AxisYMax.Text = "-";
            // 
            // Lbl_AxisYMin
            // 
            this.Lbl_AxisYMin.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Lbl_AxisYMin.AutoSize = true;
            this.Lbl_AxisYMin.Location = new System.Drawing.Point(276, 191);
            this.Lbl_AxisYMin.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_AxisYMin.Name = "Lbl_AxisYMin";
            this.Lbl_AxisYMin.Size = new System.Drawing.Size(14, 20);
            this.Lbl_AxisYMin.TabIndex = 56;
            this.Lbl_AxisYMin.Text = "-";
            // 
            // lbl14
            // 
            this.lbl14.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lbl14.AutoSize = true;
            this.lbl14.Location = new System.Drawing.Point(233, 211);
            this.lbl14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl14.Name = "lbl14";
            this.lbl14.Size = new System.Drawing.Size(42, 20);
            this.lbl14.TabIndex = 55;
            this.lbl14.Text = "Max:";
            // 
            // lbl13
            // 
            this.lbl13.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lbl13.AutoSize = true;
            this.lbl13.Location = new System.Drawing.Point(233, 191);
            this.lbl13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl13.Name = "lbl13";
            this.lbl13.Size = new System.Drawing.Size(38, 20);
            this.lbl13.TabIndex = 54;
            this.lbl13.Text = "Min:";
            // 
            // Lbl_AxisXMax
            // 
            this.Lbl_AxisXMax.AutoSize = true;
            this.Lbl_AxisXMax.Location = new System.Drawing.Point(53, 211);
            this.Lbl_AxisXMax.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_AxisXMax.Name = "Lbl_AxisXMax";
            this.Lbl_AxisXMax.Size = new System.Drawing.Size(14, 20);
            this.Lbl_AxisXMax.TabIndex = 53;
            this.Lbl_AxisXMax.Text = "-";
            // 
            // Lbl_AxisXMin
            // 
            this.Lbl_AxisXMin.AutoSize = true;
            this.Lbl_AxisXMin.Location = new System.Drawing.Point(53, 191);
            this.Lbl_AxisXMin.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_AxisXMin.Name = "Lbl_AxisXMin";
            this.Lbl_AxisXMin.Size = new System.Drawing.Size(14, 20);
            this.Lbl_AxisXMin.TabIndex = 52;
            this.Lbl_AxisXMin.Text = "-";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 211);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(42, 20);
            this.label7.TabIndex = 51;
            this.label7.Text = "Max:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(10, 191);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 20);
            this.label4.TabIndex = 50;
            this.label4.Text = "Min:";
            // 
            // Cbo_AxisY
            // 
            this.Cbo_AxisY.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Cbo_AxisY.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbo_AxisY.FormattingEnabled = true;
            this.Cbo_AxisY.Location = new System.Drawing.Point(297, 152);
            this.Cbo_AxisY.Name = "Cbo_AxisY";
            this.Cbo_AxisY.Size = new System.Drawing.Size(71, 28);
            this.Cbo_AxisY.TabIndex = 49;
            this.Cbo_AxisY.SelectionChangeCommitted += new System.EventHandler(this.Cbo_AxisY_SelectionChangeCommitted);
            // 
            // Cbo_AxisX
            // 
            this.Cbo_AxisX.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Cbo_AxisX.FormattingEnabled = true;
            this.Cbo_AxisX.Location = new System.Drawing.Point(74, 152);
            this.Cbo_AxisX.Name = "Cbo_AxisX";
            this.Cbo_AxisX.Size = new System.Drawing.Size(71, 28);
            this.Cbo_AxisX.TabIndex = 48;
            this.Cbo_AxisX.SelectionChangeCommitted += new System.EventHandler(this.Cbo_AxisX_SelectionChangeCommitted);
            // 
            // Lbl_AxisX_Txt
            // 
            this.Lbl_AxisX_Txt.AutoSize = true;
            this.Lbl_AxisX_Txt.Location = new System.Drawing.Point(10, 155);
            this.Lbl_AxisX_Txt.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_AxisX_Txt.Name = "Lbl_AxisX_Txt";
            this.Lbl_AxisX_Txt.Size = new System.Drawing.Size(57, 20);
            this.Lbl_AxisX_Txt.TabIndex = 47;
            this.Lbl_AxisX_Txt.Text = "Axis X:";
            // 
            // FlowPanelButtons
            // 
            this.FlowPanelButtons.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FlowPanelButtons.Location = new System.Drawing.Point(11, 244);
            this.FlowPanelButtons.Name = "FlowPanelButtons";
            this.FlowPanelButtons.Size = new System.Drawing.Size(438, 107);
            this.FlowPanelButtons.TabIndex = 46;
            // 
            // Lbl_Nbr_Buttons
            // 
            this.Lbl_Nbr_Buttons.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Lbl_Nbr_Buttons.AutoSize = true;
            this.Lbl_Nbr_Buttons.Location = new System.Drawing.Point(347, 97);
            this.Lbl_Nbr_Buttons.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Nbr_Buttons.Name = "Lbl_Nbr_Buttons";
            this.Lbl_Nbr_Buttons.Size = new System.Drawing.Size(14, 20);
            this.Lbl_Nbr_Buttons.TabIndex = 45;
            this.Lbl_Nbr_Buttons.Text = "-";
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(192, 97);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(147, 20);
            this.label3.TabIndex = 44;
            this.label3.Text = "Number of Buttons:";
            // 
            // Lbl_AxisY_Txt
            // 
            this.Lbl_AxisY_Txt.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Lbl_AxisY_Txt.AutoSize = true;
            this.Lbl_AxisY_Txt.Location = new System.Drawing.Point(233, 155);
            this.Lbl_AxisY_Txt.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_AxisY_Txt.Name = "Lbl_AxisY_Txt";
            this.Lbl_AxisY_Txt.Size = new System.Drawing.Size(57, 20);
            this.Lbl_AxisY_Txt.TabIndex = 43;
            this.Lbl_AxisY_Txt.Text = "Axis Y:";
            // 
            // Lbl_Product
            // 
            this.Lbl_Product.AutoSize = true;
            this.Lbl_Product.Location = new System.Drawing.Point(79, 69);
            this.Lbl_Product.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Product.Name = "Lbl_Product";
            this.Lbl_Product.Size = new System.Drawing.Size(14, 20);
            this.Lbl_Product.TabIndex = 42;
            this.Lbl_Product.Text = "-";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(7, 69);
            this.label24.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(68, 20);
            this.label24.TabIndex = 41;
            this.label24.Text = "Product:";
            // 
            // Lbl_Nbr_Axis
            // 
            this.Lbl_Nbr_Axis.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Lbl_Nbr_Axis.AutoSize = true;
            this.Lbl_Nbr_Axis.Location = new System.Drawing.Point(347, 117);
            this.Lbl_Nbr_Axis.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Nbr_Axis.Name = "Lbl_Nbr_Axis";
            this.Lbl_Nbr_Axis.Size = new System.Drawing.Size(14, 20);
            this.Lbl_Nbr_Axis.TabIndex = 33;
            this.Lbl_Nbr_Axis.Text = "-";
            // 
            // Lbl_Nbr_Axis_Txt
            // 
            this.Lbl_Nbr_Axis_Txt.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Lbl_Nbr_Axis_Txt.AutoSize = true;
            this.Lbl_Nbr_Axis_Txt.Location = new System.Drawing.Point(192, 117);
            this.Lbl_Nbr_Axis_Txt.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Nbr_Axis_Txt.Name = "Lbl_Nbr_Axis_Txt";
            this.Lbl_Nbr_Axis_Txt.Size = new System.Drawing.Size(120, 20);
            this.Lbl_Nbr_Axis_Txt.TabIndex = 32;
            this.Lbl_Nbr_Axis_Txt.Text = "Number of Axis:";
            // 
            // Lbl_Manufacturer
            // 
            this.Lbl_Manufacturer.AutoSize = true;
            this.Lbl_Manufacturer.Location = new System.Drawing.Point(114, 49);
            this.Lbl_Manufacturer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Manufacturer.Name = "Lbl_Manufacturer";
            this.Lbl_Manufacturer.Size = new System.Drawing.Size(14, 20);
            this.Lbl_Manufacturer.TabIndex = 31;
            this.Lbl_Manufacturer.Text = "-";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(7, 49);
            this.label17.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(108, 20);
            this.label17.TabIndex = 30;
            this.label17.Text = "Manufacturer:";
            // 
            // Lbl_dwType
            // 
            this.Lbl_dwType.AutoSize = true;
            this.Lbl_dwType.Location = new System.Drawing.Point(107, 29);
            this.Lbl_dwType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_dwType.Name = "Lbl_dwType";
            this.Lbl_dwType.Size = new System.Drawing.Size(14, 20);
            this.Lbl_dwType.TabIndex = 29;
            this.Lbl_dwType.Text = "-";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(7, 29);
            this.label19.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(99, 20);
            this.label19.TabIndex = 28;
            this.label19.Text = "DeviceType :";
            // 
            // Lbl_VID
            // 
            this.Lbl_VID.AutoSize = true;
            this.Lbl_VID.Location = new System.Drawing.Point(56, 117);
            this.Lbl_VID.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_VID.Name = "Lbl_VID";
            this.Lbl_VID.Size = new System.Drawing.Size(14, 20);
            this.Lbl_VID.TabIndex = 27;
            this.Lbl_VID.Text = "-";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(7, 117);
            this.label21.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(41, 20);
            this.label21.TabIndex = 26;
            this.label21.Text = "VID:";
            // 
            // Lbl_PID
            // 
            this.Lbl_PID.AutoSize = true;
            this.Lbl_PID.Location = new System.Drawing.Point(56, 97);
            this.Lbl_PID.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_PID.Name = "Lbl_PID";
            this.Lbl_PID.Size = new System.Drawing.Size(14, 20);
            this.Lbl_PID.TabIndex = 25;
            this.Lbl_PID.Text = "-";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(7, 97);
            this.label23.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(40, 20);
            this.label23.TabIndex = 24;
            this.label23.Text = "PID:";
            // 
            // Txt_Log
            // 
            this.Txt_Log.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Txt_Log.Location = new System.Drawing.Point(647, 292);
            this.Txt_Log.Name = "Txt_Log";
            this.Txt_Log.Size = new System.Drawing.Size(183, 375);
            this.Txt_Log.TabIndex = 42;
            this.Txt_Log.Text = "";
            this.Txt_Log.Visible = false;
            // 
            // Btn_Export
            // 
            this.Btn_Export.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Btn_Export.Location = new System.Drawing.Point(655, 35);
            this.Btn_Export.Name = "Btn_Export";
            this.Btn_Export.Size = new System.Drawing.Size(177, 30);
            this.Btn_Export.TabIndex = 43;
            this.Btn_Export.Text = "Export device data";
            this.Btn_Export.UseVisualStyleBackColor = true;
            this.Btn_Export.Visible = false;
            this.Btn_Export.Click += new System.EventHandler(this.Btn_Export_Click);
            // 
            // Tmr_RefreshGui
            // 
            this.Tmr_RefreshGui.Enabled = true;
            this.Tmr_RefreshGui.Interval = 25;
            this.Tmr_RefreshGui.Tick += new System.EventHandler(this.Tmr_RefreshGui_Tick);
            // 
            // DsDiag
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(844, 679);
            this.Controls.Add(this.Btn_Export);
            this.Controls.Add(this.Txt_Log);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Pbox_Target);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Cbo_Dev);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "DsDiag";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DemulShooter Devices Diagnostic";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Ds_Diag_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Ds_Diag_Paint);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Pbox_Target)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox Cbo_Dev;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
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
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.FlowLayoutPanel FlowPanelButtons;
        private System.Windows.Forms.Label Lbl_Nbr_Buttons;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label Lbl_AxisY_Txt;
        private System.Windows.Forms.Label Lbl_Product;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Label Lbl_Nbr_Axis;
        private System.Windows.Forms.Label Lbl_Nbr_Axis_Txt;
        private System.Windows.Forms.Label Lbl_Manufacturer;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label Lbl_dwType;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label Lbl_VID;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label Lbl_PID;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label Lbl_AxisX_Txt;
        private System.Windows.Forms.ComboBox Cbo_AxisY;
        private System.Windows.Forms.ComboBox Cbo_AxisX;
        private System.Windows.Forms.RichTextBox Txt_Log;
        private System.Windows.Forms.Button Btn_Export;
        private System.Windows.Forms.Timer Tmr_RefreshGui;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label Lbl_AxisYMax;
        private System.Windows.Forms.Label Lbl_AxisYMin;
        private System.Windows.Forms.Label lbl14;
        private System.Windows.Forms.Label lbl13;
        private System.Windows.Forms.Label Lbl_AxisXMax;
        private System.Windows.Forms.Label Lbl_AxisXMin;

    }
}

