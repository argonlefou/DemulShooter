using System;
using System.Runtime.InteropServices;

namespace DsCore.XInput
{
    /// <summary>
    /// This class is used to handle various XInput devices plugged on the computer.
    /// We can read axis and buttons values, and even set rumble force and speed.
    /// </summary>
    public class XInputHandler
    {
        //
        // Gamepad thresholds
        //
        public const int XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE = 7849;
        public const int XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE = 8689;
        public const int XINPUT_GAMEPAD_TRIGGER_THRESHOLD = 30;

        //
        //Various stuff
        //
        public const int MAX_CONTROLLER_COUNT = 4;
        public const int FIRST_CONTROLLER_INDEX = 0;
        public const int ERROR_DEVICE_NOT_CONNECTED = 0x48f;
        public const int ERROR_SUCCES = 0;


        /// <summary>
        /// Retrieves the capabilities and features of a connected controller.
        /// </summary>
        /// <param name="dwUserIndex">
        /// Index of the user's controller. Can be a value in the range 0–3. 
        /// </param>
        /// <param name="dwFlags">
        /// Input flags that identify the controller type. 
        /// If this value is 0, then the capabilities of all controllers connected to the system are returned. 
        /// Currently, only one value is supported: XINPUT_FLAG_GAMEPAD
        /// </param>
        /// <param name="pCapabilities">
        /// Pointer to an XINPUT_CAPABILITIES structure that receives the controller capabilities.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is ERROR_SUCCESS.
        /// If the controller is not connected, the return value is ERROR_DEVICE_NOT_CONNECTED.
        /// If the function fails, the return value is an error code defined in WinError.h. 
        /// The function does not use SetLastError to set the calling thread's last-error code.
        /// </returns>
        [DllImport("xinput9_1_0.dll")]
        public static extern int XInputGetCapabilities
        (
            int dwUserIndex,
            int dwFlags, 
            ref XInputCapabilities pCapabilities
        );
        public const int XINPUT_FLAG_GAMEPAD = 0x00000001;

        /// <summary>
        /// Retrieves the current state of the specified controller.
        /// </summary>
        /// <param name="dwUserIndex">Index of the user's controller. Can be a value from 0 to 3.</param>
        /// <param name="pState">Pointer to an XINPUT_STATE structure that receives the current state of the controller.</param>
        /// <returns></returns>
        [DllImport("xinput9_1_0.dll")]
        public static extern int XInputGetState
        (
            int dwUserIndex,
            ref XInputState pState
        );

        /// <summary>
        /// Sends data to a connected controller. This function is used to activate the vibration function of a controller.
        /// </summary>
        /// <param name="dwUserIndex">Index of the user's controller. Can be a value from 0 to 3.</param>
        /// <param name="pVibration">Pointer to an XINPUT_VIBRATION structure containing the vibration information to send to the controller.</param>
        /// <returns></returns>
        [DllImport("xinput9_1_0.dll")]
        public static extern int XInputSetState
        (
            int dwUserIndex,
            ref XInputVibration pVibration
        );        
    }
}
