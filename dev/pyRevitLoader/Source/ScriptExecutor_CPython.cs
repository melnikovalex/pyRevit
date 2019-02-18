using System;
using System.Linq;
using System.Text;
using System.IO;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Reflection;

using Python.Runtime;

namespace PyRevitLoader {
    // Executes a script
    public class ScriptExecutor {
        private readonly UIApplication _revit = null;

        public ScriptExecutor() {
        }

        public ScriptExecutor(UIApplication uiApplication) {
            _revit = uiApplication;
        }

        public string Message { get; private set; } = null;

        public static string EngineVersion {
            get {
                var assmVersion = Assembly.GetAssembly(typeof(ScriptExecutor)).GetName().Version;
                return string.Format("{0}{1}{2}", assmVersion.Minor, assmVersion.Build, assmVersion.Revision);
            }
        }

        public Result ExecuteScript(string sourcePath,
                                    IEnumerable<string> sysPaths = null,
                                    string logFilePath = null,
                                    IDictionary <string, object> variables = null) {
            try {
                using (Py.GIL()) {
                    // initialize
                    if (!PythonEngine.IsInitialized)
                        PythonEngine.Initialize();

                    // set output stream
                    dynamic sys = PythonEngine.ImportModule("sys");

                    // set uiapplication
                    sys.host = _revit;

                    // Add script directory address to sys search paths
                    foreach (var sysPath in sysPaths)
                        sys.path.append(sysPath);

                    // set globals
                    //scope.SetVariable("__file__", sourcePath);

                    //if (variables != null)
                    //    foreach (var keyPair in variables)
                    //        scope.SetVariable(keyPair.Key, keyPair.Value);


                    // run
                    var scriptContents = File.ReadAllText(sourcePath);
                    PythonEngine.Exec(scriptContents);

                    // shutdown halts and breaks Revit
                    // let's not do that!
                    // PythonEngine.Shutdown();

                    return Result.Succeeded;
                }
            }
            catch (Exception ex) {
                Message = ex.ToString();
                return Result.Failed;
            }
        }

    }
}