using bluez.DBus;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
/// <summary>
/// see https://github.com/tmds/Tmds.DBus/blob/main/docs/protocol.md
/// https://man.archlinux.org/man/org.bluez.Agent.5.en#uint32_RequestPasskey(object_device)
/// </summary>
/// 

namespace bletests
{
    public class AgentHandler : IMethodHandler
    {
        public const string MY_AGENT = "/my/agent";
        private const string BLE_AGENT_INTERFACE = "org.bluez.Agent1";
        public string Path => MY_AGENT;

        private readonly ILogger _logger;
        public delegate uint HandlePasskeyDelegate(string device);
        private HandlePasskeyDelegate _handlePasskey;

        public AgentHandler(ILogger logger, HandlePasskeyDelegate handlePasskey)
        {
            _logger = logger;
            _handlePasskey = handlePasskey;
        }

        private static ReadOnlyMemory<byte> InterfaceXml { get; } =
            """
        <interface name="org.bluez.Agent1">
          <method name="Add">
            <arg direction="in" type="i"/>
            <arg direction="in" type="i"/>
            <arg direction="out" type="i"/>
          </method>
        </interface>

        """u8.ToArray();

        public bool RunMethodHandlerSynchronously(Message message) => true;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            //if (context.IsDBusIntrospectRequest)
            //{
            //   context.ReplyIntrospectXml([InterfaceXml]);
            //    return default;
            //}
            try
            {
              

                var request = context.Request;

                _logger.LogInformation($"Request {request.InterfaceAsString} | {request.MemberAsString} | {request.SignatureAsString}");
                switch (request.InterfaceAsString)
                {
                    case BLE_AGENT_INTERFACE:
                        switch ((request.MemberAsString, request.SignatureAsString))
                        {
                           
                            case ("RequestPasskey", "o"):
            
                                var reader = request.GetBodyReader();
                                var device = reader.ReadObjectPath();

                                _logger.LogInformation($"Request passkey for device {device}");
                                
                                var passkey = _handlePasskey(device);

                                _logger.LogInformation($"Passkey for {device} entered {passkey}");

                                ReplyToPasskeyRequest(context, passkey);

                                return ValueTask.CompletedTask;
                        }
                        break;
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while handling pairing agent calls: {ex.Message}");
            }

            return ValueTask.CompletedTask;

        }


        private void ReplyToPasskeyRequest(MethodContext context, uint passkey)
        {
            using var writer = context.CreateReplyWriter("u");
            writer.WriteUInt32(passkey);
            context.Reply(writer.CreateMessage());
        }

        //var interfaceName = "org.bluez.Agent1";

        //    switch (context.Member)
        //    {
        //        case "RequestPasskey":
        //            var device = reader.ReadObjectPath();
        //            Console.WriteLine($"Enter passkey for {device}: ");
        //            var passkey = uint.Parse(Console.ReadLine());
        //            writer.Write(passkey);
        //            break;

        //        case "RequestConfirmation":
        //            var device2 = reader.ReadObjectPath();
        //            var passkey2 = reader.ReadUInt32();
        //            Console.WriteLine($"Confirm passkey {passkey2} for {device2} (y/n)?");
        //            var confirm = Console.ReadLine()?.ToLower() == "y";
        //            if (!confirm)
        //                throw new Exception("User rejected confirmation");
        //            break;

        //        case "Cancel":
        //            Console.WriteLine("Pairing cancelled");
        //            break;

        //        default:
        //            throw new NotImplementedException($"Unhandled method: {context.Member}");
        //    }
    }
}
