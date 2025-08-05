
using bluez.DBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;

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
    
            }
            .WithHandler<CommandProcessor>(this, nameof(Pair)));

          //  cmd.AddCommand(new Command("passkey", "Pass paysskey")
          //  {
          //         new Option<string>(new string[] { "--key", "-k" }, "passkey").IsRequired()
          //  }
          //.WithHandler<CommandProcessor>(this, nameof(Passkey)));

            cmd.AddCommand(new Command("trust", "Trust")
            {

            }
            .WithHandler<CommandProcessor>(this, nameof(Trust)));

            cmd.AddCommand(new Command("dispose", "Dispose")
            {

            }
          .WithHandler<CommandProcessor>(this, nameof(Dispose)));



            return cmd;
        }

        private void Initialize()
        {

        }

        private void StartDiscovering()
        {
            if (OperatingSystem.IsWindows())
                return;

            _logger.LogWarning("start discovering (begin)");

            DiscoveredDevices.Clear();

            BleAdapter.StartDiscoveryAsync().GetAwaiter().GetResult();

            _logger.LogWarning("start discovering (end)");

        }

        private void StopDiscovering()
        {
            if (OperatingSystem.IsWindows())
                return;

            _logger.LogWarning("stop discovering (begin)");

            BleAdapter.StopDiscoveryAsync().GetAwaiter().GetResult();

            _logger.LogWarning("stop discovering (end)");
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

        private void Pair()
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

                while (!deviceDiscovered)
                {
                    Thread.Sleep(1000);

                    if (DiscoveredDevices.FirstOrDefault(d => d.Contains("C4:D3:6A:B4:82:F2") == true) != null)
                    {
                        deviceDiscovered = true;
                    }
                }

                StopDiscovering();

                // var pairingServer = new Tmds.DBus.Connection(new ServerConnectionOptions());
                // await pairingServer.RegisterObjectAsync(new MyAgent());
                var device = BleService.CreateDevice1("/org/bluez/hci0/dev_C4_D3_6A_B4_82_F2");
                device.PairAsync().GetAwaiter().GetResult();

                _logger.LogInformation("pair (end)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while pairing: {ex.Message}");
            }
        }

        //private static string _passkey = "";
        //private static ManualResetEvent _waitForPassKey = new ManualResetEvent(false);
        //private void Passkey(string key)
        //{ 
        //    _passkey = key;
        //    _waitForPassKey.Set();
        //}

        private void Trust()
        {
            if (OperatingSystem.IsWindows())
                return;

            _logger.LogInformation("trust (begin)");

            var device = BleService.CreateDevice1("/org/bluez/hci0/dev_C4_D3_6A_B4_82_F2");
            device.SetTrustedAsync(true);

            _logger.LogInformation("trust (end)");
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

        private void Dispose()
        {
            if (OperatingSystem.IsWindows())
                return;

            BleConnection.Dispose();
        }

    }
}
