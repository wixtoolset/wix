// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data.WindowsInstaller;
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
    }
}
