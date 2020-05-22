using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DsCore.Config;

namespace DemulShooter_GUI
{
    public partial class GUI_RawInputHID : UserControl
    {
        private List<GUI_Button> _Buttons = new List<GUI_Button>();
        private PlayerSettings _PlayerData;

        public GUI_RawInputHID()
        {
            InitializeComponent();
        }

        public void UpdateData(PlayerSettings PlayerData)
        {
            _PlayerData = PlayerData;
            //Force virtual mouse buttons to OFF with joypad
            _PlayerData.isVirtualMouseButtonsEnabled = false;

            //Clear controls
            Cbox_HID_XAxis.Items.Clear();
            Cbox_HID_YAxis.Items.Clear();
            Cbox_HID_ActionButton.Items.Clear();
            Cbox_HID_OffScreenButton.Items.Clear();
            Cbox_HID_OnScreenButton.Items.Clear();
            _Buttons.Clear();
            Pnl_ButtonsViewer.Controls.Clear();

            //Fill device-related values
            int indexX = 0;
            int indexY = 0;
            Cbox_HID_XAxis.Items.Add("");
            Cbox_HID_YAxis.Items.Add("");
            for (int i = 0; i < PlayerData.RIController.AxisList.Count; i++)
            {
                Cbox_HID_XAxis.Items.Add("0x" + PlayerData.RIController.AxisList[i].ToString("X2"));
                if (_PlayerData.RIController.Selected_AxisX == PlayerData.RIController.AxisList[i])
                    indexX = i + 1;
                Cbox_HID_YAxis.Items.Add("0x" + PlayerData.RIController.AxisList[i].ToString("X2"));
                if (_PlayerData.RIController.Selected_AxisY == PlayerData.RIController.AxisList[i])
                    indexY = i + 1;
            }
            Cbox_HID_XAxis.SelectedItem = Cbox_HID_XAxis.Items[indexX];
            Cbox_HID_YAxis.SelectedItem = Cbox_HID_YAxis.Items[indexY];

            int indexBtnAction = 0;            
            int indexBtnOnScreen = 0;
            int indexBtnOffScreen = 0;
            Cbox_HID_ActionButton.Items.Add("");
            Cbox_HID_OnScreenButton.Items.Add("");
            Cbox_HID_OffScreenButton.Items.Add("");
            for (int i = 0; i < PlayerData.RIController.NumberOfButtons; i++)
            {
                _Buttons.Add(new GUI_Button(i + 1));
                Pnl_ButtonsViewer.Controls.Add(_Buttons[i]);
                Cbox_HID_ActionButton.Items.Add(i + 1);
                if (_PlayerData.RIController.Selected_ActionButton == (i + 1))
                    indexBtnAction = i + 1;
                Cbox_HID_OnScreenButton.Items.Add(i+1);
                if (_PlayerData.RIController.Selected_OnScreenTriggerButton == (i + 1))
                    indexBtnOnScreen = i + 1;
                Cbox_HID_OffScreenButton.Items.Add(i+1);
                if (_PlayerData.RIController.Selected_OffScreenTriggerButton == (i + 1))
                    indexBtnOffScreen = i + 1;
            }
            Cbox_HID_ActionButton.SelectedItem = Cbox_HID_ActionButton.Items[indexBtnAction];
            Cbox_HID_OnScreenButton.SelectedItem = Cbox_HID_OnScreenButton.Items[indexBtnOnScreen];
            Cbox_HID_OffScreenButton.SelectedItem = Cbox_HID_OffScreenButton.Items[indexBtnOffScreen];
        }

        private void Cbox_HID_OnScreenButton_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (Cbox_HID_OnScreenButton.Text.Length > 0)
                _PlayerData.RIController.Selected_OnScreenTriggerButton = int.Parse(Cbox_HID_OnScreenButton.Text);
            else
                _PlayerData.RIController.Selected_OnScreenTriggerButton = 99;
        }

        private void Cbox_HID_ActionButton_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (Cbox_HID_ActionButton.Text.Length > 0)
                _PlayerData.RIController.Selected_ActionButton = int.Parse(Cbox_HID_ActionButton.Text);
            else
                _PlayerData.RIController.Selected_ActionButton = 99;
        }

        private void Cbox_HID_OffScreenButton_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (Cbox_HID_OffScreenButton.Text.Length > 0)
                _PlayerData.RIController.Selected_OffScreenTriggerButton = int.Parse(Cbox_HID_OffScreenButton.Text);
            else
                _PlayerData.RIController.Selected_OffScreenTriggerButton = 99;
        }

        private void Cbox_HID_XAxis_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (Cbox_HID_XAxis.Text.Length > 0)
                _PlayerData.RIController.Selected_AxisX = ushort.Parse(Cbox_HID_XAxis.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
            else
                _PlayerData.RIController.Selected_AxisX = 0;
        }

        private void Cbox_HID_YAxis_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (Cbox_HID_YAxis.Text.Length > 0)
                _PlayerData.RIController.Selected_AxisY = ushort.Parse(Cbox_HID_YAxis.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
            else
                _PlayerData.RIController.Selected_AxisY = 0;
        }

        #region GUI Animation

        public void UpdateGui()
        {
            for (int i = 0; i < _PlayerData.RIController.Hid_Buttons.Length; i++)
            {
                SetButtonState(i, _PlayerData.RIController.Hid_Buttons[i]);
            }
            Pnl_AxisViewer.Invalidate();
        }

        public void SetButtonState(int ButtonID, bool ButtonState)
        {
            if (ButtonID <= _Buttons.Count)
            {
                _Buttons[ButtonID].Activate(ButtonState);
            }
        }

        private void Pnl_AxisViewer_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);
            int X = Pnl_AxisViewer.Width / 2;
            int Y = Pnl_AxisViewer.Height / 2;

            try
            {
                double Xratio = (double)_PlayerData.RIController.Axis_X_Max / (double)Pnl_AxisViewer.Width;
                double Yratio = (double)_PlayerData.RIController.Axis_Y_Max / (double)Pnl_AxisViewer.Height;

                X = Convert.ToInt32(Math.Round((double)_PlayerData.RIController.Computed_X / Xratio));
                Y = Convert.ToInt32(Math.Round((double)_PlayerData.RIController.Computed_Y / Yratio));
            }
            catch { }

            // Draw Crosshair
            Graphics g = e.Graphics;
            SolidBrush b = new SolidBrush(Color.Black);
            Pen p = new Pen(b, 1);
            g.DrawLine(p, new PointF(X - 4, Y), new PointF(X + 4, Y));
            g.DrawLine(p, new PointF(X, Y + 4), new PointF(X, Y - 4));

            g.Dispose();
        }

        #endregion
    }
}
