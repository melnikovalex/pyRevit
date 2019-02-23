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
    internal static class PyRevitCLLInitCmd {
        static Logger logger = LogManager.GetCurrentClassLogger();

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

    }
}
