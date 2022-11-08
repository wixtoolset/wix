// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using Xunit;

    public class MsiCabinetFixture
    {
        [Fact]
        public void CanBuildSingleFileCompressed()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\example.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(Path.Combine(folder, @"data\test.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"test.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildSingleFileCompressedWithMediaTemplate()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-d", "MediaTemplateCompressionLevel",
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\cab1.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildSingleFileCompressedWithMediaTemplateWithLowCompression()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-d", "MediaTemplateCompressionLevel=low",
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\low1.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildMultipleFilesCompressed()
        {
            var folder = TestData.Get(@"TestData\MultiFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    "-sw1079", // TODO: why does this test need to create a second cab which is empty?
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, "bin", "test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, "bin", "example1.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, "bin", "example2.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, "bin", "test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildMultipleFilesSpanningCabinets()
        {
            var folder = TestData.Get(@"TestData", "MsiCabinet");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var dataFolder = Path.Combine(folder, "data");
                var gendataFolder = Path.Combine(baseFolder, "generated-data");
                var cabFolder = Path.Combine(baseFolder, "cab");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var msiPath = Path.Combine(binFolder, "test.msi");
                var wixpdbPath = Path.ChangeExtension(msiPath, "wixpdb");

                TestData.CreateFile(Path.Combine(gendataFolder, "abc.gen"), (long)(25 * 1024 * 1024), fill: true);
                TestData.CreateFile(Path.Combine(gendataFolder, "mno.gen"), (long)(45 * 1024 * 1024), fill: true);
                TestData.CreateFile(Path.Combine(gendataFolder, "xyz.gen"), (long)(25 * 1024 * 1024), fill: true);

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MultiFileSpanningCabinets.wxs"),
                    "-bindpath", dataFolder,
                    "-bindpath", gendataFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-cc", cabFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var files = Directory.GetFiles(binFolder, "*.*", SearchOption.AllDirectories).Select(s => s.Substring(binFolder.Length + 1)).OrderBy(s => s).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "cab1.cab",
                    "cab1a.cab",
                    "cab2.cab",
                    "cab3.cab",
                    "cab3a.cab",
                    "cab3b.cab",
                    "cab4.cab",
                    "cab5.cab",
                    "cab5a.cab",
                    "test.msi",
                    "test.wixpdb"
                }, files);

                var query = Query.QueryDatabase(msiPath, new[] { "Media", "File" });
                WixAssert.CompareLineByLine(new[]
                {
                    "File:fil2WOk5jeIBsvmL0db5z96JfeEZoU\tfil2WOk5jeIBsvmL0db5z96JfeEZoU\thij.txt\t18\t\t\t512\t3",
                    "File:filgErUV04C8ZBKWWWA0Zg5Fu_6NyM\tfilgErUV04C8ZBKWWWA0Zg5Fu_6NyM\tmno.gen\t47185920\t\t\t512\t4",
                    "File:filGqbzmUDXsQhijNpBL2rNX3dtCoo\tfilGqbzmUDXsQhijNpBL2rNX3dtCoo\tced.txt\t18\t\t\t512\t2",
                    "File:filj2uZN0Q5bCgR2YY6RRg1c9FqylQ\tfilj2uZN0Q5bCgR2YY6RRg1c9FqylQ\ttuv.txt\t18\t\t\t512\t6",
                    "File:filjEKfcIaBHXFyDRtZciPv4j105jQ\tfiljEKfcIaBHXFyDRtZciPv4j105jQ\tqrs.txt\t18\t\t\t512\t5",
                    "File:filKv93aSvdbL6M6UutiKdGim1UzcA\tfilKv93aSvdbL6M6UutiKdGim1UzcA\tabc.gen\t26214400\t\t\t512\t1",
                    "File:fillf1kS2G7fDKwbyrQIIw1OXPi.eY\tfillf1kS2G7fDKwbyrQIIw1OXPi.eY\txyz.gen\t26214400\t\t\t512\t7",
                    "Media:1\t1\t\tcab1.cab\t\t",
                    "Media:2\t1\t\tcab1a.cab\t\t",
                    "Media:3\t3\t\tcab2.cab\t\t",
                    "Media:4\t4\t\tcab3.cab\t\t",
                    "Media:5\t4\t\tcab3a.cab\t\t",
                    "Media:6\t4\t\tcab3b.cab\t\t",
                    "Media:7\t6\t\tcab4.cab\t\t",
                    "Media:8\t7\t\tcab5.cab\t\t",
                    "Media:9\t7\t\tcab5a.cab\t\t",
                }, query);

                var wixpdb = WixOutput.Read(wixpdbPath);

                var data = WindowsInstallerData.Load(wixpdb);
                var fileRows = data.Tables["File"].Rows.Cast<FileRow>().OrderBy(f => f.Sequence);
                var mediaRows = data.Tables["Media"].Rows.Cast<MediaRow>().OrderBy(f => f.DiskId);

                var intermediate = Intermediate.Load(wixpdb);
                var section = intermediate.Sections.Single();
                var fileSymbols = section.Symbols.OfType<FileSymbol>().OrderBy(f => f.Sequence);
                var mediaSymbols = section.Symbols.OfType<MediaSymbol>().OrderBy(f => f.DiskId);

                var expectedFiles = new[]
                {
                    "filKv93aSvdbL6M6UutiKdGim1UzcA abc.gen 1 1",
                    "filGqbzmUDXsQhijNpBL2rNX3dtCoo ced.txt 2 3",
                    "fil2WOk5jeIBsvmL0db5z96JfeEZoU hij.txt 3 3",
                    "filgErUV04C8ZBKWWWA0Zg5Fu_6NyM mno.gen 4 4",
                    "filjEKfcIaBHXFyDRtZciPv4j105jQ qrs.txt 5 7",
                    "filj2uZN0Q5bCgR2YY6RRg1c9FqylQ tuv.txt 6 7",
                    "fillf1kS2G7fDKwbyrQIIw1OXPi.eY xyz.gen 7 8",
                };

                var expectedMedia = new[]
                {
                    "1 cab1.cab 1",
                    "2 cab1a.cab 1",
                    "3 cab2.cab 3",
                    "4 cab3.cab 4",
                    "5 cab3a.cab 4",
                    "6 cab3b.cab 4",
                    "7 cab4.cab 6",
                    "8 cab5.cab 7",
                    "9 cab5a.cab 7",
                };

                WixAssert.CompareLineByLine(expectedFiles, fileRows.Select(r => String.Join(" ", r.File, r.FileName, r.Sequence, r.DiskId)).ToArray());

                WixAssert.CompareLineByLine(expectedFiles, fileSymbols.Select(s => String.Join(" ", s.Id.Id, s.Name, s.Sequence, s.DiskId)).ToArray());

                WixAssert.CompareLineByLine(expectedMedia, mediaRows.Select(r => String.Join(" ", r.DiskId, r.Cabinet, r.LastSequence)).ToArray());

                WixAssert.CompareLineByLine(expectedMedia, mediaSymbols.Select(s => String.Join(" ", s.DiskId, s.Cabinet, s.LastSequence)).ToArray());
            }
        }
    }
}
