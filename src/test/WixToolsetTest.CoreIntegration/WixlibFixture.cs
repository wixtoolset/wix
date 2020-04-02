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
        public void CanBuildSimpleBundleUsingWixlib()
        {
            var burnStubPath = TestData.Get(@"TestData\.Data\burn.exe");
            var folder = TestData.Get(@"TestData\SimpleBundle");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MultiFileBootstrapperApplication.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"test.wixlib")
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MultiFileBundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-lib", Path.Combine(intermediateFolder, @"test.wixlib"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-burnStub", burnStubPath,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
#if TODO
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
#endif

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"test.wir"));
                var section = intermediate.Sections.Single();

                var bundleTuple = section.Tuples.OfType<WixBundleTuple>().Single();
                Assert.Equal("1.0.0.0", bundleTuple.Version);

                var previousVersion = bundleTuple.Fields[(int)WixBundleTupleFields.Version].PreviousValue;
                Assert.Equal("!(bind.packageVersion.test.msi)", previousVersion.AsString());

                var msiTuple = section.Tuples.OfType<WixBundlePackageTuple>().Single();
                Assert.Equal("test.msi", msiTuple.Id.Id);
            }
        }

        [Fact]
        public void CanBuildSingleFileUsingWixlib()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();

                var wixlib = Intermediate.Load(wixlibPath);

                Assert.True(wixlib.HasLevel(IntermediateLevels.Compiled));
                Assert.True(wixlib.HasLevel(IntermediateLevels.Combined));
                Assert.False(wixlib.HasLevel(IntermediateLevels.Linked));
                Assert.False(wixlib.HasLevel(IntermediateLevels.Resolved));

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

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));

                Assert.False(intermediate.HasLevel(IntermediateLevels.Compiled));
                Assert.False(intermediate.HasLevel(IntermediateLevels.Combined));
                Assert.True(intermediate.HasLevel(IntermediateLevels.Linked));
                Assert.True(intermediate.HasLevel(IntermediateLevels.Resolved));

                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<FileTuple>().First();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[FileTupleFields.Source].PreviousValue.AsPath().Path);
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

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);

                var example = section.Tuples.Where(t => t.Definition.Type == TupleDefinitionType.MustBeFromAnExtension).Single();
                Assert.Null(example.Id?.Id);
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

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileTuples = section.Tuples.OfType<FileTuple>().OrderBy(t => Path.GetFileName(t.Source.Path)).ToArray();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), fileTuples[0][FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", fileTuples[0][FileTupleFields.Source].PreviousValue.AsPath().Path);
                Assert.Equal(Path.Combine(folder, @"data\other.txt"), fileTuples[1][FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"other.txt", fileTuples[1][FileTupleFields.Source].PreviousValue.AsPath().Path);

                var examples = section.Tuples.Where(t => t.Definition.Type == TupleDefinitionType.MustBeFromAnExtension).ToArray();
                Assert.Equal(new string[] { null, null }, examples.Select(t => t.Id?.Id).ToArray());
                Assert.Equal(new[] { "Foo", "Other" }, examples.Select(t => t.AsString(0)).ToArray());
                Assert.Equal(new[] { "Bar", "Value" }, examples.Select(t => t[1].AsString()).ToArray());
            }
        }
    }
}
