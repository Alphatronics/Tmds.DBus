
using bletests.Entities;
using bluez.DBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using Tmds.DBus.Protocol;

namespace bletests.Processors
{
    public class CoreCommandProcessor : CommandProcessor
    {
        public bool agentOn = false;
        public CoreCommandProcessor(ILogger logger, IConfiguration configuration) : base(logger, configuration)
        {

        }

        public Command GetCommandStructure()
        {
            var cmd = new Command("core", "Core commands");

            cmd.AddCommand(new Command("init", "initialize")
            {
            }
            .WithHandler(this, nameof(Initialize)));

            cmd.AddCommand(new Command("devices", "List current devices")
            {
            }
           .WithHandler(this, nameof(ListDevices)));

            cmd.AddCommand(new Command("start-discover", "Start Discovering")
            {

            }
            .WithHandler<CommandProcessor>(this, nameof(StartDiscovering)));

            cmd.AddCommand(new Command("stop-discover", "Stop Discovering")
            {

            }
            .WithHandler<CommandProcessor>(this, nameof(StopDiscovering)));

            cmd.AddCommand(new Command("agent-on", "Agent On")
            {

            }
           .WithHandler<CommandProcessor>(this, nameof(AgentOn)));

            cmd.AddCommand(new Command("agent-off", "Agent Off")
            {

            }
            .WithHandler<CommandProcessor>(this, nameof(AgentOff)));

            cmd.AddCommand(new Command("pair", "Pair")
            {
                new Option<string>(new string[] { "--address", "-a" }, "Mac Address").IsRequired()
            }
            .WithHandler<CommandProcessor>(this, nameof(Pair)));

          //  cmd.AddCommand(new Command("passkey", "Pass paysskey")
          //  {
          //         new Option<string>(new string[] { "--key", "-k" }, "passkey").IsRequired()
          //  }
          //.WithHandler<CommandProcessor>(this, nameof(Passkey)));

            cmd.AddCommand(new Command("trust", "Trust")
            {
                   new Option<string>(new string[] { "--address", "-a" }, "Mac Address").IsRequired()
            }
            .WithHandler<CommandProcessor>(this, nameof(Trust)));

            cmd.AddCommand(new Command("unpair", "Unpair")
              {
                     new Option<string>(new string[] { "--address", "-a" }, "Mac Address").IsRequired()
              }
          .WithHandler<CommandProcessor>(this, nameof(Unpair)));

            cmd.AddCommand(new Command("open", "Open door")
              {
                     new Option<string>(new string[] { "--address", "-a" }, "Mac Address").IsRequired()
              }
         .WithHandler<CommandProcessor>(this, nameof(OpenDoor)));

            cmd.AddCommand(new Command("binstate", "Get binstate")
              {
                     new Option<string>(new string[] { "--address", "-a" }, "Mac Address").IsRequired()
              }
            .WithHandler<CommandProcessor>(this, nameof(BinState)));

            cmd.AddCommand(new Command("state", "Get device state")
              {
                     new Option<string>(new string[] { "--address", "-a" }, "Mac Address").IsRequired()
              }
          .WithHandler<CommandProcessor>(this, nameof(DeviceState)));

            cmd.AddCommand(new Command("dispose", "Dispose")
            {

            }
          .WithHandler<CommandProcessor>(this, nameof(Dispose)));



            return cmd;
        }

        private void Initialize()
        {

        }

        private void ListDevices()
        {
            _logger.LogInformation("listing devices (begin)");

            var devices = BleObjectManager.QueryDeviceInfosAsync().GetAwaiter().GetResult();

                _logger.LogInformation(String.Join<BleDeviceInfo>('\n',devices));
      
           

            _logger.LogInformation("listing devices (end)");
        }

        private void StartDiscovering()
        {
            if (OperatingSystem.IsWindows())
                return;

            _logger.LogInformation("start discovering (begin)");

            DiscoveredDevices.Clear();

            foreach(var dev in BleObjectManager.QueryDeviceInfosAsync().GetAwaiter().GetResult())
                DiscoveredDevices.Enqueue(dev);

            BleAdapter.StartDiscoveryAsync().GetAwaiter().GetResult();

            _logger.LogInformation("start discovering (end)");

        }

        private void StopDiscovering()
        {
            if (OperatingSystem.IsWindows())
                return;

            _logger.LogInformation("stop discovering (begin)");

            BleAdapter.StopDiscoveryAsync().GetAwaiter().GetResult();

            _logger.LogInformation("stop discovering (end)");
        }

       

        private void AgentOn()
        {
            if (OperatingSystem.IsWindows())
                return;


            _logger.LogInformation("agent on (begin)");
            BleConnection.AddMethodHandler(new AgentHandler(_logger, HandlePasskey));
            BleAgentManager.RegisterAgentAsync(AgentHandler.MY_AGENT, "KeyboardDisplay").GetAwaiter().GetResult();
            BleAgentManager.RequestDefaultAgentAsync(AgentHandler.MY_AGENT).GetAwaiter().GetResult();

            agentOn = true;

            _logger.LogInformation("agent on (end)");
        }

        private void AgentOff()
        {
            if (OperatingSystem.IsWindows())
                return;

            _logger.LogInformation("agent off (begin)");

            BleConnection.RemoveMethodHandler(AgentHandler.MY_AGENT);
            BleAgentManager.UnregisterAgentAsync(AgentHandler.MY_AGENT).GetAwaiter().GetResult();

            agentOn = false;
            _logger.LogInformation("agent off (end)");
        }

        private void Pair(string address)
        {

            try
            {
                if (OperatingSystem.IsWindows())
                    return;

                _logger.LogInformation("pair (begin)");

                if (agentOn)
                    AgentOff();

                if (!agentOn)
                    AgentOn();


                StartDiscovering();

                bool deviceDiscovered = false;

                BleDeviceInfo bleDevice = null;
                while (!deviceDiscovered)
                {
                    Thread.Sleep(1000);

                     bleDevice = DiscoveredDevices.FirstOrDefault(d => d.Address.Contains(address));
                   
                    if (bleDevice != null)
                    {
                        deviceDiscovered = true;
                    }
                }

                StopDiscovering();

                if (bleDevice != null)
                {
                    var device = GetDevice(address);
                    device.PairAsync().GetAwaiter().GetResult();

                    _logger.LogInformation("pair (end)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while pairing: {ex.Message}");
            }
        }

        private void Trust(string address)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                    return;

                _logger.LogInformation("trust (begin)");

                var device = GetDevice(address);
                device.SetTrustedAsync(true);

                _logger.LogInformation("trust (end)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while trusting: {ex.Message}");
            }
        }

        private uint HandlePasskey(string device)
        {
            string fileName = "passkey.txt";
            string file = (Path.Combine(Directory.GetCurrentDirectory(), fileName));

            if(File.Exists(file))
                File.Delete(file);

            _logger.LogWarning($"waiting for passkey in file {fileName} (begin)");
            
          
            bool readFile = false;
            string passkey = "";
            while (!readFile)
            {
                if (File.Exists(file))
                {
                    var content = File.ReadAllText(file);
                    if (!String.IsNullOrWhiteSpace(content))
                    {
                        passkey = content;
                        readFile = true;
                    }
                }
                Thread.Sleep(1000);
              }
            //_waitForPassKey.Reset();
            //_waitForPassKey.WaitOne();

            _logger.LogWarning($"passkey entered: {passkey}");
            _logger.LogWarning("waiting for passkey (end)");

            return UInt32.Parse(passkey);
        }

        private void Unpair(string address)
        {
            try
            {
                _logger.LogInformation("unpairing (begin)");

                var device = GetDevice(address);
                BleAdapter.RemoveDeviceAsync(device.Path);

                _logger.LogInformation("unpairing (end)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while upairing: {ex.Message}");
            }
        }

        private void OpenDoor(string address)
        {
            try
            {
                _logger.LogInformation("open door (begin)");

                var device = GetDevice(address);

                var c = device.Service.CreateGattCharacteristic1($"{device.Path}/service000d/char0012");
                c.WriteValueAsync(
                    new byte[] { 0x01 },
                    new Dictionary<string, VariantValue> {
                        { "type", "request" },
                        { "offset", (ushort)0 }
                    }).GetAwaiter().GetResult();

                _logger.LogInformation("opening door (end)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while opening door: {ex.Message}");
            }
        }

        private void BinState(string address)
        {
            try
            {
                _logger.LogInformation("bin state (begin)");

                var device = GetDevice(address);


                //var gattService = service.CreateGattService1("/org/bluez/hci0/dev_C4_D3_6A_B4_82_F2/service000d");
                //var gattServiceProps = await gattService.GetPropertiesAsync();

                var c = device.Service.CreateGattCharacteristic1($"{device.Path}/service000d/char000f");
                var v = c.ReadValueAsync(
                    new Dictionary<string, VariantValue> {
                        { "type", "request" },
                        { "offset", (ushort)0 }
                    }).GetAwaiter().GetResult();

                if (v != null)
                    _logger.LogInformation($"bin state: {BitConverter.ToString(v).Replace("-","")}");

                _logger.LogInformation("bin state (end)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while getting binstate: {ex.Message}");
            }
        }

        private void DeviceState(string address)
        {
            try
            {
                _logger.LogInformation("device state (begin)");

                var device = GetDevice(address);
;               
                var paired = device.GetPairedAsync().GetAwaiter().GetResult();
                var connected = device.GetConnectedAsync().GetAwaiter().GetResult();

                var deviceMetadata = BleObjectManager.QueryDeviceInfoAsync(address).GetAwaiter().GetResult();

                _logger.LogInformation($"device state: paired:{paired}, connected:{connected}");
                _logger.LogInformation($"device metadata: {deviceMetadata}");

                _logger.LogInformation("device state (end)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while getting device state: {ex.Message}");
            }
        }

        private void Dispose()
        {
            if (OperatingSystem.IsWindows())
                return;

            BleConnection.Dispose();
        }

    }
}
