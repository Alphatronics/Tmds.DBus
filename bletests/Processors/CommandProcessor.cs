using bletests.Entities;
using bluez.DBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace bletests.Processors
{
    public abstract class CommandProcessor
    {
        protected readonly ILogger _logger;
        protected readonly IConfiguration _configuration;

        protected readonly Connection BleConnection;
        protected readonly bluezService BleService;
        protected readonly bluez.DBus.ObjectManager BleObjectManager;
        protected readonly Adapter1 BleAdapter;
        protected readonly AgentManager1 BleAgentManager;

        protected ConcurrentQueue<BleDeviceInfo> DiscoveredDevices = new ConcurrentQueue<BleDeviceInfo>();

        public CommandProcessor(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            if (OperatingSystem.IsLinux())
            {
                if (BleConnection == null)
                {
                    string? systemBusAddress = Address.System;
                    if (systemBusAddress is null)
                    {
                        _logger.LogError("Can not determine system bus address");
                        return;
                    }

                    BleConnection = new Connection(Address.System!);
                    BleConnection.ConnectAsync().GetAwaiter().GetResult();
                    _logger.LogInformation("Connected to system bus.");

                }

                if (BleService == null)
                {
                    BleService = new bluezService(BleConnection, "org.bluez");
                }




                if (BleObjectManager == null)
                {
                    BleObjectManager = BleService.CreateObjectManager("/");

                    foreach (var dev in BleObjectManager.QueryDeviceInfosAsync().GetAwaiter().GetResult())
                        DiscoveredDevices.Enqueue(dev);

                    BleObjectManager.WatchInterfacesAddedAsync((ex, interfaces) =>
                    {
                        if (interfaces.Interfaces.TryGetValue("org.bluez.Device1", out var properties))
                        {
                            var bleDevice = new BleDeviceInfo(interfaces.Interfaces, interfaces.Object,null);
                            DiscoveredDevices.Enqueue(bleDevice);

                            _logger.LogWarning($"Discovered: {bleDevice}] at {interfaces.Object}");

                        }
                    }).GetAwaiter().GetResult();
                }

                if (BleAdapter == null)
                {
                    BleAdapter = BleService.CreateAdapter1("/org/bluez/hci0");
                }


                if (BleAgentManager == null)
                {
                    BleAgentManager = BleService.CreateAgentManager1("/org/bluez");
                }
            }
        }

        protected Device1 GetDevice(string address)
        {
            var bleDevice = DiscoveredDevices.FirstOrDefault(d => d.Address.Contains(address));
            if (bleDevice == null)
                throw new Exception($"Device with address {address} not yet discovered, please execute the 'core devices' command first");

            return BleService.CreateDevice1(bleDevice.Path);
        }

        //private BleDeviceInfo CreateBleDevice(Dictionary<string, VariantValue> properties, ObjectPath path)
        //{
        //    var devName = properties.TryGetValue("Name", out var nameVal) ? nameVal.ToString() : "(unknown)";
        //    var devAddress = properties.TryGetValue("Address", out var addressVal) ? addressVal.ToString() : "(unknown)";
        //    var devPaired = properties.TryGetValue("Paired", out var pairedVal) ? pairedVal.GetBool() : false;
        //    var devConnected = properties.TryGetValue("Connected", out var connectedVal) ? connectedVal.GetBool() : false;

        //    return new BleDeviceInfo() { Path = path, Name = devName, Address = devAddress, IsPaired = devPaired, IsConnected = devConnected };
        //}

        //protected async Task<BleDevice[]> QueryDevicesAsync()
        //{
        //    var objects = await BleObjectManager.GetManagedObjectsAsync();
        //    var devices = new List<BleDevice>();

        //    foreach (var obj in objects)
        //    {
        //        var path = obj.Key;
        //        var interfaces = obj.Value;

        //        //find  devices
        //        if (interfaces.TryGetValue("org.bluez.Device1", out var properties))
        //        {


        //            var bleDevice = CreateBleDevice(properties, path);
        //            devices.Add(bleDevice);

        //            _logger.LogInformation($"Device - {bleDevice}");

        //            if (bleDevice.IsConnected)
        //            {
        //                // Step 2: Find GATT services under this device
        //                foreach (var serviceObj in objects)
        //                {
        //                    var servicePath = serviceObj.Key;
        //                    var serviceInterfaces = serviceObj.Value;

        //                    if (servicePath.ToString().StartsWith(path.ToString()) &&
        //                        serviceInterfaces.ContainsKey("org.bluez.GattService1"))
        //                    {
        //                        var uuid = serviceInterfaces["org.bluez.GattService1"].TryGetValue("UUID", out var uuidVal)
        //                                   ? uuidVal.ToString()
        //                                   : "(unknown)";
        //                        _logger.LogInformation($"    Service: {servicePath} (UUID: {uuid})");


        //                        // Step 3: Find characteristics under this service
        //                        foreach (var charObj in objects)
        //                        {
        //                            var charPath = charObj.Key;
        //                            var charInterfaces = charObj.Value;

        //                            if (charPath.ToString().StartsWith(servicePath.ToString()) &&
        //                                charInterfaces.ContainsKey("org.bluez.GattCharacteristic1"))
        //                            {
        //                                var serviceUid = charInterfaces["org.bluez.GattCharacteristic1"].TryGetValue("UUID", out var serviceUuidVal)
        //                                    ? serviceUuidVal.ToString()
        //                                    : "(unknown)";
        //                                _logger.LogInformation($"    Characteristic: {charPath} (UUID: {serviceUid})");
        //                            }
        //                        }
        //                    }
        //                }
        //            }

        //        }
        //    }

        //    return devices.ToArray();

        //}
    }
}
