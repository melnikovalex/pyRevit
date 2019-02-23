using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.Common;
using pyRevitLabs.CommonCLI;
using pyRevitLabs.Common.Extensions;
using pyRevitLabs.TargetApps.Revit;
using pyRevitLabs.Language.Properties;

using NLog;


namespace pyRevitManager {
    internal enum PyRevitCLICommandType {
        Main,
        Version,
        Help,
        Releases
    }

    internal static class PyRevitCLICommands {
        static Logger logger = LogManager.GetCurrentClassLogger();

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
    }
}
