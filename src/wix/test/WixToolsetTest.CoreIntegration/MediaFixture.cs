// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class MediaFixture
    {
        [Fact]
        public void CanBuildMultiMedia()
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
                    Path.Combine(folder, "Media", "MultiMedia.wxs"),
                    "-bindpath", Path.Combine(folder, "Media", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var mediaSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.MediaSymbol>().OrderBy(m => m.DiskId).ToList();
                var fileSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.FileSymbol>().OrderBy(f => f.Sequence).ToList();
                Assert.Equal(1, mediaSymbols[0].DiskId);
                Assert.Equal(2, mediaSymbols[0].LastSequence);
                Assert.Equal(2, mediaSymbols[1].DiskId);
                Assert.Equal(4, mediaSymbols[1].LastSequence);
                Assert.Equal(new[]
                {
                    "a1.txt",
                    "a2.txt",
                    "b1.txt",
                    "b2.txt",
                }, fileSymbols.Select(f => f.Name).ToArray());
                Assert.Equal(new[]
                {
                    1,
                    2,
                    3,
                    4,
                }, fileSymbols.Select(f => f.Sequence).ToArray());
            }
        }
    }
}
