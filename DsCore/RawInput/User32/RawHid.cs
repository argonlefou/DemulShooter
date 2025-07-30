using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    /// <summary>
    /// Describes the format of the raw input from a Human Interface Device (HID).
    /// </summary>
    public struct RawHid
    {
        /// <summary>
        /// The size, in bytes, of each HID input in bRawData.
        /// </summary>
        public int dwSizeHid;
        /// <summary>
        /// The number of HID inputs in bRawData.
        /// </summary>
        public int dwCount;
        /// <summary>
        /// The raw input data, as an array of bytes.
        /// </summary>
        public byte[] bRawData;        


        /// <summary>
        /// The bRawData array is variable in lenght, and using GetDeviceInfo we can get a pointer to some RawInput structure
        /// containing the RawInputHeader + RawHID structures.
        /// This functions will parse bytes from the pointer to fill a struct with a fixed size byte array as bRawData
        /// </summary>
        /// <param name="Ptr">Pointer to RAWINPUT struct obtained with a call GetRawInputDeviceInfo</param>
        /// <returns>RawHid struct filled with data</returns>
        public static RawHid FromIntPtr(IntPtr Ptr)
        {
            RawHid result = new RawHid();
            //Bytes 0-3 = dwSizeHid
            //Bytes 4-7 = dwCount
            byte[] buffer = new byte[8];
            Marshal.Copy(Ptr, buffer, 0, 8);
            result.dwSizeHid = BitConverter.ToInt32(buffer, 0);
            result.dwCount = BitConverter.ToInt32(buffer, 4);
            //Creating a fixed size byte array
            result.bRawData = new byte[result.dwCount * result.dwSizeHid];            
            try
            {
                Marshal.Copy(IntPtr.Add(Ptr, 8), result.bRawData, 0, result.bRawData.Length);
                return result;
            }
            catch
            {
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("dwCount : {0}, dwSize : {1}, rawData: {2}", dwCount, dwSizeHid, BitConverter.ToString(bRawData).Replace("-", " "));
        }
    }
}
