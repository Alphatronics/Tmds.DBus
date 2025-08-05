using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace bletests
{
    internal class CommandLineParser
    {


        public string[] CommandParts { get; private set; }

        public CommandLineParser(string[] arguments)
        {
            CommandParts = Parse(arguments);
        }

        public CommandLineParser(string commandLineWithArguments)
        {
            var arguments = SplitOnSpaceOutsideQuotes(commandLineWithArguments);

            CommandParts = Parse(arguments);
        }

        private string[] Parse(string[] arguments)
        {

            arguments = arguments.Select(a => a.Trim(new char[] { '\'', '\"' })).ToArray();

            return arguments;
        }


        private string[] SplitOnSpaceOutsideQuotes(string content)
        {
            Regex regx = new Regex(" " + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            var args = regx.Split(content);

            return RemoveEscapedQuotes(args);
        }

        //changes "-e=\"dev\"" to "-e=dev"
        private string[] RemoveEscapedQuotes(string[] arguments)
        {
            if (arguments == null || arguments.Length == 0) return arguments;

            var trimmedArgs = new string[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
            {
                var splittedArgs = arguments[i].Split('=');
                if (splittedArgs.Length == 2)
                {
                    trimmedArgs[i] = $"{splittedArgs[0]}={splittedArgs[1].Trim('\"')}";
                }
                else
                    trimmedArgs[i] = arguments[i];


            }
            return trimmedArgs;
        }
    }
}

