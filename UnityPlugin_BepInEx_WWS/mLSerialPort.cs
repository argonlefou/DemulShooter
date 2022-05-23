using System;
using System.Collections.Generic;
using HarmonyLib;

namespace UnityPlugin_BepInEx_WWS
{
    class mLSerialPort
    {
        static String ByteArrayToString(byte[] bArray)
        {
            string s = "";
            for (int i = 0; i < bArray.Length; i++)
            {
                s += bArray[i].ToString("X2") + " ";
            }
            return s;
        }

        [HarmonyPatch(typeof(LSerialPort), "Start")]
        class Start
        {
            static bool Prefix(bool ___m_com_opened, bool ___m_loop_thread)
            {
                UnityEngine.Debug.Log("mLSerialPort.Start()");
                ___m_loop_thread = true;
                ___m_com_opened = Demulshooter_Plugin.WWS_Mmf.IsOpened;                
                return false;
            }
        }

        /// <summary>
        /// Replacing the COM port parsing by Shared Memory Parsing
        /// </summary>
        [HarmonyPatch(typeof(LSerialPort), "GetAllMsg")]
        class GetAllMsg
        {   
            static bool Prefix(ref List<byte[]> msg_list, List<Byte[]> ___m_recv_pack)
            {
                //Check Shared Memory parameters instead of reading COM port messages
                int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                if (r != 00)
                    UnityEngine.Debug.LogError("mLSerialPort.GetAllMsg() => Error reading MemoryMappedFile : " + r.ToString());
                else
                {
                    //Entering TEST mode
                    if (Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_TEST] != 0)
                    {
                        byte[] b = new Byte[7];
                        b[0] = 0xDD;
                        b[1] = 16;
                        b[2] = 0;
                        b[3] = 0;
                        b[4] = 0;
                        b[5] = 0;
                        b[6] = 0;
                        ___m_recv_pack.Add(b);
                        Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_TEST] = 0;   
                    }

                    //For Triggers, DemulShooter will output "1" for ButtonUp and "2" for ButtonDown. 0 When no inputs
                    if (Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_TRIGGER] != 0 || Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_TRIGGER] != 0)
                    {
                        byte[] b = new Byte[7];
                        b[0] = 0xDD;
                        b[1] = 0;
                        if (Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_TRIGGER] != 0)
                            b[1] = (byte)(Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_TRIGGER] - 1);
                        b[2] = 0;
                        if (Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_TRIGGER] != 0)
                            b[2] = (byte)(Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_TRIGGER] - 1);
                        b[3] = 0;
                        b[4] = 0;
                        b[5] = 0;
                        b[6] = 0;
                        ___m_recv_pack.Add(b);
                        Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_TRIGGER] = 0;
                        Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_TRIGGER] = 0;
                    }
                    
                    //Adding CREDITS to Player 1
                    UInt32 ibuffer = BitConverter.ToUInt32(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_COIN);
                    if (ibuffer != 0)
                    {
                        byte[] b = new Byte[7];
                        b[0] = 0xCC;
                        b[1] = 1;
                        b[2] = (byte)((ibuffer >> 12) & 0x0F);
                        b[3] = (byte)((ibuffer >> 8) & 0x0F);
                        b[4] = (byte)((ibuffer >> 4) & 0x0F);
                        b[5] = (byte)(ibuffer & 0x0F);
                        b[6] = 0;
                        ___m_recv_pack.Add(b);
                        Array.Copy(new Byte[] { 0, 0, 0, 0 }, 0, Demulshooter_Plugin.WWS_Mmf.Payload, (long)WWS_MemoryMappedFile_Controller.INDEX_P1_COIN, 4);                        
                    }

                    //Adding CREDITS to Player 2
                    ibuffer = BitConverter.ToUInt32(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P2_COIN);
                    if (ibuffer != 0)
                    {
                        byte[] b = new Byte[7];
                        b[0] = 0xCC;
                        b[1] = 2;
                        b[2] = (byte)((ibuffer >> 12) & 0x0F);
                        b[3] = (byte)((ibuffer >> 8) & 0x0F);
                        b[4] = (byte)((ibuffer >> 4) & 0x0F);
                        b[5] = (byte)(ibuffer & 0x0F);
                        b[6] = 0;
                        ___m_recv_pack.Add(b);
                        Array.Copy(new Byte[] { 0, 0, 0, 0 }, 0, Demulshooter_Plugin.WWS_Mmf.Payload, (long)WWS_MemoryMappedFile_Controller.INDEX_P2_COIN, 4);                        
                    }

                    //Reload command for Player 1
                    if (Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_RELOAD] != 0)
                    {
                        try
                        {
                            if (GameUIController.Instance.playerUI[0] != null)
                                GameUIController.Instance.playerUI[0].ReLoadBullets();                            
                        }
                        catch { }
                        Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_RELOAD] = 0;
                    }

                    //Reload command for Player 2
                    if (Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_RELOAD] != 0)
                    {
                        try
                        {
                            if (GameUIController.Instance.playerUI[1] != null)
                                GameUIController.Instance.playerUI[1].ReLoadBullets();
                        }
                        catch { }
                        Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_RELOAD] = 0;
                    }


                    /*-----------------------------------------------------*/
                    // Sending Data to DemulShooter as Well during this loop
                    PlayerData Pdata = GameData.GetPlayerData(PlayerType.Player1);
                    if (Pdata != null)
                    {
                        Array.Copy(BitConverter.GetBytes(Pdata.coins), 0, Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_CREDITS, 4);
                        Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_LIFE] = (byte)Pdata.life;
                    }
                    Pdata = GameData.GetPlayerData(PlayerType.Player2);
                    if (Pdata != null)
                    {
                        Array.Copy(BitConverter.GetBytes(Pdata.coins), 0, Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P2_CREDITS, 4);
                        Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_LIFE] = (byte)Pdata.life;
                    }
                    Array.Copy(BitConverter.GetBytes(UnityEngine.Screen.width), 0, Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_VIEWPORT_WIDTH, 4);
                    Array.Copy(BitConverter.GetBytes(UnityEngine.Screen.height), 0, Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_VIEWPORT_HEIGHT, 4);
                    
                    //Rewrite Updated Memory
                    Demulshooter_Plugin.WWS_Mmf.Writeall();
                }    

                //Putting results into BaseCom array given as parameter for further actions
                for (int index = 0; index < ___m_recv_pack.Count; ++index)
                {
                    //UnityEngine.Debug.Log("Received Data Packet = " + ByteArrayToString(___m_recv_pack[index]));
                    msg_list.Add(___m_recv_pack[index]);
                }
                ___m_recv_pack.Clear();

                return false;
            }
        }

        /// <summary>
        /// Only used by BaseCom.SendGunSignal()
        /// ??? Purpose ???
        /// </summary>
        [HarmonyPatch(typeof(LSerialPort), "DiretSend")]
        class DiretSend
	    {
            static bool Prefix(byte[] buf)
            {
                UnityEngine.Debug.Log("mLSerialPort.DirectSend(byte[]) => data_buf = " + ByteArrayToString(buf));
                return false;
            }
	    }              

        [HarmonyPatch(typeof(LSerialPort), "SendData", new[] { typeof(byte []), typeof(int) })]
        class SendData
        {
            static bool Prefix(List<Byte[]> ___m_recv_pack, byte[] data_buf, int send_count = 1)
            {
                byte[] destinationArray = new byte[8];
                destinationArray[0] = byte.MaxValue;
                Array.Copy((Array) data_buf, 0, (Array) destinationArray, 1, data_buf.Length);
                //UnityEngine.Debug.Log("mLSerialPort.SendData(byte[]) => data_buf = " + ByteArrayToString(destinationArray));

                if (destinationArray[1] == 0x5A) //CheckIO Board
                {
                    ___m_recv_pack.Add(data_buf);   //reply with same packet
                }
                else
                {
                    int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                    if (r == 0)
                    {
                        if (destinationArray[1] == 0xF9)    //SendOpen()
                        {
                            Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_GUNOPEN] = destinationArray[2];
                            Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_GUNOPEN] = destinationArray[3];
                            Demulshooter_Plugin.WWS_Mmf.Writeall();
                        }
                        else if (destinationArray[1] == 0xD6)    //SendTestGun()
                        {
                            Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_GUNTEST] = destinationArray[2];
                            Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_GUNTEST] = destinationArray[3];
                            Demulshooter_Plugin.WWS_Mmf.Writeall();
                        }
                        else if (destinationArray[1] == 0xD9)    //SendState()
                        {
                            Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_GUNSTATE] = destinationArray[2];
                            Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_GUNSTATE] = destinationArray[3];
                            Demulshooter_Plugin.WWS_Mmf.Writeall();
                        }
                        else if (destinationArray[1] == 0xD9)    //SendGunSignal()
                        {
                            Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_GUNSIGNAL] = destinationArray[2];
                            Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P2_GUNSIGNAL] = destinationArray[3];
                            Demulshooter_Plugin.WWS_Mmf.Writeall();
                        }
                    }
                    else
                        UnityEngine.Debug.LogError("mLSerialPort.SendData(byte[]) => DemulShooter MMF read error : " + r.ToString());
                }
                return false;
            }            
        }

        /// <summary>
        /// This one seems to be unused
        /// </summary>
        [HarmonyPatch(typeof(LSerialPort), "SendData", new[] { typeof(List<byte>), typeof(int) })]
        class SendData_1
        {
            static bool Prefix(List<byte> data_buf, int send_count = 1)
            {
                UnityEngine.Debug.Log("mLSerialPort.SendData(List<Byte>)");
                byte[] destinationArray = new byte[data_buf.Count];
                Array.Copy((Array)data_buf.ToArray(), 0, (Array)destinationArray, 0, data_buf.Count);
                //UnityEngine.Debug.Log("data_buf = " + ByteArrayToString(destinationArray));

                /*this.frame_ready.WaitOne();
                this.frame_ready.Reset();
                for (int index = 0; index < send_count; ++index)
                    this.m_send_pack.Add(destinationArray);*/

                return false;
            }
        }
    }
}
