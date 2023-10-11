using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DsCore.Win32;
using System.Timers;

namespace DsCore.MameOutput
{
    public class MameOutputHelper
    {
        private static MameOutputHelper _Instance = null;

        private IntPtr _hWnd = IntPtr.Zero;
        private List<OutputClient> _RegisteredClients;

        private IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;
        private const uint COPYDATA_MESSAGE_ID_STRING = 1;

        private const  String MAME_START_STRING =  "MAMEOutputStart";
        private const  String MAME_STOP_STRING =  "MAMEOutputStop";
        private const  String MAME_UPDATE_STRING = "MAMEOutputUpdateState";
        private const  String MAME_REGISTER_STRING = "MAMEOutputRegister";
        private const  String MAME_UNREGISTER_STRING = "MAMEOutputUnregister";
        private const  String MAME_GETID_STRING = "MAMEOutputGetIDString";

        private uint _Mame_OnStartMsg = 0;
        private uint _Mame_OnStopMsg = 0;
        private uint _Mame_UpdateStateMsg = 0;
        private uint _Mame_RegisterClientMsg = 0;
        private uint _Mame_UnregisterClientMsg = 0;
        private uint _Mame_GetIdStringMsg = 0;
        #region Accessors
        
        public uint MameOutput_RegisterClient
        { get { return _Mame_RegisterClientMsg; } }

        public uint MameOutput_UnregisterClient
        { get { return _Mame_UnregisterClientMsg; } }

        public uint MameOutput_GetIdString
        { get { return _Mame_GetIdStringMsg; } }

        #endregion

        private static int _CustomRecoilOnDelay = 50;
        private static int _CustomRecoilOffDelay = 50;
        private static int _CustomDamageDelay = 200;
        #region Accessors
        public static int CustomRecoilOnDelay
        { 
            get { return _CustomRecoilOnDelay; } 
        }
        public static int CustomRecoilOffDelay
        {
            get { return _CustomRecoilOffDelay; }
        }
        public static int CustomDamageDelay
        {
            get { return _CustomDamageDelay; }
        }
        #endregion

        private List<GameOutput> _OutputsBefore;
        private bool _FirstOutputs = true;

        public MameOutputHelper(IntPtr MainWindowHandle, int RecoilOnDelay, int RecoilOffDelay, int DamagedDelay)
        {
            if (_Instance == null)
            {
                _hWnd = MainWindowHandle;
                _RegisteredClients = new List<OutputClient>();

                _Mame_OnStartMsg = RegisterMameOutputMessage(MAME_START_STRING);
                _Mame_OnStopMsg = RegisterMameOutputMessage(MAME_STOP_STRING);
                _Mame_UpdateStateMsg = RegisterMameOutputMessage(MAME_UPDATE_STRING);
                _Mame_RegisterClientMsg = RegisterMameOutputMessage(MAME_REGISTER_STRING);
                _Mame_UnregisterClientMsg = RegisterMameOutputMessage(MAME_UNREGISTER_STRING);
                _Mame_GetIdStringMsg = RegisterMameOutputMessage(MAME_GETID_STRING);

                _CustomRecoilOnDelay = RecoilOnDelay;
                _CustomRecoilOffDelay = RecoilOffDelay;
                _CustomDamageDelay = DamagedDelay;

                _Instance = this;
            }
        }

        public static MameOutputHelper Instance()
        {
            return _Instance;
        }

        /// <summary>
        /// Broadcast a "MAMEOutput_Start" message to inform all potentials clients
        /// </summary>
        public void Start()
        {
            Win32API.PostMessage(HWND_BROADCAST, _Mame_OnStartMsg, _hWnd, IntPtr.Zero); 
        }

        /// <summary>
        /// Broadcast a "MAMEOutput_Stop" message to inform all potentials clients
        /// </summary>
        public void Stop()
        {
            Win32API.PostMessage(HWND_BROADCAST, _Mame_OnStopMsg, _hWnd, IntPtr.Zero);
        }

        /// <summary>
        /// Add (or update if already exixting) a client to the OutputClient list
        /// </summary>
        /// <param name="hWnd">Client Handle</param>
        /// <param name="Id">Client Id</param>
        public int RegisterClient(IntPtr hWnd, UInt32 Id)
        {
            for (int i = 0; i < _RegisteredClients.Count; i++)
            {
                if (_RegisteredClients[i].Id == Id)
                {
                    //C# can't modify directly a field of struct in a List<>, that's why we are forced to use a temporary local variable
                    OutputClient c = _RegisteredClients[i];
                    c.hWnd = hWnd;
                    _RegisteredClients[i] = c;
                    Logger.WriteLog("Successfully updated following MameOutput Client : Id=" + Id.ToString() + ", hWnd=0x" + hWnd.ToString("X8"));
                    return 1;
                }
            }

            OutputClient NewClient = new OutputClient();
            NewClient.hWnd = hWnd;
            NewClient.Id = Id;
            _RegisteredClients.Add(NewClient);
            Logger.WriteLog("Successfully registered following MameOutput Client : Id=" + Id.ToString() + ", hWnd=0x" + hWnd.ToString("X8"));
            return 0;
        }

        /// <summary>
        /// Remove any MameOutput client with the corresponding ID
        /// </summary>
        /// <param name="hWnd">Client Handle</param>
        /// <param name="Id">Client ID</param>
        public void UnregisterClient(IntPtr hWnd, UInt32 Id)
        {
            for (int i = _RegisteredClients.Count - 1; i >= 0; i--)
            {
                if (_RegisteredClients[i].Id == Id)
                {
                    _RegisteredClients.RemoveAt(i);
                    Logger.WriteLog("Successfully unregistered following MameOutput Client : Id=" + Id.ToString() + ", hWnd=0x" + hWnd.ToString("X8")); 
                }
            }
        }

        /// <summary>
        /// Reply to a Client String/Id request
        /// MameHooker is sending a Request with Id=0 to get the rom name
        /// Other Id are OutputId string identification
        /// </summary>
        /// <param name="hWnd">client hwnd</param>
        /// <param name="Id">Requested Id</param>
        public void SendIdString(IntPtr hWnd, String lpStr, UInt32 Id)
        {
            OutputDataStruct data = new OutputDataStruct();
            data.Id = Id;
            data.lpStr = lpStr;
            IntPtr buffer = IntPtrAlloc(data);
            CopyDataStruct copyData = new CopyDataStruct();
            copyData.dwData = new IntPtr(COPYDATA_MESSAGE_ID_STRING);
            copyData.lpData = buffer;
            copyData.cbData = Marshal.SizeOf(data);
            IntPtr copyDataBuff = IntPtrAlloc(copyData);
            Win32API.SendMessage(hWnd, Win32Define.WM_COPYDATA, _hWnd, copyDataBuff);
            IntPtrFree(ref copyDataBuff);
            IntPtrFree(ref buffer);
        }

        /*public void SendIdStringV2(IntPtr hWnd, String lpStr, UInt32 Id)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(lpStr);
            int length = data.Length;

            IntPtr ptr = Marshal.AllocHGlobal(IntPtr.Size * 3 + length);
            Marshal.WriteIntPtr(ptr, 0, IntPtr.Zero);
            Marshal.WriteIntPtr(ptr, IntPtr.Size, (IntPtr)length);
            IntPtr dataPtr = new IntPtr(ptr.ToInt64() + IntPtr.Size * 3);
            Marshal.WriteIntPtr(ptr, IntPtr.Size * 2, dataPtr);
            Marshal.Copy(data, 0, dataPtr, length);
            IntPtr result = Win32API.SendMessage(hWnd, Win32Define.WM_COPYDATA, _hWnd, ptr);
            Marshal.FreeHGlobal(ptr);
        }*/

        /// <summary>
        /// Send updated values to all registered clients
        /// A small "filtering" method was added, to send output message to MameHooker only on changed state of a value
        /// </summary>
        /// <param name="Outputs">List of values to send</param>
        public void SendValues(List<GameOutput> Outputs)
        {
            if (_FirstOutputs)
            {
                //For MameHooker compatibility : Sending orientation once
                Outputs.Insert(0, new GameOutput(OutputDesciption.MameOrientation, OutputId.MameOrientation));
                Outputs[0].OutputValue = 0;

                //Cloning the output list without references to the GameOutput object
                _OutputsBefore = Outputs.ConvertAll(x => new GameOutput(x));
                for (int i = 0; i < Outputs.Count; i++)
                {
                    SendValue(Outputs[i].Id, Outputs[i].OutputValue);
                    //DEBUG Only :
                    //Logger.WriteLog("MAME Output sent : " + Outputs[i].Name + " [Value=" + Outputs[i].OutputValue.ToString() + "]");
                }
                _FirstOutputs = false;
            }
            else
            {
                for (int i = 0; i < Outputs.Count; i++)
                {
                    //DEBUG only :
                    //Logger.WriteLog(Outputs[i].Name + " : Before=" + _OutputsBefore[i].OutputValue + ", Current=" + Outputs[i].OutputValue); 
                    if (Outputs[i].OutputValue != _OutputsBefore[i].OutputValue)
                    {
                        SendValue(Outputs[i].Id, Outputs[i].OutputValue);
                        //DEBUG only :
                        //Logger.WriteLog("MAME Output sent : " + Outputs[i].Name + " [Value=" + Outputs[i].OutputValue.ToString() + "]");
                        _OutputsBefore[i].OutputValue = Outputs[i].OutputValue;
                    }
                }
            }
        }

        /// <summary>
        /// Send a specific updated value to all registered clients
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Value"></param>
        public void SendValue(uint Id, int Value)
        {
            foreach (OutputClient c in _RegisteredClients)
            {
                Win32API.PostMessage(c.hWnd, _Mame_UpdateStateMsg, new IntPtr(Id), new IntPtr(Value));
            }
        }

        /// <summary>
        /// Registering specifid Windows Messages used by MAME (and so MameHooker) for inter-process communication
        /// </summary>
        private uint RegisterMameOutputMessage(String lpString)
        {
            uint id = Win32API.RegisterWindowMessage(lpString);
            if (id == 0)
                Logger.WriteLog("Error registering the following MameHooker message : " + lpString);
            return id;
        }

        /// <summary>
        /// Allocate a pointer to an arbitrary structure on the global heap.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="param"></param>
        /// <returns></returns> 
        public static IntPtr IntPtrAlloc<T>(T param)
        {
            IntPtr retval = Marshal.AllocHGlobal(Marshal.SizeOf(param));
            Marshal.StructureToPtr(param, retval, false);
            return retval;
        }
                
        /// <summary>
        /// Free a pointer to an arbitrary structure from the global heap.
        /// </summary>
        /// <param name="preAllocated">Pointer to a previously allocated memory</param>
        public static void IntPtrFree(ref IntPtr preAllocated)
        {
            if (IntPtr.Zero == preAllocated)
                Logger.WriteLog("MameHookerHelper->SendIdString() error : Impossible to free unallocated Pointer");
            Marshal.FreeHGlobal(preAllocated);
            preAllocated = IntPtr.Zero;
        }
    }
}
