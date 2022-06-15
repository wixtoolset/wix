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

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-bindpath", dotDatafolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, "bin", "test.exe")
                });

                result.AssertSuccess();
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.exe")));
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

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleUsingCertificateVerification.wxs"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-bindpath", dotDatafolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, "bin", "test.exe")
                });

                result.AssertSuccess();
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.exe")));
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

                Assert.Equal(10, result.ExitCode);
                var message = result.Messages.Single();
                Assert.Equal("The MsuPackage/@CacheId attribute was not found; it is required when attribute CertificatePublicKey is specified.", message.ToString());
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
