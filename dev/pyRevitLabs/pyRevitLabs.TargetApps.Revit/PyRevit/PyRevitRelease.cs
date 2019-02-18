using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace pyRevitLabs.TargetApps.Revit {
    public class PyRevitRelease {

        public override string ToString() {
            return string.Format("{0} | Tag: {1} | Version: {2} | Url: \"{3}\"",
                                 PreRelease ? Name + " (pre-release)" : Name, Tag, Version, Url);
        }

        // Github API JSON Properties
        public string name { get; set; }
        public string tag_name { get; set; }
        public string html_url { get; set; }
        public bool prerelease { get; set; }
        public string tarball_url { get; set; }
        public string zipball_url { get; set; }
        public string body { get; set; }
        public JArray assets { get; set; }

        // Check whether this is a pyRevit release
        public bool IsPyRevitRelease { get { return !tag_name.Contains(PyRevitConsts.CLIReleasePrefix); } }

        // Check whether this is a CLI release
        public bool IsCLIRelease { get { return tag_name.Contains(PyRevitConsts.CLIReleasePrefix); } }

        // Extract version object from tag_name
        public Version Version {
            get {
                // Cleanup tag_name first
                return new Version(
                    // replace from larger string to smaller
                    tag_name.ToLower()
                            .Split('-')[0]
                            .Replace(PyRevitConsts.CLIReleasePrefix, "")
                            .Replace(PyRevitConsts.ReleasePrefix, "")
                            );
            }
        }

        public string Name => name;
        public string Url => html_url;
        public string Tag => tag_name;
        public string ReleaseNotes => body.Trim();
        public bool PreRelease => prerelease;

        // Extract archive download url from zipball_url
        public string ArchiveUrl {
            get { return PyRevitConsts.GetTagArchiveUrl(Tag); }
        }

        // Extract archive download url from assets.browser_download_url
        public string InstallerUrl {
            get {
                if (assets != null && assets.Count > 0) {
                    var firstAsset = assets[0];
                    return firstAsset.SelectToken("browser_download_url").Value<string>();
                }
                return string.Empty;
            }
        }

        // Find latest releases
        public static List<PyRevitRelease> GetLatestReleases() {
            // make github api call and get a list of releases
            // https://developer.github.com/v3/repos/releases/
            HttpWebRequest request = CommonUtils.GetHttpWebRequest(PyRevitConsts.APIReleasesUrl);
            var response = request.GetResponse();

            // extract list of  PyRevitRelease from json
            IList<PyRevitRelease> releases;
            using (var reader = new StreamReader(response.GetResponseStream())) {
                releases = JsonConvert.DeserializeObject<IList<PyRevitRelease>>(reader.ReadToEnd());
            }

            return releases.ToList();
        }

        // find latest release version
        public static Version GetLatestPyRevitReleaseVersion() {
            // pick the latest release and return
            // could be null
            return GetLatestReleases().Where(r => r.IsPyRevitRelease).Select(r => r.Version).Max();
        }

        // find latest cli release version
        public static Version GetLatestCLIReleaseVersion() {
            // pick the latest release and return
            // could be null
            return GetLatestReleases().Where(r => r.IsCLIRelease).Select(r => r.Version).Max();
        }
    }

}
