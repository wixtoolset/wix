// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
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
    }
}
