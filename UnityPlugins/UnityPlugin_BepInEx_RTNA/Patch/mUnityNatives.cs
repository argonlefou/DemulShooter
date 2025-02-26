using HarmonyLib;
using System;
using System.Runtime.InteropServices;

namespace UnityPlugin_BepInEx_RTNA
{
    class mUnityNatives
    {

        [HarmonyPatch(typeof(UnityNatives), "ReadTicketsOwed")]
        class ReadTicketsOwed
        {
            static bool Prefix(ref int p1Tickets, ref int p2Tickets)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.ReadTicketsOwed()");
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "WriteTicketsOwed")]
        class WriteTicketsOwed
        {
            static bool Prefix(int p1Tickets, int p2Tickets)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.WriteTicketsOwed()");
                return false;
            }
        }

        /// <summary>
        /// Coin audit is read using some native linux lib, and causing an error because it does not exists under windows
        /// Deactivating the read untill it can be replaced by a custom functions
        /// </summary>    
        [HarmonyPatch(typeof(UnityNatives), "ReadCoinAuditsFromFile")]
        class ReadCoinAuditsFromFile
        {
            static bool Prefix(ref CoinAudits_ForReadWrite coinAud, ref bool __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.ReadCoinAuditsFromFile()");
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "WriteCoinAuditsToFile")]
        class WriteCoinAuditsToFile
        {
            static bool Prefix(ref CoinAudits_ForReadWrite coinAud)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.WriteCoinAuditsToFile()");
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "GetPeriodicRebootInfo")]
        class GetPeriodicRebootInfo
        {
            static bool Prefix(ref float rebootTime, ref float rebootDelay)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.GetPeriodicRebootInfo()");
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "GetRIOKeys")]
        class GetRIOKeys
        {
            static bool Prefix(IntPtr pub, IntPtr priv)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.GetRIOKeys()");
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "SetHaspLoginDelegate")]
        class SetHaspLoginDelegate
        {
            static bool Prefix([MarshalAs(UnmanagedType.FunctionPtr)] HaspLoginDelegate callbackPointer)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.SetHaspLoginDelegate()");
                return false;
            }
        }

        /// <summary>
        /// Simulate dongle Login
        /// </summary>
        [HarmonyPatch(typeof(UnityNatives), "HaspLogin")]
        class HaspLogin
        {
            static bool Prefix(int featureID, ref int handle, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.HaspLogin()");
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "SetHaspLogoutDelegate")]
        class SetHaspLogoutDelegate
        {
            static bool Prefix([MarshalAs(UnmanagedType.FunctionPtr)] HaspLogoutDelegate callbackPointer)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.SetHaspLogoutDelegate()");
                return false;
            }
        }

        /// <summary>
        /// Simulate Dongle Logout
        /// </summary>
        [HarmonyPatch(typeof(UnityNatives), "HaspLogout")]
        class HaspLogout
        {
            static bool Prefix(int handle, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.HaspLogout()");
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "SetHaspReadDelegate")]
        class SetHaspReadDelegate
        {
            static bool Prefix([MarshalAs(UnmanagedType.FunctionPtr)] HaspReadDelegate callbackPointer)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.SetHaspReadDelegate()");
                return false;
            }
        }

        /// <summary>
        /// Get dongle serial number in 'dongleVersion' variable
        /// </summary>
        [HarmonyPatch(typeof(UnityNatives), "HaspReadDongleVersion")]
        class HaspReadDongleVersion
        {
            static bool Prefix(int handle, ref int dongleVersion, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.HaspReadDongleVersion()");
                dongleVersion = 1;
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "HaspReadLifetimeCoinCount")]
        class HaspReadLifetimeCoinCount
        {
            static bool Prefix(int handle, ref int coinCount, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.HaspReadLifetimeCoinCount()");
                coinCount = 0;
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "HaspReadCountryCode")]
        class HaspReadCountryCode
        {
            static bool Prefix(int handle, ref byte countryCode, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.HaspReadCountryCode()");
                countryCode = 0;
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        /// <summary>
        /// Get Cabinet type in 'cabType' variable :
        /// Must return 0 ? 
        [HarmonyPatch(typeof(UnityNatives), "HaspReadCabType")]
        class HaspReadCabType
        {
            static bool Prefix(int handle, ref byte cabType, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("DongleControl.HaspReadCabType() : handle=" + handle);
                cabType = (byte)FactorySetUpConsts.CABINET_IDS.DEFAULT; //0
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        /// <summary>
        /// Get Cabinet system in 'cabTemplate' variable (between 0 to 9), in FactorySetUpConsts.TEMPLATE_IDS enum
        /// Value is -1 so :
        /// 0 = USA_COIN  
        /// ...
        /// 9 = NUM_IDS
        /// </summary>
        [HarmonyPatch(typeof(UnityNatives), "HaspReadCabTemplate")]
        class HaspReadCabTemplate
        {
            static bool Prefix(int handle, ref byte cabTemplate, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("DongleControl.HaspReadCabTemplate() : handle=" + handle);
                __result = (int)HaspErrorCode.STATUS_OK;
                cabTemplate = NerfArcade_Plugin.CabTemplate;
                return false;
            }
        }

        /// <summary>
        /// Get cab serial number in 'serialNum' variable
        /// </summary>
        [HarmonyPatch(typeof(UnityNatives), "HaspReadCabSerialNum")]
        class HaspReadCabSerialNum
        {
            static bool Prefix(int handle, ref int serialNum, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("DongleControl.HaspReadCabSerialNum() : handle=" + handle);
                serialNum = 11871514;
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "SetHaspWriteDelegate")]
        class SetHaspWriteDelegate
        {
            static bool Prefix([MarshalAs(UnmanagedType.FunctionPtr)] HaspWriteDelegate callbackPointer)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("UnityNatives.SetHaspWriteDelegate()");
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "HaspWriteLifetimeCoinCount")]
        class HaspWriteLifetimeCoinCount
        {
            static bool Prefix(int handle, ref int coinCount, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("DongleControl.HaspWriteLifetimeCoinCount() : handle=" + handle + ", coinCount=" + coinCount);
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "HaspWriteCountryCode")]
        class HaspWriteCountryCode
        {
            static bool Prefix(int handle, ref byte countryCode, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("DongleControl.HaspWriteCountryCode() : handle=" + handle + ", countryCode=" + countryCode);
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "HaspWriteCabType")]
        class HaspWriteCabType
        {
            static bool Prefix(int handle, ref byte cabType, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("DongleControl.HaspWriteCabType() : handle=" + handle + ", cabType=" + cabType);
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "HaspWriteCabTemplate")]
        class HaspWriteCabTemplate
        {
            static bool Prefix(int handle, ref byte cabTemplate, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("DongleControl.HaspWriteCabTemplate() : handle=" + handle + ", cabTemplate=" + cabTemplate);
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }

        [HarmonyPatch(typeof(UnityNatives), "HaspWriteCabSerialNum")]
        class HaspWriteCabSerialNum
        {
            static bool Prefix(int handle, ref int serialNum, ref int __result)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("DongleControl.HaspWriteCabSerialNum() : handle=" + handle + ", serialNum=" + serialNum);
                __result = (int)HaspErrorCode.STATUS_OK;
                return false;
            }
        }
    }
}
