using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace UnityPlugin_BepInEx_NHA2_Configurator
{
    public partial class Wnd_Main : Form
    {
        //Gameplay Settings
        private int _ResolutionWidth = 1920;
        private int _ResolutionHeight = 1080;
        private int _Fullscreen = 0;
        private int _Language = 0;
        private int _InputMode = 0; //0=Mouse(1P), 1=DemulShooter(2P)
        private int _RemoveCrosshair = 0;
        private int _RemoveLaser = 0;
        private int _RemoveGun = 0;

        private const string CONF_FILENAME = "InputPlugin_Config.ini";

        public Wnd_Main()
        {
            InitializeComponent();
            ReadConf(CONF_FILENAME);
            SetGUI();           
        }

        private void SetGUI()
        {
            Txt_Width.Text = _ResolutionWidth.ToString();
            Txt_Height.Text = _ResolutionHeight.ToString();
            Cbox_Screenmode.SelectedIndex = _Fullscreen;
            Cbox_Lang.SelectedIndex = _Language;
            Cbox_InputMode.SelectedIndex = _InputMode;
            Cbox_Crosshair.SelectedIndex = _RemoveCrosshair;
            Cbox_Laser.SelectedIndex = _RemoveLaser;
            Cbox_Gun.SelectedIndex = _RemoveGun;
        }

        public void ReadConf(string ConfigFilePath)
        {
            if (File.Exists(Application.StartupPath + @"\" + CONF_FILENAME))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(Application.StartupPath + @"\" + CONF_FILENAME))
                    {
                        String line = sr.ReadLine();
                        String[] buffer;

                        while (line != null)
                        {
                            if (!line.StartsWith(";"))
                            {
                                buffer = line.Split('=');
                                if (buffer.Length == 2)
                                {
                                    String StrKey = buffer[0].Trim();
                                    String StrValue = buffer[1].Trim();

                                    FieldInfo f = this.GetType().GetField("_" + StrKey, BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (f != null)
                                    {
                                        try
                                        {
                                            int i = int.Parse(StrValue);
                                            f.SetValue(this, i);
                                        }
                                        catch
                                        {
                                            MessageBox.Show("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show("Error : _" + StrKey + " variable not found", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                            }
                            line = sr.ReadLine();
                        }
                        sr.Close();
                    }
                }
                catch (Exception ex)                
                {
                    MessageBox.Show("Error reading " + ConfigFilePath + " : " + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void WriteConf()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(Application.StartupPath + @"\" + CONF_FILENAME, false))
                {
                    sw.WriteLine("ResolutionWidth=" + Txt_Width.Text);
                    sw.WriteLine("ResolutionHeight=" + Txt_Height.Text);
                    sw.WriteLine("Fullscreen=" + Cbox_Screenmode.SelectedIndex);
                    sw.WriteLine("Language=" + Cbox_Lang.SelectedIndex);  
                    sw.WriteLine("InputMode=" + Cbox_InputMode.SelectedIndex);
                    sw.WriteLine("RemoveCrosshair=" + Cbox_Crosshair.SelectedIndex);
                    sw.WriteLine("RemoveLaser=" + Cbox_Laser.SelectedIndex);
                    sw.WriteLine("RemoveGun=" + Cbox_Gun.SelectedIndex);
                    MessageBox.Show("Operator config succesfully saved to :\n" + Application.StartupPath + @"\" + CONF_FILENAME, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Can't save config data to :\n" + Application.StartupPath + @"\" + CONF_FILENAME + " :\n\n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            WriteConf();
        }
    }
}
