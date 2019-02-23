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
    internal static class PyRevitCLIClonesCmd {
        static Logger logger = LogManager.GetCurrentClassLogger();

        internal static void
        PrintClones() {
            PyRevitCLICommands.PrintHeader("Registered Clones (full git repos)");
            var clones = PyRevit.GetRegisteredClones().OrderBy(x => x.Name);
            foreach (var clone in clones.Where(c => c.IsRepoDeploy))
                Console.WriteLine(clone);

            PyRevitCLICommands.PrintHeader("Registered Clones (deployed from archive)");
            foreach (var clone in clones.Where(c => !c.IsRepoDeploy))
                Console.WriteLine(clone);
        }

        internal static void
        PrintAttachments(int revitYear = 0) {
            PyRevitCLICommands.PrintHeader("Attachments");
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
                PyRevitCLICommands.PrintHeader("Clone info");
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
                        PyRevitCLICommands.ReportCloneAsNoGit(clone);
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
                        PyRevitCLICommands.ReportCloneAsNoGit(clone);
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
                        PyRevitCLICommands.ReportCloneAsNoGit(clone);
                }
            }
        }

        internal static void
        PrintCloneDeployments(string cloneName) {
            if (cloneName != null) {
                var clone = PyRevit.GetRegisteredClone(cloneName);
                if (clone != null) {
                    PyRevitCLICommands.PrintHeader(string.Format("Deployments for \"{0}\"", clone.Name));
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
                    PyRevitCLICommands.PrintHeader(string.Format("Deployments for \"{0}\"", clone.Name));
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
                    if (PyRevitCLICommands.IsRunningInsideClone(clone))
                        myClone = clone;
                    else
                        targetClones.Add(clone);
            }
            // or single clone
            else {
                if (cloneName != null) {
                    var clone = PyRevit.GetRegisteredClone(cloneName);
                    if (PyRevitCLICommands.IsRunningInsideClone(clone))
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
                PyRevitCLICommands.UpdateFromOutsideAndClose(myClone);

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
    }
}
