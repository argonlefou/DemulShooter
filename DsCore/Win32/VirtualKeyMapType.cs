namespace DsCore.Win32
{
    /// <summary>
    /// Used for MapVirtualKey() or MapVirtualKeyEx() 
    /// The translation to be performed. The value of this parameter depends on the value of the uCode parameter.
    /// </summary>
    public enum VirtualKeyMapType
    {
        /// <summary>
        /// uCode is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does not distinguish between left- and right-hand keys, the left-hand scan code is returned. If there is no translation, the function returns 0. 
        /// </summary>
        MAPVK_VK_TO_VSC = 0x00,
        /// <summary>
        /// uCode is a scan code and is translated into a virtual-key code that does not distinguish between left- and right-hand keys. If there is no translation, the function returns 0. 
        /// </summary>
        MAPVK_VSC_TO_VK = 0x01,
        /// <summary>
        /// uCode is a virtual-key code and is translated into an unshifted character value in the low-order word of the return value. Dead keys (diacritics) are indicated by setting the top bit of the return value. If there is no translation, the function returns 0. 
        /// </summary>
        MAPVK_VK_TO_CHAR = 0x02,
        /// <summary>
        /// uCode is a scan code and is translated into a virtual-key code that distinguishes between left- and right-hand keys. If there is no translation, the function returns 0. 
        /// </summary>
        MAPVK_VSC_TO_VK_EX = 0x03,
        /// <summary>
        /// The uCode parameter is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does not distinguish between left- and right-hand keys, the left-hand scan code is returned. If the scan code is an extended scan code, the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan code. If there is no translation, the function returns 0. 
        /// </summary>
        MAPVK_VK_TO_VSC_EX = 0x04,
    }
}
