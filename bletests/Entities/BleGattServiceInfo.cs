using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace bletests.Entities
{
    public class BleGattServiceInfo
    {
        public string Path { get; private set; }
        public string UUID { get; private set; }

        public IEnumerable<BleGattCharacteristicInfo> GattCharacteristicInfos { get; private set; }

        public BleGattServiceInfo(string path, Dictionary<string, Dictionary<string, VariantValue>> bleServiceInterfaces, Dictionary<ObjectPath, Dictionary<string, Dictionary<string, VariantValue>>> bleObjects)
        {
            Path = path;
            UUID = bleServiceInterfaces["org.bluez.GattService1"].TryGetValue("UUID", out var uuidVal) ? uuidVal.ToString() : "(unknown)";

            var gattCharacteristicInfos = new List<BleGattCharacteristicInfo>();
            foreach (var charObj in bleObjects)
            {
                var charPath = charObj.Key;
                var charInterfaces = charObj.Value;

                if (charPath.ToString().StartsWith(Path.ToString()) &&
                    charInterfaces.ContainsKey("org.bluez.GattCharacteristic1"))
                {
                    var gattCharInfo = new BleGattCharacteristicInfo(charPath, charInterfaces);
                    gattCharacteristicInfos.Add(gattCharInfo);
                }
            }


            GattCharacteristicInfos = gattCharacteristicInfos.ToArray();
        }



        public override string ToString()
        {
            return $"Service: {Path} - {UUID}\n{String.Join<BleGattCharacteristicInfo>('\n',GattCharacteristicInfos)}";
        }

    }
}

