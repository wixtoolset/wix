// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using Example.Extension;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class WixlibFixture
    {
        [Fact]
        public void CanBuildSingleFileUsingWixlib()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"test.wixlib")
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-lib", Path.Combine(intermediateFolder, @"test.wixlib"),
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
        public void CanBuildWithExtensionUsingWixlib()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-ext", extensionPath,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"test.wixlib")
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-lib", Path.Combine(intermediateFolder, @"test.wixlib"),
                    "-ext", extensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"test.wir"));
                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);

                var example = section.Tuples.Where(t => t.Definition.Type == TupleDefinitionType.MustBeFromAnExtension).Single();
                Assert.Equal("Foo", example.Id.Id);
                Assert.Equal("Foo", example[0].AsString());
                Assert.Equal("Bar", example[1].AsString());
            }
        }

        [Fact]
        public void CanBuildWithExtensionUsingMultipleWixlibs()
        {
            var folder = TestData.Get(@"TestData\ComplexExampleExtension");
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-ext", extensionPath,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"components.wixlib")
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "OtherComponents.wxs"),
                    "-ext", extensionPath,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"other.wixlib")
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-lib", Path.Combine(intermediateFolder, @"components.wixlib"),
                    "-lib", Path.Combine(intermediateFolder, @"other.wixlib"),
                    "-ext", extensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"test.wir"));
                var section = intermediate.Sections.Single();

                var wixFiles = section.Tuples.OfType<WixFileTuple>().OrderBy(t => Path.GetFileName(t.Source.Path)).ToArray();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), wixFiles[0][WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", wixFiles[0][WixFileTupleFields.Source].PreviousValue.AsPath().Path);
                Assert.Equal(Path.Combine(folder, @"data\other.txt"), wixFiles[1][WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"other.txt", wixFiles[1][WixFileTupleFields.Source].PreviousValue.AsPath().Path);

                var examples = section.Tuples.Where(t => t.Definition.Type == TupleDefinitionType.MustBeFromAnExtension).ToArray();
                Assert.Equal(new[] { "Foo", "Other" }, examples.Select(t => t.Id.Id).ToArray());
                Assert.Equal(new[] { "Foo", "Other" }, examples.Select(t => t.AsString(0)).ToArray());
                Assert.Equal(new[] { "Bar", "Value" }, examples.Select(t => t[1].AsString()).ToArray());
            }
        }
    }
}
