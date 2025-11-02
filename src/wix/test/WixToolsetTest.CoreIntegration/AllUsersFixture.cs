// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class AllUsersFixture
    {
        [Fact]
        public void CanCheckPerMachineMsi()
        {
            var propertyRows = BuildAndQueryPropertyTable("PerMachine.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:WordCount\t2",
                "Property:ALLUSERS\t1"
            }, propertyRows);
        }

        [Fact]
        public void CanCheckPerMachineOrUserMsi()
        {
            var propertyRows = BuildAndQueryPropertyTable("PerMachineOrUser.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:WordCount\t2",
                "Property:ALLUSERS\t2"
            }, propertyRows);
        }

        [Fact]
        public void CanCheckPerUserMsi()
        {
            var propertyRows = BuildAndQueryPropertyTable("PerUser.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:WordCount\t10"
            }, propertyRows);
        }

        [Fact]
        public void CanCheckPerUserOrMachineMsi()
        {
            var propertyRows = BuildAndQueryPropertyTable("PerUserOrMachine.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:WordCount\t2",
                "Property:ALLUSERS\t2",
                "Property:MSIINSTALLPERUSER\t1"
            }, propertyRows);
        }

        private static string[] BuildAndQueryPropertyTable(string file)
        {
            var folder = TestData.Get("TestData", "AllUsers");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var msiPath = Path.Combine(binFolder, "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, file),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", folder,
                    "-o", msiPath,
                });
                result.AssertSuccess();

                return Query.QueryDatabase(msiPath, new[] { "Property", "_SummaryInformation" })
                    .Where(s => s.StartsWith("Property:ALLUSERS") || s.StartsWith("Property:MSIINSTALLPERUSER") || s.StartsWith("_SummaryInformation:WordCount"))
                    .OrderBy(s => s)
                    .ToArray();
            }
        }
    }
}
