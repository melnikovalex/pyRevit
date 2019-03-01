using Microsoft.VisualStudio.TestTools.UnitTesting;
using pyRevitLabs.TargetApps.Revit;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.Common;

// https://docs.microsoft.com/en-us/visualstudio/test/getting-started-with-unit-testing
namespace pyRevitLabs.TargetApps.Revit.Tests {
    [TestClass()]
    public class PyRevitTests {
        #region Utils
        public static string TempPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PyRevitLabsTests");
        #endregion
        #region Setup and Teardown
        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context) { }

        //https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.classinitializeattribute
        [ClassInitialize()]
        public static void ClassInit(TestContext context) { }

        [TestInitialize()]
        public void Initialize() {
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);
            else
                Cleanup();
        }

        [TestCleanup()]
        public void Cleanup() {
            try {
                CommonUtils.DeleteDirectory(TempPath);
            }
            catch {

            }
        }

        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.classcleanupattribute
        [ClassCleanup()]
        public static void ClassCleanup() { }

        [AssemblyCleanup()]
        public static void AssemblyCleanup() { }
        #endregion

        private string testCloneName = "TestClone";

        [TestMethod()]
        public void DeployFromImage_Full_Test() {
            var testCloneBranch = "master";

            PyRevit.DeployFromImage(
                cloneName: testCloneName,
                deploymentName: null,
                branchName: null,
                imagePath: null,
                destPath: TempPath
                );

            var clone = PyRevit.GetRegisteredClone(testCloneName);
            PyRevit.UnregisterClone(clone);

            Assert.AreEqual(testCloneBranch, clone.Branch,
                           string.Format("{0} != {1}", testCloneBranch, clone.Branch));
        }

        [TestMethod()]
        public void DeployFromImage_WithOptions_Test() {
            var testCloneDeployment = "core";
            var testCloneBranch = "develop";

            PyRevit.DeployFromImage(
                cloneName: testCloneName,
                deploymentName: testCloneDeployment,
                branchName: testCloneBranch,
                imagePath: null,
                destPath: TempPath
                );

            var clone = PyRevit.GetRegisteredClone(testCloneName);
            PyRevit.UnregisterClone(clone);

            Assert.AreEqual(testCloneDeployment, clone.Deployment.Name,
                            string.Format("{0} != {1}", testCloneDeployment, clone.Deployment.Name));
            Assert.AreEqual(testCloneBranch, clone.Branch,
                            string.Format("{0} != {1}", testCloneBranch, clone.Branch));
        }

        [TestMethod()]
        public void DeployFromRepo_Full_Test() {
            var testCloneBranch = "master";

            PyRevit.DeployFromRepo(
                cloneName: testCloneName,
                deploymentName: null,
                branchName: null,
                repoUrl: null,
                destPath: TempPath
                );

            var clone = PyRevit.GetRegisteredClone(testCloneName);
            PyRevit.UnregisterClone(clone);

            Assert.IsTrue(clone.IsRepoDeploy);
            Assert.AreEqual(testCloneBranch, clone.Branch);
        }

        [TestMethod()]
        public void DeployFromRepo_Develop_Test() {
            var testCloneBranch = "develop";

            PyRevit.DeployFromRepo(
                cloneName: testCloneName,
                deploymentName: null,
                branchName: testCloneBranch,
                repoUrl: null,
                destPath: TempPath
                );

            var clone = PyRevit.GetRegisteredClone(testCloneName);
            PyRevit.UnregisterClone(clone);

            Assert.IsTrue(clone.IsRepoDeploy);
            Assert.AreEqual(testCloneBranch, clone.Branch);
        }

        [TestMethod()]
        public void DeployFromRepo_Deployment_Test() {
            var testCloneDeployment = "core";
            var testCloneBranch = "develop";

            try {
                PyRevit.DeployFromRepo(
                    cloneName: testCloneName,
                    deploymentName: testCloneDeployment,
                    branchName: testCloneBranch,
                    repoUrl: null,
                    destPath: TempPath
                    );
            }
            catch (NotImplementedException) {
            }
            catch (Exception ex) {
                Assert.Fail(ex.Message);
            }
        }
    }
}