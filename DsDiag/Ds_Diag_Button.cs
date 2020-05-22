using System.Drawing;
using System.Windows.Forms;

namespace DsDiag
{
    public partial class Ds_Diag_Button : UserControl
    {
        private int _Number;

        public Ds_Diag_Button(int Number)
        {
            InitializeComponent();
            _Number = Number;
            Lbl_Number.Text = Number.ToString();
        }

        public void Activate(bool isActivated)
        {
            if (isActivated)
                Pnl_Background.BackColor = Color.Green;
            else
                Pnl_Background.BackColor = Color.Crimson;
        }
    }
}
