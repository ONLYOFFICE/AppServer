using System.Collections.Generic;

using ASC.Common;

using CommandLine;

namespace ASC.Mail.Aggregator.CollectionService.Console
{
    [Singletone]
    public class ConsoleParameters
    {
        [Option('u', "users", MetaValue = "STRING ARRAY", Required = false, HelpText = "An array of users for which the aggregator will take tasks. " +
                                                                                           "Separator = ';' " +
                                                                                           "Example: -u\"{tl_userId_1}\";\"{tl_userId_2}\";\"{tl_userId_3}\"", Separator = ';')]
        public IList<string> OnlyUsers { get; set; }

        [Option("console", Required = false, HelpText = "Console state")]
        public bool IsConsole { get; set; }

        [Option("myInt", Required = false, HelpText = "fucking int")]
        public int myInt { get; set; }

        [Option("unlimit", Required = false, HelpText = "Unlimit messages per mailbox session")]
        public bool NoMessagesLimit { get; set; }
    }
}
