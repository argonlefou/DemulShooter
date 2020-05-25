using System;
using System.Collections.Generic;
using System.Text;
using DsCore.RawInput;
using DsCore.Win32;
using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    public class RawInputController
    {
        private IntPtr _hDevice;
        private RawInputDeviceType _dwType;
        private String _DeviceName;
        private RawInputDeviceInfo _DeviceInfo;
        private string _ManufacturerName;
        private String _ProductName;
        #region Accessors

        public IntPtr DeviceHandle
        {
            get { return _hDevice; }
        }

        public RawInputDeviceType DeviceType
        {
            get { return _dwType; }
        }

        public String DeviceName
        {
            get { return _DeviceName; }
        }

        public HidUsage DHidUsage
        {
            get
            {
                if (_dwType == RawInputDeviceType.RIM_TYPEHID)
                    return (HidUsage)_Caps.Usage;
                else if (_dwType == RawInputDeviceType.RIM_TYPEMOUSE)
                    return HidUsage.Mouse;
                else
                    return HidUsage.Keyboard;
            }
        }

        public String PID
        {
            get
            {
                if (_dwType == RawInputDeviceType.RIM_TYPEHID)
                {
                    return "0x" + _DeviceInfo.hid.dwProductId.ToString("X4");
                }
                else if (_DeviceName.Contains("PID_"))
                {
                    return "0x" + _DeviceName.Substring(_DeviceName.IndexOf("PID_") + 4, 4);
                }
                else return "";
            }
        }

        public String VID
        {
            get
            {
                if (_dwType == RawInputDeviceType.RIM_TYPEHID)
                {
                    return "0x" + _DeviceInfo.hid.dwVendorId.ToString("X4");
                }
                else if (_DeviceName.Contains("VID_"))
                {
                    return "0x" + _DeviceName.Substring(_DeviceName.IndexOf("VID_") + 4, 4);
                }
                else return "";
            }
        }

        public String ManufacturerName
        {
            get { return _ManufacturerName; }
        }

        public String ProductName
        {
            get { return _ProductName; }
        }

        #endregion

        //Common struct used for any type of devices
        private RawInputHeader _RawInputHeader;
        private IntPtr _pPreparsedData;

        //If the controller is HID device, we will use the following struct to parse WM_INPUT:
        private RawHid _RawHidData;
        private int _Hid_NumberofButtons;
        private bool[] _Hid_Buttons;
        private int _Hid_NumberofAxis;
        private List<ushort> _Hid_Axis_List;
        private Int32 _Hid_Axis_X_Max;
        private Int32 _Hid_Axis_X_Min;
        private Int32 _Hid_Axis_Y_Max;
        private Int32 _Hid_Axis_Y_Min;
        #region Accessors

        public int NumberOfButtons
        {
            get
            {
                int b = 0;
                if (_dwType == RawInputDeviceType.RIM_TYPEHID)
                    b = _Hid_NumberofButtons;
                else if (_dwType == RawInputDeviceType.RIM_TYPEMOUSE)
                    b = _DeviceInfo.mouse.dwNumberOfButtons;
                else if (_dwType == RawInputDeviceType.RIM_TYPEKEYBOARD)
                    b = _DeviceInfo.keyboard.dwNumberOfKeysTotal;
                return b;
            }
        }

        public bool[] Hid_Buttons
        {
            get { return _Hid_Buttons; }
        }

        public int NumberOfAxis
        {
            get { return _Hid_NumberofAxis; }
        }

        public List<ushort> AxisList
        {
            get { return _Hid_Axis_List; }
        }

        public Int32 Axis_X_Min
        {
            get { return _Hid_Axis_X_Min; }
        }

        public Int32 Axis_X_Max
        {
            get { return _Hid_Axis_X_Max; }
        }

        public Int32 Axis_Y_Min
        {
            get { return _Hid_Axis_Y_Min; }
        }

        public Int32 Axis_Y_Max
        {
            get
            {
                //Special case for Sony Dual SHock 3 or WiiMotes with XInput driver :
                //The driver is returning 0xFFFFFFFF as LogicalMax (which is -1 for a signed long)
                //whereas real data are between 0x0000 and 0xFFFF                
                if (_Hid_Axis_Y_Max == -1)
                    return 0x0000FFFF;
                else
                    return _Hid_Axis_Y_Max;
            }
        }

        #endregion

        //If the controller is Mouse device, we will use the following struct 
        private RawInputDataMouse _RawInputDataMouse;

        //HidP variables
        private HidPCaps _Caps;
        private HidPButtonCaps[] _pButtonCaps;
        private HidPValueCaps[] _pValueCaps;
        private HidPValueCaps[] _pOutputValueCaps;
        #region Accessors

        public HidPCaps HID_Capabilities
        {
            get { return _Caps; }
        }

        public HidPButtonCaps[] HID_ButtonCapabilitiesArray
        {
            get { return _pButtonCaps; }
        }

        public HidPValueCaps[] HID_ValueCapabilitiesArray
        {
            get { return _pValueCaps; }
        }

        public HidPValueCaps[] HID_OutputValueCapabilitiesArray
        {
            get { return _pOutputValueCaps; }
        }

        #endregion

        //Our Controller data to be used for each player
        public struct ControllerComputedData
        {
            public POINT Axis;
            public RawInputcontrollerButtonEvent Buttons;
        }
        private ControllerComputedData _ControllerData;
        private ushort _Selected_HidAxisX = 0x30;
        private ushort _Selected_HidAxisY = 0x31; 
        #region Accessor
        public ushort Selected_AxisX
        {
            get { return _Selected_HidAxisX; }
            set { _Selected_HidAxisX = value; }
        }
        public ushort Selected_AxisY
        {
            get { return _Selected_HidAxisY; }
            set { _Selected_HidAxisY = value; }
        }
        public Int32 Computed_X
        {
            get { return _ControllerData.Axis.X; }
            set { _ControllerData.Axis.X = value; }
        }
        public Int32 Computed_Y
        {
            get { return _ControllerData.Axis.Y; }
            set { _ControllerData.Axis.Y = value; }
        }
        public RawInputcontrollerButtonEvent Computed_Buttons
        {
            get { return _ControllerData.Buttons; }
            set { _ControllerData.Buttons = value; }
        }
        #endregion

        //At start, DemulShooter was created for Aimtraks : 
        // - Aimtrak Trigger action was set to LeftButton (i.e Trigger)
        // - Aimtrak OutOfScreen Trigger action was set to RightButton (i.e Reload)
        // - Aimtrak "Another button" if available was set to MiddleButton (i.e Action)
        //With these 3 actions we could use RawInputMouseButtonState fully, but with Joypad and HID it's a little bit different :
        //There can be a lot of buttons so the user has to choose it's buttons for these 3 actions.
        //Default values are for MouseType device, and they will be changed by the HID device if needed
        private int _Button_OnScreenTrigger_Index = 0;
        private int _Button_OffScreenTrigger_Index = 1;
        private int _Button_Action_Index = 2;
        #region Accessors

        //Buttons index are 0-based for code handling, but 1-based for config file and easy user choice
        //That's why we are making this little change here :
        public int Selected_OnScreenTriggerButton
        {
            get { return _Button_OnScreenTrigger_Index + 1; }
            set { _Button_OnScreenTrigger_Index = value - 1; }
        }

        public int Selected_ActionButton
        {
            get { return _Button_Action_Index + 1; }
            set { _Button_Action_Index = value - 1; }
        }

        public int Selected_OffScreenTriggerButton
        {
            get { return _Button_OffScreenTrigger_Index + 1; }
            set { _Button_OffScreenTrigger_Index = value - 1; }
        }

        #endregion

        /// <summary>
        /// Constructor, fill as much Hidp_Struct as we can at the creation
        /// </summary>
        /// <param name="DeviceHandle">A handle to the raw input device.</param>
        /// <param name="DeviceType">The type of the raw input device</param>
        /// <param name="Caller">Caller class so that we can send data back for WriteLogDebug</param>
        public RawInputController(IntPtr DeviceHandle, RawInputDeviceType DeviceType)
        {
            _hDevice = DeviceHandle;
            _dwType = DeviceType;
            _DeviceName = String.Empty;
            _ManufacturerName = String.Empty;
            _ProductName = String.Empty;
            _pPreparsedData = IntPtr.Zero;
            _Hid_NumberofButtons = 0;
            _Hid_Axis_List = new List<ushort>();

            GetDeviceName();
            GetDeviceInfo();

            //DeviceName is also the path to acces the device with OpenFile
            GetManufacturerAndProductString(_DeviceName);

            if (_dwType == RawInputDeviceType.RIM_TYPEHID)
            {
                _pPreparsedData = GetPreparsedData();
                if (_pPreparsedData != IntPtr.Zero)
                {
                    if (!GetCapabilities(_pPreparsedData, out _Caps))
                    {
                        Logger.WriteLog("Error: Impossible to get Capabilities for device " + _DeviceName);
                        return;
                    }

                    if (!GetButtonCapabilities(_pPreparsedData, _Caps, out _pButtonCaps))
                    {
                        Logger.WriteLog("Error: Impossible to get Button Capabilities for device " + _DeviceName);
                        return;
                    }
                    _Hid_NumberofButtons = GetNumberOfHidDeviceButtons(_pPreparsedData, _pButtonCaps);
                    _Hid_Buttons = new bool[_Hid_NumberofButtons]; 

                    if (!GetValueCapabilities(_pPreparsedData, _Caps, out _pValueCaps))
                    {
                        Logger.WriteLog("Error: Impossible to get Value Capabilities for device " + _DeviceName);
                        return;
                    }

                    if (!GetOutputValueCapabilities(_pPreparsedData, _Caps, out _pOutputValueCaps))
                    {
                        Logger.WriteLog("Error: Impossible to get Output Value Capabilities for device " + _DeviceName);
                        return;
                    }

                    //Number of absolute axis
                    for (int i = 0; i < _pValueCaps.Length; i++)
                    {
                        if (_pValueCaps[i].IsAbsolute)
                        {
                            _Hid_NumberofAxis++;
                            _Hid_Axis_List.Add(_pValueCaps[i].Range.UsageMin);
                        }
                    }                                  
                }
            }
            else if (_dwType == RawInputDeviceType.RIM_TYPEMOUSE)
            {
                //A mouse only have 1 set of axis : X,Y
                _Hid_Buttons = new bool[_DeviceInfo.mouse.dwNumberOfButtons];
            }
        }

        /// <summary>
        /// Getting WM_INPUT data => RawInputHeader to compare the hDevice handle and check if this specific device is the good one
        /// </summary>
        /// <param name="LParam">LParam parameter from WindowLoop during WM_INPUT message</param>
        /// <returns></returns>
        public Boolean isSourceOfRawInputMessage(IntPtr LParam)
        {
            if (GetRawInputHeader(LParam))
            {
                if (_RawInputHeader.hDevice == _hDevice)
                    return true;
                else
                    return false;
            }
            else
            {
                //Error
                return false;
            }
        }

        /// <summary>
        /// Getting RawInputHEader, RawInputData and getting Axis and Buttons information
        /// If the controller is type of Mouse, Axis values and Buttons are directly available in the struct
        /// If the controller is HID, we need to work a little bit more with hid.dll to determine axis and buttons values
        /// </summary>
        /// <param name="LParam">LParam parameter from WindowLoop during WM_INPUT message</param>
        public void ProcessRawInputData(IntPtr LParam)
        {
            if (!GetRawInputHeader(LParam))
            {
                Logger.WriteLog("ProcessRawInputData error: Impossible to get RawInputHeader for device " + _DeviceName);
                return;
            }
            if (!GetRawInputData(LParam))
            {
                Logger.WriteLog("ProcessRawInputData error: Impossible to get RawInputData for device " + _DeviceName);               
                return;
            }
            //If this controller is HID device, we need to use Hid.dll calls
            //to determine buttons and axis messages in the variable RawData array
            if (_dwType == RawInputDeviceType.RIM_TYPEHID)
            {
                try
                {
                    //Updating buttons
                    ushort[] usage = new ushort[128];
                    uint usageLength = (uint)_Hid_NumberofButtons;

                    if (Win32API.HidP_GetUsages(HidPReportType.Input, _pButtonCaps[0].UsagePage, 0, usage, ref usageLength, _pPreparsedData, _RawHidData.bRawData, (uint)_RawHidData.dwSizeHid) != NtStatus.Success)
                    {
                        Logger.WriteLog("ProcessRawInputData error: Impossible to get Usages for device " + _DeviceName); 
                    }

                    bool[] bButtonStates = new bool[128];
                    string strButtons = string.Empty;
                    for (int i = 0; i < usageLength; i++)
                    {
                        bButtonStates[usage[i] - _pButtonCaps[0].Range.UsageMin] = true;
                    }

                    _ControllerData.Buttons = 0;
                    for (int i = 0; i < _Hid_Buttons.Length; i++)
                    {
                        if (i == _Button_OnScreenTrigger_Index)
                        {
                            if (!_Hid_Buttons[i] && bButtonStates[i])                            
                                _ControllerData.Buttons |= RawInputcontrollerButtonEvent.OnScreenTriggerDown;
                            
                            else if (_Hid_Buttons[i] && !bButtonStates[i])
                                _ControllerData.Buttons |= RawInputcontrollerButtonEvent.OnScreenTriggerUp;                            
                        }

                        if (i == _Button_Action_Index)
                        {
                            if (!_Hid_Buttons[i] && bButtonStates[i])
                                _ControllerData.Buttons |= RawInputcontrollerButtonEvent.ActionDown;

                            else if (_Hid_Buttons[i] && !bButtonStates[i])
                                _ControllerData.Buttons |= RawInputcontrollerButtonEvent.ActionUp;
                        }

                        if (i == _Button_OffScreenTrigger_Index)
                        {
                            if (!_Hid_Buttons[i] && bButtonStates[i])
                                _ControllerData.Buttons |= RawInputcontrollerButtonEvent.OffScreenTriggerDown;

                            else if (_Hid_Buttons[i] && !bButtonStates[i])
                                _ControllerData.Buttons |= RawInputcontrollerButtonEvent.OffScreenTriggerUp;
                        }                        
                        _Hid_Buttons[i] = bButtonStates[i];
                    }

                    //Updating Values
                    for (int i = 0; i < _pValueCaps.Length; i++)
                    {
                        Int32 value;

                        if (!_pValueCaps[i].IsRange)
                        {
                            if (Win32API.HidP_GetUsageValue(HidPReportType.Input, _pValueCaps[i].UsagePage, 0, _pValueCaps[i].NotRange.Usage, out value, _pPreparsedData, _RawHidData.bRawData, (uint)_RawHidData.dwSizeHid) != NtStatus.Success)
                            {
                                Logger.WriteLog("ProcessRawInputData error: Impossible to get UsageValue for device " + _DeviceName); 
                                return;
                            }

                            if (_pValueCaps[i].NotRange.Usage == _Selected_HidAxisX)
                            {
                                _Hid_Axis_X_Min = _pValueCaps[i].LogicalMin;
                                _Hid_Axis_X_Max = Correct_Axis_Max(_pValueCaps[i].LogicalMax);
                                _ControllerData.Axis.X = CorrectNegative_Value(value, _Hid_Axis_X_Min);
                            }
                            if (_pValueCaps[i].NotRange.Usage == _Selected_HidAxisY)
                            {
                                _Hid_Axis_Y_Min = _pValueCaps[i].LogicalMin;
                                _Hid_Axis_Y_Max = Correct_Axis_Max(_pValueCaps[i].LogicalMax);
                                _ControllerData.Axis.Y = CorrectNegative_Value(value, _Hid_Axis_Y_Min);
                            }
                        }
                        else
                        {
                            if (Win32API.HidP_GetUsageValue(HidPReportType.Input, _pValueCaps[i].UsagePage, 0, _pValueCaps[i].Range.UsageMin, out value, _pPreparsedData, _RawHidData.bRawData, (uint)_RawHidData.dwSizeHid) != NtStatus.Success)
                            {
                                Logger.WriteLog("ProcessRawInputData error: Impossible to get UsageValue for device " + _DeviceName); 
                                return;
                            }

                            if (_pValueCaps[i].Range.UsageMin == _Selected_HidAxisX)
                            {
                                _Hid_Axis_X_Min = _pValueCaps[i].LogicalMin;
                                _Hid_Axis_X_Max = Correct_Axis_Max(_pValueCaps[i].LogicalMax);
                                _ControllerData.Axis.X = CorrectNegative_Value(value, _Hid_Axis_X_Min);
                            }
                            if (_pValueCaps[i].Range.UsageMax == _Selected_HidAxisY)
                            {
                                _Hid_Axis_Y_Min = _pValueCaps[i].LogicalMin;
                                _Hid_Axis_Y_Max = Correct_Axis_Max(_pValueCaps[i].LogicalMax);
                                _ControllerData.Axis.Y = CorrectNegative_Value(value, _Hid_Axis_Y_Min);
                            }
                        }                                              
                    }
                }
                catch (Exception Ex)
                {
                    Logger.WriteLog("ProcessRawInputData error: " + Ex.Message.ToString());
                }
            }
            //If this controller is a mouse device, we can directly access
            //axis and buttons info from the RawInputData struct
            else if (_dwType == RawInputDeviceType.RIM_TYPEMOUSE)
            {
                _ControllerData.Axis.X = _RawInputDataMouse.data.lLastX;
                _ControllerData.Axis.Y = _RawInputDataMouse.data.lLastY;
                _ControllerData.Buttons = 0;
                //This is what was used before with AIMTRAK and other lighguns so we assume we can keep it !
                _Hid_Axis_X_Max = 0x0000FFFF;
                _Hid_Axis_X_Min = 0;
                _Hid_Axis_Y_Max = 0x0000FFFF;
                _Hid_Axis_Y_Min = 0;

                //For mouse device, buttons event have fixed index:
                // LeftCLick = OnScreenTrigger
                // MiddleClick = Action
                // RightClick = OffScreenTrigger
                // We're just changing the state of _HidButtons[i] for diplay purposes in the GUI
                if (_RawInputDataMouse.data.usButtonFlags == RawMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    _ControllerData.Buttons |= RawInputcontrollerButtonEvent.OnScreenTriggerDown;
                    _Hid_Buttons[0] = true;
                }
                else if (_RawInputDataMouse.data.usButtonFlags == RawMouseButtonFlags.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    _ControllerData.Buttons |= RawInputcontrollerButtonEvent.ActionDown;
                    _Hid_Buttons[1] = true;
                }
                else if (_RawInputDataMouse.data.usButtonFlags == RawMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    _ControllerData.Buttons |= RawInputcontrollerButtonEvent.OffScreenTriggerDown;
                    _Hid_Buttons[2] = true;
                }
                else if (_RawInputDataMouse.data.usButtonFlags == RawMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    _ControllerData.Buttons |= RawInputcontrollerButtonEvent.OnScreenTriggerUp;
                    _Hid_Buttons[0] = false;
                }
                else if (_RawInputDataMouse.data.usButtonFlags == RawMouseButtonFlags.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    _ControllerData.Buttons |= RawInputcontrollerButtonEvent.ActionUp;
                    _Hid_Buttons[1] = false;
                }
                else if (_RawInputDataMouse.data.usButtonFlags == RawMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    _ControllerData.Buttons |= RawInputcontrollerButtonEvent.OffScreenTriggerUp;
                    _Hid_Buttons[2] = false;
                }
            }
        }

        /// <summary>
        /// Special case for Sony Dual SHock 3 or WiiMotes with XInput driver :
        /// The driver is returning 0xFFFFFFFF as LogicalMax (which is -1 for a signed long)
        /// whereas real data are between 0x00000000 and 0x0000FFFF            
        /// </summary>
        /// <param name="Max_Value"></param>
        private int Correct_Axis_Max(int Max_Value)
        {
            if (Max_Value == -1) //-1 = 0xFFFFFFFF
                return 0x0000FFFF;
            else
                return Max_Value;
        }

        /// <summary>
        /// Trying to resolve an issue for APAC or device with negative Xmin [-2048;2048]
        /// GetUsageValue here is returning a 16bits signed values (i.e 0x0000F800 for -2048) instead of a full 32bits (i.e 0xFFFFF800)
        /// As a consequence, the Xmin being 0xFFFFF800, the value is not good for later calculation.
        /// Not sure if it's my code which is faulty and I do not have a device like this to test myself.
        /// The following workaround will simply add missing 0xFFFF0000 bytes when it's needed            
        /// </summary>
        /// <param name="Value"></param>
        private int CorrectNegative_Value(int Value, int MinValue)
        {
            if (MinValue < 0)
            {
                if ((Int16)Value < 0)
                    return (int)((uint)Value | 0xFFFF0000);
            }
            
            return Value;
        }

        /// <summary>
        /// Retrieve the DeviceName from a raw input device
        /// This name is also the filename used to open a File Handle with this device
        /// </summary>
        private void GetDeviceName() 
        {
            uint pcbSize = 0;
            Win32API.GetRawInputDeviceInfo(_hDevice, RawInputUiCommand.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
            if (pcbSize <= 0) return;

            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            Win32API.GetRawInputDeviceInfo(_hDevice, RawInputUiCommand.RIDI_DEVICENAME, pData, ref pcbSize);
            _DeviceName = Marshal.PtrToStringAnsi(pData);
            Marshal.FreeHGlobal(pData);
        }

        /// <summary>
        /// Retrive RawInputDeviceInformation structure for the device
        /// </summary>
        private void GetDeviceInfo()
        {           
            uint pcbSize = 0;
            Win32API.GetRawInputDeviceInfo(_hDevice, RawInputUiCommand.RIDI_DEVICEINFO, IntPtr.Zero, ref pcbSize);
            if (pcbSize <= 0) return;

            IntPtr pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(RawInputDeviceInfo)));
            Win32API.GetRawInputDeviceInfo(_hDevice, RawInputUiCommand.RIDI_DEVICEINFO, pData, ref pcbSize);
            _DeviceInfo = (RawInputDeviceInfo)Marshal.PtrToStructure(pData, typeof(RawInputDeviceInfo));
            Marshal.FreeHGlobal(pData);
        }

        /// <summary>
        /// Read and store Manufacturer string and Product string from the device
        /// These values are read by opening a File Handle with the device
        /// </summary>
        private void GetManufacturerAndProductString(String DevicePath)
        {
            IntPtr DeviceFileHandle = Win32API.CreateFile(DevicePath,
                                                        Win32API.DesiredAccess.None,
                                                        Win32API.ShareMode.Read | Win32API.ShareMode.Write,
                                                        IntPtr.Zero,
                                                        Win32API.CreateDisposition.OpenExisting,
                                                        0,
                                                        IntPtr.Zero);
            if (DeviceFileHandle != new IntPtr(-1))
            {
                byte[] buf = new byte[256];
                if (!Win32API.HidD_GetManufacturerString(DeviceFileHandle, buf, (uint)buf.Length))
                {
                    //Error
                    _ManufacturerName = "[Unknown Manufacturer]";
                }
                else
                {
                    _ManufacturerName = Encoding.Unicode.GetString(buf, 0, buf.Length);
                    if (_ManufacturerName.Contains("\0"))                    
                        _ManufacturerName = _ManufacturerName.Substring(0, _ManufacturerName.IndexOf("\0"));
                }

                if (!Win32API.HidD_GetProductString(DeviceFileHandle, buf, (uint)buf.Length))
                {
                    //Error
                    _ProductName = "[Unknown Product]";
                }
                else
                {
                    _ProductName = Encoding.Unicode.GetString(buf, 0, buf.Length);
                    if (_ProductName.Contains("\0"))
                    {
                        _ProductName = _ProductName.Substring(0, _ProductName.IndexOf("\0"));
                    }                        
                }

                Win32API.CloseHandle(DeviceFileHandle);
            }
        }

        /// <summary>
        /// Retrieve a top-level collection's HIDP_CAPS structure for this device.
        /// </summary>
        /// <param name="pPreparsedData">Pointer to a top-level collection's preparsed data.</param>
        /// <param name="Caps">Pointer to a caller-allocated buffer that the routine uses to return a collection's HIDP_CAPS structure</param>
        /// <returns>TRUE is success, otherwise FALSE</returns>
        private bool GetCapabilities(IntPtr pPreparsedData, out HidPCaps Caps)
        {
            if (Win32API.HidP_GetCaps(pPreparsedData, out Caps) != NtStatus.Success)
            {  
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieve a button capability array that describes all the HID control buttons in a top-level collection for a specified type of HID report.
        /// </summary>
        /// <param name="pPreparsedData">Pointer to a top-level collection's preparsed data.</param>
        /// <param name="Caps">Pointer to a caller-allocated buffer that the routine uses to return a collection's HIDP_CAPS structure</param>
        /// <param name="pButtonCaps">Pointer to a caller-allocated buffer that the routine uses to return a button capability array for the specified report type.</param>
        /// <returns>TRUE is success, otherwise FALSE</returns>
        private bool GetButtonCapabilities(IntPtr pPreparsedData, HidPCaps Caps, out HidPButtonCaps[] pButtonCaps)
        {
            ushort capsLength = Caps.NumberInputButtonCaps;
            pButtonCaps = new HidPButtonCaps[capsLength];
            if (Win32API.HidP_GetButtonCaps(HidPReportType.Input, pButtonCaps, ref capsLength, pPreparsedData) != NtStatus.Success)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieve a value capability array that describes all the HID control buttons in a top-level collection for a specified type of HID report.
        /// </summary>
        /// <param name="pPreparsedData">Pointer to a top-level collection's preparsed data.</param>
        /// <param name="Caps">Pointer to a caller-allocated buffer that the routine uses to return a collection's HIDP_CAPS structure</param>
        /// <param name="pButtonCaps">Pointer to a caller-allocated buffer that the routine uses to return a value capability array for the specified report type.</param>
        /// <returns>TRUE is success, otherwise FALSE</returns>
        private bool GetValueCapabilities(IntPtr pPreparsedData, HidPCaps Caps, out HidPValueCaps[] pValueCaps)
        {
            ushort capsLength = Caps.NumberInputValueCaps;
            pValueCaps = new HidPValueCaps[capsLength];
            if (Win32API.HidP_GetValueCaps(HidPReportType.Input, pValueCaps, ref capsLength, pPreparsedData) != NtStatus.Success)
            {
                return false;
            }
            return true;
        }
        private bool GetOutputValueCapabilities(IntPtr pPreparsedData, HidPCaps Caps, out HidPValueCaps[] pValueCaps)
        {
            ushort capsLength = Caps.NumberOutputValueCaps;
            pValueCaps = new HidPValueCaps[capsLength];
            if (Win32API.HidP_GetValueCaps(HidPReportType.Output, pValueCaps, ref capsLength, pPreparsedData) != NtStatus.Success)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieve the pointer to the previously parsed data for a HID device
        /// </summary>
        /// <returns>A pointer to the pPreparsedData struct is succes, otherwise IntPtr.Zero</returns>
        private IntPtr GetPreparsedData()
        {
            uint result = 0;
            uint bufferSize = 0;
            result = Win32API.GetRawInputDeviceInfo(_hDevice, RawInputUiCommand.RID_PREPARSEDDATA, IntPtr.Zero, ref bufferSize);
            if (result != 0)
            {
                return IntPtr.Zero;
            }
            
            IntPtr pPreparsedData = Marshal.AllocHGlobal((int)bufferSize);
            result = Win32API.GetRawInputDeviceInfo(_hDevice, RawInputUiCommand.RID_PREPARSEDDATA, pPreparsedData, ref bufferSize);
            if (result == 0)
            {
                Marshal.FreeHGlobal(pPreparsedData);
                return IntPtr.Zero;
            }

            return pPreparsedData;
        }

        /// <summary>
        /// Count the number of available buttons for this specific HID device
        /// Only ised when this Controller is of type HID (not MOUSE or KEYBOARD)
        /// </summary>
        /// <param name="pPreparsedData">Pointer to a previously parsed data structure</param>        
        /// <returns>Number of available input buttons for this HID device</returns>
        private int GetNumberOfHidDeviceButtons(IntPtr pPreparsedData, HidPButtonCaps[] pButtonCaps)
        {
            if (_pPreparsedData != IntPtr.Zero)
            {                
                int nButtons = pButtonCaps[0].Range.UsageMax - pButtonCaps[0].Range.UsageMin + 1;
                return nButtons;
            }
            return 0;
        }

        /// <summary>
        /// Retrieve the RawInputHeader struct from a WM_INPUT LParam
        /// </summary>
        /// <param name="LParam">LParam parameter from WindowLoop during WM_INPUT message</param>
        /// <returns>TRUE if success, otherwise, FALSE</returns>
        private bool GetRawInputHeader(IntPtr LParam)
        {            
            uint HeaderSize = (uint)Marshal.SizeOf(typeof(RawInputHeader));
            if (Win32API.GetRawInputData(LParam, RawInputUiCommand.RID_HEADER, out _RawInputHeader, ref HeaderSize, HeaderSize) != HeaderSize)
            {
                //Error
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieving the raw input data struct when a WM_INPUT message has arrived
        /// For Mouse and keyboard we can directly fill the RawInput struct
        /// For HID we need to get RawHeader and RawHid separatly because RawHid is not constant size
        /// </summary>
        /// <param name="LParam">LParam parameter from WindowLoop during WM_INPUT message</param>
        /// <returns></returns>
        private bool GetRawInputData(IntPtr LParam)
        {
            uint HeaderSize = (uint)Marshal.SizeOf(typeof(RawInputHeader));
            uint dwSize = 0;
            uint size = (uint)_RawInputHeader.dwSize;
            Win32API.GetRawInputData(LParam, RawInputUiCommand.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RawInputHeader)));
            uint result;
            
            if (_dwType == RawInputDeviceType.RIM_TYPEHID)
            {
                IntPtr p = Marshal.AllocHGlobal((int)size);
                result = Win32API.GetRawInputData(LParam, RawInputUiCommand.RID_INPUT, p, ref size, HeaderSize);
                if (result != 0xFFFF && result == size)
                {
                    _RawHidData = RawHid.FromIntPtr((IntPtr)((int)p + HeaderSize));
                    Marshal.FreeHGlobal(p);
                    return true;
                }
            }
            else if (_dwType == RawInputDeviceType.RIM_TYPEMOUSE)
            {
                result = Win32API.GetRawInputData(LParam, RawInputUiCommand.RID_INPUT, out _RawInputDataMouse, ref size, (uint)Marshal.SizeOf(typeof(RawInputHeader)));
                if (result != 0xFFFF && result == size)
                {
                    return true;
                }
                else
                    return false;
            }
            else if (_dwType == RawInputDeviceType.RIM_TYPEKEYBOARD)
            {
                //TODO
                return false;
            }
        
            return false;
        }

        
        
        
        
        
        
        
        public void Set_SONY_DS4_Output(byte RightMotor, byte LeftMotor, byte RedLed, byte GreenLed, byte BlueLed)
        {
            IntPtr DeviceFileHandle = Win32API.CreateFile(_DeviceName,
                                                        Win32API.DesiredAccess.Read | Win32API.DesiredAccess.Write,
                                                        Win32API.ShareMode.Read | Win32API.ShareMode.Write,
                                                        IntPtr.Zero,
                                                        Win32API.CreateDisposition.OpenExisting,
                                                        0,
                                                        IntPtr.Zero);
            if (DeviceFileHandle != new IntPtr(-1))
            {
                byte[] buf = new byte[32];
                buf[0] = 0x05;
                buf[1] = 0xFF;
                buf[4] = RightMotor;  // 0-255
                buf[5] = LeftMotor;   // 0-255
                buf[6] = RedLed;         // 0-255
                buf[7] = GreenLed;       // 0-255
                buf[8] = BlueLed;        // 0-255
                uint bytes_written;
                bool res = Win32API.WriteFile(DeviceFileHandle, buf, (uint)buf.Length, out bytes_written, IntPtr.Zero);


                Win32API.CloseHandle(DeviceFileHandle);
            }
        }
        public void Set_SONY_PS3_Output(byte RightMotor, byte LeftMotor, byte RedLed, byte GreenLed, byte BlueLed)
        {
            IntPtr DeviceFileHandle = Win32API.CreateFile(_DeviceName,
                                                        Win32API.DesiredAccess.Read | Win32API.DesiredAccess.Write,
                                                        Win32API.ShareMode.Read | Win32API.ShareMode.Write,
                                                        IntPtr.Zero,
                                                        Win32API.CreateDisposition.OpenExisting,
                                                        0,
                                                        IntPtr.Zero);
            if (DeviceFileHandle != new IntPtr(-1))
            {
                byte[] command = new byte[37];
                command = new byte[] {0x52,
                   0x01,
                   0x00, 0xfe, RightMotor, 0xfe, LeftMotor,        
                   0x00, 0x00, 0x00, 0x00, RedLed,
                   0xff, 0x27, 0x10, 0x00, 0x32,      
                   0xff, 0x27, 0x10, 0x00, 0x32,     
                   0xff, 0x27, 0x10, 0x00, 0x32,       
                   0xff, 0x27, 0x10, 0x00, 0x32,      
                   0x00, 0x00, 0x00, 0x00, 0x00
                };

                uint bytes_written;
                bool res = Win32API.WriteFile(DeviceFileHandle, command, (uint)command.Length, out bytes_written, IntPtr.Zero);


                Win32API.CloseHandle(DeviceFileHandle);
            }
        }
    }

    [Flags]
    public enum RawInputcontrollerButtonEvent : int
    {
        OnScreenTriggerDown = 0x00000001,
        OnScreenTriggerUp = 0x00000002,
        OffScreenTriggerDown = 0x00000010,
        OffScreenTriggerUp = 0x00000020,
        ActionDown = 0x000000100,
        ActionUp = 0x00000200,
    }
}
