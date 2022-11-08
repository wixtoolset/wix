// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using Xunit;

    public class BindVariablesFixture
    {
        [Fact]
        public void CanBuildBundleWithPackageBindVariables()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleBindVariables", "CacheIdFromPackageDescription.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
            }
        }

        [Fact]
        public void CanBuildPackageWithBindVariables()
        {
            var folder = TestData.Get(@"TestData", "BindVariables");
            var dotDataFolder = TestData.Get(@"TestData", ".Data");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(intermediateFolder, @"test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageWithBindVariables.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", folder,
                    "-bindpath", dotDataFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                var queryResults = Query.QueryDatabase(msiPath, new[] { "Property" }).ToDictionary(s => s.Split('\t')[0]);
                WixAssert.StringEqual("Property:ProductVersion\t3.14.1703.0", queryResults["Property:ProductVersion"]);
                WixAssert.StringEqual("Property:TestPackageManufacturer\tExample Corporation", queryResults["Property:TestPackageManufacturer"]);
                WixAssert.StringEqual("Property:TestPackageName\tPacakgeWithBindVariables", queryResults["Property:TestPackageName"]);
                WixAssert.StringEqual("Property:TestPackageVersion\t3.14.1703.0", queryResults["Property:TestPackageVersion"]);
                WixAssert.StringEqual("Property:TestTextVersion\tv", queryResults["Property:TestTextVersion"]);
                Assert.False(queryResults.ContainsKey("Property:TestTextLanguage"));
            }
        }

        [Fact]
        public void CanBuildWithDefaultValue()
        {
            var folder = TestData.Get(@"TestData", "BindVariables");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "DefaultedVariable.wxs"),
                    "-bf",
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", folder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();
            }
        }

        [Fact]
        public void CannotBuildWixlibWithBinariesFromMissingNamedBindPaths()
        {
            var folder = TestData.Get(@"TestData", "WixlibWithBinaries");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-bf",
                    "-bindpath", Path.Combine(folder, "data"),
                    // Use names that aren't excluded in default .gitignores.
                    "-bindpath", $"AlphaBits={Path.Combine(folder, "data", "alpha")}",
                    "-bindpath", $"PowerBits={Path.Combine(folder, "data", "powerpc")}",
                    "-bindpath", $"{Path.Combine(folder, "data", "alpha")}",
                    "-bindpath", $"{Path.Combine(folder, "data", "powerpc")}",
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.Equal(103, result.ExitCode);
            }
        }
    }
}
