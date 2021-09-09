using System;

namespace DsCore.MameOutput
{
    /// <summary>
    /// A specific Output from a game to send to MameHooker
    /// </summary>
    public class GameOutput
    {
        protected String _Name;
        protected UInt32 _Id;
        protected int _OutputValue;

        public String Name
        { get { return _Name; } }

        public UInt32 Id
        { get { return _Id; } }

        public virtual int OutputValue
        {
            get { return _OutputValue; }
            set {  _OutputValue = value;}
        }

        public GameOutput(String Name, OutputId Id)
        {
            _Name = Name;
            _Id = (UInt32)Id;
            _OutputValue = 0;
        }

        public GameOutput(GameOutput Output)
        {
            _Name = Output.Name;
            _Id = Output.Id;
            _OutputValue = Output.OutputValue;
        }
    }
}
