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
    internal static class PyRevitCLIRevitCmd {
        static Logger logger = LogManager.GetCurrentClassLogger();

        internal static void
        PrintRevits(bool running = false) {
            if (running) {
                PyRevitCLICommands.PrintHeader("Running Revit Instances");
                foreach (var revit in RevitController.ListRunningRevits().OrderByDescending(x => x.RevitProduct.Version))
                    Console.WriteLine(revit);
            }
            else {
                PyRevitCLICommands.PrintHeader("Installed Revits");
                foreach (var revit in RevitProduct.ListInstalledProducts().OrderByDescending(x => x.Version))
                    Console.WriteLine(revit);
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

        // privates:
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
    }
}
