using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bletests
{
    public class BleDevice
    {
        public string Path { get; set; }

        public string Name { get; set; }
        public string Address { get; set; }

        
        public bool IsPaired { get; set; }

        public bool IsConnected { get; set; }

        public override string ToString()
        {
            var deviceString = $"{Address} - {Name}";

            if (IsPaired)
                deviceString = deviceString + " PAIRED";

            if (IsConnected)
                deviceString = deviceString + " CONNECTED";

            return deviceString;
        }
    }
}
