using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore.Config;
using DsCore.Win32;

namespace DemulShooter_GUI
{
    public partial class GUI_RawInputMouse : UserControl
    {
        private List<GUI_Button> _Buttons = new List<GUI_Button>();
        PlayerSettings _PlayerData;

        // DIK registering TextBox
        private TextBox _SelectedTextBox;
        private String _SelectedTextBoxTextBackup = String.Empty;
        private bool _Start_KeyRecord = false;
        private Win32API.HookProc _KeyboardHookProc;
        private IntPtr _KeyboardHookID = IntPtr.Zero;

        public GUI_RawInputMouse()
        {
            InitializeComponent();

            //Installing KeyboardHook for DIK TextBox
            _KeyboardHookProc = new Win32API.HookProc(GuiKeyboardHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
                _KeyboardHookID = Win32API.SetWindowsHookEx(Win32Define.WH_KEYBOARD_LL, _KeyboardHookProc, Win32API.GetModuleHandle(curModule.ModuleName), 0);
            if (_KeyboardHookID == IntPtr.Zero)
            {
                MessageBox.Show("Failed to register LowLevel Keyboard Hook.", "DemulShooter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void UpdateData(PlayerSettings PlayerData)
        {
            _PlayerData = PlayerData;
            _Buttons.Clear();
            Pnl_ButtonsViewer.Controls.Clear();

            for (int i = 0; i < _PlayerData.RIController.NumberOfButtons; i++)
            {
                _Buttons.Add(new GUI_Button(i + 1));
                Pnl_ButtonsViewer.Controls.Add(_Buttons[i]);
            }

            Txt_VirtualMiddleBtn.Text = _PlayerData.DIK_VirtualMouseButton_Middle.ToString();
            Txt_VirtualRightBtn.Text = _PlayerData.DIK_VirtualMouseButton_Right.ToString();

            if (!_PlayerData.isVirtualMouseButtonsEnabled)
            {
                Chk_VirtualMiddleBtn.Checked = false;
                Txt_VirtualMiddleBtn.Enabled = false;
                Txt_VirtualRightBtn.Enabled = false;
            }
            else
            {
                Chk_VirtualMiddleBtn.Checked = true;
                Txt_VirtualMiddleBtn.Enabled = true;
                Txt_VirtualRightBtn.Enabled = true;
            }
        }

        private void Chk_VirtualMiddleBtn_CheckedChanged(object sender, EventArgs e)
        {
            _PlayerData.isVirtualMouseButtonsEnabled = Chk_VirtualMiddleBtn.Checked;
            if (!_PlayerData.isVirtualMouseButtonsEnabled)
            {
                Txt_VirtualMiddleBtn.Enabled = false;
                Txt_VirtualRightBtn.Enabled = false;
            }
            else
            {
                Txt_VirtualMiddleBtn.Enabled = true;
                Txt_VirtualRightBtn.Enabled = true;
            }
        }

        /// <summary>
        /// Generic "MouseClick" procedure for TextBox, to be able to set keys for config
        /// </summary>
        public void TXT_DIK_MouseClick(object sender, MouseEventArgs e)
        {
            if (_SelectedTextBox != null && _SelectedTextBox != ((TextBox)sender))
            {
                _SelectedTextBox.Text = _SelectedTextBoxTextBackup;
            }
            _SelectedTextBox = ((TextBox)sender);
            _SelectedTextBoxTextBackup = _SelectedTextBox.Text;
            _SelectedTextBox.Text = "";
            _Start_KeyRecord = true;
        }

        /// <summary>
        /// Keyboard hook for the GUI part, to detect buttons for config
        /// </summary>
        private IntPtr GuiKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (_Start_KeyRecord)
                    {
                        KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                        _SelectedTextBox.Text = s.scanCode.ToString();

                        if (_SelectedTextBox == Txt_VirtualMiddleBtn)
                            _PlayerData.DIK_VirtualMouseButton_Middle = s.scanCode;
                        else if (_SelectedTextBox == Txt_VirtualRightBtn)
                            _PlayerData.DIK_VirtualMouseButton_Right = s.scanCode;

                        _SelectedTextBox = null;
                        _Start_KeyRecord = false;
                        return new IntPtr(1);
                    }
                }
            }
            return Win32API.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }

        #region GUI Animation

        public void UpdateGui()
        {
            for (int i = 0; i < _PlayerData.RIController.Hid_Buttons.Length; i++ )
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
                int Xratio = (int)_PlayerData.RIController.Axis_X_Max / Pnl_AxisViewer.Width;
                int Yratio = (int)_PlayerData.RIController.Axis_Y_Max / Pnl_AxisViewer.Height;

                X = _PlayerData.RIController.Computed_X / Xratio;
                Y = _PlayerData.RIController.Computed_Y / Xratio;
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
