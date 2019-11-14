// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class DecompileFixture
    {
        [Fact]
        public void CanDecompileSingleFileCompressed()
        {
            var folder = TestData.Get(@"TestData\DecompileSingleFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "decompile",
                    Path.Combine(folder, "example.msi"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath
                });

                result.AssertSuccess();

                var actual = File.ReadAllText(outputPath);
                var actualFormatted = XDocument.Parse(actual, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();
                var expected = XDocument.Load(Path.Combine(folder, "Expected.wxs"), LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();

                Assert.Equal(expected, actualFormatted);
            }
        }

        [Fact]
        public void CanDecompile64BitSingleFileCompressed()
        {
            var folder = TestData.Get(@"TestData\DecompileSingleFileCompressed64");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "decompile",
                    Path.Combine(folder, "example.msi"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath
                });

                result.AssertSuccess();

                var actual = File.ReadAllText(outputPath);
                var actualFormatted = XDocument.Parse(actual, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();
                var expected = XDocument.Load(Path.Combine(folder, "Expected.wxs"), LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();

                Assert.Equal(expected, actualFormatted);
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
        public void CanDecompileNestedDirSearchUnderRegSearch()
        {
            var folder = TestData.Get(@"TestData\AppSearch");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "decompile",
                    Path.Combine(folder, "NestedDirSearchUnderRegSearch.msi"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath
                });

                result.AssertSuccess();

                var actual = File.ReadAllText(outputPath);
                var actualFormatted = XDocument.Parse(actual, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();
                var expected = XDocument.Load(Path.Combine(folder, "DecompiledNestedDirSearchUnderRegSearch.wxs"), LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();

                Assert.Equal(expected, actualFormatted);
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
        public void CanDecompileOldClassTableDefinition()
        {
            // The input MSI was not created using standard methods, it is an example of a real world database that needs to be decompiled.
            // The Class/@Feature_ column has length of 32, the File/@Attributes has length of 2,
            // and numerous foreign key relationships are missing.
            var folder = TestData.Get(@"TestData\Class");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "decompile",
                    Path.Combine(folder, "OldClassTableDef.msi"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath
                });

                result.AssertSuccess();

                var actual = File.ReadAllText(outputPath);
                var actualFormatted = XDocument.Parse(actual, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();
                var expected = XDocument.Load(Path.Combine(folder, "DecompiledOldClassTableDef.wxs"), LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();

                Assert.Equal(expected, actualFormatted);
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
        public void CanDecompileSequenceTables()
        {
            var folder = TestData.Get(@"TestData\SequenceTables");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "decompile",
                    Path.Combine(folder, "SequenceTables.msi"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath
                });

                result.AssertSuccess();

                var actual = File.ReadAllText(outputPath);
                var actualFormatted = XDocument.Parse(actual, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();
                var expected = XDocument.Load(Path.Combine(folder, "DecompiledSequenceTables.wxs"), LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();

                Assert.Equal(expected, actualFormatted);
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
        public void CanDecompileShortcuts()
        {
            var folder = TestData.Get(@"TestData\Shortcut");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "decompile",
                    Path.Combine(folder, "shortcuts.msi"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath
                });

                result.AssertSuccess();

                var actual = File.ReadAllText(outputPath);
                var actualFormatted = XDocument.Parse(actual, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();
                var expected = XDocument.Load(Path.Combine(folder, "DecompiledShortcuts.wxs"), LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo).ToString();

                Assert.Equal(expected, actualFormatted);
            }
        }
    }
}
