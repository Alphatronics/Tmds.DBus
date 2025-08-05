using bluez.DBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        protected ConcurrentQueue<string> DiscoveredDevices = new ConcurrentQueue<string>();

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
                    BleObjectManager.WatchInterfacesAddedAsync((ex, interfaces) =>
                    {
                        if (interfaces.Interfaces.TryGetValue("org.bluez.Device1", out var props))
                        {
                            var name = props.TryGetValue("Name", out var nameVal) ? nameVal.ToString() : "(unknown)";
                            var address = props.TryGetValue("Address", out var addrVal) ? addrVal.ToString() : "(unknown)";
                            System.Console.WriteLine($"Discovered: {name} [{address}] at {interfaces.Object}");
                            DiscoveredDevices.Enqueue(address);
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

        //protected async Task<bool> IsAgentRegistered()
        //{
        //    //var services = BleConnection.ListServicesAsync().GetAwaiter().GetResult();

        //    var om = BleService.CreateObjectManager("/");

        //   // return false;

        //    var objects = await om.GetManagedObjectsAsync();

        //    foreach (var obj in objects)
        //    {
        //        var path = obj.Key;
        //        var interfaces = obj.Value;

        //        if (interfaces.TryGetValue("org.bluez.Agent1", out var properties))
        //        {
        //            if (path == AgentHandler.MY_AGENT)
        //                return true;
        //        }
        //    }

        //    return false;
        //}

        private async Task DiscoverDevicesAsync(bluez.DBus.ObjectManager objectManager)
        {
            var objects = await objectManager.GetManagedObjectsAsync();

            foreach (var obj in objects)
            {
                var path = obj.Key;
                var interfaces = obj.Value;

                //find paired devices
                if (interfaces.TryGetValue("org.bluez.Device1", out var properties) &&
                    properties.TryGetValue("Paired", out var paired))
                {
                    var devName = properties.TryGetValue("Name", out var nameVal) ? nameVal.ToString() : "(unknown)";
                    System.Console.WriteLine($"Paired device - {devName} ({path}) : {paired}");
                }

                // Step 1: Find connected devices
                if (interfaces.TryGetValue("org.bluez.Device1", out var deviceProps) &&
                    deviceProps.TryGetValue("Connected", out var connected))
                {
                    System.Console.WriteLine($"Connected device: {connected} ({path})");
                    if (connected.GetBool())
                    {
                        // Step 2: Find GATT services under this device
                        foreach (var serviceObj in objects)
                        {
                            var servicePath = serviceObj.Key;
                            var serviceInterfaces = serviceObj.Value;

                            if (servicePath.ToString().StartsWith(path.ToString()) &&
                                serviceInterfaces.ContainsKey("org.bluez.GattService1"))
                            {
                                var uuid = serviceInterfaces["org.bluez.GattService1"].TryGetValue("UUID", out var uuidVal)
                                           ? uuidVal.ToString()
                                           : "(unknown)";
                                System.Console.WriteLine($"    Service: {servicePath} (UUID: {uuid})");


                                // Step 3: Find characteristics under this service
                                foreach (var charObj in objects)
                                {
                                    var charPath = charObj.Key;
                                    var charInterfaces = charObj.Value;

                                    if (charPath.ToString().StartsWith(servicePath.ToString()) &&
                                        charInterfaces.ContainsKey("org.bluez.GattCharacteristic1"))
                                    {
                                        var serviceUid = charInterfaces["org.bluez.GattCharacteristic1"].TryGetValue("UUID", out var serviceUuidVal)
                                            ? serviceUuidVal.ToString()
                                            : "(unknown)";
                                        System.Console.WriteLine($"    Characteristic: {charPath} (UUID: {serviceUid})");
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }
    }
}
