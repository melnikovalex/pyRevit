using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

using pyRevitLabs.Common;
using pyRevitLabs.CommonCLI;
using pyRevitLabs.Common.Extensions;
using pyRevitLabs.TargetApps.Revit;
using pyRevitLabs.Language.Properties;

using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Console = Colorful.Console;

namespace pyRevitManager {
    internal enum PyRevitCLICommandType {
        Main,
        Version,
        Help,
        Releases,
        Env,
        Clone,
        Clones,
        Attach,
        Attached,
        Detach,
        Switch,
        Extend,
        Extensions,
        Revits,
        Run,
        Init,
        Caches,
        Config,
        Configs,
        Cli
    }

    internal static class PyRevitCLICommands {
        static Logger logger = LogManager.GetCurrentClassLogger();

        // private consts:
        internal const string updaterBinaryName = "pyrevit-updater";
        internal const string autocompleteBinaryName = "pyrevit-complete";
        internal const string shortcutIconName = "pyRevit.ico";

        // privates:
        internal static string GetProcessFileName() => Process.GetCurrentProcess().MainModule.FileName;
        internal static string GetProcessPath() => Path.GetDirectoryName(GetProcessFileName());

        internal static void PrintHeader(string header) =>
            Console.WriteLine(string.Format("==> {0}", header), Color.Green);

        internal static void ReportCloneAsNoGit(PyRevitClone clone) =>
            Console.WriteLine(
                string.Format("Clone \"{0}\" is a deployment and is not a git repo.",
                clone.Name)
                );

        internal static bool IsRunningInsideClone(PyRevitClone clone) =>
            GetProcessPath().NormalizeAsPath().Contains(clone.ClonePath.NormalizeAsPath());

        internal static void UpdateFromOutsideAndClose(PyRevitClone clone) {
            logger.Debug("Updating clone \"{0}\" using outside process", clone.Name);

            // prepare outside updater
            var updaterTempBinary = updaterBinaryName + ".exe";
            var updaterBinaryPath = Path.Combine(GetProcessPath(), updaterBinaryName);
            var updaterTempPath = Path.Combine(UserEnv.UserTemp, updaterTempBinary);
            logger.Debug("Setting up \"{0}\" to \"{1}\"", updaterBinaryPath, updaterTempPath);
            File.Copy(updaterBinaryPath, updaterTempPath, overwrite: true);

            // make a updater bat file
            var updaterBATFile = Path.Combine(UserEnv.UserTemp, updaterBinaryName + ".bat");
            using (var batFile = new StreamWriter(File.Create(updaterBATFile))) {
                batFile.WriteLine("@ECHO OFF");
                batFile.WriteLine("TIMEOUT /t 1 /nobreak >NUL  2>NUL");
                batFile.WriteLine("TASKKILL /IM \"{0}\" >NUL  2>NUL", Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName));
                batFile.WriteLine("START \"\" /B \"{0}\" \"{1}\"", updaterTempPath, clone.ClonePath);
            }

            // launch update
            ProcessStartInfo updaterProcessInfo = new ProcessStartInfo(updaterBATFile);
            updaterProcessInfo.WorkingDirectory = Path.GetDirectoryName(updaterTempPath);
            updaterProcessInfo.UseShellExecute = false;
            updaterProcessInfo.CreateNoWindow = true;
            logger.Debug("Calling outside update and exiting...");
            Process.Start(updaterProcessInfo);
            // and exit self
            Environment.Exit(0);
        }

        // internals:
        // print version and check for latest
        internal static void
        PrintVersion() {
            Console.WriteLine(string.Format(StringLib.ConsoleVersionFormat, PyRevitCLI.CLIVersion.ToString()));
            if (CommonUtils.CheckInternetConnection()) {
                var latestVersion = PyRevitRelease.GetLatestCLIReleaseVersion();
                if (latestVersion != null) {
                    logger.Debug("Latest release: {0}", latestVersion);
                    if (PyRevitCLI.CLIVersion < latestVersion) {
                        Console.WriteLine(
                            string.Format(
                                "Newer v{0} is available.\nGo to {1} to download the installer.",
                                latestVersion,
                                PyRevitConsts.ReleasesUrl)
                            );
                    }
                    else
                        Console.WriteLine("You have the latest version.");
                }
                else
                    logger.Debug("Failed getting latest release list OR no CLI releases.");
            }
        }

        // open external help page
        internal static void
        OpenHelp() {
            string helpUrl = string.Format(PyRevitConsts.CLIHelpUrl, PyRevitCLI.CLIVersion.ToString());
            if (CommonUtils.VerifyUrl(helpUrl)) {
                CommonUtils.OpenUrl(
                    helpUrl,
                    logErrMsg: "Can not open online help page. Try `pyrevit --help` instead"
                    );
            }
            else
                throw new pyRevitException(
                    string.Format("Help page is not reachable for version {0}", PyRevitCLI.CLIVersion.ToString())
                    );
        }

        internal static void
        PrintEnvJson() {
            // collecet search paths
            var searchPaths = new List<string>() { PyRevit.pyRevitDefaultExtensionsPath };
            searchPaths.AddRange(PyRevit.GetRegisteredExtensionSearchPaths());

            // collect list of lookup sources
            var lookupSrc = new List<string>() { PyRevit.GetDefaultExtensionLookupSource() };
            lookupSrc.AddRange(PyRevit.GetRegisteredExtensionLookupSources());

            // create json data object
            var jsonData = new Dictionary<string, object>() {
                        { "meta", new Dictionary<string, object>() {
                                { "version", "0.1.0"}
                            }
                        },
                        { "clones", PyRevit.GetRegisteredClones() },
                        { "attachments", PyRevit.GetAttachments() },
                        { "extensions", PyRevit.GetInstalledExtensions() },
                        { "searchPaths", searchPaths },
                        { "lookupSources", lookupSrc },
                        { "installed", RevitProduct.ListInstalledProducts() },
                        { "running", RevitController.ListRunningRevits() },
                        { "pyrevitDataDir", PyRevit.pyRevitAppDataPath },
                        { "userEnv", new Dictionary<string, object>() {
                                { "osVersion", UserEnv.GetWindowsVersion() },
                                { "execUser", string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName) },
                                { "activeUser", UserEnv.GetLoggedInUserName() },
                                { "isAdmin", UserEnv.IsRunAsAdmin() },
                                { "userAppdata", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) },
                                { "latesFramework", UserEnv.GetInstalledDotNetVersion() },
                                { "targetPacks", UserEnv.GetInstalledDotnetTargetPacks() },
                                { "targetPacksCore", UserEnv.GetInstalledDotnetCoreTargetPacks() },
                                { "cliVersion", PyRevitCLI.CLIVersion },
                            }
                        },
                    };

            Console.WriteLine(
                JsonConvert.SerializeObject(
                    jsonData,
                    new JsonSerializerSettings {
                        Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args) {
                            args.ErrorContext.Handled = true;
                        },
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    })
                );
        }

        internal static void
        PrintEnvReport() {
            PyRevitCLIClonesCmd.PrintClones();
            PyRevitCLIClonesCmd.PrintAttachments();
            PyRevitCLIExtensionsCmd.PrintExtensions();
            PyRevitCLIExtensionsCmd.PrintExtensionSearchPaths();
            PyRevitCLIExtensionsCmd.PrintExtensionLookupSources();
            PyRevitCLIRevitCmd.PrintRevits();
            PyRevitCLIRevitCmd.PrintRevits(running: true);
            PrinUserEnv();
        }

        internal static void
        PrintPyRevitPaths() {
            PrintHeader("Cache Directory");
            Console.WriteLine(string.Format("\"{0}\"", PyRevit.pyRevitAppDataPath));
        }

        internal static void
        PrinUserEnv() {
            PrintHeader("User Environment");
            Console.WriteLine(UserEnv.GetWindowsVersion());
            Console.WriteLine(string.Format("Executing User: {0}\\{1}",
                                            Environment.UserDomainName, Environment.UserName));
            Console.WriteLine(string.Format("Active User: {0}", UserEnv.GetLoggedInUserName()));
            Console.WriteLine(string.Format("Adming Access: {0}", UserEnv.IsRunAsAdmin() ? "Yes" : "No"));
            Console.WriteLine(string.Format("%APPDATA%: \"{0}\"",
                                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
            Console.WriteLine(string.Format("Latest Installed .Net Framework: \"{0}\"",
                                            UserEnv.GetInstalledDotNetVersion()));
            try {
                string targetPacks = "";
                foreach (string targetPackagePath in UserEnv.GetInstalledDotnetTargetPacks())
                    targetPacks += string.Format("{0} ", Path.GetFileName(targetPackagePath));
                Console.WriteLine(string.Format("Installed .Net Target Packs: {0}", targetPacks));
            }
            catch {
                Console.WriteLine("No .Net Target Packs are installed.");
            }

            try {
                string targetPacks = "";
                foreach (string targetPackagePath in UserEnv.GetInstalledDotnetCoreTargetPacks())
                    targetPacks += string.Format("v{0} ", Path.GetFileName(targetPackagePath));
                Console.WriteLine(string.Format("Installed .Net-Core Target Packs: {0}", targetPacks));
            }
            catch {
                Console.WriteLine("No .Ne-Core Target Packs are installed.");
            }

            Console.WriteLine(string.Format("pyRevit CLI {0}", PyRevitCLI.CLIVersion.ToString()));
        }

        internal static void
        AddCLIShortcut(string shortcutName, string shortcutArgs, string shortcutDesc, bool allUsers) {
            if (shortcutName != null && shortcutArgs != null) {
                var processPath = GetProcessPath();
                var iconPath = Path.Combine(processPath, shortcutIconName);
                CommonUtils.AddShortcut(
                    shortcutName,
                    PyRevitConsts.ProductName,
                    GetProcessFileName(),
                    shortcutArgs,
                    processPath,
                    iconPath,
                    shortcutDesc,
                    allUsers: allUsers
                );
            }
        }

        internal static void
        ActivateAutoComplete() {
            var processPath = GetProcessPath();
            var installAutoCompleteCommand = Path.Combine(processPath, autocompleteBinaryName + ".exe");

            logger.Debug("Autocomplete installer is \"{0}\"", installAutoCompleteCommand);
            ProcessStartInfo updaterProcessInfo = new ProcessStartInfo(installAutoCompleteCommand);
            updaterProcessInfo.Arguments = "-install -y";
            updaterProcessInfo.WorkingDirectory = Path.GetDirectoryName(processPath);
            updaterProcessInfo.UseShellExecute = false;
            updaterProcessInfo.CreateNoWindow = true;
            logger.Debug("Calling autocomplete installer and exiting...");
            Process.Start(updaterProcessInfo);
        }
    }
}
