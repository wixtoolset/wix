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
    }
}
