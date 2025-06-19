using System;
using System.Windows.Forms;
using DsCore.Config;
using DsCore.RawInput;
using DsCore;

namespace DemulShooter_GUI
{
    public partial class GUI_Player : UserControl
    {
        private const String XINPUT_DEVICE_PREFIX = "XInput Gamepad #";
        private PlayerSettings _PlayerData;
        private RawInputController[] _AvailableControllers;

        GUI_RawInputMouse _RimData;
        GUI_RawInputHID _RihData;
        
        public GUI_Player(PlayerSettings PlayerData, RawInputController[] AvailableControllers)
        {
            InitializeComponent();
            _PlayerData = PlayerData;
            _AvailableControllers = AvailableControllers;

            _RimData = new GUI_RawInputMouse();
            _RihData = new GUI_RawInputHID();
            _RimData.Visible = false;
            _RihData.Visible = false;
            Pnl_Options.Controls.Add(_RimData);
            Pnl_Options.Controls.Add(_RihData);

            Logger.WriteLog("Initializing Player" + _PlayerData.ID.ToString() + " devices:");
            Lbl_Player.Text = "P" + _PlayerData.ID.ToString() + " Device :";

            //GUI init
            AddDevice("");
            foreach (RawInputController Controller in AvailableControllers)
            {
                try
                {
                    Logger.WriteLog("Adding " + Controller.DeviceName);
                    AddDevice("[" + Controller.DHidUsage + "] " + Controller.ManufacturerName + " - " + Controller.ProductName );
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("GUI_Player() ERROR : " + ex.Message.ToString());
                }
            }

            if (_PlayerData.DeviceName.Length > 0)
            {
                Logger.WriteLog("Current selected device : " + _PlayerData.DeviceName);
                for (int i = 0; i < _AvailableControllers.Length; i++)
                {
                    if (_PlayerData.DeviceName == _AvailableControllers[i].DeviceName)
                    {
                        Cbo_Device.SelectedItem = Cbo_Device.Items[i+1];
                        SelectRawInputController(_AvailableControllers[i].DeviceName);
                    }
                }
            }
            else
                Logger.WriteLog("Current selected device : [None]");            
        }

        /// <summary>
        /// Add a device in the drop down list
        /// </summary>
        /// <param name="DeviceName">Name of the device to display</param>
        public void AddDevice(string DeviceName)
        {
            Cbo_Device.Items.Add(DeviceName);
        }

        /// <summary>
        /// Display whethet the vibration are enabled or not for the gamepad
        /// </summary>
        /// <param name="Enabled">True or False</param>
        public void SetVibrationEnabled(bool Enabled)
        {
            //Chk_VibrationEnable.Checked = Enabled;
        }

        /// <summary>
        /// Return a RawInputController based on a Device Name
        /// </summary>
        /// <param name="RawInputDeviceName">The Device Name corresponding to the wanted Device Controller</param>
        /// <returns>a RawInputController if found in the list, otherwise NULL</returns>
        private RawInputController GetRawInputControllerFromDeviceName(String RawInputDeviceName)
        {
            if (RawInputDeviceName.Length > 0)
            {
                foreach (RawInputController c in _AvailableControllers)
                {
                    if (c.DeviceName == RawInputDeviceName)
                        return c;
                }
            }
            return null;
        }

        /// <summary>
        /// Select a RawInputController and set values (selected Axis and Buttons)
        /// </summary>
        /// <param name="RawInputDeviceName">DeviceName string of the wanted RawInputController</param>
        private void SelectRawInputController(String RawInputDeviceName)
        {
            _PlayerData.RIController = GetRawInputControllerFromDeviceName(RawInputDeviceName);
            if (_PlayerData.RIController != null)
            {
                if (_PlayerData.RIController.DeviceType == RawInputDeviceType.RIM_TYPEMOUSE)
                {
                    _RihData.Visible = false;
                    _RimData.Visible = true;
                    _RimData.UpdateData(_PlayerData);
                }
                else if (_PlayerData.RIController.DeviceType == RawInputDeviceType.RIM_TYPEHID)
                {
                    _RihData.Visible = true;
                    _RimData.Visible = false;
                    _RihData.UpdateData(_PlayerData);
                }
                else
                {
                    _RihData.Visible = false;
                    _RimData.Visible = false; 
                }
                Lbl_ProductManu.Text = _PlayerData.RIController.DeviceName; //_PlayerData.RIController.ManufacturerName + " " + _PlayerData.RIController.ProductName;
            }
        }

        /// <summary>
        /// Clear all buttons and axis values if no controller is selected
        /// </summary>
        private void ClearRawInputController()
        {
            _PlayerData.RIController = null;
            Lbl_ProductManu.Text = "";
            _RihData.Visible = false;
            _RimData.Visible = false; 
        }

        #region GUI

        private void Cbo_Device_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (Cbo_Device.Text.Length > 0)
                SelectRawInputController(_AvailableControllers[Cbo_Device.SelectedIndex - 1].DeviceName);
            else
                ClearRawInputController();
        }

        /// <summary>
        /// Called by the main window chen a RawInput event occurs to update graphical representation.
        /// </summary>
        public void UpdateGui()
        {
            if (_RihData.Visible)
                _RihData.UpdateGui();
            else if (_RimData.Visible)
                _RimData.UpdateGui();            
        }

        #endregion
    }
}
