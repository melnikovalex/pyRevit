using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Diagnostics;

using pyRevitManager.Properties;

using pyRevitLabs.Common;
using pyRevitLabs.CommonCLI;
using pyRevitLabs.Common.Extensions;
using pyRevitLabs.TargetApps.Revit;

using DocoptNet;
using NLog;
using NLog.Config;
using NLog.Targets;

using Console = Colorful.Console;


// NOTE:
// ## Add a new command:
// 1) Update docopt usage pattern file
// 2) Add new command to PyRevitCLICommandType
// 3) Update the logic in PyRevitCLI.ProcessArguments
// 4) Add command code and make sure PyRevitCLI.ProcessArguments correctly parses the arguments
// 5) Update AppHelps to accept and print help for new command type
// 6) Make sure PyRevitCLI.ProcessArguments checks and ask for help print
// 7) Update the pyrevit-complete.go file with command completion suggestions


namespace pyRevitManager {

    internal enum PyRevitCLILogLevel {
        Quiet,
        InfoMessages,
        Debug,
    }

    internal enum PyRevitCLICommandType {
        Main,
        Version,
        Help,
        Blog,
        Docs,
        Source,
        YouTube,
        Support,
        Env,
        Clone,
        Clones,
        Attach,
        Attached,
        Switch,
        Detach,
        Extend,
        Extensions,
        Releases,
        Image,
        Images,
        Revits,
        Run,
        Init,
        Caches,
        Config,
        Configs,
        Cli
    }

    internal static class PyRevitCLI {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // uage patterns
        internal static string UsagePatterns => Resources.UsagePatterns;

        // arguments dict
        private static IDictionary<string, ValueObject> arguments = null;

        internal static bool IsVersionMode = false;
        internal static bool IsHelpMode = false;

        // cli version property
        public static Version CLIVersion => Assembly.GetExecutingAssembly().GetName().Version;

        // cli entry point:
        static void Main(string[] args) {

            // process arguments for logging level
            var argsList = new List<string>(args);

            // check for testing and set the global test flag
            if (argsList.Contains("--test")) {
                argsList.Remove("--test");
                GlobalConfigs.UnderTest = true;
            }

            // setup logger
            // process arguments for hidden debug mode switch
            PyRevitCLILogLevel logLevel = PyRevitCLILogLevel.InfoMessages;
            var config = new LoggingConfiguration();
            var logconsole = new ConsoleTarget("logconsole") { Layout = @"${level}: ${message} ${exception}" };
            config.AddTarget(logconsole);
            config.AddRule(LogLevel.Error, LogLevel.Fatal, logconsole);

            if (argsList.Contains("--verbose")) {
                argsList.Remove("--verbose");
                logLevel = PyRevitCLILogLevel.InfoMessages;
                config.AddRule(LogLevel.Info, LogLevel.Info, logconsole);
            }

            if (argsList.Contains("--debug")) {
                argsList.Remove("--debug");
                logLevel = PyRevitCLILogLevel.Debug;
                config.AddRule(LogLevel.Debug, LogLevel.Debug, logconsole);
            }


            try {
                // process docopt
                // docopt raises exception if pattern matching fails
                arguments = new Docopt().Apply(PyRevitCLIAppHelps.UsagePatterns, argsList, exit: false, help: false);

                // print active arguments in debug mode
                if (logLevel == PyRevitCLILogLevel.Debug)
                    PrintArguments(arguments);

                // setup output log
                if (arguments["--log"] != null) {
                    var logfile = new FileTarget("logfile") { FileName = arguments["--log"].Value as string };
                    config.AddTarget(logfile);
                    config.AddRuleForAllLevels(logfile);

                    arguments.Remove("--log");
                }

                // check if requesting version
                IsVersionMode = arguments["--version"].IsTrue || arguments["-V"].IsTrue;
                
                // check if requesting help
                IsHelpMode = arguments["--help"].IsTrue || arguments["-h"].IsTrue;

                // config logger
                LogManager.Configuration = config;

                try {
                    // now call methods based on inputs
                    ProcessArguments();

                    // process global error codes
                    ProcessErrorCodes();
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                // Flush and close down internal threads and timers
                LogManager.Shutdown();
            }
            catch (Exception ex) {
                // when docopt fails, print help
                logger.Debug("Arg processing failed. | {0}", ex.Message);
                PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Main);
            }
        }

        // cli argument processor
        private static void ProcessArguments() {
            if (IsVersionMode) PyRevitCLIAppCmds.PrintVersion();

            else if (all("help")) PyRevitCLIAppHelps.OpenHelp();

            else if (all("blog")) CommonUtils.OpenUrl(PyRevitConsts.BlogsUrl);

            else if (all("docs")) CommonUtils.OpenUrl(PyRevitConsts.DocsUrl);

            else if (all("source")) CommonUtils.OpenUrl(PyRevitConsts.SourceRepoUrl);

            else if (all("youtube")) CommonUtils.OpenUrl(PyRevitConsts.YoutubeUrl);

            else if (all("support")) CommonUtils.OpenUrl(PyRevitConsts.SupportRepoUrl);

            else if (all("env")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Env);
                else
                    PyRevitCLIAppCmds.MakeEnvReport(json: arguments["--json"].IsTrue);
            }

            else if (all("clone")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Clone);
                else
                    PyRevitCLICloneCmds.CreateClone(
                        cloneName: TryGetValue("<clone_name>"),
                        deployName: TryGetValue("<deployment_name>"),
                        branchName: TryGetValue("--branch"),
                        source: TryGetValue("--source"),
                        imageName: TryGetValue("--image"),
                        destPath: TryGetValue("--dest")
                    );
            }

            else if (all("clones")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Clones);

                else if (all("info"))
                    PyRevitCLICloneCmds.PrintCloneInfo(TryGetValue("<clone_name>"));

                else if (all("open"))
                    PyRevitCLICloneCmds.OpenClone(TryGetValue("<clone_name>"));

                else if (all("add"))
                    PyRevitCLICloneCmds.RegisterClone(
                        TryGetValue("<clone_name>"),
                        TryGetValue("<clone_path>")
                        );

                else if (all("forget"))
                    PyRevitCLICloneCmds.ForgetClone(
                        allClones: arguments["--all"].IsTrue,
                        cloneName: TryGetValue("<clone_name>")
                        );

                else if (all("rename"))
                    PyRevitCLICloneCmds.RenameClone(
                        cloneName: TryGetValue("<clone_name>"),
                        cloneNewName: TryGetValue("<clone_new_name>")
                        );

                else if (all("delete"))
                    PyRevitCLICloneCmds.DeleteClone(
                        allClones: arguments["--all"].IsTrue,
                        cloneName: TryGetValue("<clone_name>"),
                        clearConfigs: arguments["--clearconfigs"].IsTrue
                        );

                else if (all("branch"))
                    PyRevitCLICloneCmds.GetSetCloneBranch(
                       cloneName: TryGetValue("<clone_name>"),
                       branchName: TryGetValue("<branch_name>")
                       );

                else if (all("version"))
                    PyRevitCLICloneCmds.GetSetCloneTag(
                       cloneName: TryGetValue("<clone_name>"),
                       tagName: TryGetValue("<tag_name>")
                       );

                else if (all("commit"))
                    PyRevitCLICloneCmds.GetSetCloneCommit(
                       cloneName: TryGetValue("<clone_name>"),
                       commitHash: TryGetValue("<commit_hash>")
                       );

                else if (all("origin"))
                    PyRevitCLICloneCmds.GetSetCloneOrigin(
                       cloneName: TryGetValue("<clone_name>"),
                       originUrl: TryGetValue("<origin_url>"),
                       reset: arguments["--reset"].IsTrue
                       );

                else if (all("deployments"))
                    PyRevitCLICloneCmds.PrintCloneDeployments(TryGetValue("<clone_name>"));

                else if (all("engines"))
                    PyRevitCLICloneCmds.PrintCloneEngines(TryGetValue("<clone_name>"));

                else if (all("update"))
                    PyRevitCLICloneCmds.UpdateClone(
                        allClones: arguments["--all"].IsTrue,
                        cloneName: TryGetValue("<clone_name>")
                        );

                else
                    PyRevitCLICloneCmds.PrintClones();
            }

            else if (all("attach")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Attach);
                else
                    PyRevitCLICloneCmds.AttachClone(
                        cloneName: TryGetValue("<clone_name>"),
                        latest: arguments["latest"].IsTrue,
                        dynamoSafe: arguments["dynamosafe"].IsTrue,
                        engineVersion: TryGetValue("<engine_version>"),
                        revitYear: TryGetValue("<revit_year>"),
                        installed: arguments["--installed"].IsTrue,
                        attached: arguments["--attached"].IsTrue,
                        allUsers: arguments["--allusers"].IsTrue
                        );
            }

            else if (all("detach")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Detach);
                else
                    PyRevitCLICloneCmds.DetachClone(
                        revitYear: TryGetValue("<revit_year>"),
                        all: arguments["--all"].IsTrue
                        );
            }

            else if (all("attached")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Attached);
                else
                    PyRevitCLICloneCmds.ListAttachments(revitYear: TryGetValue("<revit_year>"));
            }

            else if (all("switch")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Switch);
                else
                    PyRevitCLICloneCmds.SwitchAttachment(
                        cloneName: TryGetValue("<clone_name>"),
                        revitYear: TryGetValue("<revit_year>")
                        );
            }

            else if (all("extend")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Extend);

                else if (any("ui", "lib", "run"))
                    PyRevitCLIExtensionCmds.Extend(
                        ui: arguments["ui"].IsTrue,
                        lib: arguments["lib"].IsTrue,
                        run: arguments["run"].IsTrue,
                        extName: TryGetValue("<extension_name>"),
                        destPath: TryGetValue("--dest"),
                        repoUrl: TryGetValue("<repo_url>"),
                        branchName: TryGetValue("--branch")
                        );

                else
                    PyRevitCLIExtensionCmds.Extend(
                        extName: TryGetValue("<extension_name>"),
                        destPath: TryGetValue("--dest"),
                        branchName: TryGetValue("--branch")
                        );
            }

            else if (all("extensions")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Extensions);

                else if (all("search"))
                    PyRevitCLIExtensionCmds.PrintExtensionDefinitions(
                        searchPattern: TryGetValue("<search_pattern>"),
                        headerPrefix: "Matched"
                    );

                else if (any("info", "help", "open"))
                    PyRevitCLIExtensionCmds.ProcessExtensionInfoCommands(
                        extName: TryGetValue("<extension_name>"),
                        info: arguments["info"].IsTrue,
                        help: arguments["help"].IsTrue,
                        open: arguments["open"].IsTrue
                    );

                else if (all("delete"))
                    PyRevitCLIExtensionCmds.DeleteExtension(TryGetValue("<extension_name>"));

                else if (all("origin"))
                    PyRevitCLIExtensionCmds.GetSetExtensionOrigin(
                        extName: TryGetValue("<extension_name>"),
                        originUrl: TryGetValue("<origin_url>"),
                        reset: arguments["--reset"].IsTrue
                        );

                else if (all("paths")) {

                    if (all("add"))
                        PyRevitCLIExtensionCmds.AddExtensionPath(
                            searchPath: TryGetValue("<extensions_path>")
                        );

                    else if (all("forget"))
                        PyRevitCLIExtensionCmds.ForgetAllExtensionPaths(
                            all: arguments["--all"].IsTrue,
                            searchPath: TryGetValue("<extensions_path>")
                        );

                    else
                        PyRevitCLIExtensionCmds.PrintExtensionSearchPaths();
                }

                else if (any("enable", "disable"))
                    PyRevitCLIExtensionCmds.ToggleExtension(
                        enable: arguments["enable"].IsTrue,
                        extName: TryGetValue("<extension_name>")
                    );

                else if (all("sources")) {

                    if (all("add"))
                        PyRevitCLIExtensionCmds.AddExtensionLookupSource(
                            lookupPath: TryGetValue("<source_json_or_url>")
                        );

                    else if (all("forget"))
                        PyRevitCLIExtensionCmds.ForgetExtensionLookupSources(
                            all: arguments["--all"].IsTrue,
                            lookupPath: TryGetValue("<source_json_or_url>")
                        );

                    else
                        PyRevitCLIExtensionCmds.PrintExtensionLookupSources();
                }

                else if (all("update"))
                    PyRevitCLIExtensionCmds.UpdateExtension(
                        all: arguments["--all"].IsTrue,
                        extName: TryGetValue("<extension_name>")
                    );

                else
                    PyRevitCLIExtensionCmds.PrintExtensions();
            }

            else if (all("releases")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Releases);

                else if (all("open"))
                    PyRevitCLIReleaseCmds.OpenReleasePage(
                        searchPattern: TryGetValue("<search_pattern>"),
                        latest: arguments["latest"].IsTrue,
                        listPreReleases: arguments["--pre"].IsTrue
                        );

                else if (all("download"))
                    PyRevitCLIReleaseCmds.DownloadReleaseAsset(
                        arguments["archive"].IsTrue ? PyRevitReleaseAssetType.Archive : PyRevitReleaseAssetType.Installer,
                        destPath: TryGetValue("--dest"),
                        searchPattern: TryGetValue("<search_pattern>"),
                        latest: arguments["latest"].IsTrue,
                        listPreReleases: arguments["--pre"].IsTrue
                        );

                else
                    PyRevitCLIReleaseCmds.PrintReleases(
                        searchPattern: TryGetValue("<search_pattern>"),
                        latest: arguments["latest"].IsTrue,
                        printReleaseNotes: arguments["--notes"].IsTrue,
                        listPreReleases: arguments["--pre"].IsTrue
                        );
            }

            else if (all("image"))
                throw new NotImplementedException();

            else if (all("images"))
                throw new NotImplementedException();

            else if (all("revits")) {
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Revits);

                else if (all("killall"))
                    PyRevitCLIRevitCmds.KillAllRevits(
                        revitYear: TryGetValue("<revit_year>")
                    );

                else
                    PyRevitCLIRevitCmds.PrintRevits(running: arguments["--installed"].IsFalse);
            }


            else if (all("revits", "fileinfo")) {
                var targetPath = TryGetValue("<file_or_dir_path>");
                var outputCSV = TryGetValue("--csv");

                // if targetpath is a single model print the model info
                if (File.Exists(targetPath))
                    if (outputCSV != null)
                        ExportModelInfoToCSV(
                            new List<RevitModelFile>() { new RevitModelFile(targetPath) },
                            outputCSV
                            );
                    else
                        PrintModelInfo(new RevitModelFile(targetPath));

                // collect all revit models
                else {
                    var models = new List<RevitModelFile>();
                    var errorList = new List<(string, string)>();

                    logger.Info(string.Format("Searching for revit files under \"{0}\"", targetPath));
                    FileAttributes attr = File.GetAttributes(targetPath);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                        var files = Directory.EnumerateFiles(targetPath, "*.rvt", SearchOption.AllDirectories);
                        logger.Info(string.Format(" {0} revit files found under \"{1}\"", files.Count(), targetPath));
                        foreach (var file in files) {
                            try {
                                logger.Info(string.Format("Revit file found \"{0}\"", file));
                                var model = new RevitModelFile(file);
                                models.Add(model);
                            }
                            catch (Exception ex) {
                                errorList.Add((file, ex.Message));
                            }
                        }
                    }

                    if (outputCSV != null)
                        ExportModelInfoToCSV(models, outputCSV, errorList);
                    else {
                        // report info on all files
                        foreach (var model in models) {
                            Console.WriteLine(model.FilePath);
                            PrintModelInfo(new RevitModelFile(model.FilePath));
                            Console.WriteLine();
                        }

                        // write list of files with errors
                        if (errorList.Count > 0) {
                            Console.WriteLine("An error occured while processing these files:");
                            foreach (var errinfo in errorList)
                                Console.WriteLine(string.Format("\"{0}\": {1}\n", errinfo.Item1, errinfo.Item2));
                        }
                    }

                }
            }

            // =======================================================================================================
            // $ pyrevit revits addons
            // $ pyrevit revits addons prepare <revit_year> [--allusers]
            // $ pyrevit revits addons install <addon_name> <dest_path> [--allusers]
            // $ pyrevit revits addons uninstall <addon_name>
            // =======================================================================================================
            else if (all("revits", "addons", "prepare")) {
                // setup the addon folders
                var revitYear = TryGetValue("<revit_year>");
                if (revitYear != null)
                    Addons.PrepareAddonPath(int.Parse(revitYear), allUsers: arguments["--allusers"].IsTrue);
            }

            else if (all("revits", "addons")
                        || all("revits", "addons", "install")
                        || all("revits", "addons", "uninstall")) {
                // TODO: implement revit addon manager
                logger.Error("Revit addon manager is not implemented yet");
            }

            // =======================================================================================================
            // $ pyrevit run <script_file_or_command_name> [--revit=<revit_year>] [--purge]
            // $ pyrevit run <script_file_or_command_name> <model_file> [--revit=<revit_year>] [--purge]
            // =======================================================================================================
            else if (all("run")) {

                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Run);

                var inputCommand = TryGetValue("<script_file_or_command_name>");
                var targetFile = TryGetValue("<model_file>");
                var revitYear = TryGetValue("--revit");

                // determine if script or command

                var modelFiles = new List<string>();
                // make sure file exists
                if (targetFile != null)
                    CommonUtils.VerifyFile(targetFile);

                if (inputCommand != null) {
                    // determine target revit year
                    int revitYearNumber = 0;
                    // if revit year is not specified try to get from model file
                    if (revitYear == null) {
                        if (targetFile != null) {
                            try {
                                revitYearNumber = new RevitModelFile(targetFile).RevitProduct.ProductYear;
                                // collect model names also
                                modelFiles.Add(targetFile);
                            }
                            catch (Exception ex) {
                                logger.Error(
                                    "Revit version must be explicitly specifies if using a model list file. | {0}",
                                    ex.Message
                                    );
                            }
                        }
                        // if no revit year and no model, run with latest revit
                        else
                            revitYearNumber = RevitProduct.ListInstalledProducts().Max(r => r.ProductYear);
                    }
                    // otherwise, grab the year from argument
                    else {
                        revitYearNumber = int.Parse(revitYear);
                        // prepare model list of provided
                        if (targetFile != null) {
                            try {
                                var modelVer = new RevitModelFile(targetFile).RevitProduct.ProductYear;
                                if (revitYearNumber < modelVer)
                                    logger.Warn("Model is newer than the target Revit version.");
                                else
                                    modelFiles.Add(targetFile);
                            }
                            catch {
                                // attempt at reading the list file and grab the model files only
                                foreach (var modelPath in File.ReadAllLines(targetFile)) {
                                    if (CommonUtils.VerifyFile(modelPath)) {
                                        try {
                                            var modelVer = new RevitModelFile(modelPath).RevitProduct.ProductYear;
                                            if (revitYearNumber < modelVer)
                                                logger.Warn("Model is newer than the target Revit version.");
                                            else
                                                modelFiles.Add(modelPath);
                                        }
                                        catch {
                                            logger.Error("File is not a valid Revit file: \"{0}\"", modelPath);
                                        }
                                    }
                                    else
                                        logger.Error("File does not exist: \"{0}\"", modelPath);
                                }
                            }
                        }
                    }

                    // now run
                    if (revitYearNumber != 0) {
                        // determine attached clone
                        var attachment = PyRevit.GetAttached(revitYearNumber);
                        if (attachment == null)
                            logger.Error("pyRevit is not attached to Revit \"{0}\". " +
                                         "Runner needs to use the attached clone and engine to execute the script.",
                                         revitYear);
                        else {
                            // determine script to run
                            string commandScriptPath = null;

                            if (!CommonUtils.VerifyPythonScript(inputCommand)) {
                                logger.Debug("Input is not a script file \"{0}\"", inputCommand);
                                logger.Debug("Attempting to find run command matching \"{0}\"", inputCommand);

                                // try to find run command in attached clone being used for execution
                                // if not found, try to get run command from all other installed extensions
                                var targetExtensions = new List<PyRevitExtension>();
                                targetExtensions.AddRange(attachment.Clone.GetExtensions());
                                targetExtensions.AddRange(PyRevit.GetInstalledExtensions());

                                foreach (PyRevitExtension ext in targetExtensions) {
                                    logger.Debug("Searching for run command in: \"{0}\"", ext.ToString());
                                    if (ext.Type == PyRevitExtensionTypes.RunnerExtension) {
                                        try {
                                            var cmdScript = ext.GetRunCommand(inputCommand);
                                            if (cmdScript != null) {
                                                logger.Debug("Run command matching \"{0}\" found: \"{1}\"",
                                                             inputCommand, cmdScript);
                                                commandScriptPath = cmdScript;
                                                break;
                                            }
                                        }
                                        catch {
                                            // does not include command
                                            continue;
                                        }
                                    }
                                }
                            }
                            else
                                commandScriptPath = inputCommand;

                            // if command is not found, stop
                            if (commandScriptPath == null)
                                throw new pyRevitException(
                                    string.Format("Run command not found: \"{0}\"", inputCommand)
                                    );

                            // RUN!
                            var execEnv = PyRevitRunner.Run(
                                attachment,
                                commandScriptPath,
                                modelFiles,
                                purgeTempFiles: arguments["--purge"].IsTrue
                                );

                            // print results (exec env)
                            PrintHeader("Execution Environment");
                            Console.WriteLine(string.Format("Execution Id: \"{0}\"", execEnv.ExecutionId));
                            Console.WriteLine(string.Format("Product: {0}", execEnv.Revit));
                            Console.WriteLine(string.Format("Clone: {0}", execEnv.Clone));
                            Console.WriteLine(string.Format("Engine: {0}", execEnv.Engine));
                            Console.WriteLine(string.Format("Script: \"{0}\"", execEnv.Script));
                            Console.WriteLine(string.Format("Working Directory: \"{0}\"", execEnv.WorkingDirectory));
                            Console.WriteLine(string.Format("Journal File: \"{0}\"", execEnv.JournalFile));
                            Console.WriteLine(string.Format("Manifest File: \"{0}\"", execEnv.PyRevitRunnerManifestFile));
                            Console.WriteLine(string.Format("Log File: \"{0}\"", execEnv.LogFile));
                            // report whether the env was purge or not
                            if (execEnv.Purged)
                                Console.WriteLine("Execution env is successfully purged.");

                            // print target models
                            if (execEnv.ModelPaths.Count() > 0) {
                                PrintHeader("Target Models");
                                foreach (var modelPath in execEnv.ModelPaths)
                                    Console.WriteLine(modelPath);
                            }

                            // print log file contents if exists
                            if (File.Exists(execEnv.LogFile)) {
                                PrintHeader("Execution Log");
                                Console.WriteLine(File.ReadAllText(execEnv.LogFile));
                            }
                        }
                    }
                }
            }

            // =======================================================================================================
            // $ pyrevit init --help
            // =======================================================================================================
            else if (all("init") && arguments["--help"].IsTrue) {
                PyRevitCLIAppHelps.BuildHelp(new List<string>() { "init" },
                                    "Init pyRevit bundles");
            }


            // =======================================================================================================
            //  $ pyrevit init (ui | lib | run) <extension_name> [--usetemplate] [--templates=<templates_path>]
            // =======================================================================================================
            else if (all("init", "ui")
                        || all("init", "lib")
                        || all("init", "run")) {

                PyRevitExtensionTypes extType = GetExtentionTypeFromArgument(arguments);

                var extDirPostfix = PyRevitExtension.GetExtensionDirExt(extType);

                var extensionName = TryGetValue("<extension_name>");
                var templatesDir = TryGetValue("--templates");
                if (extensionName != null) {
                    var pwd = Directory.GetCurrentDirectory();

                    if (CommonUtils.ConfirmFileNameIsUnique(pwd, extensionName)) {
                        var extDir = Path.Combine(
                            pwd,
                            string.Format("{0}{1}", extensionName, extDirPostfix)
                            );

                        var extTemplateDir = GetExtensionTemplate(extType, templatesDir: templatesDir);
                        if (arguments["--usetemplate"].IsTrue && extTemplateDir != null) {
                            CommonUtils.CopyDirectory(extTemplateDir, extDir);
                            Console.WriteLine(
                                string.Format("Extension directory created from template: \"{0}\"", extDir)
                                );
                        }
                        else {
                            if (!Directory.Exists(extDir)) {
                                var dinfo = Directory.CreateDirectory(extDir);
                                Console.WriteLine(string.Format("{0} directory created: \"{1}\"", extType, extDir));
                            }
                            else
                                throw new pyRevitException("Directory already exists.");
                        }

                    }
                    else
                        throw new pyRevitException(
                            string.Format("Another extension with name \"{0}\" already exists.", extensionName)
                            );
                }
            }

            // =======================================================================================================
            // $ pyrevit caches --help
            // =======================================================================================================
            else if (all("caches") && arguments["--help"].IsTrue) {
                PyRevitCLIAppHelps.BuildHelp(new List<string>() { "caches" },
                                    "Manage pyRevit caches");
            }

            // =======================================================================================================
            // $ pyrevit init (tab | panel | panelopt | pull | split | splitpush | push | smart | command) <bundle_name> [--usetemplate] [--templates=<templates_path>]
            // =======================================================================================================
            else if (all("init", "tab")
                        || all("init", "panel")
                        || all("init", "panelopt")
                        || all("init", "pull")
                        || all("init", "split")
                        || all("init", "splitpush")
                        || all("init", "push")
                        || all("init", "smart")
                        || all("init", "command")) {

                // determine bundle
                PyRevitBundleTypes bundleType = PyRevitBundleTypes.Unknown;

                if (arguments["tab"].IsTrue)
                    bundleType = PyRevitBundleTypes.Tab;
                else if (arguments["panel"].IsTrue)
                    bundleType = PyRevitBundleTypes.Panel;
                else if (arguments["panelopt"].IsTrue)
                    bundleType = PyRevitBundleTypes.PanelButton;
                else if (arguments["pull"].IsTrue)
                    bundleType = PyRevitBundleTypes.PullDown;
                else if (arguments["split"].IsTrue)
                    bundleType = PyRevitBundleTypes.SplitButton;
                else if (arguments["splitpush"].IsTrue)
                    bundleType = PyRevitBundleTypes.SplitPushButton;
                else if (arguments["push"].IsTrue)
                    bundleType = PyRevitBundleTypes.PushButton;
                else if (arguments["smart"].IsTrue)
                    bundleType = PyRevitBundleTypes.SmartButton;
                else if (arguments["command"].IsTrue)
                    bundleType = PyRevitBundleTypes.NoButton;

                if (bundleType != PyRevitBundleTypes.Unknown) {
                    var bundleName = TryGetValue("<bundle_name>");
                    var templatesDir = TryGetValue("--templates");
                    if (bundleName != null) {
                        var pwd = Directory.GetCurrentDirectory();

                        if (CommonUtils.ConfirmFileNameIsUnique(pwd, bundleName)) {
                            var bundleDir = Path.Combine(
                                pwd,
                                string.Format("{0}{1}", bundleName, PyRevitBundle.GetBundleDirExt(bundleType))
                                );

                            var bundleTempDir = GetBundleTemplate(bundleType, templatesDir: templatesDir);
                            if (arguments["--usetemplate"].IsTrue && bundleTempDir != null) {
                                CommonUtils.CopyDirectory(bundleTempDir, bundleDir);
                                Console.WriteLine(
                                    string.Format("Bundle directory created from template: \"{0}\"", bundleDir)
                                    );
                            }
                            else {
                                if (!Directory.Exists(bundleDir)) {
                                    var dinfo = Directory.CreateDirectory(bundleDir);
                                    Console.WriteLine(string.Format("Bundle directory created: \"{0}\"", bundleDir));
                                }
                                else
                                    throw new pyRevitException("Directory already exists.");
                            }

                        }
                        else
                            throw new pyRevitException(
                                string.Format("Another bundle with name \"{0}\" already exists.", bundleName)
                                );
                    }
                }
            }

            // =======================================================================================================
            // $ pyrevit init templates
            // $ pyrevit init templates (add | forget) <init_templates_path>
            // =======================================================================================================
            else if (all("init", "templates")) {
                Console.WriteLine(Directory.GetCurrentDirectory());
            }

            // =======================================================================================================
            // $ pyrevit caches clear (--all | <revit_year>)
            // =======================================================================================================
            else if (all("caches", "clear")) {
                if (arguments["--all"].IsTrue)
                    PyRevit.ClearAllCaches();
                else if (arguments["<revit_year>"] != null) {
                    var revitYear = TryGetValue("<revit_year>");
                    if (revitYear != null)
                        PyRevit.ClearCache(int.Parse(revitYear));
                }
            }

            // =======================================================================================================
            // $ pyrevit config --help
            // $ pyrevit config <template_config_path>
            // =======================================================================================================
            else if (all("config")) {

                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Config);
                else
                    PyRevitCLIConfigCmds.SeedConfigs(
                        templateConfigFilePath: TryGetValue("<template_config_path>")
                    );
            }

            // =======================================================================================================
            // $ pyrevit configs
            // =======================================================================================================
            else if (all("configs"))
                if (IsHelpMode)
                    PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Configs);

                // =======================================================================================================
                // $ pyrevit configs logs [(none | verbose | debug)]
                // =======================================================================================================
                else if (all("configs", "logs"))
                    Console.WriteLine(string.Format("Logging Level is {0}", PyRevit.GetLoggingLevel().ToString()));

                else if (all("configs", "logs", "none"))
                    PyRevit.SetLoggingLevel(PyRevitLogLevels.None);

                else if (all("configs", "logs", "verbose"))
                    PyRevit.SetLoggingLevel(PyRevitLogLevels.Verbose);

                else if (all("configs", "logs", "debug"))
                    PyRevit.SetLoggingLevel(PyRevitLogLevels.Debug);

                // =======================================================================================================
                // $ pyrevit configs allowremotedll [(enable | disable)]
                // =======================================================================================================
                // TODO: Implement allowremotedll
                else if (all("configs", "allowremotedll"))
                    logger.Error("Not Yet Implemented");

                // =======================================================================================================
                // $ pyrevit configs checkupdates [(enable | disable)]
                // =======================================================================================================
                else if (all("configs", "checkupdates"))
                    Console.WriteLine(
                        string.Format("Check Updates is {0}",
                        PyRevit.GetCheckUpdates() ? "Enabled" : "Disabled")
                        );

                else if (all("configs", "checkupdates", "enable")
                        || all("configs", "checkupdates", "disable"))
                    PyRevit.SetCheckUpdates(arguments["enable"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs autoupdate [(enable | disable)]
                // =======================================================================================================
                else if (all("configs", "autoupdate"))
                    Console.WriteLine(
                        string.Format("Auto Update is {0}",
                        PyRevit.GetAutoUpdate() ? "Enabled" : "Disabled")
                        );

                else if (all("configs", "autoupdate", "enable")
                        || all("configs", "autoupdate", "disable"))
                    PyRevit.SetAutoUpdate(arguments["enable"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs rocketmode [(enable | disable)]
                // =======================================================================================================
                else if (all("configs", "rocketmode"))
                    Console.WriteLine(
                        string.Format("Rocket Mode is {0}",
                        PyRevit.GetRocketMode() ? "Enabled" : "Disabled")
                        );

                else if (all("configs", "rocketmode", "enable")
                        || all("configs", "rocketmode", "disable"))
                    PyRevit.SetRocketMode(arguments["enable"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs filelogging [(enable | disable)]
                // =======================================================================================================
                else if (all("configs", "filelogging"))
                    Console.WriteLine(
                        string.Format("File Logging is {0}",
                        PyRevit.GetFileLogging() ? "Enabled" : "Disabled")
                        );

                else if (all("configs", "filelogging", "enable")
                        || all("configs", "filelogging", "disable"))
                    PyRevit.SetFileLogging(arguments["enable"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs loadbeta [(enable | disable)]
                // =======================================================================================================
                else if (all("configs", "loadbeta"))
                    Console.WriteLine(
                        string.Format("Load Beta is {0}",
                        PyRevit.GetLoadBetaTools() ? "Enabled" : "Disabled")
                        );

                else if (all("configs", "loadbeta", "enable")
                        || all("configs", "loadbeta", "disable"))
                    PyRevit.SetLoadBetaTools(arguments["enable"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs usercanupdate [(Yes | No)]
                // =======================================================================================================
                else if (all("configs", "usercanupdate"))
                    Console.WriteLine(
                        string.Format("User {0} update.",
                        PyRevit.GetUserCanUpdate() ? "CAN" : "CAN NOT")
                        );

                else if (all("configs", "usercanupdate", "Yes")
                        || all("configs", "usercanupdate", "No"))
                    PyRevit.SetUserCanUpdate(arguments["Yes"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs usercanextend [(Yes | No)]
                // =======================================================================================================
                else if (all("configs", "usercanextend"))
                    Console.WriteLine(
                        string.Format("User {0} extend.",
                        PyRevit.GetUserCanExtend() ? "CAN" : "CAN NOT")
                        );

                else if (all("configs", "usercanextend", "Yes")
                        || all("configs", "usercanextend", "No"))
                    PyRevit.SetUserCanExtend(arguments["Yes"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs usercanconfig [(Yes | No)]
                // =======================================================================================================
                else if (all("configs", "usercanconfig"))
                    Console.WriteLine(
                        string.Format("User {0} config.",
                        PyRevit.GetUserCanConfig() ? "CAN" : "CAN NOT")
                        );

                else if (all("configs", "usercanconfig", "Yes")
                        || all("configs", "usercanconfig", "No"))
                    PyRevit.SetUserCanConfig(arguments["Yes"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs usagelogging
                // =======================================================================================================
                else if (all("configs", "usagelogging")) {
                    Console.WriteLine(
                        string.Format("Usage logging is {0}",
                        PyRevit.GetUsageReporting() ? "Enabled" : "Disabled")
                        );
                    Console.WriteLine(string.Format("Log File Path: {0}", PyRevit.GetUsageLogFilePath()));
                    Console.WriteLine(string.Format("Log Server Url: {0}", PyRevit.GetUsageLogServerUrl()));
                }

                // =======================================================================================================
                // $ pyrevit configs usagelogging enable (file | server) <dest_path>
                // =======================================================================================================
                else if (all("configs", "usagelogging", "enable", "file"))
                    PyRevit.EnableUsageReporting(logFilePath: TryGetValue("<dest_path>"));

                else if (all("configs", "usagelogging", "enable", "server"))
                    PyRevit.EnableUsageReporting(logServerUrl: TryGetValue("<dest_path>"));

                // =======================================================================================================
                // $ pyrevit configs usagelogging disable
                // =======================================================================================================
                else if (all("configs", "usagelogging", "disable"))
                    PyRevit.DisableUsageReporting();

                // =======================================================================================================
                // $ pyrevit configs outputcss [<css_path>]
                // =======================================================================================================
                else if (all("configs", "outputcss")) {
                    if (arguments["<css_path>"] == null)
                        Console.WriteLine(
                            string.Format("Output Style Sheet is set to: {0}",
                            PyRevit.GetOutputStyleSheet()
                            ));
                    else
                        PyRevit.SetOutputStyleSheet(TryGetValue("<css_path>"));
                }

                // =======================================================================================================
                // $ pyrevit configs seed [--lock]
                // =======================================================================================================
                else if (all("configs", "seed"))
                    PyRevit.SeedConfig(makeCurrentUserAsOwner: arguments["--lock"].IsTrue);

                // =======================================================================================================
                // $ pyrevit configs <option_path> [(enable | disable)]
                // $ pyrevit configs <option_path> [<option_value>]
                // =======================================================================================================
                else if (all("configs")) {
                    if (arguments["<option_path>"] != null) {
                        // extract section and option names
                        string orignalOptionValue = TryGetValue("<option_path>");
                        if (orignalOptionValue.Split(':').Count() == 2) {
                            string configSection = orignalOptionValue.Split(':')[0];
                            string configOption = orignalOptionValue.Split(':')[1];

                            // if no value provided, read the value
                            if (arguments["<option_value>"] != null)
                                PyRevit.SetConfig(
                                    configSection,
                                    configOption,
                                    TryGetValue("<option_value>")
                                    );
                            else if (arguments["<option_value>"] == null)
                                Console.WriteLine(
                                    string.Format("{0} = {1}",
                                    configOption,
                                    PyRevit.GetConfig(configSection, configOption)
                                    ));
                        }
                    }
                }

                else if (all("configs", "enable")
                        || all("configs", "disable")) {
                    if (arguments["<option_path>"] != null) {
                        // extract section and option names
                        string orignalOptionValue = TryGetValue("<option_path>");
                        if (orignalOptionValue.Split(':').Count() == 2) {
                            string configSection = orignalOptionValue.Split(':')[0];
                            string configOption = orignalOptionValue.Split(':')[1];

                            PyRevit.SetConfig(configSection, configOption, arguments["enable"].IsTrue);
                        }
                    }
                }

                // =======================================================================================================
                // $ pyrevit cli --help
                // =======================================================================================================
                else if (all("cli"))
                    if (IsHelpMode)
                        PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Cli);

                    // =======================================================================================================
                    // $ pyrevit cli addshortcut <shortcut_name> <shortcut_args> [--desc=<shortcut_description>] [--allusers]
                    // =======================================================================================================
                    else if (all("cli", "addshortcut"))
                        PyRevitCLIAppCmds.AddCLIShortcut(
                            shortcutName: TryGetValue("<shortcut_name>"),
                            shortcutArgs: TryGetValue("<shortcut_args>"),
                            shortcutDesc: TryGetValue("--desc"),
                            allUsers: arguments["--allusers"].IsTrue
                        );

                    // =======================================================================================================
                    // $ pyrevit cli installautocomplete
                    // =======================================================================================================
                    else if (all("cli", "installautocomplete"))
                        PyRevitCLIAppCmds.ActivateAutoComplete();

                    // =======================================================================================================
                    // $ pyrevit (-h|--help)
                    // =======================================================================================================
                    else if (IsHelpMode)
                        PyRevitCLIAppHelps.PrintHelp(PyRevitCLICommandType.Main);
        }

        // internal helper functions:
        private static bool all(params string[] keywords) {
            foreach (var keyword in keywords)
                if (!arguments.ContainsKey(keyword))
                    return false;
            return true;
        }

        private static bool any(params string[] keywords) {
            foreach (var keyword in keywords)
                if (arguments.ContainsKey(keyword))
                    return true;
            return false;
        }

        internal static string TryGetValue(string key, string defaultValue = null) {
            return arguments[key] != null ? arguments[key].Value as string : defaultValue;
        }

        // private:
        private static void PrintArguments(IDictionary<string, ValueObject> arguments) {
            var activeArgs = arguments.Where(x => x.Value != null && (x.Value.IsTrue || x.Value.IsString));
            foreach (var arg in activeArgs)
                Console.WriteLine("{0} = {1}", arg.Key, arg.Value.ToString());
        }

        private static void ProcessErrorCodes() {
        }

        private static void LogException(Exception ex, PyRevitCLILogLevel logLevel) {
            if (logLevel == PyRevitCLILogLevel.Debug)
                logger.Error(string.Format("{0} ({1})\n{2}", ex.Message, ex.GetType().ToString(), ex.StackTrace));
            else
                logger.Error(string.Format("{0}\nRun with \"--debug\" option to see debug messages", ex.Message));
        }
    }
}
