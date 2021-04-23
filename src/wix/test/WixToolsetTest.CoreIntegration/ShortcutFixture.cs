// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class ShortcutFixture
    {
        [Fact]
        public void CanBuildShortcutNameWithShortname()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Shortcut", "ShortcutSameNameShortName.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var results = Query.QueryDatabase(msiPath, new[] { "Shortcut" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Shortcut:sctzJpBYlrhdx4Mm9Xh41X0KPWYiX0\tINSTALLFOLDER\tDaName\tShortcutComp\t[#filcV1yrx0x8wJWj4qMzcH21jwkPko]\t\t\t\t\t\t\t\t\t\t\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesMsiShortcutPropertyTable()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Shortcut", "ShortcutProperty.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "MsiShortcutProperty", "Shortcut" });
                WixAssert.CompareLineByLine(new[]
                {
                    "MsiShortcutProperty:scp4GOCIx4Eskci4nBG1MV_vSUOZt4\tTheShortcut\tCustomShortcutKey\tCustomShortcutValue",
                    "Shortcut:TheShortcut\tINSTALLFOLDER\td\tShortcutComp\t[#filcV1yrx0x8wJWj4qMzcH21jwkPko]\t\t\t\t\t\t\t\t\t\t\t",
                }, results);
            }
        }
    }
}
