// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class PackagePayloadFixture
    {
        [Fact]
        public void CanSpecifyPackagePayloadInPayloadGroup()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackagePayload", "PackagePayloadInPayloadGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var exePackageElements = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:ExePackage");
                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "ExePackage", new List<string> { "CacheId", "InstallSize", "Size" } },
                };
                Assert.Equal(1, exePackageElements.Count);
                Assert.Equal("<ExePackage Id='PackagePayloadInPayloadGroup' Cache='yes' CacheId='*' InstallSize='*' Size='*' PerMachine='yes' Permanent='yes' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_PackagePayloadInPayloadGroup' RollbackLogPathVariable='WixBundleRollbackLog_PackagePayloadInPayloadGroup' DetectCondition='none' InstallArguments='' UninstallArguments='' RepairArguments='' Repairable='no'><PayloadRef Id='burn.exe' /></ExePackage>", exePackageElements[0].GetTestXml(ignoreAttributesByElementName));
            }
        }

        [Fact]
        public void ErrorWhenMissingSourceFileAndHash()
        {
            var folder = TestData.Get(@"TestData", "PackagePayload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "MissingSourceFileAndHash.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                Assert.Equal(44, result.ExitCode);
                WixAssert.CompareLineByLine(new[]
                {
                    "The MsuPackagePayload element's SourceFile or Hash attribute was not found; one of these is required.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
            }
        }

        [Fact]
        public void ErrorWhenMissingSourceFileAndName()
        {
            var folder = TestData.Get(@"TestData", "PackagePayload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "MissingSourceFileAndName.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                Assert.Equal(44, result.ExitCode);
                WixAssert.CompareLineByLine(new[]
                {
                    "The MsiPackagePayload element's Name or SourceFile attribute was not found; one of these is required.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
            }
        }

        [Fact]
        public void ErrorWhenSpecifiedHash()
        {
            var folder = TestData.Get(@"TestData", "PackagePayload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SpecifiedHash.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                Assert.Equal(4, result.ExitCode);
                WixAssert.CompareLineByLine(new[]
                {
                    "The MspPackagePayload element contains an unexpected attribute 'Hash'.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
            }
        }

        [Fact]
        public void ErrorWhenSpecifiedHashAndMissingDownloadUrl()
        {
            var folder = TestData.Get(@"TestData", "PackagePayload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SpecifiedHashAndMissingDownloadUrl.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                Assert.Equal(10, result.ExitCode);
                WixAssert.CompareLineByLine(new[]
                {
                    "The MsuPackagePayload/@DownloadUrl attribute was not found; it is required when attribute Hash is specified.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
            }
        }

        [Fact]
        public void ErrorWhenSpecifiedSourceFileAndHash()
        {
            var folder = TestData.Get(@"TestData", "PackagePayload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SpecifiedSourceFileAndHash.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                Assert.Equal(35, result.ExitCode);
                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackagePayload/@Hash attribute cannot be specified when attribute SourceFile is present.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
            }
        }

        [Fact]
        public void ErrorWhenWrongPackagePayloadInPayloadGroup()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackagePayload", "WrongPackagePayloadInPayloadGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                Assert.Equal(407, result.ExitCode);
                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackagePayload element can only be used for ExePackages.",
                    "The location of the package related to previous error.",
                    "There is no payload defined for package 'WrongPackagePayloadInPayloadGroup'. This is specified on the MsiPackage element or a child MsiPackagePayload element.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
            }
        }
    }
}
