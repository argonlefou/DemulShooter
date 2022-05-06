using System;
using System.Collections.Generic;
using System.Text;

namespace DsCore.IPC
{
    public class MMF_DataStruct
    {
        public UInt32 RawValue_X;
        public UInt32 RawValue_Y;
        public Int32 ComputedValue_X;
        public Int32 ComputedValue_Y;
        public UInt32 ComputedButtonEvent;

        public MMF_DataStruct()
        { 
            RawValue_X = 0;
            RawValue_Y = 0;
            ComputedValue_X = 0;
            ComputedValue_Y = 0;
            ComputedButtonEvent = 0;
        }

        public void UpdateRawValues(UInt32 NewX, UInt32 NewY)
        {
            RawValue_X = NewX;
            RawValue_Y = NewY;
        }

        public void UpdateComputedValues(Int32 NewX, Int32 NewY, bool[] NewButtons)
        {
            ComputedValue_X = NewX;
            ComputedValue_Y = NewY;

            //Temporary set up for buttons :
            //Buttons bool array on a 32bits variable (for fix length dtruct to send)
            //Better to send directly the boolean array and add a byte header to get the variable length data
            ComputedButtonEvent = 0;
            for (int i = 0; i < NewButtons.Length; i++)
            {
                if (NewButtons[i])
                    ComputedButtonEvent += (uint)(1 << i);
                if (i >= 31)
                    break;
            }
        }
    }
}
