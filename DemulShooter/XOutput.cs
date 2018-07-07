using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DemulShooter
{
    class XOutput
    {
        #region Struct

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        public enum MessageType
        {
            Plugin = 0x2A4000,
            Report = 0x2A400C,
            Unplug = 0x2A4004,
            IsDevPlug = 0x2AE404,
            EmptySlots = 0x2AE408
        }

        #endregion

        #region Constant

        private const int INVALID_HANDLE_VALUE = -1;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        private const uint FILE_SHARE_READ = 1;
        private const uint FILE_SHARE_WRITE = 2;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint OPEN_EXISTING = 3;
        private const int DIGCF_PRESENT = 0x0002;
        private const int DIGCF_DEVICEINTERFACE = 0x0010;

        //Gamepad
        private const int FEEDBACK_BUFFER_LENGTH = 9;
        public const int MAX_NUMBER_XBOX_CTRLS = 4;
        public const short AXIS_MAX = 32767;
        public const short AXIS_MIN = -32768;


        #endregion

        #region Win32 PInvoke

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, UIntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(IntPtr hDevice, int dwIoControlCode, byte[] lpInBuffer, uint nInBufferSize, byte[] lpOutBuffer, uint nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern int SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, int flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, ref SP_DEVICE_INTERFACE_DATA deviceInfoData);

        #endregion

        private class Gamepad_state
        {
            private byte[] _StateBuffer;
            public byte[] Buffer
            {
                get { return _StateBuffer; }
                set { _StateBuffer = Buffer; }
            }

            public Gamepad_state()
            {
                _StateBuffer = new byte[18];
            }
        }

        private Gamepad_state[] _Gamepads;
        private const string SCP_BUS_CLASS_GUID = "{F679F562-3164-42CE-A4DB-E7DDBE723909}";
        private IntPtr _hBus;
        private int _Win32Error;

        public int Win32ErrorNo
        {
            get { return _Win32Error; }
        }

        public XOutput()
        {
            _hBus = new IntPtr(INVALID_HANDLE_VALUE);
            _Gamepads = new Gamepad_state[4];
        }

        /// <summary>
        /// Look for an installed ScpVBus on the system
        /// </summary>
        /// <returns>
        /// True if Bus is fount
        /// False if not</returns>
        public bool isVBusExists()
        {
            string Path = "";
            int n = GetVXbusPath(ref Path);

            if (n > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Plug a virtual Controller on one of the user port.
        /// Port are from 1 to 4, and are not the same as Player ID of 
        /// the gamepad (LED Number)
        /// </summary>
        /// <param name="UserIndex">Virtual port on which the GamePad should be plugged</param>
        /// <returns>
        /// True if succes.
        /// False if failed or port already used</returns>
        public bool PlugIn(int UserIndex)
        {
            bool OUT = false;

            if (UserIndex < 1 || UserIndex > 4)
                return OUT;

            if (_hBus == new IntPtr(INVALID_HANDLE_VALUE))
                _hBus = GetVXbusHandle();
            if (_hBus == new IntPtr(INVALID_HANDLE_VALUE))
                return false;

            int Transfered = 0;
            byte[] buffer = new byte[16];
            buffer[0] = 0x10;
            buffer[4] = (byte)((UserIndex >> 0) & 0xFF);
            buffer[5] = (byte)((UserIndex >> 8) & 0xFF);
            buffer[6] = (byte)((UserIndex >> 16) & 0xFF);
            buffer[8] = (byte)((UserIndex >> 24) & 0xFF);

            if (DeviceIoControl(_hBus, (int)MessageType.Plugin, buffer, 16, null, 0, ref Transfered, IntPtr.Zero))
            {
                _Gamepads[UserIndex - 1] = new Gamepad_state();
                OUT = true;
            }

            _Win32Error = 0;
            if (!OUT)
                _Win32Error = Marshal.GetLastWin32Error();

            return OUT;
        }

        /// <summary>
        /// Unplug a virtual Gamepad from a virtual Port
        /// </summary>
        /// <param name="UserIndex">Virtual Port targetted for unplug</param>
        /// <param name="Force">If true, can unplug non-owned gamepad</param>
        /// <returns>
        /// True if succes.
        /// Else if failed.
        /// </returns>
        public bool Unplug(int UserIndex, bool Force = false)
        {
            bool b = UnplugOpt(UserIndex, Force);
            if (b)
                _Gamepads[UserIndex - 1] = null;
            return b;
        }

        public bool UnplugAll(bool Force = false)
        {
            bool b = UnplugOpt(0, true);
            if (b)
            {
                for (int i = 0; i < MAX_NUMBER_XBOX_CTRLS; i++)
                    _Gamepads[i] = null;
            }
            return b;
        }

        public bool GetLedNumber(int UserIndex, byte[] Led)
        {
            bool rep = XOutputSetGetState(UserIndex, _Gamepads[UserIndex - 1], null, null, null, Led);
            if (rep)
                Led[0]++;
            return rep;
        }

        public bool SetDPad_Up(int UserIndex)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[0] |= 1 << 0;
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetDPad_Down(int UserIndex)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[0] |= 1 << 1;
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetDPad_Left(int UserIndex)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[0] |= 1 << 2;
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetDPad_Right(int UserIndex)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[0] |= 1 << 3;
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetDPad_Off(int UserIndex)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[0] &= 0 << 0;
                _Gamepads[UserIndex - 1].Buffer[0] &= 0 << 1;
                _Gamepads[UserIndex - 1].Buffer[0] &= 0 << 2;
                _Gamepads[UserIndex - 1].Buffer[0] &= 0 << 3;
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_Start(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[0] |= 1 << 4;
                else
                    _Gamepads[UserIndex - 1].Buffer[0] &= 0 << 4;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_Back(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[0] |= 1 << 5;
                else
                    _Gamepads[UserIndex - 1].Buffer[0] &= 0 << 5;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_L3(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[0] |= 1 << 6;
                else
                    _Gamepads[UserIndex - 1].Buffer[0] &= 0 << 6;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_R3(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[0] |= 1 << 7;
                else
                    _Gamepads[UserIndex - 1].Buffer[0] &= 0 << 7;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_L1(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[1] |= 1 << 0;
                else
                    _Gamepads[UserIndex - 1].Buffer[1] &= 0 << 0;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_R1(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[1] |= 1 << 1;
                else
                    _Gamepads[UserIndex - 1].Buffer[1] &= 0 << 1;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_A(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[1] |= 1 << 4;
                else
                    _Gamepads[UserIndex - 1].Buffer[1] &= 0 << 4;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_B(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[1] |= 1 << 5;
                else
                    _Gamepads[UserIndex - 1].Buffer[1] &= 0 << 5;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_X(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[1] |= 1 << 6;
                else
                    _Gamepads[UserIndex - 1].Buffer[1] &= 0 << 6;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_Y(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[1] |= 1 << 7;
                else
                    _Gamepads[UserIndex - 1].Buffer[1] &= 0 << 7;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_Guide(int UserIndex, bool Pressed)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                if (Pressed)
                    _Gamepads[UserIndex - 1].Buffer[1] |= 1 << 2;
                else
                    _Gamepads[UserIndex - 1].Buffer[1] &= 0 << 2;

                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetLAxis_X(int UserIndex, short Value)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[4] = (byte)(Value & 0xFF);
                _Gamepads[UserIndex - 1].Buffer[5] = (byte)((Value >> 8) & 0xFF);
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetLAxis_Y(int UserIndex, short Value)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[6] = (byte)(Value & 0xFF);
                _Gamepads[UserIndex - 1].Buffer[7] = (byte)((Value >> 8) & 0xFF);
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetRAxis_X(int UserIndex, short Value)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[8] = (byte)(Value & 0xFF);
                _Gamepads[UserIndex - 1].Buffer[9] = (byte)((Value >> 8) & 0xFF);
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetRAxis_Y(int UserIndex, short Value)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[10] = (byte)(Value & 0xFF);
                _Gamepads[UserIndex - 1].Buffer[11] = (byte)((Value >> 8) & 0xFF);
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_L2(int UserIndex, byte Value)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[2] = Value;
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        public bool SetButton_R2(int UserIndex, byte Value)
        {
            if (_Gamepads[UserIndex - 1] != null)
            {
                _Gamepads[UserIndex - 1].Buffer[3] = Value;
                return XOutputSetState(UserIndex, _Gamepads[UserIndex - 1]);
            }
            else
                return false;
        }

        private int GetVXbusPath(ref String Path)
        {
            IntPtr detailDataBuffer = IntPtr.Zero;
            IntPtr deviceInfoSet = IntPtr.Zero;

            SP_DEVICE_INTERFACE_DATA DeviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
            SP_DEVICE_INTERFACE_DATA Da = new SP_DEVICE_INTERFACE_DATA();
            DeviceInterfaceData.cbSize = Marshal.SizeOf(DeviceInterfaceData);
            Da.cbSize = Marshal.SizeOf(DeviceInterfaceData);
            Guid deviceClassGuid = new Guid(SCP_BUS_CLASS_GUID);
            int memberIndex = 0;
            int requiredSize = 0;

            deviceInfoSet = SetupDiGetClassDevs(ref deviceClassGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            if (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref deviceClassGuid, memberIndex, ref DeviceInterfaceData))
            {
                //get required target buffer size
                SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, IntPtr.Zero, 0, ref requiredSize, ref Da);

                //Allocate target buffer
                detailDataBuffer = Marshal.AllocHGlobal(requiredSize);
                if (detailDataBuffer == IntPtr.Zero)
                    return -1;

                Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                //Get detail Buffer
                if (!SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, detailDataBuffer, requiredSize, ref requiredSize, ref Da))
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                    Marshal.FreeHGlobal(detailDataBuffer);
                    return -1;
                }

                IntPtr pDevicePathName = new IntPtr(detailDataBuffer.ToInt64() + 4);
                Path = Marshal.PtrToStringAuto(pDevicePathName).ToUpper(CultureInfo.InvariantCulture);

                //CleanUp
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
                Marshal.FreeHGlobal(detailDataBuffer);
            }
            else
                return -1;

            return requiredSize;
        }

        private IntPtr GetVXbusHandle()
        {
            string Path = "";
            int n = GetVXbusPath(ref Path);

            if (n < 1)
                return new IntPtr(INVALID_HANDLE_VALUE);

            //bus found, open it and get handle
            _hBus = CreateFile(Path, (GENERIC_WRITE | GENERIC_READ), FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, UIntPtr.Zero);
            return _hBus;
        }

        private bool UnplugOpt(int UserIndex, bool Force)
        {
            bool OUT = false;

            if (UserIndex < 0 || UserIndex > 4)
                return OUT;

            if (_hBus == new IntPtr(INVALID_HANDLE_VALUE))
                _hBus = GetVXbusHandle();
            if (_hBus == new IntPtr(INVALID_HANDLE_VALUE))
                return OUT;

            int Transfered = 0;

            //BUSENUM_UNPLUG_HARDWARE struct equivalent
            byte[] buf = new byte[16];
            //Size of struct
            buf[0] = 0x10;
            //SerialNo
            buf[4] = (byte)((UserIndex) & 0xFF);
            buf[5] = (byte)((UserIndex >> 8) & 0xFF);
            buf[6] = (byte)((UserIndex >> 16) & 0xFF);
            buf[7] = (byte)((UserIndex >> 24) & 0xFF);
            //Flag
            if (Force)
                buf[8] = 1;

            if (DeviceIoControl(_hBus, (int)MessageType.Unplug, buf, 16, null, 0, ref Transfered, IntPtr.Zero))
            {
                OUT = true;
            }

            _Win32Error = 0;
            if (!OUT)
                _Win32Error = Marshal.GetLastWin32Error();

            return OUT;
        }

        private bool XOutputSetState(int UserIndex, Gamepad_state Gamepad)
        {
            bool OUT = false;

            if (UserIndex < 1 || UserIndex > 4)
                return OUT;

            if (_hBus == new IntPtr(INVALID_HANDLE_VALUE))
                _hBus = GetVXbusHandle();
            if (_hBus == new IntPtr(INVALID_HANDLE_VALUE))
                return false;

            int Transfered = 0;
            byte[] buffer = new byte[28];

            buffer[0] = 0x1C;

            // encode user index
            buffer[4] = (byte)((UserIndex >> 0) & 0xFF);
            buffer[5] = (byte)((UserIndex >> 8) & 0xFF);
            buffer[6] = (byte)((UserIndex >> 16) & 0xFF);
            buffer[7] = (byte)((UserIndex >> 24) & 0xFF);

            buffer[9] = 0x14;
            Array.Copy(Gamepad.Buffer, 0, buffer, 10, Gamepad.Buffer.Length);

            // vibration and LED info end up here
            byte[] output = new byte[FEEDBACK_BUFFER_LENGTH];

            // send report to bus, receive vibration and LED status
            if (!DeviceIoControl(_hBus, (int)MessageType.Report, buffer, (uint)buffer.Length, output, FEEDBACK_BUFFER_LENGTH, ref Transfered, IntPtr.Zero))
            {
                _Win32Error = 0;
                return false;
            }

            return true;
        }

        private bool XOutputSetGetState(int UserIndex, Gamepad_state Gamepad, byte[] bVibrate, byte[] bLargeMotor, byte[] bSmallMotor, byte[] bLed)
        {
            bool OUT = false;

            if (UserIndex < 1 || UserIndex > 4)
                return OUT;

            if (_Gamepads[UserIndex - 1] == null)
                return OUT;

            int Transfered = 0;
            byte[] Buffer = new byte[28];

            Buffer[0] = 0x1C;

            // encode user index
            Buffer[4] = (byte)((UserIndex >> 0) & 0xFF);
            Buffer[5] = (byte)((UserIndex >> 8) & 0xFF);
            Buffer[6] = (byte)((UserIndex >> 16) & 0xFF);
            Buffer[7] = (byte)((UserIndex >> 24) & 0xFF);

            Buffer[9] = 0x14;

            // concat gamepad info to buffer
            Array.Copy(Gamepad.Buffer, 0, Buffer, 10, Gamepad.Buffer.Length);

            // vibration and LED info end up here
            byte[] Output = new byte[FEEDBACK_BUFFER_LENGTH];

            // send report to bus, receive vibration and LED status
            if (!DeviceIoControl(_hBus, (int)MessageType.Report, Buffer, (uint)Buffer.Length, Output, FEEDBACK_BUFFER_LENGTH, ref Transfered, IntPtr.Zero))
            {
                return false;
            }

            // cache feedback
            /*if (bVibrate != null)
            {
                *bVibrate = (output[1] == 0x08) ? 0x01 : 0x00;
            }

            if (bLargeMotor != null)
            {
                *bLargeMotor = output[3];
            }

            if (bSmallMotor != null)
            {
                *bSmallMotor = output[4];
            }

            if (bLed != null)
            {
                *bLed = output[8];
            }*/

            return true;
        }
    }
}
