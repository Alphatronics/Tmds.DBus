using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace bletests.Entities
{
    public class BleGattCharacteristicInfo
    {
        public string Path { get; private set; }
        public string UUID { get; private set; }

        public BleGattCharacteristicInfo(string path, Dictionary<string, Dictionary<string, VariantValue>> charInterfaces)
        {
            Path = path;
            UUID = charInterfaces["org.bluez.GattCharacteristic1"].TryGetValue("UUID", out var serviceUuidVal) ? serviceUuidVal.ToString() : "(unknown)";
        }

        public override string ToString()
        {
            return $"Characteristic: {Path} - {UUID}";
        }
    }
}