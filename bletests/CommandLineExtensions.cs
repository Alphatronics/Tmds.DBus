using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace bletests
{
    public static class CommandLineExtensions
    {
        public static Command WithHandler<T>(this Command command, T commandGroup, string methodName) where T : class
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var method = commandGroup.GetType().GetMethod(methodName, flags);

            if (method == null)
                return null;

            var handler = CommandHandler.Create(method, commandGroup);
            command.Handler = handler;
            return command;
        }
    }
}
