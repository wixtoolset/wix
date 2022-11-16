// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class WixVariableFixture
    {
        [Fact]
        public void CanBuildMsiWithBindVariable()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, "bin", "test1.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "WixVariable", "PackageWithReplaceableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile"/*, this part of the path is added as a bind variable: "data"*/),
                    "-bindvariable", "DataBindVariable=data",
                    "-bindvariable", "VersionVar=4.3.2.1",
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var productVersion = GetProductVersionFromMsi(msiPath);
                Assert.Equal("4.3.2.1", productVersion);
            }
        }

        [Fact]
        public void CanBuildMsiWithDefaultedBindVariable()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, "bin", "test1.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "WixVariable", "PackageWithBindVariableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var productVersion = GetProductVersionFromMsi(msiPath);
                Assert.Equal("1.1.1.1", productVersion);

                var directoryTable = Query.QueryDatabase(msiPath, new[] { "Directory" }).OrderBy(s => s).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "Directory:DesktopFolder\tTARGETDIR\tDesktop",
                    "Directory:INSTALLFOLDER\tDesktopFolder\tfcuah1wu|MsiPackage v1.1.1.1 and 1.1.1.1",
                    "Directory:TARGETDIR\t\tSourceDir"
                }, directoryTable);
            }
        }

        [Fact]
        public void CanBuildMsiWithPrefixedVersionBindVariable()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, "bin", "test1.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "WixVariable", "PackageWithBindVariableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindvariable", "VersionVar=v9.8.7.6",
                    "-o", msiPath
                });

                result.AssertSuccess();

                var productVersion = GetProductVersionFromMsi(msiPath);
                Assert.Equal("9.8.7.6", productVersion);

                var directoryTable = Query.QueryDatabase(msiPath, new[] { "Directory" }).OrderBy(s => s).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "Directory:DesktopFolder\tTARGETDIR\tDesktop",
                    "Directory:INSTALLFOLDER\tDesktopFolder\tpja2bznq|MsiPackage v9.8.7.6 and 9.8.7.6",
                    "Directory:TARGETDIR\t\tSourceDir"
                }, directoryTable);
            }
        }

        [Fact]
        public void CanBuildBundleWithBindVariable()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, "bin", "test1.msi");
                var msi2Path = Path.Combine(baseFolder, "bin", "test2.msi");
                var bundlePath = Path.Combine(baseFolder, "bin", "bundle.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "WixVariable", "PackageWithReplaceableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile"/*, this part of the path is added as a bind variable: "data"*/),
                    "-bv", "DataBindVariable=data",
                    "-bv", "VersionVar=255.255.65535",
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var result3 = WixRunner.Execute(new[]
{
                    "build",
                    Path.Combine(folder, "WixVariable", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(baseFolder, "bin"),
                    "-intermediateFolder", intermediateFolder,
                    "-bv", "VersionVar=2022.3.9-preview.0-build.5+0987654321abcdef1234567890",
                    "-o", bundlePath
                });

                result3.AssertSuccess();

                var productVersion = GetProductVersionFromMsi(msiPath);
                WixAssert.StringEqual("255.255.65535", productVersion);

                var extractResult = BundleExtractor.ExtractAllContainers(null, bundlePath, Path.Combine(baseFolder, "ba"), Path.Combine(baseFolder, "attached"), Path.Combine(baseFolder, "extract"));
                extractResult.AssertSuccess();

                var bundleVersion = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Registration/@Version")
                                                 .Cast<XmlAttribute>()
                                                 .Single();
                WixAssert.StringEqual("2022.3.9-preview.0-build.5+0987654321abcdef1234567890", bundleVersion.Value);
            }
        }

        private static string GetProductVersionFromMsi(string msiPath)
        {
            var propertyTable = Query.QueryDatabase(msiPath, new[] { "Property" }).Select(r => r.Split('\t')).ToDictionary(r => r[0].Substring("Property:".Length), r => r[1]);
            Assert.True(propertyTable.TryGetValue("ProductVersion", out var productVersion));

            return productVersion;
        }
    }
}
