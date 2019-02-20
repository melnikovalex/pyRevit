using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.TargetApps.Revit;

// https://docs.microsoft.com/en-us/visualstudio/test/getting-started-with-unit-testing
namespace pyRevitLabs.TargetApps.Revit.Tests {
    [TestClass()]
    public class PyRevitTests {
        private static string TempPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PyRevitLabsTests");

        //[AssemblyInitialize()]
        //public static void AssemblyInit(TestContext context) { }

        //https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.classinitializeattribute
        [ClassInitialize()]
        public static void ClassInit(TestContext context) {
            Directory.CreateDirectory(TempPath);
        }

        //[TestInitialize()]
        //public void Initialize() { }

        //[TestCleanup()]
        //public void Cleanup() { }

        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.classcleanupattribute
        [ClassCleanup()]
        public static void ClassCleanup() {
            Directory.Delete(TempPath);
        }

        //[AssemblyCleanup()]
        //public static void AssemblyCleanup() { }

        [TestMethod()]
        public void GetAttachedTest() {
            Assert.Fail();
        }
    }
}