namespace DsDiag
{
    partial class Ds_Diag_Button
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
            this.Pnl_Background = new System.Windows.Forms.Panel();
            this.Lbl_Number = new System.Windows.Forms.Label();
            this.Pnl_Background.SuspendLayout();
            this.SuspendLayout();
            // 
            // Pnl_Background
            // 
            this.Pnl_Background.BackColor = System.Drawing.Color.Crimson;
            this.Pnl_Background.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Pnl_Background.Controls.Add(this.Lbl_Number);
            this.Pnl_Background.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Pnl_Background.Location = new System.Drawing.Point(0, 0);
            this.Pnl_Background.Name = "Pnl_Background";
            this.Pnl_Background.Size = new System.Drawing.Size(26, 26);
            this.Pnl_Background.TabIndex = 0;
            // 
            // Lbl_Number
            // 
            this.Lbl_Number.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.Lbl_Number.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Lbl_Number.ForeColor = System.Drawing.Color.White;
            this.Lbl_Number.Location = new System.Drawing.Point(-2, 0);
            this.Lbl_Number.Name = "Lbl_Number";
            this.Lbl_Number.Size = new System.Drawing.Size(26, 22);
            this.Lbl_Number.TabIndex = 0;
            this.Lbl_Number.Text = "1";
            this.Lbl_Number.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Ds_Diag_Button
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Pnl_Background);
            this.Name = "Ds_Diag_Button";
            this.Size = new System.Drawing.Size(26, 26);
            this.Pnl_Background.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel Pnl_Background;
        private System.Windows.Forms.Label Lbl_Number;
    }
}
