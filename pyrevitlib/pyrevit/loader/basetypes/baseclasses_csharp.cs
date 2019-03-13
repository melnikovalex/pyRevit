using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System.Windows.Input;
using System.Runtime.Remoting;
using System.Reflection;


namespace PyRevitBaseClasses {
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public abstract class PyRevitCommandCSharp : IExternalCommand {
        public string baked_scriptSource = null;

        public PyRevitCommandCSharp(string scriptSource) {
            baked_scriptSource = scriptSource;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {

            // 1: ---------------------------------------------------------------------------------------------------------------------------------------------
            #region Processing modifier keys
            // Processing modifier keys

            bool ALT = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            bool SHIFT = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool CTRL = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool WIN = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);

            // If Alt clicking on button, open the script in explorer and return.
            if (ALT) {
                // combine the arguments together
                // it doesn't matter if there is a space after ','
                string argument = "/select, \"" + baked_scriptSource + "\"";

                System.Diagnostics.Process.Start("explorer.exe", argument);
                return Result.Succeeded;
            }
            #endregion

            // 2: ---------------------------------------------------------------------------------------------------------------------------------------------
            #region Execute and return results
            #endregion
        }
    }
}
