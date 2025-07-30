using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.RawInput;
using DsCore.Win32;

namespace DsDiag
{
    public partial class DsDiag : Form
    {
        public List<RawInputController> _Controllers = new List<RawInputController>();
        public List<Ds_Diag_Button> _Btn_List = new List<Ds_Diag_Button>();

        private string _SelectedDevice = String.Empty;
       
        /*** RAWINPUT data ***/

        private int _ScreenWidth = 0;
        private int _ScreenHeight = 0;
        private int _ClientWidth = 0;
        private int _ClientHeight = 0;
        private int _OnScreenX = 0;
        private int _OnScreenY = 0;
        private int _OnClientX = -1000; //force drawing crosshair out of screen at start
        private int _OnClientY = -1000;

        /// <summary>
        /// Construcor
        /// </summary>
        public DsDiag()
        {
            InitializeComponent();

            this.Text = "DemulShooter Devices Diagnostic " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

            //Fill the device list
            GetRawInputDevices();
            foreach (RawInputController Controller in _Controllers)
            {
                //Cbo_Dev.Items.Add(Controller.DeviceName);
                Cbo_Dev.Items.Add("[" + Controller.DHidUsage + "] " + Controller.ManufacturerName + " - " + Controller.ProductName);                
            }

            //Register to RawInput
            RawInputDevice[] rid = new RawInputDevice[3];
            rid[0].UsagePage = HidUsagePage.GENERIC;
            rid[0].Usage = HidUsage.Joystick;
            rid[0].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[0].hwndTarget = this.Handle;

            rid[1].UsagePage = HidUsagePage.GENERIC;
            rid[1].Usage = HidUsage.Mouse;
            rid[1].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[1].hwndTarget = this.Handle;

            rid[2].UsagePage = HidUsagePage.GENERIC;
            rid[2].Usage = HidUsage.Gamepad;
            rid[2].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[2].hwndTarget = this.Handle;

            if (!Win32API.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                MessageBox.Show("Failed to register raw input device(s).", "DemulShooter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            } 
        }

        #region GUI

        private void Ds_Diag_Load(object sender, System.EventArgs e)
        {
            GetScreenResolution();
            GetClientSize();
            //Hide PNG Target
            SetTargetLocation(-100, -100);
        }

        /// <summary>
        /// Change of selected device in the list
        /// </summary>
        private void Cbo_Dev_SelectionChangeCommitted(object sender, System.EventArgs e)
        {
            _OnClientX = -1000;
            _OnClientY = -1000;

            if (Cbo_Dev.Text.Length > 0)
            {
                RawInputController Controller = GetControllerFromName(_Controllers[Cbo_Dev.SelectedIndex].DeviceName);
                Lbl_dwType.Text = Controller.DeviceType.ToString() + " - " + Controller.DHidUsage;
                Lbl_Manufacturer.Text = Controller.ManufacturerName;
                Lbl_Product.Text = Controller.ProductName;
                Lbl_PID.Text = Controller.PID;
                Lbl_VID.Text = Controller.VID;
                _Btn_List.Clear();
                Lbl_Nbr_Buttons.Text = Controller.NumberOfButtons.ToString();
                FlowPanelButtons.Controls.Clear();
                for (int i = 0; i < Controller.NumberOfButtons; i++)
                {
                    Ds_Diag_Button b = new Ds_Diag_Button(i + 1);
                    b.Width = 15;
                    b.Height = 15;
                    FlowPanelButtons.Controls.Add(b);
                    _Btn_List.Add(b);
                }

                if (Controller.DeviceType != RawInputDeviceType.RIM_TYPEHID)
                {
                    Lbl_AxisX_Txt.Visible = false;
                    Lbl_AxisY_Txt.Visible = false;
                    Lbl_Nbr_Axis_Txt.Visible = false;
                    Lbl_Nbr_Axis.Visible = false;
                    Cbo_AxisX.Visible = false;
                    Cbo_AxisY.Visible = false;
                }
                else
                {
                    Lbl_AxisX_Txt.Visible = true;
                    Lbl_AxisY_Txt.Visible = true;
                    Lbl_Nbr_Axis_Txt.Visible = true;
                    Lbl_Nbr_Axis.Visible = true;
                    Cbo_AxisX.Visible = true;
                    Cbo_AxisY.Visible = true;

                    Lbl_Nbr_Axis.Text = Controller.NumberOfAxis.ToString();

                    Cbo_AxisX.Items.Clear();
                    Cbo_AxisY.Items.Clear();
                    for (int i = 0; i < Controller.AxisList.Count; i++)
                    {
                        Cbo_AxisX.Items.Add("0x" + Controller.AxisList[i].ToString("X2"));
                        Cbo_AxisY.Items.Add("0x" + Controller.AxisList[i].ToString("X2"));
                    }
                    if (Cbo_AxisX.Items.Count > 1)
                    {
                        Cbo_AxisX.SelectedItem = Cbo_AxisX.Items[1];
                        Cbo_AxisY.SelectedItem = Cbo_AxisX.Items[0];
                        Controller.Selected_AxisX = ushort.Parse(Cbo_AxisX.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
                        Controller.Selected_AxisY = ushort.Parse(Cbo_AxisY.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
                    }
                }

                _SelectedDevice = _Controllers[Cbo_Dev.SelectedIndex].DeviceName;
                Btn_Export.Visible = true;
            }
            else
            {
                Btn_Export.Visible = false;
            }
        }

        private void Cbo_AxisX_SelectionChangeCommitted(object sender, EventArgs e)
        {
            RawInputController c = GetControllerFromName(_Controllers[Cbo_Dev.SelectedIndex].DeviceName);
            c.Selected_AxisX = ushort.Parse(Cbo_AxisX.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
        }

        private void Cbo_AxisY_SelectionChangeCommitted(object sender, EventArgs e)
        {
            RawInputController c = GetControllerFromName(_Controllers[Cbo_Dev.SelectedIndex].DeviceName);
            c.Selected_AxisY = ushort.Parse(Cbo_AxisY.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
        }

        /// <summary>
        /// Export device HID specifications for analysis 
        /// </summary>
        private void Btn_Export_Click(object sender, EventArgs e)
        {
            RawInputController c = GetControllerFromName(_Controllers[Cbo_Dev.SelectedIndex].DeviceName);
            String DataFile = Application.StartupPath + @"\DsDiag_Report_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
            System.IO.StreamWriter sw = new System.IO.StreamWriter(DataFile, false);
            try
            {

                sw.WriteLine("----- Ds_Diag.exe Device Report File -----");
                sw.WriteLine("Device Name = " + c.DeviceName);
                sw.WriteLine("Device Type = " + c.DeviceType);
                if (c.DeviceType == RawInputDeviceType.RIM_TYPEHID)
                    sw.WriteLine("HID Usage = " + c.DHidUsage.ToString());
                sw.WriteLine("VID = " + c.VID);
                sw.WriteLine("PID = " + c.PID);
                sw.WriteLine("Device Description = " + c.ManufacturerName + " - " + c.ProductName);
                sw.WriteLine("Number of Buttons = " + c.NumberOfButtons);
                sw.WriteLine("Number of Axis = " + c.NumberOfAxis);
                string s = string.Empty;
                for (int i = 0; i < c.NumberOfAxis; i++)
                {
                    s += "0x" + c.AxisList[i].ToString("X2") + ", ";
                }
                sw.WriteLine("Axis List = " + s);
                if (c.DeviceType == RawInputDeviceType.RIM_TYPEHID)
                {
                    sw.WriteLine("----- HID detailed data  -----");
                    sw.WriteLine("Device Capabilities:");
                    sw.WriteLine("+ " + c.HID_Capabilities.ToString().Replace(", ", "\n+ "));
                    sw.WriteLine("");
                    sw.WriteLine("Button Capabilities :");
                    for (int i = 0; i < c.HID_ButtonCapabilitiesArray.Length; i++)
                    {
                        sw.WriteLine("+ [" + i.ToString("D2") + "]\n    + " + c.HID_ButtonCapabilitiesArray[i].ToString().Replace(", ", "\n    + "));
                    }
                    sw.WriteLine("");
                    sw.WriteLine("Values Capabilities :");
                    for (int i = 0; i < c.HID_ValueCapabilitiesArray.Length; i++)
                    {
                        sw.WriteLine("+ [" + i.ToString("D2") + "]\n    + " + c.HID_ValueCapabilitiesArray[i].ToString().Replace(", ", "\n    + "));
                    }
                    sw.WriteLine("");
                    sw.WriteLine("Values Capabilities :");
                    for (int i = 0; i < c.HID_OutputValueCapabilitiesArray.Length; i++)
                    {
                        sw.WriteLine("+ [" + i.ToString("D2") + "]\n    + " + c.HID_OutputValueCapabilitiesArray[i].ToString().Replace(", ", "\n    + "));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
            finally
            {
                sw.Close();
                MessageBox.Show("Device report saved to :\n" + DataFile);
            }
        }
        
        #endregion

        #region RAW_INPUT

        /// <summary>
        /// Enumerates the Raw Input Devices and places their corresponding RawInputDevice structures into a List<string>
        /// We just filter to remove Keyboard devices and keep Mouse + HID devices
        /// </summary>
        private void GetRawInputDevices()
        {
            uint deviceCount = 0;
            var dwSize = (Marshal.SizeOf(typeof(RawInputDeviceList)));

            if (Win32API.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
            {
                var pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                Win32API.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                for (var i = 0; i < deviceCount; i++)
                {
                    // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
                    RawInputDeviceList rid = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr((pRawInputDeviceList.ToInt64() + (dwSize * i))), typeof(RawInputDeviceList));
                    
                    RawInputController c = new RawInputController(rid.hDevice, rid.dwType);                    

                    if (c.DeviceType == RawInputDeviceType.RIM_TYPEHID || c.DeviceType == RawInputDeviceType.RIM_TYPEMOUSE)
                    {
                        _Controllers.Add(c);
                    }
                }
                Marshal.FreeHGlobal(pRawInputDeviceList);
                return;
            }
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Get Handle from DeviceName
        /// </summary>
        private RawInputController GetControllerFromName(String DeviceName)
        {
            foreach (RawInputController Controller in _Controllers)
            {
                if (Controller.DeviceName == DeviceName)
                {
                    return Controller;
                }
            }
            return null;
        }       

        /// <summary>
        /// Get events and process 
        /// </summary>
        private bool ProcessRawInput(IntPtr LParam)
        { 
            foreach (RawInputController Controller in _Controllers)
            {
                if (Controller.isSourceOfRawInputMessage(LParam))
                {
                    if (_SelectedDevice == Controller.DeviceName)
                    {
                        Controller.ProcessRawInputData(LParam);
                        //Update Axis
                        UpdateRawAxis(Controller);
                        //Update Buttons
                        for (int i = 0; i < Controller.Hid_Buttons.Length; i++)
                        {
                            _Btn_List[i].Activate(Controller.Hid_Buttons[i]);
                        }
                        break;
                    }
                }
            }
            return true;
        }       

        #endregion        

        #region SCREEN

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
        private void ClientScale(AxisInfo AxisData)
        {
            //_OnClientX = mouse.pTarget.X * _ClientWidth / _ScreenWidth;
            //_OnClientY = mouse.pTarget.Y * _ClientHeight / _ScreenHeight;
            //_OnClientX = mouse.pTarget.X - this.ClientRectangle.Left;
            //_OnClientY = mouse.pTarget.Y - this.ClientRectangle.Top;
            POINT lp = new POINT(AxisData.pTarget.X, AxisData.pTarget.Y);
            Win32API.ScreenToClient(this.Handle, ref lp);
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
                    {
                        //read in new mouse values.
                        ProcessRawInput(m.LParam);
                    }   break;
            }
            base.WndProc(ref m);
        }

        #endregion

        /// <summary>
        /// Update labels on window with Device data
        /// </summary>
        /// <param name="Controller"></param>
        public void UpdateRawAxis(RawInputController Controller)
        {
            Lbl_RawInput.Text = "[ 0x" + Controller.Computed_X.ToString("X8") + ", " + Controller.Computed_Y.ToString("X8") + " ]";
            Lbl_AxisXMin.Text = Controller.Axis_X_Min.ToString() + " [ 0x" + Controller.Axis_X_Min.ToString("X8") + " ]";
            Lbl_AxisXMax.Text = Controller.Axis_X_Max.ToString() + " [ 0x" + Controller.Axis_X_Max.ToString("X8") + " ]";
            Lbl_AxisYMin.Text = Controller.Axis_Y_Min.ToString() + " [ 0x" + Controller.Axis_Y_Min.ToString("X8") + " ]";
            Lbl_AxisYMax.Text = Controller.Axis_Y_Max.ToString() + " [ 0x" + Controller.Axis_Y_Max.ToString("X8") + " ]";

            // Update screen/client info
            GetScreenResolution();
            GetClientSize();

            // Convert RAW to SCREEN    
            Controller.Computed_X = ScreenScale(Controller.Computed_X, Controller.Axis_X_Min, Controller.Axis_X_Max, 0, _ScreenWidth);
            Controller.Computed_Y = ScreenScale(Controller.Computed_Y, Controller.Axis_Y_Min, Controller.Axis_Y_Max, 0, _ScreenHeight);
            _OnScreenX = Controller.Computed_X;
            _OnScreenY = Controller.Computed_Y;
            Lbl_OnScreen.Text = _OnScreenX.ToString() + "x" + _OnScreenY.ToString();

            // Convert SCREEN to CLIENT
            AxisInfo i = new AxisInfo();
            i.pTarget.X = Controller.Computed_X;
            i.pTarget.Y = Controller.Computed_Y;
            ClientScale(i);
            Lbl_OnClient.Text = _OnClientX.ToString() + "x" + _OnClientY.ToString();

            // Display mark on client, moved to Timer to not overload GUI thread with too many rawinout data drawing
            //this.Invalidate();

            //Custom crosshair with PNG, not displaying well at all....
            //SetTargetLocation(i.pTarget.X, i.pTarget.Y);
        }

        //Draw Crasshair on repaint
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

            //Crosshair
            Pen TargetPen = new Pen(Color.Red, 3);
            e.Graphics.DrawEllipse(TargetPen, _OnClientX - 20, _OnClientY - 20, 40, 40);
            e.Graphics.DrawLine(TargetPen, _OnClientX - 30, _OnClientY, _OnClientX - 10, _OnClientY);
            e.Graphics.DrawLine(TargetPen, _OnClientX +10, _OnClientY, _OnClientX +30, _OnClientY);
            e.Graphics.DrawLine(TargetPen, _OnClientX, _OnClientY - 30, _OnClientX, _OnClientY - 10);
            e.Graphics.DrawLine(TargetPen, _OnClientX, _OnClientY + 10, _OnClientX, _OnClientY + 30);
            TargetPen.Dispose();
        }

        /// <summary>
        /// Display some custom Crosshair PNG in PictureBox
        /// </summary>
        /// <param name="X">X value in the window</param>
        /// <param name="Y">Y value in the window</param>
        private void SetTargetLocation(int X, int Y)
        {
            Pbox_Target.Left = X - Pbox_Target.Width / 2;
            Pbox_Target.Top = Y - Pbox_Target.Height / 2;
        }

        //Used for Debug, not activated for release
        public void WriteLog(String Data)
        {
            Txt_Log.AppendText(Data + "\n");
            Txt_Log.ScrollToCaret();
        }

        /// <summary>
        /// Forcing the GUI redraw at a forced refresh rate 
        /// If not, too many RAWINPUT event will block GUI thread from refreshing normally
        /// </summary>
        private void Tmr_RefreshGui_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }
    }

    

    class AxisInfo
    {
        public IntPtr devHandle;
        public string devName;
        public System.Drawing.Point pTarget;
        public int button;

        public AxisInfo()
        {
            devHandle = IntPtr.Zero;
            devName = String.Empty;
            pTarget.X = 0;
            pTarget.Y = 0;
            button = 0;
        }
    }

    
}
