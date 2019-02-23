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
        internal static string UsagePatterns => Resources.UsagePatterns;
        private static string PrettyHelp => Resources.PrettyHelp;

        internal static void PrintCommandHelpAndExit(PyRevitCLICommandType commandType) {
            switch (commandType) {
                
                // pyrevit
                case PyRevitCLICommandType.Main:
                    Console.WriteLine(PrettyHelp);
                    break;
                
                // pyrevit help
                case PyRevitCLICommandType.Help:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "help" },
                        title: "Open help in default browser"
                        );
                    break;

                // pyrevit releases
                case PyRevitCLICommandType.Releases:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "releases" },
                        title: "Info on pyRevit Releases",
                        commands: new Dictionary<string, string>() {
                            { "open",                   "Open release page in default browser" },
                            { "download installer",     "Download EXE installer for given release, if exists" },
                            { "download archive",       "Download Zip archive for given release" }
                        },
                        options: new Dictionary<string, string>() {
                            { "latest",                 "Match latest release only" },
                            { "<search_pattern>",       "Pattern to search releases" },
                            { "<dest_path>",            "Destination file or directory to download to" },
                            { "--pre",                  "Include pre-releases in the search" },
                            { "--notes",                "Print release notes" }
                        });
                    break;

                // pyrevit env
                case PyRevitCLICommandType.Env:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "env" },
                        title: "Print environment information.",
                        options: new Dictionary<string, string>() {
                            { "--json",                 "Switch output format to json" },
                        });
                    break;

                // pyrevit clone
                case PyRevitCLICommandType.Clone:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "clone" },
                        title: "Create a clone of pyRevit on this machine",
                        options: new Dictionary<string, string>() {
                            { "<clone_name>",           "Name of this new clone" },
                            { "<deployment_name>",      "Deployment configuration to deploy from" },
                            { "<dest_path>",            "Clone destination directory" },
                            { "<archive_url>",          "Clone source Zip archive url or path" },
                            { "<repo_url>",             "Clone source git repo url" },
                            { "<branch_name>",          "Branch to clone from" },
                        });
                    break;

                // pyrevit clones
                case PyRevitCLICommandType.Clones:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "clones" },
                        title: "Manage pyRevit clones",
                        commands: new Dictionary<string, string>() {
                            { "info",                   "Print info about clone" },
                            { "open",                   "Open clone directory in file browser" },
                            { "add",                    "Register an existing clone" },
                            { "forget",                 "Forget a registered clone" },
                            { "rename",                 "Rename a clone" },
                            { "delete",                 "Delete a clone" },
                            { "branch",                 "Get/Set branch of a clone deployed from git repo" },
                            { "version",                "Get/Set version of a clone deployed from git repo" },
                            { "commit",                 "Get/Set head commit of a clone deployed from git repo" },
                            { "origin",                 "Get/Set origin of a clone deployed from git repo" },
                            { "update",                 "Update clone to latest using the original source, deployment, and branch" },
                            { "deployments",            "List deployments available in a clone" },
                            { "engines",                "List engines available in a clone" },
                        },
                        options: new Dictionary<string, string>() {
                            { "<clone_name>",           "Name of target clone" },
                            { "<clone_path>",           "Path of clone" },
                            { "<clone_new_name>",       "New name of clone" },
                            { "<branch_name>",          "Clone branch to checkout" },
                            { "<tag_name>",             "Clone tag to rebase to" },
                            { "<commit_hash>",          "Clone commit rebase to" },
                            { "<origin_url>",           "New clone remote origin url" },
                            { "--reset",                "Reset remote origin url to default" },
                            { "--clearconfigs",         "Clear pyRevit configurations." },
                            { "--all",                  "All clones" },
                            { "--branch",               "Branch to clone from" },
                        });
                    break;

                // pyrevit attach
                case PyRevitCLICommandType.Attach:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "attach" },
                        title: "Attach pyRevit clone to installed Revit",
                        options: new Dictionary<string, string>() {
                            { "<clone_name>",           "Name of target clone" },
                            { "<revit_year>",           "Revit version year e.g. 2019" },
                            { "<engine_version>",       "Engine version to be used e.g. 277" },
                            { "latest",                 "Use latest engine" },
                            { "dynamosafe",             "Use latest engine that is compatible with DynamoBIM" },
                            { "--installed",            "All installed Revits" },
                            { "--attached",             "All currently attached Revits" },
                            { "--allusers",             "Attach for all users" },
                        });
                    break;

                case PyRevitCLICommandType.Detach:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "detach" },
                        title: "Detach a clone from Revit.",
                        options: new Dictionary<string, string>() {
                            { "<revit_year>",           "Revit version year e.g. 2019" },
                            { "--all",                  "All registered clones" },
                        }
                    );
                    break;

                case PyRevitCLICommandType.Attached:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "attached" },
                        title: "List all attached clones.",
                        options: new Dictionary<string, string>() {
                            { "<revit_year>",           "Revit version year e.g. 2019" },
                        }
                    );
                    break;

                case PyRevitCLICommandType.Switch:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "switch" },
                        title: "Quick switch clone of an existing attachment to another.",
                        options: new Dictionary<string, string>() {
                            { "<clone_name>",           "Name of target clone to switch to" },
                            { "<revit_year>",           "Revit version year e.g. 2019" },
                        }
                    );
                    break;

                case PyRevitCLICommandType.Extend:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "extend" },
                        title: "Create a clone of a third-party pyRevit extension on this machine"
                    );
                    break;

                case PyRevitCLICommandType.Extensions:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "extensions" },
                        title: "Manage pyRevit extensions"
                    );
                    break;

                case PyRevitCLICommandType.Revits:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "revits" },
                        title: "Manage installed and running Revits"
                    );
                    break;

                case PyRevitCLICommandType.Cli:
                    GenerateHelpFromUsagePatterns(
                        new List<string>() { "cli" },
                        title: "Manage this utility"
                    );
                    break;

            }

            // now exit
            Environment.Exit(0);
        }

        private static void GenerateHelpFromUsagePatterns(IEnumerable<string> docoptKeywords,
                                                string title,
                                                IDictionary<string, string> commands = null,
                                                IDictionary<string, string> options = null) {
            // build a help guide for a subcommand based on doctop usage entries
            Console.WriteLine(title + Environment.NewLine);
            foreach (var hline in UsagePatterns.GetLines())
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
        }
    }
}
