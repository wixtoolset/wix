// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class CommentsFixture
    {
        [Fact]
        public void PackageNoSummaryInformation()
        {
            var propertyRows = BuildPackageAndQuerySummaryInformation("PackageNoSummaryInformation.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:Comments\tThis installer database contains the logic and data required to install MsiPackage."
            }, propertyRows);
        }

        [Fact]
        public void PackageDefaultComments()
        {
            var propertyRows = BuildPackageAndQuerySummaryInformation("PackageDefault.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:Comments\tThis installer database contains the logic and data required to install MsiPackage."
            }, propertyRows);
        }

        [Fact]
        public void PackageEmptyCommentsThrows()
        {
            var exception = Assert.Throws<WixException>(() => BuildPackageAndQuerySummaryInformation("PackageEmpty.wxs"));
            WixAssert.StringEqual("The SummaryInformation/@Comments attribute's value cannot be an empty string. If a value is not required, simply remove the entire attribute.", exception.Message);
        }

        [Fact]
        public void PackageCustomComments()
        {
            var propertyRows = BuildPackageAndQuerySummaryInformation("PackageCustom.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:Comments\tExample comments"
            }, propertyRows);
        }

        [Fact]
        public void ModuleNoSummaryInformation()
        {
            var propertyRows = BuildModuleAndQuerySummaryInformation("ModuleNoSummaryInformation.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:Comments\tThis merge module contains the logic and data required to install Module."
            }, propertyRows);
        }

        [Fact]
        public void ModuleDefaultComments()
        {
            var propertyRows = BuildModuleAndQuerySummaryInformation("ModuleDefault.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:Comments\tThis merge module contains the logic and data required to install Module."
            }, propertyRows);
        }

        [Fact]
        public void ModuleEmptyCommentsThrows()
        {
            var exception = Assert.Throws<WixException>(() => BuildModuleAndQuerySummaryInformation("ModuleEmpty.wxs"));
            WixAssert.StringEqual("The SummaryInformation/@Comments attribute's value cannot be an empty string. If a value is not required, simply remove the entire attribute.", exception.Message);
        }

        [Fact]
        public void ModuleCustomComments()
        {
            var propertyRows = BuildModuleAndQuerySummaryInformation("ModuleCustom.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "_SummaryInformation:Comments\tExample comments"
            }, propertyRows);
        }

        private static string[] BuildPackageAndQuerySummaryInformation(string file)
        {
            var folder = TestData.Get("TestData", "Comments");

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

                if(result.ExitCode != 0)
                {
                    throw new WixException(result.Messages.First());
                }

                return Query.QueryDatabase(msiPath, new[] { "Property", "_SummaryInformation" })
                    .Where(s => s.StartsWith("_SummaryInformation:Comments"))
                    .OrderBy(s => s)
                    .ToArray();
            }
        }

        private static string[] BuildModuleAndQuerySummaryInformation(string file)
        {
            var folder = TestData.Get("TestData", "Comments");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var msmPath = Path.Combine(binFolder, "test.msm");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, file),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-o", msmPath,
                });

                if (result.ExitCode != 0)
                {
                    throw new WixException(result.Messages.First());
                }

                return Query.QueryDatabase(msmPath, new[] { "Property", "_SummaryInformation" })
                    .Where(s => s.StartsWith("_SummaryInformation:Comments"))
                    .OrderBy(s => s)
                    .ToArray();
            }
        }
    }
}
