// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolsetTest.CoreIntegration.Utility;
    using Xunit;

    public class ProgramFixture
    {
        [Fact]
        public void CanBuildSingleFile()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                Assert.Equal(0, result);

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wir"));
                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildSingleFileCompressed()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                Assert.Equal(0, result);

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\example.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wir"));
                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildSingleFileCompressedWithMediaTemplate()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
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

                Assert.Equal(0, result);

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

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
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

                Assert.Equal(0, result);

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\lowcab1.cab")));
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

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
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

                Assert.Equal(0, result);

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\cab1.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildSimpleModule()
        {
            var folder = TestData.Get(@"TestData\SimpleModule");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
                {
                    "build",
                    Path.Combine(folder, "Module.wxs"),
                    "-loc", Path.Combine(folder, "Module.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msm")
                });

                Assert.Equal(0, result);

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msm")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wir"));
                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildManualUpgrade()
        {
            var folder = TestData.Get(@"TestData\ManualUpgrade");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                Assert.Equal(0, result);

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wir"));
                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildWixout()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.wixout")
                });

                Assert.Equal(0, result);

                var builtFiles = Directory.GetFiles(Path.Combine(baseFolder, @"bin"));

                Assert.Equal(new[]{
                    "test.wixout"
                }, builtFiles.Select(Path.GetFileName).ToArray());
            }
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CanBuildInstanceTransform()
        {
            var folder = TestData.Get(@"TestData\InstanceTransform");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                Assert.Equal(0, result);

                var pdb = Pdb.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"), false);
                Assert.NotEmpty(pdb.Output.SubStorages);
            }
        }
    }
}
