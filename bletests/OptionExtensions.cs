using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bletests
{
    public static class OptionExtensions
    {
        public static Option IsRequired(this Option option)
        {
            option.IsRequired = true;
            return option;
        }

        //public static Option AddToCache(this Option option, OptionResult commandResult)
        //{

        //    return option;
        //}
    }
}
