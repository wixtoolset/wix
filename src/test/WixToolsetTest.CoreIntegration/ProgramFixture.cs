// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
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
                var intermediateFolder = fs.GetFolder();

                var program = new Program();
                var result = program.Run(new WixToolsetServiceProvider(), new[]
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
                Assert.Single(intermediate.Sections);

                var wixFile = intermediate.Sections.SelectMany(s => s.Tuples).OfType<WixFileTuple>().Single();
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
                var result = program.Run(new WixToolsetServiceProvider(), new[]
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
                Assert.Single(intermediate.Sections);

                var wixFile = intermediate.Sections.SelectMany(s => s.Tuples).OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
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
                var result = program.Run(new WixToolsetServiceProvider(), new[]
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
                Assert.Single(intermediate.Sections);

                var wixFile = intermediate.Sections.SelectMany(s => s.Tuples).OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
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
                var result = program.Run(new WixToolsetServiceProvider(), new[]
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
