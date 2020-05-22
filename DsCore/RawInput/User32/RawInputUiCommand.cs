
namespace DsCore.RawInput
{
    /// <summary>
    /// Specifies what data will be returned in pData.
    /// </summary>
    public enum RawInputUiCommand : uint
    {
        /// <summary>
        /// 
        /// </summary>
        RID_INPUT = 0x10000003,
        /// <summary>
        /// 
        /// </summary>
        RID_HEADER = 0x10000005,
        /// <summary>
        /// pData points to a string that contains the device name.
        /// For this uiCommand only, the value in pcbSize is the character count (not the byte count).
        /// </summary>
        RIDI_DEVICENAME = 0x20000007,
        /// <summary>
        /// pData points to an <b><i>RawInputDeviceInfo</i></b> struct.
        /// </summary>
        RIDI_DEVICEINFO = 0x2000000b,
        /// <summary>
        /// pData points to the previously parsed data.
        /// </summary>
        RID_PREPARSEDDATA = 0x20000005
    } 
}
