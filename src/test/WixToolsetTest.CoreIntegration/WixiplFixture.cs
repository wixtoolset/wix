// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using Example.Extension;
    using Xunit;

    public class WixiplFixture
    {
        [Fact]
        public void CanBuildSingleFile()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixiplPath = Path.Combine(intermediateFolder, @"test.wixipl");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixiplPath,
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(wixiplPath);

                Assert.False(intermediate.HasLevel(IntermediateLevels.Compiled));
                Assert.True(intermediate.HasLevel(IntermediateLevels.Linked));
                Assert.False(intermediate.HasLevel(IntermediateLevels.Resolved));

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(intermediateFolder, @"test.wixipl"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));

                Assert.False(intermediate.HasLevel(IntermediateLevels.Compiled));
                Assert.True(intermediate.HasLevel(IntermediateLevels.Linked));
                Assert.True(intermediate.HasLevel(IntermediateLevels.Resolved));

                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().First();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CannotBuildWithSourceFileAndWixipl()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"test.wixipl")
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(intermediateFolder, @"test.wixipl"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });
                Assert.Equal((int)ErrorMessages.Ids.WixiplSourceFileIsExclusive, result.ExitCode);
            }
        }

        [Fact]
        public void CanBuildMsiUsingExtensionLibrary()
        {
            var folder = TestData.Get(@"TestData\Wixipl");
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    "-ext", extensionPath,
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi"),
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                {
                    var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                    Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                    Assert.Equal(@"test.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
                }

                {
                    var binary = section.Tuples.OfType<BinaryTuple>().Single();
                    var path = binary[BinaryTupleFields.Data].AsPath().Path;
                    Assert.StartsWith(Path.Combine(baseFolder, @"obj\Example.Extension"), path);
                    Assert.EndsWith(@"wix-ir\example.txt", path);
                    Assert.Equal(@"BinFromWir", binary.Id.Id);
                }
            }
        }

        [Fact]
        public void CanBuildWixiplUsingExtensionLibrary()
        {
            var folder = TestData.Get(@"TestData\Wixipl");
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    "-ext", extensionPath,
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"test.wixipl"),
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(intermediateFolder, @"test.wixipl"),
                    "-ext", extensionPath,
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi"),
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                {
                    var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                    Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                    Assert.Equal(@"test.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
                }

                {
                    var binary = section.Tuples.OfType<BinaryTuple>().Single();
                    var path = binary[BinaryTupleFields.Data].AsPath().Path;
                    Assert.StartsWith(Path.Combine(baseFolder, @"obj\test"), path);
                    Assert.EndsWith(@"wix-ir\example.txt", path);
                    Assert.Equal(@"BinFromWir", binary.Id.Id);
                }
            }
        }
    }
}
