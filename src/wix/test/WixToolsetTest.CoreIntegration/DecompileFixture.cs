// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using Xunit;

    public class DecompileFixture
    {
        private static void DecompileAndCompare(string msiName, string expectedWxsName, params string[] sourceFolder)
        {
            var folder = TestData.Get(sourceFolder);

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "msi", "decompile",
                    Path.Combine(folder, msiName),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath
                }, out var messages);

                Assert.Equal(0, result);

                WixAssert.CompareXml(Path.Combine(folder, expectedWxsName), outputPath);
            }
        }

        [Fact]
        public void CanDecompileSingleFileCompressed()
        {
            DecompileAndCompare("example.msi", "Expected.wxs", "TestData", "DecompileSingleFileCompressed");
        }

        [Fact]
        public void CanDecompile64BitSingleFileCompressed()
        {
            DecompileAndCompare("example.msi", "Expected.wxs", "TestData", "DecompileSingleFileCompressed64");
        }

        [Fact]
        public void CanDecompileNestedDirSearchUnderRegSearch()
        {
            DecompileAndCompare("NestedDirSearchUnderRegSearch.msi", "DecompiledNestedDirSearchUnderRegSearch.wxs", "TestData", "AppSearch");
        }

        [Fact]
        public void CanDecompileOldClassTableDefinition()
        {
            // The input MSI was not created using standard methods, it is an example of a real world database that needs to be decompiled.
            // The Class/@Feature_ column has length of 32, the File/@Attributes has length of 2,
            // and numerous foreign key relationships are missing.
            DecompileAndCompare("OldClassTableDef.msi", "DecompiledOldClassTableDef.wxs", "TestData", "Class");
        }

        [Fact]
        public void CanDecompileSequenceTables()
        {
            DecompileAndCompare("SequenceTables.msi", "DecompiledSequenceTables.wxs", "TestData", "SequenceTables");
        }

        [Fact]
        public void CanDecompileShortcuts()
        {
            DecompileAndCompare("shortcuts.msi", "DecompiledShortcuts.wxs", "TestData", "Shortcut");
        }

        [Fact]
        public void CanDecompileNullComponent()
        {
            DecompileAndCompare("example.msi", "Expected.wxs", "TestData", "DecompileNullComponent");
        }

        [Fact]
        public void CanDecompileMergeModuleWithTargetDirComponent()
        {
            DecompileAndCompare("MergeModule1.msm", "Expected.wxs", "TestData", "DecompileTargetDirMergeModule");
        }

        [Fact]
        public void CanDecompileUI()
        {
            DecompileAndCompare("ui.msi", "ExpectedUI.wxs", "TestData", "Decompile");
        }
    }
}
