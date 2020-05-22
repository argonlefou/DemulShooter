namespace DemulShooter_GUI
{
    partial class GUI_Player
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
            this.Cbo_Device = new System.Windows.Forms.ComboBox();
            this.Lbl_Player = new System.Windows.Forms.Label();
            this.Pnl_Options = new System.Windows.Forms.Panel();
            this.Lbl_ProductManu = new System.Windows.Forms.Label();
            this.SuspendLayout();
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
            this.Lbl_Player.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Lbl_Player.Location = new System.Drawing.Point(10, 7);
            this.Lbl_Player.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_Player.Name = "Lbl_Player";
            this.Lbl_Player.Size = new System.Drawing.Size(74, 13);
            this.Lbl_Player.TabIndex = 41;
            this.Lbl_Player.Text = "P1 Device :";
            // 
            // Pnl_Options
            // 
            this.Pnl_Options.Location = new System.Drawing.Point(0, 57);
            this.Pnl_Options.Name = "Pnl_Options";
            this.Pnl_Options.Size = new System.Drawing.Size(576, 202);
            this.Pnl_Options.TabIndex = 42;
            // 
            // Lbl_ProductManu
            // 
            this.Lbl_ProductManu.AutoSize = true;
            this.Lbl_ProductManu.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Lbl_ProductManu.Location = new System.Drawing.Point(81, 7);
            this.Lbl_ProductManu.Name = "Lbl_ProductManu";
            this.Lbl_ProductManu.Size = new System.Drawing.Size(0, 13);
            this.Lbl_ProductManu.TabIndex = 43;
            // 
            // GUI_Player
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.Lbl_ProductManu);
            this.Controls.Add(this.Pnl_Options);
            this.Controls.Add(this.Cbo_Device);
            this.Controls.Add(this.Lbl_Player);
            this.Name = "GUI_Player";
            this.Size = new System.Drawing.Size(576, 259);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox Cbo_Device;
        private System.Windows.Forms.Label Lbl_Player;
        private System.Windows.Forms.Panel Pnl_Options;
        private System.Windows.Forms.Label Lbl_ProductManu;
    }
}
