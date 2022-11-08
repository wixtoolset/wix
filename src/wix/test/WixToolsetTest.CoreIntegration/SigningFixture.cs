// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using Xunit;

    public class SigningFixture
    {
        [Fact]
        public void CanInscribeMsiWithSignedCabinet()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");
            var signedFolder = TestData.Get(@"TestData\.Data");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var outputMsi = Path.Combine(intermediateFolder, @"bin\test.msi");
                var signedMsi = Path.Combine(baseFolder, @"signed.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputMsi
                });

                result.AssertSuccess();

                var beforeRows = Query.QueryDatabase(outputMsi, new[] { "MsiDigitalSignature", "MsiDigitalCertificate" });
                Assert.Empty(beforeRows);

                // Swap in a pre-signed cabinet since signing during the unit test
                // is a challenge. The cabinet contents almost definitely don't
                // match but that's okay for these testing purposes.
                File.Copy(Path.Combine(signedFolder, "signed_cab1.cab"), Path.Combine(Path.GetDirectoryName(outputMsi), "example.cab"), true);

                result = WixRunner.Execute(new[]
                {
                    "msi",
                    "inscribe",
                    outputMsi,
                    "-o", signedMsi,
                    "-intermediateFolder", intermediateFolder,
                });

                result.AssertSuccess();

                var rows = Query.QueryDatabase(signedMsi, new[] { "MsiDigitalSignature", "MsiDigitalCertificate" });
                WixAssert.CompareLineByLine(new[]
                {
                    "MsiDigitalCertificate:cer8xpsawK5TG4sIx4em8F.i7ocIKU\t[Binary data]",
                    "MsiDigitalSignature:Media\t1\tcer8xpsawK5TG4sIx4em8F.i7ocIKU\t"
                }, rows);
            }
        }

        [Fact]
        public void CanInscribeBundle()
        {
            var folder = TestData.Get(@"TestData", "SimpleBundle");
            var signedFolder = TestData.Get(@"TestData", ".Data");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var signedExe = Path.Combine(intermediateFolder, @"signed.exe");
                var reattachedExe = Path.Combine(baseFolder, @"bin\final.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-bindpath", signedFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
{
                    "burn",
                    "detach",
                    exePath,
                    "-engine", signedExe
                });

                result.AssertSuccess();

                // Swap in a pre-signed executable since signing during the unit test
                // is a challenge. The exe isn't an exact match but that's okay for
                // these testing purposes.
                File.Copy(Path.Combine(signedFolder, "signed_bundle_engine.exe"), signedExe, true);

                result = WixRunner.Execute(new[]
{
                    "burn",
                    "reattach",
                    exePath,
                    "-engine", signedExe,
                    "-o", reattachedExe
                });

                result.AssertSuccess();
                Assert.True(File.Exists(reattachedExe));
            }
        }

        [Fact]
        public void CanInscribe64BitBundle()
        {
            var folder = TestData.Get(@"TestData", "SimpleBundle");
            var signedFolder = TestData.Get(@"TestData", ".Data");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var signedExe = Path.Combine(intermediateFolder, @"signed.exe");
                var reattachedExe = Path.Combine(baseFolder, @"bin\final.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-platform", "x64",
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-bindpath", signedFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "burn",
                    "detach",
                    exePath,
                    "-engine", signedExe
                });

                result.AssertSuccess();
                Assert.True(File.Exists(signedExe));
            }
        }

        [Fact]
        public void CanInscribeUncompressedBundle()
        {
            var folder = TestData.Get(@"TestData", "BundleUncompressed");
            var bindPath = TestData.Get(@"TestData", "SimpleBundle", "data");
            var signedFolder = TestData.Get(@"TestData", ".Data");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var signedExe = Path.Combine(intermediateFolder, @"signed.exe");
                var reattachedExe = Path.Combine(baseFolder, @"bin\final.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "UncompressedBundle.wxs"),
                    "-bindpath", bindPath,
                    "-bindpath", signedFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
{
                    "burn",
                    "detach",
                    exePath,
                    "-engine", signedExe
                });

                result.AssertSuccess();

                // Swap in a pre-signed executable since signing during the unit test
                // is a challenge. The exe isn't an exact match but that's okay for
                // these testing purposes.
                File.Copy(Path.Combine(signedFolder, "signed_bundle_engine.exe"), signedExe, true);

                result = WixRunner.Execute(new[]
{
                    "burn",
                    "reattach",
                    exePath,
                    "-engine", signedExe,
                    "-o", reattachedExe
                });

                Assert.True(File.Exists(reattachedExe));
                Assert.Equal(-1000, result.ExitCode);
            }
        }
    }
}
