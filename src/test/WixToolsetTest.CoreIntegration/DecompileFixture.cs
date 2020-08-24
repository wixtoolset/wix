// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class DecompileFixture
    {
        private static void DecompileAndCompare(string sourceFolder, string msiName, string expectedWxsName)
        {
            var folder = TestData.Get(sourceFolder);

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "decompile",
                    Path.Combine(folder, msiName),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath
                });

                result.AssertSuccess();

                WixAssert.CompareXml(Path.Combine(folder, expectedWxsName), outputPath);
            }
        }

        [Fact]
        public void CanDecompileSingleFileCompressed()
        {
            DecompileAndCompare(@"TestData\DecompileSingleFileCompressed", "example.msi", "Expected.wxs");
        }

        [Fact]
        public void CanDecompile64BitSingleFileCompressed()
        {
            DecompileAndCompare(@"TestData\DecompileSingleFileCompressed64", "example.msi", "Expected.wxs");
        }

        [Fact]
        public void CanDecompileNestedDirSearchUnderRegSearch()
        {
            DecompileAndCompare(@"TestData\AppSearch", "NestedDirSearchUnderRegSearch.msi", "DecompiledNestedDirSearchUnderRegSearch.wxs");
        }

        [Fact]
        public void CanDecompileOldClassTableDefinition()
        {
            // The input MSI was not created using standard methods, it is an example of a real world database that needs to be decompiled.
            // The Class/@Feature_ column has length of 32, the File/@Attributes has length of 2,
            // and numerous foreign key relationships are missing.
            DecompileAndCompare(@"TestData\Class", "OldClassTableDef.msi", "DecompiledOldClassTableDef.wxs");
        }

        [Fact]
        public void CanDecompileSequenceTables()
        {
            DecompileAndCompare(@"TestData\SequenceTables", "SequenceTables.msi", "DecompiledSequenceTables.wxs");
        }

        [Fact]
        public void CanDecompileShortcuts()
        {
            DecompileAndCompare(@"TestData\Shortcut", "shortcuts.msi", "DecompiledShortcuts.wxs");
        }
    }
}
