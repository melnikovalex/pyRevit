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
        // external binary names
        private const string updaterBinaryName = "pyrevit-updater";
        private const string autocompleteBinaryName = "pyrevit-complete";
        private const string shortcutIconName = "pyRevit.ico";

        // privates:
        static string GetProcessFileName() => Process.GetCurrentProcess().MainModule.FileName;
        static string GetProcessPath() => Path.GetDirectoryName(GetProcessFileName());

        private static void PrintHeader(string header) =>
            Console.WriteLine(string.Format("==> {0}", header), Color.Green);

        private static void ReportCloneAsNoGit(PyRevitClone clone) =>
            Console.WriteLine(
                string.Format("Clone \"{0}\" is a deployment and is not a git repo.",
                clone.Name)
                );

        private static bool IsRunningInsideClone(PyRevitClone clone) =>
            GetProcessPath().NormalizeAsPath().Contains(clone.ClonePath.NormalizeAsPath());

        private static void UpdateFromOutsideAndClose(PyRevitClone clone) {
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

        // private extensions and bundles
        private static string GetExtensionTemplate(PyRevitExtensionTypes extType, string templatesDir = null) {
            templatesDir = templatesDir != null ? templatesDir : Path.Combine(GetProcessPath(), "templates");
            if (CommonUtils.VerifyPath(templatesDir)) {
                var extTempPath =
                    Path.Combine(templatesDir, "template" + PyRevitExtension.GetExtensionDirExt(extType));
                if (CommonUtils.VerifyPath(extTempPath))
                    return extTempPath;
            }
            else
                throw new pyRevitException(
                    string.Format("Templates directory does not exist at \"{0}\"", templatesDir)
                    );


            return null;
        }

        private static string GetBundleTemplate(PyRevitBundleTypes bundleType, string templatesDir = null) {
            templatesDir = templatesDir != null ? templatesDir : Path.Combine(GetProcessPath(), "templates");
            if (CommonUtils.VerifyPath(templatesDir)) {
                var bundleTempPath =
                    Path.Combine(templatesDir, "template" + PyRevitBundle.GetBundleDirExt(bundleType));
                if (CommonUtils.VerifyPath(bundleTempPath))
                    return bundleTempPath;
            }
            else
                throw new pyRevitException(
                    string.Format("Templates directory does not exist at \"{0}\"", templatesDir)
                    );

            return null;
        }


        // private fileinfo
        // print info on a revit model
        private static void PrintModelInfo(RevitModelFile model) {
            Console.WriteLine(string.Format("Created in: {0} ({1}({2}))",
                                model.RevitProduct.ProductName,
                                model.RevitProduct.BuildNumber,
                                model.RevitProduct.BuildTarget));
            Console.WriteLine(string.Format("Workshared: {0}", model.IsWorkshared ? "Yes" : "No"));
            if (model.IsWorkshared)
                Console.WriteLine(string.Format("Central Model Path: {0}", model.CentralModelPath));
            Console.WriteLine(string.Format("Last Saved Path: {0}", model.LastSavedPath));
            Console.WriteLine(string.Format("Document Id: {0}", model.UniqueId));
            Console.WriteLine(string.Format("Open Workset Settings: {0}", model.OpenWorksetConfig));
            Console.WriteLine(string.Format("Document Increment: {0}", model.DocumentIncrement));

            if (model.IsFamily) {
                Console.WriteLine("Model is a Revit Family!");
                Console.WriteLine(string.Format("Category Name: {0}", model.CategoryName));
                Console.WriteLine(string.Format("Host Category Name: {0}", model.HostCategoryName));
            }
        }

        // export model info to csv
        private static void ExportModelInfoToCSV(IEnumerable<RevitModelFile> models,
                                                 string outputCSV,
                                                 List<(string, string)> errorList = null) {
            logger.Info(string.Format("Building CSV data to \"{0}\"", outputCSV));
            var csv = new StringBuilder();
            csv.Append(
                "filepath,productname,buildnumber,isworkshared,centralmodelpath,lastsavedpath,uniqueid,error\n"
                );
            foreach (var model in models) {
                var data = new List<string>() {
                    string.Format("\"{0}\"", model.FilePath),
                    string.Format("\"{0}\"", model.RevitProduct != null ? model.RevitProduct.ProductName : ""),
                    string.Format("\"{0}\"", model.RevitProduct != null ? model.RevitProduct.BuildNumber : ""),
                    string.Format("\"{0}\"", model.IsWorkshared ? "True" : "False"),
                    string.Format("\"{0}\"", model.CentralModelPath),
                    string.Format("\"{0}\"", model.LastSavedPath),
                    string.Format("\"{0}\"", model.UniqueId.ToString()),
                    ""
                };

                csv.Append(string.Join(",", data) + "\n");
            }

            // write list of files with errors
            logger.Debug("Adding errors to \"{0}\"", outputCSV);
            foreach (var errinfo in errorList)
                csv.Append(string.Format("\"{0}\",,,,,,,\"{1}\"\n", errinfo.Item1, errinfo.Item2));

            logger.Info(string.Format("Writing results to \"{0}\"", outputCSV));
            File.WriteAllText(outputCSV, csv.ToString());
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

        // handle releases
        internal static void
        PrintReleases(string searchPattern, bool latest = false, bool printReleaseNotes = false, bool listPreReleases = false) {
            List<PyRevitRelease> releasesToList = new List<PyRevitRelease>();

            // determine latest release
            if (latest) {
                var latestRelease = PyRevitRelease.GetLatestRelease(includePreRelease: listPreReleases);

                if (latestRelease == null)
                    throw new pyRevitException("Can not determine latest release.");

                releasesToList.Add(latestRelease);
            }
            else {
                if (searchPattern != null)
                    releasesToList = PyRevitRelease.FindReleases(searchPattern, includePreRelease: listPreReleases);
                else
                    releasesToList = PyRevitRelease.GetReleases().Where(r => r.IsPyRevitRelease).ToList();
            }

            foreach (var prelease in releasesToList) {
                Console.WriteLine(prelease);
                if (printReleaseNotes)
                    Console.WriteLine(prelease.ReleaseNotes.Indent(1));
            }

        }

        internal static void
        OpenReleasePage(string searchPattern, bool latest = false, bool listPreReleases = false) {
            PyRevitRelease matchedRelease = null;
            // determine latest release
            if (latest) {
                matchedRelease = PyRevitRelease.GetLatestRelease(includePreRelease: listPreReleases);

                if (matchedRelease == null)
                    throw new pyRevitException("Can not determine latest release.");
            }
            // or find first release matching given pattern
            else if (searchPattern != null) {
                matchedRelease = PyRevitRelease.FindReleases(searchPattern, includePreRelease: listPreReleases).First();
                if (matchedRelease == null)
                    throw new pyRevitException(
                        string.Format("No release matching \"{0}\" were found.", searchPattern)
                        );
            }

            CommonUtils.OpenUrl(matchedRelease.Url);
        }

        internal static void
        DownloadReleaseAsset(PyRevitReleaseAssetType assetType, string destPath, string searchPattern, bool latest = false, bool listPreReleases = false) {
            // get dest path
            if (destPath == null)
                throw new pyRevitException("Destination path is not specified.");

            PyRevitRelease matchedRelease = null;
            // determine latest release
            if (latest) {
                matchedRelease = PyRevitRelease.GetLatestRelease(includePreRelease: listPreReleases);

                if (matchedRelease == null)
                    throw new pyRevitException("Can not determine latest release.");

            }
            // or find first release matching given pattern
            else {
                if (searchPattern != null)
                    matchedRelease = PyRevitRelease.FindReleases(searchPattern, includePreRelease: listPreReleases).First();

                if (matchedRelease == null)
                    throw new pyRevitException(
                        string.Format("No release matching \"{0}\" were found.", searchPattern)
                        );
            }

            // grab download url
            string downloadUrl = null;
            switch (assetType) {
                case PyRevitReleaseAssetType.Archive: downloadUrl = matchedRelease.ArchiveUrl; break;
                case PyRevitReleaseAssetType.Installer: downloadUrl = matchedRelease.InstallerUrl; break;
                case PyRevitReleaseAssetType.Unknown: downloadUrl = null; break;
            }

            if (downloadUrl != null) {
                logger.Debug("Downloading release package from \"{0}\"", downloadUrl);

                // ensure destpath is to a file
                if (CommonUtils.VerifyPath(destPath))
                    destPath = Path.Combine(destPath, Path.GetFileName(downloadUrl)).NormalizeAsPath();
                logger.Debug("Saving package to \"{0}\"", destPath);

                // download file and report
                CommonUtils.DownloadFile(downloadUrl, destPath);
                Console.WriteLine(
                    string.Format("Downloaded package to \"{0}\"", destPath)
                    );
            }
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
            PrintClones();
            PrintAttachments();
            PrintExtensions();
            PrintExtensionSearchPaths();
            PrintExtensionLookupSources();
            PrintRevits();
            PrintRevits(running: true);
            PrinUserEnv();
        }

        internal static void
        PrintClones() {
            PrintHeader("Registered Clones (full git repos)");
            var clones = PyRevit.GetRegisteredClones().OrderBy(x => x.Name);
            foreach (var clone in clones.Where(c => c.IsRepoDeploy))
                Console.WriteLine(clone);

            PrintHeader("Registered Clones (deployed from archive)");
            foreach (var clone in clones.Where(c => !c.IsRepoDeploy))
                Console.WriteLine(clone);
        }

        internal static void
        PrintAttachments(int revitYear = 0) {
            PrintHeader("Attachments");
            foreach (var attachment in PyRevit.GetAttachments().OrderByDescending(x => x.Product.Version)) {
                if (attachment.Clone != null && attachment.Engine != null) {
                    if (revitYear == 0)
                        Console.WriteLine(attachment);
                    else if (revitYear == attachment.Product.ProductYear)
                        Console.WriteLine(attachment);
                }
                else
                    logger.Error(
                        string.Format("pyRevit is attached to Revit {0} but can not determine the clone and engine",
                                      attachment.Product.ProductYear)
                        );
            }
        }

        internal static void
        PrintExtensions(IEnumerable<PyRevitExtension> extList = null, string headerPrefix = "Installed") {
            if (extList == null)
                extList = PyRevit.GetInstalledExtensions();

            PrintHeader(string.Format("{0} Extensions", headerPrefix));
            foreach (PyRevitExtension ext in extList.OrderBy(x => x.Name))
                Console.WriteLine(ext);
        }

        internal static void
        PrintExtensionDefinitions(string searchPattern, string headerPrefix = "Registered") {
            PrintHeader(string.Format("{0} Extensions", headerPrefix));
            foreach (PyRevitExtensionDefinition ext in PyRevit.LookupRegisteredExtensions(searchPattern))
                Console.WriteLine(ext);
        }

        internal static void
        PrintExtensionSearchPaths() {
            PrintHeader("Default Extension Search Path");
            Console.WriteLine(PyRevit.pyRevitDefaultExtensionsPath);
            PrintHeader("Extension Search Paths");
            foreach (var searchPath in PyRevit.GetRegisteredExtensionSearchPaths())
                Console.WriteLine(searchPath);
        }

        internal static void
        PrintExtensionLookupSources() {
            PrintHeader("Extension Sources - Default");
            Console.WriteLine(PyRevit.GetDefaultExtensionLookupSource());
            PrintHeader("Extension Sources - Additional");
            foreach (var extLookupSrc in PyRevit.GetRegisteredExtensionLookupSources())
                Console.WriteLine(extLookupSrc);
        }

        internal static void
        PrintRevits(bool running = false) {
            if (running) {
                PrintHeader("Running Revit Instances");
                foreach (var revit in RevitController.ListRunningRevits().OrderByDescending(x => x.RevitProduct.Version))
                    Console.WriteLine(revit);
            }
            else {
                PrintHeader("Installed Revits");
                foreach (var revit in RevitProduct.ListInstalledProducts().OrderByDescending(x => x.Version))
                    Console.WriteLine(revit);
            }
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
        CreateClone(string cloneName, string deployName, string branchName, string source, string destPath) {
            if (cloneName != null) {
                PyRevit.Clone(
                    cloneName,
                    deploymentName: deployName,
                    branchName: branchName,
                    repoOrArchivePath: source,
                    destPath: destPath
                    );
            }
        }

        internal static void
        PrintCloneInfo(string cloneName) {
            PyRevitClone clone = PyRevit.GetRegisteredClone(cloneName);
            if (clone != null) {
                PrintHeader("Clone info");
                Console.WriteLine(clone);
            }
        }

        internal static void
        OpenClone(string cloneName) {
            PyRevitClone clone = PyRevit.GetRegisteredClone(cloneName);
            if (clone != null)
                CommonUtils.OpenInExplorer(clone.ClonePath);
        }

        internal static void
        RegisterClone(string cloneName, string clonePath) {
            if (clonePath != null)
                PyRevit.RegisterClone(cloneName, clonePath);
        }

        internal static void
        ForgetClone(bool allClones, string cloneName) {
            if (allClones)
                PyRevit.UnregisterAllClones();
            else
                PyRevit.UnregisterClone(
                    PyRevit.GetRegisteredClone(cloneName)
                    );
        }

        internal static void
        RenameClone(string cloneName, string cloneNewName) {
            if (cloneNewName != null) {
                PyRevit.RenameClone(cloneName, cloneNewName);
            }
        }

        internal static void
        DeleteClone(bool allClones, string cloneName, bool clearConfigs) {
            if (allClones)
                PyRevit.DeleteAllClones(clearConfigs: clearConfigs);
            else {
                if (cloneName != null) {
                    var clone = PyRevit.GetRegisteredClone(cloneName);
                    if (clone != null)
                        PyRevit.Delete(clone, clearConfigs: clearConfigs);
                }
            }
        }

        internal static void
        GetSetCloneBranch(string cloneName, string branchName) {
            if (cloneName != null) {
                var clone = PyRevit.GetRegisteredClone(cloneName);
                if (clone != null) {
                    if (clone.IsRepoDeploy) {
                        if (branchName != null) {
                            clone.SetBranch(branchName);
                        }
                        else {
                            Console.WriteLine(string.Format("Clone \"{0}\" is on branch \"{1}\"",
                                                             clone.Name, clone.Branch));
                        }
                    }
                    else
                        ReportCloneAsNoGit(clone);
                }
            }
        }

        internal static void
        GetSetCloneTag(string cloneName, string tagName) {
            if (cloneName != null) {
                var clone = PyRevit.GetRegisteredClone(cloneName);
                if (clone != null) {
                    // get version for git clones
                    if (clone.IsRepoDeploy) {
                        if (tagName != null) {
                            clone.SetTag(tagName);
                        }
                        else {
                            logger.Error("Version finder not yet implemented for git clones.");
                            // TODO: grab version from repo (last tag?)
                        }
                    }
                    // get version for other clones
                    else {
                        if (tagName != null) {
                            logger.Error("Version setter not yet implemented for clones.");
                            // TODO: set version for archive clones?
                        }
                        else {
                            Console.WriteLine(clone.ModuleVersion);
                        }
                    }
                }
            }
        }

        internal static void
        GetSetCloneCommit(string cloneName, string commitHash) {
            if (cloneName != null) {
                var clone = PyRevit.GetRegisteredClone(cloneName);
                if (clone != null) {
                    if (clone.IsRepoDeploy) {
                        if (commitHash != null) {
                            clone.SetCommit(commitHash);
                        }
                        else {
                            Console.WriteLine(string.Format("Clone \"{0}\" is on commit \"{1}\"",
                                                             clone.Name, clone.Commit));
                        }
                    }
                    else
                        ReportCloneAsNoGit(clone);
                }
            }
        }

        internal static void
        GetSetCloneOrigin(string cloneName, string originUrl, bool reset) {
            if (cloneName != null) {
                var clone = PyRevit.GetRegisteredClone(cloneName);
                if (clone != null) {
                    if (clone.IsRepoDeploy) {
                        if (originUrl != null || reset) {
                            string newUrl =
                                reset ? PyRevitConsts.OriginalRepoPath : originUrl;
                            clone.SetOrigin(newUrl);
                        }
                        else {
                            Console.WriteLine(string.Format("Clone \"{0}\" origin is at \"{1}\"",
                                                            clone.Name, clone.Origin));
                        }
                    }
                    else
                        ReportCloneAsNoGit(clone);
                }
            }
        }

        internal static void
        PrintCloneDeployments(string cloneName) {
            if (cloneName != null) {
                var clone = PyRevit.GetRegisteredClone(cloneName);
                if (clone != null) {
                    PrintHeader(string.Format("Deployments for \"{0}\"", clone.Name));
                    foreach (var dep in clone.GetConfiguredDeployments()) {
                        Console.WriteLine(string.Format("\"{0}\" deploys:", dep.Name));
                        foreach (var path in dep.Paths)
                            Console.WriteLine("    " + path);
                        Console.WriteLine();
                    }
                }
            }
        }

        internal static void
        PrintCloneEngines(string cloneName) {
            if (cloneName != null) {
                var clone = PyRevit.GetRegisteredClone(cloneName);
                if (clone != null) {
                    PrintHeader(string.Format("Deployments for \"{0}\"", clone.Name));
                    foreach (var engine in clone.GetConfiguredEngines()) {
                        Console.WriteLine(engine);
                    }
                }
            }
        }

        internal static void
        UpdateClone(bool allClones, string cloneName) {
            // TODO: ask for closing running Revits

            // prepare a list of clones to be updated
            var targetClones = new List<PyRevitClone>();
            // separate the clone that this process might be running from
            // this is used to update this clone from outside since the dlls will be locked
            PyRevitClone myClone = null;

            // all clones
            if (allClones) {
                foreach (var clone in PyRevit.GetRegisteredClones())
                    if (IsRunningInsideClone(clone))
                        myClone = clone;
                    else
                        targetClones.Add(clone);
            }
            // or single clone
            else {
                if (cloneName != null) {
                    var clone = PyRevit.GetRegisteredClone(cloneName);
                    if (IsRunningInsideClone(clone))
                        myClone = clone;
                    else
                        targetClones.Add(clone);
                }
            }

            // update clones that do not include this process
            foreach (var clone in targetClones) {
                logger.Debug("Updating clone \"{0}\"", clone.Name);
                PyRevit.Update(clone);
            }

            // now update myClone if any, as last step
            if (myClone != null)
                UpdateFromOutsideAndClose(myClone);

        }

        internal static void
        AttachClone(string cloneName,
                    bool latest, bool dynamoSafe, string engineVersion,
                    string revitYear, bool installed, bool attached,
                    bool allUsers) {
            var clone = PyRevit.GetRegisteredClone(cloneName);
            if (clone != null) {
                // grab engine version
                int engineVer = 0;
                int.TryParse(engineVersion, out engineVer);

                if (latest)
                    engineVer = 0;
                else if (dynamoSafe)
                    engineVer = PyRevitConsts.ConfigsDynamoCompatibleEnginerVer;

                // decide targets revits to attach to
                int revitYearNumber = 0;
                if (installed)
                    foreach (var revit in RevitProduct.ListInstalledProducts())
                        PyRevit.Attach(revit.FullVersion.Major, clone, engineVer: engineVer, allUsers: allUsers);
                else if (attached)
                    foreach (var attachment in PyRevit.GetAttachments())
                        PyRevit.Attach(attachment.Product.ProductYear, clone, engineVer: engineVer, allUsers: allUsers);
                else if (int.TryParse(revitYear, out revitYearNumber))
                    PyRevit.Attach(revitYearNumber, clone, engineVer: engineVer, allUsers: allUsers);
            }
        }

        internal static void
        DetachClone(string revitYear, bool all) {
            if (revitYear != null) {
                int revitYearNumber = 0;
                if (int.TryParse(revitYear, out revitYearNumber))
                    PyRevit.Detach(revitYearNumber);
                else
                    throw new pyRevitException(string.Format("Invalid Revit year \"{0}\"", revitYear));
            }
            else if (all)
                PyRevit.DetachAll();
        }

        internal static void
        ListAttachments(string revitYear) {
            if (revitYear != null) {
                int revitYearNumber = 0;
                if (int.TryParse(revitYear, out revitYearNumber))
                    PrintAttachments(revitYear: revitYearNumber);
                else
                    throw new pyRevitException(string.Format("Invalid Revit year \"{0}\"", revitYear));
            }
            else
                PrintAttachments();
        }

        internal static void
        SwitchAttachment(string cloneName, string revitYear) {
            var clone = PyRevit.GetRegisteredClone(cloneName);
            if (clone != null) {
                if (revitYear != null) {
                    int revitYearNumber = 0;
                    if (int.TryParse(revitYear, out revitYearNumber)) {
                        var attachment = PyRevit.GetAttached(revitYearNumber);
                        if (attachment != null)
                            PyRevit.Attach(attachment.Product.ProductYear,
                                           clone,
                                           engineVer: attachment.Engine.Version,
                                           allUsers: attachment.AllUsers);
                        else
                            throw new pyRevitException(
                                string.Format("Can not determine existing attachment for Revit \"{0}\"",
                                              revitYear)
                                );
                    }
                    else
                        throw new pyRevitException(string.Format("Invalid Revit year \"{0}\"", revitYear));
                }
                else {
                    // read current attachments and reattach using the same config with the new clone
                    foreach (var attachment in PyRevit.GetAttachments()) {
                        PyRevit.Attach(attachment.Product.ProductYear,
                                       clone,
                                       engineVer: attachment.Engine.Version,
                                       allUsers: attachment.AllUsers);
                    }
                }
            }
        }

        internal static void
        Extend(string extName, string destPath, string branchName) {
            var ext = PyRevit.FindRegisteredExtension(extName);
            if (ext != null) {
                logger.Debug("Matching extension found \"{0}\"", ext.Name);
                PyRevit.InstallExtension(ext, destPath, branchName);
            }
            else {
                if (Errors.LatestError == ErrorCodes.MoreThanOneItemMatched)
                    throw new pyRevitException(
                        string.Format("More than one extension matches the name \"{0}\"",
                                        extName));
                else
                    throw new pyRevitException(
                        string.Format("Not valid extension name or repo url \"{0}\"",
                                        extName));
            }

        }

        internal static void
        Extend(bool ui, bool lib, bool run, string extName, string destPath, string repoUrl, string branchName) {
            PyRevitExtensionTypes extType = PyRevitExtensionTypes.Unknown;
            if (ui)
                extType = PyRevitExtensionTypes.UIExtension;
            else if (lib)
                extType = PyRevitExtensionTypes.LibraryExtension;
            else if (run)
                extType = PyRevitExtensionTypes.RunnerExtension;

            PyRevit.InstallExtension(extName, extType, repoUrl, destPath, branchName);
        }

        internal static void
        ProcessExtensionInfoCommands(string extName, bool info, bool help, bool open) {
            if (extName != null) {
                var ext = PyRevit.FindRegisteredExtension(extName);
                if (Errors.LatestError == ErrorCodes.MoreThanOneItemMatched)
                    logger.Warn("More than one extension matches the search pattern \"{0}\"", extName);
                else {
                    if (info)
                        Console.WriteLine(ext.ToString());
                    else if (help)
                        Process.Start(ext.Website);
                    else if (open) {
                        var instExt = PyRevit.GetInstalledExtension(extName);
                        CommonUtils.OpenInExplorer(instExt.InstallPath);
                    }
                }
            }
        }

        internal static void
        DeleteExtension(string extName) {
            PyRevit.UninstallExtension(extName);
        }

        internal static void
        GetSetExtensionOrigin(string extName, string originUrl, bool reset) {
            if (extName != null) {
                var extension = PyRevit.GetInstalledExtension(extName);
                if (extension != null) {
                    if (reset) {
                        var ext = PyRevit.FindRegisteredExtension(extension.Name);
                        if (ext != null)
                            extension.SetOrigin(ext.Url);
                        else
                            throw new pyRevitException(
                                string.Format("Can not find the original url in the extension " +
                                              "database for extension \"{0}\"",
                                              extension.Name));
                    }
                    else if (originUrl != null) {
                        extension.SetOrigin(originUrl);
                    }
                    else {
                        Console.WriteLine(string.Format("Extension \"{0}\" origin is at \"{1}\"",
                                                        extension.Name, extension.Origin));
                    }
                }
            }
        }

        internal static void
        ForgetAllExtensionPaths(bool all, string searchPath) {
            if (all)
                foreach (string regSearchPath in PyRevit.GetRegisteredExtensionSearchPaths())
                    PyRevit.UnregisterExtensionSearchPath(regSearchPath);
            else
                PyRevit.UnregisterExtensionSearchPath(searchPath);
        }

        internal static void
        AddExtensionPath(string searchPath) {
            if (searchPath != null)
                PyRevit.RegisterExtensionSearchPath(searchPath);
        }

        internal static void
        ToggleExtension(bool enable, string extName) {
            if (extName != null) {
                if (enable)
                    PyRevit.EnableExtension(extName);
                else
                    PyRevit.DisableExtension(extName);
            }
        }

        internal static void
        ForgetExtensionLookupSources(bool all, string lookupPath) {
            if (all)
                PyRevit.UnregisterAllExtensionLookupSources();
            else if (lookupPath != null)
                PyRevit.UnregisterExtensionLookupSource(lookupPath);
        }

        internal static void
        AddExtensionLookupSource(string lookupPath) {
            if (lookupPath != null)
                PyRevit.RegisterExtensionLookupSource(lookupPath);
        }

        internal static void
        UpdateExtension(string extName) {
            if (extName != null) {
                PyRevit.UpdateExtension(extName);
            }
        }

        internal static void
        KillAllRevits(string revitYear) {
            int revitYearNumber = 0;
            if (int.TryParse(revitYear, out revitYearNumber))
                RevitController.KillRunningRevits(revitYearNumber);
            else
                RevitController.KillAllRunningRevits();
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
