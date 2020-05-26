using System;
using System.Runtime.InteropServices;
using DsCore.Win32;
using System.Collections.Generic;

namespace DsCore.RawInput
{
    public static class RawInputHelper
    {
        /// <summary>
        /// Enumerates all available RawInput Devices and returned them as an Array
        /// </summary>
        /// <returns>Array of RawInputController</returns>
        public static RawInputController[] GetRawInputDevices()
        {
            return GetRawInputDevices(new RawInputDeviceType[] { RawInputDeviceType.RIM_TYPEHID, RawInputDeviceType.RIM_TYPEKEYBOARD, RawInputDeviceType.RIM_TYPEMOUSE });
        }

        /// <summary>
        /// Enumerates available RawInput Devices and returned selected Types as an Array
        /// </summary>
        /// <param name="AccecptedDeviceType">Filter to select devices to keep based on their Type. Others won't be returned in the Array</param>
        /// <returns>Array of RawInputController from desired Type(s)</returns>
        public static RawInputController[] GetRawInputDevices(RawInputDeviceType[] AccecptedDeviceTypes)
        {
            uint deviceCount = 0;
            var dwSize = (Marshal.SizeOf(typeof(RawInputDeviceList)));
            List<RawInputController> Result = new List<RawInputController>();

            if (Win32API.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                Win32API.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                for (int i = 0; i < deviceCount; i++)
                {
                    // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
                    RawInputDeviceList rid = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr((pRawInputDeviceList.ToInt64() + (dwSize * i))), typeof(RawInputDeviceList));

                    RawInputController controller = new RawInputController(rid.hDevice, rid.dwType);

                    foreach (RawInputDeviceType Type in AccecptedDeviceTypes)
                    {
                        if (controller.DeviceType == Type)
                        {
                            Result.Add(controller);
                            break;
                        }
                    }
                }
                Marshal.FreeHGlobal(pRawInputDeviceList);
            }
            return Result.ToArray();
        }
    }
}
