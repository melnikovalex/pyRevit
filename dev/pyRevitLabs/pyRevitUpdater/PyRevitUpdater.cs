using System;
using System.Diagnostics;
using System.Windows;

using pyRevitLabs.CommonCLI;
using pyRevitLabs.TargetApps.Revit;

namespace pyRevitUpdater {
    public class PyRevitUpdaterCLI {
        static void Main(string[] args) {
            if (args.Length >= 1) {
                var clonePath = args[0];
                RunUpdate(clonePath);
            }
        }

        public static void RunUpdate(string clonePath) {
            try {
                var clone = PyRevit.GetRegisteredClone(clonePath);
                PyRevit.Update(clone);
                using (EventLog eventLog = new EventLog("Application")) {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry("Log message example", EventLogEntryType.Information, 101, 1);
                }
            }
            catch (Exception ex){
                using (EventLog eventLog = new EventLog("Application")) {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry("Log message example", EventLogEntryType.Information, 101, 1);
                }
            }
        }

    }
}
