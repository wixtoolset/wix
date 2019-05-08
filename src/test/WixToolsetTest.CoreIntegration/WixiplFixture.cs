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
                    Path.Combine(intermediateFolder, @"test.wixipl"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"obj\test.wir"));
                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
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

        [Fact(Skip = "Test demonstrates failure")]
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

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"obj\test.wir"));
                var section = intermediate.Sections.Single();

                {
                    var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                    Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                    Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
                }

                {
                    var binary = section.Tuples.OfType<BinaryTuple>().Single();
                    var path = binary[BinaryTupleFields.Data].AsPath().Path;
                    Assert.Contains("Example.Extension", path);
                    Assert.EndsWith(@"\0", path);
                    Assert.Equal(@"BinFromWir", binary[BinaryTupleFields.Name].AsString());
                }
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
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

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"obj\test.wir"));
                var section = intermediate.Sections.Single();

                {
                    var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                    Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                    Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
                }

                {
                    var binary = section.Tuples.OfType<BinaryTuple>().Single();
                    var path = binary[BinaryTupleFields.Data].AsPath().Path;
                    Assert.Contains("Example.Extension", path);
                    Assert.EndsWith(@"\0", path);
                    Assert.Equal(@"BinFromWir", binary[BinaryTupleFields.Name].AsString());
                }
            }
        }
    }
}
