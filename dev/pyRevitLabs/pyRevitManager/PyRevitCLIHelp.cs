using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pyRevitManager.Properties;

using pyRevitLabs.Common.Extensions;

namespace pyRevitManager {
    internal class PyRevitCLIHelp {
        // help strings
        internal static string doctopUsagePatterns => Resources.UsagePatterns;
        internal static string PrettyHelp => Resources.PrettyHelp;

        internal static void PrintCommandHelpAndExit(PyRevitCLICommandType commandType) {
            switch (commandType) {

                case PyRevitCLICommandType.Main:
                    Console.WriteLine(PrettyHelp);
                    break;

                case PyRevitCLICommandType.Help:
                    PrintSubHelpAndExit(
                        new List<string>() { "help" },
                        title: "Open help in default browser"
                        );
                    break;

                case PyRevitCLICommandType.Releases:
                    PrintSubHelpAndExit(
                        new List<string>() { "releases" },
                        title: "Info on pyRevit Releases",
                        commands: new Dictionary<string, string>() {
                                { "open",               "Open release page in default browser" },
                                { "download installer", "Download EXE installer for given release, if exists" },
                                { "download archive",   "Download Zip archive for given release" }
                            },
                        options: new Dictionary<string, string>() {
                                { "latest",             "Match latest release only" },
                                { "<search_pattern>",   "Pattern to search releases" },
                                { "<dest_path>",        "Destination file or directory to download to" },
                                { "--pre",              "Include pre-releases in the search" },
                                { "--notes",            "Print release notes" }
                            }
                        );
                    break;
            }

            // now exit
            Environment.Exit(0);
        }

        // TODO: make more generic to support the main app pretty help?
        internal static void PrintSubHelpAndExit(IEnumerable<string> docoptKeywords,
                                                 string title,
                                                 IDictionary<string, string> commands = null,
                                                 IDictionary<string, string> options = null) {
            // build a help guide for a subcommand based on doctop usage entries
            Console.WriteLine(title + Environment.NewLine);
            foreach (var hline in doctopUsagePatterns.GetLines())
                if (hline.Contains("Usage:"))
                    Console.WriteLine(hline);
                else
                    foreach (var kword in docoptKeywords) {
                        if ((hline.Contains("pyrevit " + kword + " ") || hline.EndsWith(" " + kword))
                            && !hline.Contains("pyrevit " + kword + " --help"))
                            Console.WriteLine(hline);
                    }

            // print commands help
            int indent = 20;
            string outputFormat = "        {0,-" + indent.ToString() + "}{1}";

            Console.WriteLine();
            if (commands != null) {
                Console.WriteLine("    Commands:");
                foreach (var commandPair in commands) {
                    Console.WriteLine(
                        string.Format(outputFormat, commandPair.Key, commandPair.Value)
                        );
                }
                Console.WriteLine();
            }

            // print options help
            if (options != null) {
                Console.WriteLine("    Arguments & Options:");
                foreach (var optionPair in options) {
                    Console.WriteLine(
                        string.Format(outputFormat, optionPair.Key, optionPair.Value)
                        );
                }

                Console.WriteLine();
            }

            // TODO: remove exit after moving all help here
            Environment.Exit(0);
        }
    }
}
