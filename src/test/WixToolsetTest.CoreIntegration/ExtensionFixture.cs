// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using Example.Extension;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolsetTest.CoreIntegration.Utility;
    using Xunit;

    public class ExtensionFixture
    {
        [Fact]
        public void CanBuildWithExampleExtension()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);

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
                    "-ext", extensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\extest.msi")
                });

                Assert.Equal(0, result);

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\extest.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\extest.wixpdb")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\MsiPackage\example.txt")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\extest.wir"));
                Assert.Single(intermediate.Sections);

                var wixFile = intermediate.Sections.SelectMany(s => s.Tuples).OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);

                var example = intermediate.Sections.SelectMany(s => s.Tuples).Where(t => t.Definition.Type == TupleDefinitionType.MustBeFromAnExtension).Single();
                Assert.Equal("Foo", example.Id.Id);
                Assert.Equal("Foo", example[0].AsString());
                Assert.Equal("Bar", example[1].AsString());
            }
        }

        [Fact]
        public void CanParseCommandLineWithExtension()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);

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
                    "-ext", extensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-example", "test",
                    "-o", Path.Combine(intermediateFolder, @"bin\extest.msi")
                });

                Assert.Equal(0, result);

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\extest.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\extest.wixpdb")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\MsiPackage\example.txt")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\extest.wir"));
                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);

                var property = section.Tuples.OfType<PropertyTuple>().Where(p => p.Id.Id == "ExampleProperty").Single();
                Assert.Equal("ExampleProperty", property.Property);
                Assert.Equal("test", property.Value);
            }
        }
    }
}
