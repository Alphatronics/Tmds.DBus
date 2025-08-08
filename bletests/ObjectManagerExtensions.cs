using bletests.Entities;
using bluez.DBus;
using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bletests
{
    public static class ObjectManagerExtensions
    {
        public static async Task<BleDeviceInfo[]> QueryDeviceInfosAsync(this ObjectManager bleObjectManager)
        {
            var objects = await bleObjectManager.GetManagedObjectsAsync();
            var deviceInfos = new List<BleDeviceInfo>();

            foreach (var obj in objects)
            {
                var path = obj.Key;
                var interfaces = obj.Value;

                //find  devices
                if (interfaces.TryGetValue("org.bluez.Device1", out var properties))
                {
                    var bleDeviceInfo = new BleDeviceInfo(interfaces, path, objects);
                    deviceInfos.Add(bleDeviceInfo);
                }
            }

            return deviceInfos.ToArray();

        }

        public static async Task<Result<BleDeviceInfo>> QueryDeviceInfoAsync(this ObjectManager bleObjectManager, string address)
        {
            try
            {
                var objects = await bleObjectManager.GetManagedObjectsAsync();

                var addressInPathFormat = address.Replace(":", "_");
                var deviceObject = objects.FirstOrDefault(o => o.Key == $"/org/bluez/hci0/dev_{addressInPathFormat}");


                return new BleDeviceInfo(deviceObject.Value, deviceObject.Key, objects);
            }
            catch (Exception ex) //no metadata found or error while querying
            {
                return Result.Failure<BleDeviceInfo>($"Error while querying metadata for device {address}: {ex.Message}");
            }
        }
    }
}
