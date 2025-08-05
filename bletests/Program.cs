using bletests;
using bletests.Processors;
using bluez.DBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using Tmds.DBus.Protocol;
//using Tmds.DBus;
using Connection = Tmds.DBus.Protocol.Connection;

namespace Alphatronics.ccTalk.Console
{
    internal class Program
    {

        private static string _newLine = "";
        private static ManualResetEvent _newLineDetected = new ManualResetEvent(false);
        private static FileSystemWatcher _fileWatcher;
        private static long _lastFileReadPosition;
        private const string FILE_NAME = "commands.txt";

        private static async Task Main(string[] args)
        {

            //disconnect and query devices
            //var device = service.CreateDevice1("/org/bluez/hci0/dev_C4_D3_6A_B4_82_F2");
            //await device.DisconnectAsync();

            //var connected = await device.GetConnectedAsync();
            //System.Console.WriteLine($"device is connected: {connected}");

            //await DiscoverDevicesAsync(objectManager);

            ////connect and query devices
            //await device.ConnectAsync();

            //connected = await device.GetConnectedAsync();
            //System.Console.WriteLine($"device is connected: {connected}");

            //await DiscoverDevicesAsync(objectManager);

            //get device data
            //var name = await device.GetNameAsync();
            //var address = await device.GetAddressAsync();
            //System.Console.WriteLine($"device: name:{name}, address {address}");


            ////TODO query device services and characteristics

            //var gattService = service.CreateGattService1("/org/bluez/hci0/dev_C4_D3_6A_B4_82_F2/service000d");
            //var gattServiceProps = await gattService.GetPropertiesAsync();


            //remove device (unpair)
            //await adapter.RemoveDeviceAsync("/org/bluez/hci0/dev_C4_D3_6A_B4_82_F2");

            //open door
            //var c = device.Service.CreateGattCharacteristic1("/org/bluez/hci0/dev_C4_D3_6A_B4_82_F2/service000d/char0012");
            //await c.WriteValueAsync(
            //    new byte[] { 0x01 },
            //    new Dictionary<string, VariantValue> { 
            //        { "type", "request" },
            //        { "offset", (ushort)0 }
            //    });

            _fileWatcher = new FileSystemWatcher();
            _fileWatcher.Filter = FILE_NAME;
            _fileWatcher.Path = Directory.GetCurrentDirectory();
            _fileWatcher.NotifyFilter = //NotifyFilters.LastAccess
                                         NotifyFilters.LastWrite
              //                          | NotifyFilters.FileName
                //                        | NotifyFilters.CreationTime
                                        ;
            _fileWatcher.Changed += _watcher_Changed;
            _fileWatcher.EnableRaisingEvents = true;

            var loggerFactory = LoggerFactory.Create(
                  builder =>
                  {
                      builder.AddConsole();
                      //builder.AddConfiguration(config.GetSection("Logging"))
                      builder.SetMinimumLevel(LogLevel.Debug);
                      //builder.AddFilter("", LogLevel.Debug)

                  });

            var logger = loggerFactory.CreateLogger<Program>();
            var appSettings = ReadAppSettings();

            var coreProcessor = new CoreCommandProcessor(logger, appSettings);

            var cmd = new RootCommand();
            cmd.AddCommand(coreProcessor.GetCommandStructure());

            CommandLineParser commandLineParser = null;

            commandLineParser = new CommandLineParser(ReadCommand());
            do
            {
                cmd.Invoke(commandLineParser.CommandParts);

                commandLineParser = new CommandLineParser(ReadCommand());

            }
            while (commandLineParser.CommandParts[0] != "q");

        }

        private static string ReadCommand()
        {
            if (Debugger.IsAttached)
                return ReadFromFile();
            else
                return ReadFromCommandLine();
        }
       
        private static string ReadFromCommandLine()
        {
            System.Console.WriteLine("\n\nenter your command");
            return System.Console.ReadLine();
        }

        private static string ReadFromFile()
        {
            System.Console.WriteLine($"\n\nenter your command in {FILE_NAME} ");

            _newLineDetected.Reset();
            _newLineDetected.WaitOne();
            
            System.Console.WriteLine(_newLine);

            return _newLine;
        }

        private static object lockFileRead = new object();
        private static void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if(e.ChangeType == WatcherChangeTypes.Changed)
            {


                try
                {
                    lock (lockFileRead)
                    {
    
                        using (FileStream fs = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                           
                            using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                            {
                                fs.Seek(_lastFileReadPosition, SeekOrigin.Begin);
                                var newContent = reader.ReadToEnd();
                                var newLines = newContent.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                if (newLines.Length > 0)
                                {
                                    _newLine = newLines[newLines.Length - 1];
                                    _newLineDetected.Set();
                                }

                                _lastFileReadPosition = fs.Position;

                                reader.Close();
                            }

                            fs.Close();
                        }
                    }
                }
                catch (IOException ex)
                {
                    System.Console.WriteLine("Error reading file: " + ex.Message);
                }

            }
        }




        private static IConfiguration ReadAppSettings()
        {
            try
            {
                var builder = new ConfigurationBuilder();
                builder.AddJsonFile($"appsettings.json", true, true);
                builder.AddJsonFile($"appsettings.Development.json", true, true);
                //.AddEnvironmentVariables();
                return builder.Build();

            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR in {nameof(ReadAppSettings)}: {ex.Message}");

            }
        }

    }  
}