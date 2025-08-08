using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace bletests.Entities
{
    public class BleDeviceInfo
    {
        public string Path { get; private set; }
        public string Name { get; private set; }
        public string Address { get; private set; }
        public bool IsPaired { get; private set; }
        public bool IsConnected { get; private set; }

        public IEnumerable<BleGattServiceInfo> GattServiceInfos { get; private set; }

        public BleDeviceInfo(Dictionary<string, Dictionary<string, VariantValue>> bleDeviceObject, ObjectPath path, Dictionary<ObjectPath, Dictionary<string, Dictionary<string, VariantValue>>> bleObjects)
        {
            Path = path;

            bleDeviceObject.TryGetValue("org.bluez.Device1", out var properties);
            Name = properties.TryGetValue("Name", out var nameVal) ? nameVal.ToString() : "(unknown)";
            Address = properties.TryGetValue("Address", out var addressVal) ? addressVal.ToString() : "(unknown)";
            IsPaired = properties.TryGetValue("Paired", out var pairedVal) ? pairedVal.GetBool() : false;
            IsConnected = properties.TryGetValue("Connected", out var connectedVal) ? connectedVal.GetBool() : false;

            var gattServiceInfos = new List<BleGattServiceInfo>();
            if (bleObjects != null)
            {
                foreach (var serviceObj in bleObjects)
                {
                    var servicePath = serviceObj.Key;
                    var serviceInterfaces = serviceObj.Value;

                    if (servicePath.ToString().StartsWith(Path.ToString()) &&
                        serviceInterfaces.ContainsKey("org.bluez.GattService1"))
                    {
                        var gattServiceInfo = new BleGattServiceInfo(servicePath, serviceInterfaces, bleObjects);
                        gattServiceInfos.Add(gattServiceInfo);
                    }
                }
            }

            GattServiceInfos = gattServiceInfos.ToArray();
        }



        public override string ToString()
        {

            return $"Device: {Path} - {Name} - {Address} - Paired={IsPaired} - Connected={IsConnected}\n{String.Join<BleGattServiceInfo>('\n',GattServiceInfos)}";
        }
    }

}
