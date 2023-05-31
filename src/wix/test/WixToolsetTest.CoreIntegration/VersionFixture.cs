// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class VersionFixture
    {
        [Fact]
        public void CanBuildMsiWithPrefixedVersion()
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
                    Path.Combine(folder, "Version", "PackageWithReplaceableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-d", "Version=v4.3.2.1",
                    "-o", msiPath
                });

                result.AssertSuccess();

                var productVersion = GetProductVersionFromMsi(msiPath);
                Assert.Equal("4.3.2.1", productVersion);
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
                    Path.Combine(folder, "Version", "PackageWithBindVariableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
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
        public void CannotBuildMsiWithExtendedVersion()
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
                    Path.Combine(folder, "Version", "PackageWithReplaceableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-d", "Version=v4.3.2-preview.1",
                    "-o", msiPath
                });

                var errorMessages = result.Messages.Where(m => m.Level == MessageLevel.Error)
                                                   .Select(m => m.ToString())
                                                   .ToArray();
                Assert.StartsWith("Invalid MSI package version: 'v4.3.2-preview.1'.", errorMessages.Single());
                Assert.Equal(1148, result.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildMsiWithInvalidMajorVersion()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test1.msi");

                var result = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "build",
                    Path.Combine(folder, "Version", "PackageWithReplaceableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-d", "Version=257.0.0",
                    "-o", msiPath
                });

                result.AssertSuccess();

                var warningMessages = result.Messages.Where(m => m.Level == MessageLevel.Warning).Select(m => m.ToString()).ToArray();
                Assert.StartsWith("Invalid MSI package version: '257.0.0'.", warningMessages[0]);
                Assert.StartsWith("Invalid MSI package version: '257.0.0'.", warningMessages[1]);
                Assert.StartsWith("Invalid MSI package version: '257.0.0'.", warningMessages[2]);
            }
        }

        [Fact]
        public void CannotBuildMsiWithInvalidBindVariableVersion()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test1.msi");

                var result = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "build",
                    Path.Combine(folder, "Version", "PackageWithUndefinedBindVariableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindvariable", "Version=257.0.0",
                    "-o", msiPath
                });

                result.AssertSuccess();

                var warningMessages = result.Messages.Where(m => m.Level == MessageLevel.Warning).Select(m => m.ToString()).ToArray();
                Assert.StartsWith("Invalid MSI package version: '257.0.0'.", warningMessages[0]);
                Assert.StartsWith("Invalid MSI package version: '257.0.0'.", warningMessages[1]);
                Assert.StartsWith("Invalid MSI package version: '257.0.0'.", warningMessages[2]);
            }
        }

        [Fact]
        public void CanBuildBundleWithSemanticVersion()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test1.msi");
                var msi2Path = Path.Combine(baseFolder, @"bin\test2.msi");
                var bundlePath = Path.Combine(baseFolder, @"bin\bundle.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Version", "PackageWithReplaceableVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-d", "Version=255.255.65535",
                    "-o", msiPath
                });

                result.AssertSuccess();

                var result3 = WixRunner.Execute(new[]
{
                    "build",
                    Path.Combine(folder, "Version", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(baseFolder, "bin"),
                    "-intermediateFolder", intermediateFolder,
                    "-d", "Version=2022.3.9-preview.0-build.5+0987654321abcdef1234567890",
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
