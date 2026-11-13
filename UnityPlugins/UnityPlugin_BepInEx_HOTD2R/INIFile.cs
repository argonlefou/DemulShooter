using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityPlugin_BepInEx_IniFile
{
    public class INIFile
    {
        private string _RelativePath = string.Empty;
        public FileInfo FInfo { get; private set; }

        [DllImport("kernel32", CharSet=CharSet.Unicode)]
		private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);


        public INIFile(string INIPath)
        {
            _RelativePath = INIPath;
            FInfo = new FileInfo(_RelativePath);
        }
        public long IniWriteValue(string Section, string Key, string Value)
        {
            return WritePrivateProfileString(Section, Key, Value, this._RelativePath);
        }

        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, this._RelativePath);
            return temp.ToString();
        }
		
		public void IniReadBoolValue(string section, string key, ref bool TargetSetting)
		{
			int iBuffer = 0;
			try
			{
				iBuffer = int.Parse(IniReadValue(section, key));
				if (iBuffer == 0) 
					TargetSetting = false;
				else
					TargetSetting = true;                 
			}
			catch { }
		}

		public void IniReadIntValue(string section, string key, ref int TargetSetting)
		{
			try
			{
				TargetSetting = int.Parse(IniReadValue(section, key));
			}
			catch { }
		}
	}
}