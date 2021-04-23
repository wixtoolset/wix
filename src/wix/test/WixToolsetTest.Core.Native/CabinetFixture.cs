// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreNative
{
    using System.IO;
    using System.Linq;
    using WixToolset.Core.Native;
    using WixToolsetTest.CoreNative.Utility;
    using WixToolset.Data;
    using Xunit;

    public class CabinetFixture
    {
        [Fact]
        public void CanCreateSingleFileCabinet()
        {
            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder(true);
                var cabPath = Path.Combine(intermediateFolder, "testout.cab");

                var files = new[] { new CabinetCompressFile(TestData.Get(@"TestData\test.txt"), "test.txt") };

                var cabinet = new Cabinet(cabPath);
                cabinet.Compress(files, CompressionLevel.Low);

                Assert.True(File.Exists(cabPath));
            }
        }

        [Fact]
        public void CanEnumerateSingleFileCabinet()
        {
            var cabinetPath = TestData.Get(@"TestData\test.cab");

            var cabinet = new Cabinet(cabinetPath);
            var files = cabinet.Enumerate();

            var file = files.Single();
            Assert.Equal("test.txt", file.FileId);
            Assert.Equal(17, file.Size);

            Assert.Equal(19259, file.Date);
            Assert.Equal(47731, file.Time);
            // TODO: This doesn't seem to always pass, not clear why but it'd be good to understand one day.
            // Assert.True(file.SameAsDateTime(new DateTime(2017, 9, 28, 0, 19, 38)));
        }

        [Fact]
        public void IntegrationTest()
        {
            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder(true);
                var cabinetPath = Path.Combine(intermediateFolder, "testout.cab");
                var extractFolder = fs.GetFolder(true);

                // Compress.
                {
                    var files = new[] {
                        new CabinetCompressFile(TestData.Get(@"TestData\test.txt"), "test1.txt"),
                        new CabinetCompressFile(TestData.Get(@"TestData\test.txt"), "test2.txt"),
                    };

                    var cabinet = new Cabinet(cabinetPath);
                    cabinet.Compress(files, CompressionLevel.Low);
                }

                // Extract.
                {
                    var cabinet = new Cabinet(cabinetPath);
                    var reportedFiles = cabinet.Extract(extractFolder);
                    Assert.Equal(2, reportedFiles.Count());
                }

                // Enumerate to compare cabinet to extracted files.
                {
                    var cabinet = new Cabinet(cabinetPath);
                    var enumerated = cabinet.Enumerate().OrderBy(f => f.FileId).ToArray();

                    var files = Directory.EnumerateFiles(extractFolder).OrderBy(f => f).ToArray();

                    for (var i = 0; i < enumerated.Length; ++i)
                    {
                        var cabFileInfo = enumerated[i];
                        var fileInfo = new FileInfo(files[i]);

                        Assert.Equal(cabFileInfo.FileId, fileInfo.Name);
                        Assert.Equal(cabFileInfo.Size, fileInfo.Length);
                        Assert.True(cabFileInfo.SameAsDateTime(fileInfo.CreationTime));
                    }
                }
            }
        }
    }
}
