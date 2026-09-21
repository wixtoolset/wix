// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Dtf.WindowsInstaller;
    using Xunit;

    public class TransformFixture
    {
        [Fact]
        public void CanBuildTransformFromEnuToJpn()
        {
            var folder = TestData.Get(@"TestData", "Language");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var enuMsiPath = Path.Combine(baseFolder, @"bin\enu.msi");
                var jpnMsiPath = Path.Combine(baseFolder, @"bin\jpn.msi");
                var mstPath = Path.Combine(baseFolder, @"bin\test.mst");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", enuMsiPath
                });
                result.AssertSuccess();


                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.ja-jp.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", jpnMsiPath
                });
                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "msi", "transform",
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-serr", "f",
                    "-o", mstPath,
                    enuMsiPath,
                    jpnMsiPath
                });
                result.AssertSuccess();

                Assert.True(File.Exists(mstPath));
            }
        }

        [Fact]
        public void CanBuildWixoutTransform()
        {
            var folder = TestData.Get(@"TestData", "Language");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var enuMsiPath = Path.Combine(baseFolder, @"bin\enu.msi");
                var jpnMsiPath = Path.Combine(baseFolder, @"bin\jpn.msi");
                var wixmstPath = Path.Combine(baseFolder, @"bin\test.wixmst");
                var mstPath = Path.Combine(baseFolder, @"bin\test.mst");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", enuMsiPath
                });
                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.ja-jp.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", jpnMsiPath
                });
                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "msi", "transform",
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-serr", "f",
                    "-xo",
                    "-o", wixmstPath,
                    enuMsiPath,
                    jpnMsiPath
                });
                result.AssertSuccess();

                var wixmst = WindowsInstallerData.Load(wixmstPath);
                var rows = wixmst.Tables.SelectMany(t => t.Rows).Where(r => r.Operation == RowOperation.Modify).ToDictionary(r => r.GetPrimaryKey());

                WixAssert.CompareLineByLine(new[]
                {
                    "NOT WIX_DOWNGRADE_DETECTED",
                    "ProductCode",
                    "ProductFeature",
                    "ProductLanguage"
                }, rows.Keys.OrderBy(s => s).ToArray());

                Assert.True(rows.TryGetValue("ProductFeature", out var productFeatureRow));
                WixAssert.StringEqual("MsiPackage ja-jp", productFeatureRow.FieldAsString(2));

                Assert.True(rows.TryGetValue("ProductLanguage", out var productLanguageRow));
                WixAssert.StringEqual("1041", productLanguageRow.FieldAsString(1));

                Assert.False(File.Exists(mstPath));

                result = WixRunner.Execute(new[]
                {
                    "msi", "transform",
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", mstPath,
                    wixmstPath
                });
                result.AssertSuccess();

                Assert.True(File.Exists(mstPath));
            }
        }

        [Fact]
        public void CanIncludeBinaryStreamDifferenceInTransform()
        {
            var folder = TestData.Get(@"TestData", "TransformBinaryDifference");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var brandA = Path.Combine(baseFolder, "brandA");
                var brandB = Path.Combine(baseFolder, "brandB");
                Directory.CreateDirectory(brandA);
                Directory.CreateDirectory(brandB);

                File.WriteAllText(Path.Combine(brandA, "test.txt"), "same file");
                File.WriteAllText(Path.Combine(brandB, "test.txt"), "same file");
                var originalBinary = new byte[] { 0x42, 0x4D, 0x01, 0x02, 0x03, 0x04 };
                var updatedBinary = new byte[] { 0x42, 0x4D, 0x99, 0x88, 0x77, 0x66, 0x55, 0x44 };
                File.WriteAllBytes(Path.Combine(brandA, "background.bin"), originalBinary);
                File.WriteAllBytes(Path.Combine(brandB, "background.bin"), updatedBinary);

                var originalMsiPath = Path.Combine(baseFolder, @"bin\main.msi");
                var updatedMsiPath = Path.Combine(baseFolder, @"bin\main.otherbrand.msi");
                var mstPath = Path.Combine(baseFolder, @"bin\sometransform.mst");
                var package = Path.Combine(folder, "Package.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    package,
                    "-bindpath", brandA,
                    "-intermediateFolder", Path.Combine(baseFolder, "objA"),
                    "-o", originalMsiPath
                });
                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    package,
                    "-bindpath", brandB,
                    "-intermediateFolder", Path.Combine(baseFolder, "objB"),
                    "-o", updatedMsiPath
                });
                result.AssertSuccess();

                Assert.Equal(originalBinary.Length, GetBinaryStreamLength(originalMsiPath, "Background"));
                Assert.Equal(updatedBinary.Length, GetBinaryStreamLength(updatedMsiPath, "Background"));

                result = WixRunner.Execute(new[]
                {
                    "msi", "transform",
                    "-p",
                    "-intermediateFolder", Path.Combine(baseFolder, "objT"),
                    "-o", mstPath,
                    originalMsiPath,
                    updatedMsiPath
                });
                result.AssertSuccess();

                Assert.True(File.Exists(mstPath));

                var appliedMsiPath = Path.Combine(baseFolder, @"bin\main.applied.msi");
                File.Copy(originalMsiPath, appliedMsiPath);

                using (var db = new Database(appliedMsiPath, DatabaseOpenMode.Transact))
                {
                    db.ApplyTransform(mstPath);
                    db.Commit();
                }

                Assert.Equal(updatedBinary.Length, GetBinaryStreamLength(appliedMsiPath, "Background"));
            }
        }

        private static int GetBinaryStreamLength(string msiPath, string binaryName)
        {
            using (var db = new Database(msiPath, DatabaseOpenMode.ReadOnly))
            using (var view = db.OpenView("SELECT `Data` FROM `Binary` WHERE `Name`='{0}'", binaryName))
            {
                view.Execute();
                using (var record = view.Fetch())
                {
                    Assert.NotNull(record);
                    return record.GetDataSize(1);
                }
            }
        }
    }
}
