// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class MsuPackageFixture
    {
        [Fact]
        public void CanBuildBundleWithMsuPackage()
        {
            var dotDatafolder = TestData.Get(@"TestData", ".Data");
            var folder = TestData.Get(@"TestData", "MsuPackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-bindpath", dotDatafolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                result.AssertSuccess();
                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var msuPackages = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Chain/burn:MsuPackage");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<MsuPackage Id='test.msu' Cache='keep' CacheId='B040F02D2F90E04E9AFBDC91C00CEB5DF97D48E205D96DC0A44E10AF8870794D' InstallSize='28' Size='28' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' DetectCondition='DetectedTheMsu'>" +
                    "<PayloadRef Id='test.msu' />" +
                    "</MsuPackage>",
                }, msuPackages);
            }
        }

        [Fact]
        public void CanBuildBundleWithMsuPackageUsingCertificateVerification()
        {
            var dotDatafolder = TestData.Get(@"TestData", ".Data");
            var folder = TestData.Get(@"TestData", "MsuPackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleUsingCertificateVerification.wxs"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-bindpath", dotDatafolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                result.AssertSuccess();
                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var msuPackages = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Chain/burn:MsuPackage");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<MsuPackage Id='Windows8.1_KB2937592_x86.msu' Cache='keep' CacheId='8cf75b99-13c0-4184-82ce-dbde45dcd55a' InstallSize='309544' Size='309544' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' DetectCondition='DetectedTheMsu'>" +
                    "<PayloadRef Id='Windows8.1_KB2937592_x86.msu' />" +
                    "</MsuPackage>",
                }, msuPackages);
            }
        }

        [Fact]
        public void CannotBuildBundleWithMsuPackageUsingCertificateVerificationWithoutCacheId()
        {
            var dotDatafolder = TestData.Get(@"TestData", ".Data");
            var folder = TestData.Get(@"TestData", "MsuPackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleUsingCertificateVerificationWithoutCacheId.wxs"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-bindpath", dotDatafolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, "bin", "test.exe")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The MsuPackage/@CacheId attribute was not found; it is required when attribute CertificatePublicKey is specified.",
                }, result.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(10, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenSpecifyingPermanent()
        {
            var folder = TestData.Get(@"TestData", "MsuPackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "PermanentMsuPackage.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The MsuPackage element contains an unexpected attribute 'Permanent'.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(4, result.ExitCode);
            }
        }
    }
}
