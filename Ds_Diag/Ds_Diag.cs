using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Ds_Diag
{
    public partial class Ds_Diag : Form
    {
        /*** RAWINPUT data ***/
        private List<MouseInfo> _MiceList;
        private string _DeviceName;
        private IntPtr _MouseHandle = IntPtr.Zero;
        private int _Act_Labs_Offset_Enable = 0;
        private int _Act_Labs_OffsetX = 0;
        private int _Act_Labs_OffsetY = 0;
        const Int32 INPUT_ABSOLUTE_MIN = 0;
        const Int32 INPUT_ABSOLUTE_MAX = 65536;

        private int _ScreenWidth = 0;
        private int _ScreenHeight = 0;
        private int _ClientWidth = 0;
        private int _ClientHeight = 0;
        private int _OnScreenX = 0;
        private int _OnScreenY = 0;
        private int _OnClientX = 0;
        private int _OnClientY = 0;

        /// <summary>
        /// Construcor
        /// </summary>
        public Ds_Diag()
        {
            InitializeComponent();

            _MiceList = new List<MouseInfo>();
            GetRawInputDevices();
            foreach (MouseInfo mouse in _MiceList)
            {
                Cbo_Dev.Items.Add(mouse.devName);
            }
            //Second step : add XInput Gamepads
            /*_XInputStates = new XInputState[XInputConstants.MAX_CONTROLLER_COUNT];
            for (int i = XInputConstants.FIRST_CONTROLLER_INDEX; i < XInputConstants.MAX_CONTROLLER_COUNT; i++)
            {
                XInputCapabilities capabilities = new XInputCapabilities();
                int ret = XInput.XInputGetCapabilities(i, XInputConstants.XINPUT_FLAG_GAMEPAD, ref capabilities);
                if (ret == XInputConstants.ERROR_SUCCES)
                {
                    if (XInput.XInputGetState(i, ref _XInputStates[i]) == XInputConstants.ERROR_SUCCES)
                    {
                        Cbo_Dev1.Items.Add(XINPUT_DEVICE_PREFIX + (i + 1));
                        Cbo_Dev2.Items.Add(XINPUT_DEVICE_PREFIX + (i + 1));
                    }
                }
            } */

            //Register to RawInput
            RawInputDevice[] rid = new RawInputDevice[1];
            rid[0].UsagePage = HidUsagePage.GENERIC;
            rid[0].Usage = HidUsage.Mouse;
            rid[0].Flags = RawInputDeviceFlags.INPUTSINK;
            rid[0].Target = this.Handle;
            if (!Win32.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                MessageBox.Show("Failed to register raw input device(s).", "DemulShooter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }  

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetScreenResolution();
            GetClientSize();
            //Hide Target
            SetTargetLocation(-100, -100);
        }

        public void GetScreenResolution()
        {
            _ScreenWidth = Screen.PrimaryScreen.Bounds.Width;
            _ScreenHeight = Screen.PrimaryScreen.Bounds.Height;
            Lbl_ScreenSize.Text = _ScreenWidth.ToString() + "x" + _ScreenHeight.ToString();
        }

        public void GetClientSize()
        {
            _ClientWidth = this.ClientSize.Width;
            _ClientHeight = this.ClientSize.Height;
            Lbl_ClientSize.Text = _ClientWidth.ToString() + "x" + _ClientHeight.ToString();
        }

        private void Cbo_Dev_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _DeviceName = Cbo_Dev.Text;
            _MouseHandle = GetRawMouseHandleFromName(Cbo_Dev.Text);            
        }

        #region RAW_INPUT

        /// <summary>
        /// Enumerates the Raw Input Devices and places their corresponding RawInputDevice structures into a List<string>
        /// </summary>
        private void GetRawInputDevices()
        {
            _MiceList.Clear();
            uint deviceCount = 0;
            var dwSize = (Marshal.SizeOf(typeof(Rawinputdevicelist)));

            if (Win32.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
            {
                var pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                Win32.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                for (var i = 0; i < deviceCount; i++)
                {
                    uint pcbSize = 0;
                    // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
                    var rid = (Rawinputdevicelist)Marshal.PtrToStructure(new IntPtr((pRawInputDeviceList.ToInt64() + (dwSize * i))), typeof(Rawinputdevicelist));
                    if (rid.dwType == DeviceType.RimTypemouse)
                    {
                        Win32.GetRawInputDeviceInfo(rid.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
                        if (pcbSize <= 0) continue;

                        var pData = Marshal.AllocHGlobal((int)pcbSize);
                        Win32.GetRawInputDeviceInfo(rid.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, pData, ref pcbSize);
                        var deviceName = Marshal.PtrToStringAnsi(pData);

                        MouseInfo mouse = new MouseInfo();
                        mouse.devHandle = rid.hDevice;
                        mouse.devName = (string)deviceName;
                        _MiceList.Add(mouse);
                    }
                }
                Marshal.FreeHGlobal(pRawInputDeviceList);
                return;
            }
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Get Handle from mouseName
        /// </summary>
        private IntPtr GetRawMouseHandleFromName(String MouseName)
        {
            foreach (MouseInfo mouse in _MiceList)
            {
                if (mouse.devName == MouseName)
                {
                    return mouse.devHandle;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Get movements/click and process 
        /// </summary>
        private bool ProcessRawInput(IntPtr hDevice)
        {
            var dwSize = 0;
            Win32.GetRawInputData(hDevice, DataCommand.RID_INPUT, IntPtr.Zero, ref dwSize, Marshal.SizeOf(typeof(Rawinputheader)));

            InputData rawBuffer;
            if (dwSize != Win32.GetRawInputData(hDevice, DataCommand.RID_INPUT, out rawBuffer, ref dwSize, Marshal.SizeOf(typeof(Rawinputheader))))
            {
                //Debug.WriteLine("Error getting the rawinput buffer");
                return false;
            }

            if (rawBuffer.header.dwType == DeviceType.RimTypemouse)
            //On Windows10 rawBuffer.data.mouse.usFlags==1 is never true.....so I removed the test
            //if (rawBuffer.header.dwType == DeviceType.RimTypemouse && rawBuffer.data.mouse.usFlags == 1)        //usFlags : 1=ABSOLUTE 0= RELATIVE
            {
                if (rawBuffer.header.hDevice == _MouseHandle)
                {
                    foreach (MouseInfo mouse in _MiceList)
                    {
                        if (rawBuffer.header.hDevice == mouse.devHandle)
                        {
                            MouseInfo mymouse = new MouseInfo();
                            mymouse.pTarget.X = rawBuffer.data.mouse.lLastX;
                            mymouse.pTarget.Y = rawBuffer.data.mouse.lLastY;
                            Lbl_RawInput.Text = "[ 0x" + mymouse.pTarget.X.ToString("X4") + ", " + mymouse.pTarget.Y.ToString("X4") + " ]";

                            // Display mouse button state
                            if (rawBuffer.data.mouse.usButtonFlags == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                                Cbox_MouseLeft.Checked = true;
                            else if (rawBuffer.data.mouse.usButtonFlags == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                                Cbox_MouseLeft.Checked = false;
                            else if (rawBuffer.data.mouse.usButtonFlags == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                                Cbox_MouseMiddle.Checked = true;
                            else if (rawBuffer.data.mouse.usButtonFlags == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                                Cbox_MouseMiddle.Checked = false;
                            else if (rawBuffer.data.mouse.usButtonFlags == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                                Cbox_MouseRight.Checked = true;
                            else if (rawBuffer.data.mouse.usButtonFlags == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                                Cbox_MouseRight.Checked = false;

                            // Update screen/client info
                            GetScreenResolution();
                            GetClientSize();

                            // Convert RAW to SCREEN
                            mymouse.pTarget.X = ScreenScale(mymouse.pTarget.X, INPUT_ABSOLUTE_MIN, INPUT_ABSOLUTE_MAX, 0, _ScreenWidth);
                            mymouse.pTarget.Y = ScreenScale(mymouse.pTarget.Y, INPUT_ABSOLUTE_MIN, INPUT_ABSOLUTE_MAX, 0, _ScreenHeight);
                            _OnScreenX = mymouse.pTarget.X;
                            _OnScreenY = mymouse.pTarget.Y;
                            Lbl_OnScreen.Text = _OnScreenX.ToString() + "x" + _OnScreenY.ToString();

                            // Optionnal OFFSET
                            if (_Act_Labs_Offset_Enable == 1)
                            {
                                mymouse.pTarget.X += _Act_Labs_OffsetX;
                                mymouse.pTarget.Y += _Act_Labs_OffsetY;
                            }

                            // Convert SCREEN to CLIENT
                            ClientScale(mymouse);
                            Lbl_OnClient.Text = _OnClientX.ToString() + "x" + _OnClientY.ToString();

                            // Display mark on client
                            this.Invalidate();
                            break;
                        }
                    }
                }
            }
            return true;
        }

        #endregion        

        #region SCREEN

        /// <summary>
        /// Contains value inside min-max range
        /// </summary>
        protected int Clamp(int val, int minVal, int maxVal)
        {
            if (val > maxVal) return maxVal;
            else if (val < minVal) return minVal;
            else return val;
        }

        /// <summary>
        /// Transforming 0x0000-0xFFFF absolute rawdata to absolute x,y position on Desktop resolution
        /// </summary>
        public int ScreenScale(int val, int fromMinVal, int fromMaxVal, int toMinVal, int toMaxVal)
        {
            return ScreenScale(val, fromMinVal, fromMinVal, fromMaxVal, toMinVal, toMinVal, toMaxVal);
        }
        protected int ScreenScale(int val, int fromMinVal, int fromOffVal, int fromMaxVal, int toMinVal, int toOffVal, int toMaxVal)
        {
            double fromRange;
            double frac;
            if (fromMaxVal > fromMinVal)
            {
                val = Clamp(val, fromMinVal, fromMaxVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMaxVal - fromOffVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMinVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else
                    return toOffVal;
            }
            else if (fromMinVal > fromMaxVal)
            {
                val = Clamp(val, fromMaxVal, fromMinVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMinVal - fromOffVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMaxVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else
                    return toOffVal;
            }
            else
                return toOffVal;
            double toRange;
            if (toMaxVal > toMinVal)
            {
                if (frac >= 0)
                    toRange = (double)(toMaxVal - toOffVal);
                else
                    toRange = (double)(toOffVal - toMinVal);
                return toOffVal + (int)(toRange * frac);
            }
            else
            {
                if (frac >= 0)
                    toRange = (double)(toOffVal - toMaxVal);
                else
                    toRange = (double)(toMinVal - toOffVal);
                return toOffVal - (int)(toRange * frac);
            }
        }

        /// <summary>
        /// Convert screen location of pointer to Client area location
        /// </summary>
        private void ClientScale(MouseInfo mouse)
        {
            //_OnClientX = mouse.pTarget.X * _ClientWidth / _ScreenWidth;
            //_OnClientY = mouse.pTarget.Y * _ClientHeight / _ScreenHeight;
            //_OnClientX = mouse.pTarget.X - this.ClientRectangle.Left;
            //_OnClientY = mouse.pTarget.Y - this.ClientRectangle.Top;
            Point lp = new Point(mouse.pTarget.X, mouse.pTarget.Y);
            Win32.ScreenToClient(this.Handle, ref lp);
            _OnClientX = lp.X;
            _OnClientY = lp.Y;
        }

        #endregion

        #region WINDOW MESSAGE LOOP

        /*************** WM Boucle *********************/
        protected const int WM_INPUT = 0x00FF;
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_INPUT:
                    //read in new mouse values.
                    ProcessRawInput(m.LParam);
                    break;
            }
            base.WndProc(ref m);
        }

        #endregion

        private void Ds_Diag_Paint(object sender, PaintEventArgs e)
        {
            // Border
            Pen RedPen = new Pen(Color.Red, 5);
            Rectangle rect = new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height);
            e.Graphics.DrawRectangle(RedPen, rect);
            RedPen = new Pen(Color.Red, 2);
            rect = new Rectangle(0, 0, this.ClientSize.Width / 2, this.ClientSize.Height);
            e.Graphics.DrawRectangle(RedPen, rect);
            rect = new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height / 2);
            e.Graphics.DrawRectangle(RedPen, rect);
            RedPen.Dispose();


            /*int SquareWidth = this.ClientSize.Width / 20;
            int SquareHeight = this.ClientSize.Height / 20;

            System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
            e.Graphics.FillRectangle(myBrush, new Rectangle(0, 0, SquareWidth, SquareHeight));
            e.Graphics.FillRectangle(myBrush, new Rectangle(this.ClientSize.Width - SquareWidth, 0, SquareWidth, SquareHeight));
            e.Graphics.FillRectangle(myBrush, new Rectangle(this.ClientSize.Width - SquareWidth, this.ClientSize.Height - SquareHeight, SquareWidth, SquareHeight));
            e.Graphics.FillRectangle(myBrush, new Rectangle(0, this.ClientSize.Height - SquareHeight, SquareWidth, SquareHeight));
            myBrush.Dispose();*/

            //Crosshair
            Pen TargetPen = new Pen(Color.Red, 3);
            e.Graphics.DrawEllipse(TargetPen, _OnClientX - 20, _OnClientY - 20, 40, 40);
            e.Graphics.DrawLine(TargetPen, _OnClientX - 30, _OnClientY, _OnClientX - 10, _OnClientY);
            e.Graphics.DrawLine(TargetPen, _OnClientX +10, _OnClientY, _OnClientX +30, _OnClientY);
            e.Graphics.DrawLine(TargetPen, _OnClientX, _OnClientY - 30, _OnClientX, _OnClientY - 10);
            e.Graphics.DrawLine(TargetPen, _OnClientX, _OnClientY + 10, _OnClientX, _OnClientY + 30);
            TargetPen.Dispose();

        }

        private void SetTargetLocation(int X, int Y)
        {
            Pbox_Target.Left = X - Pbox_Target.Width / 2;
            Pbox_Target.Top = Y - Pbox_Target.Height / 2;
        }


    }

    class MouseInfo
    {
        public IntPtr devHandle;
        public string devName;
        public System.Drawing.Point pTarget;
        public int button;

        public MouseInfo()
        {
            devHandle = IntPtr.Zero;
            devName = String.Empty;
            pTarget.X = 0;
            pTarget.Y = 0;
            button = 0;
        }
    }
}
