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
    using WixToolset.Data.Symbols;
    using Xunit;

    public class ExtensionFixture
    {
        [Fact]
        public void CanBuildAndQuery()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");
            var build = new Builder(folder, typeof(ExampleExtensionFactory), new[] { Path.Combine(folder, "data") });

            var results = build.BuildAndQuery(Build, "Wix4Example");
            Assert.Equal(new[]
            {
                "Wix4Example:Foo\tBar"
            }, results);
        }

        [Fact]
        public void CanBuildWithExampleExtension()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
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

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\extest.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\extest.wixpdb")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\PFiles\MsiPackage\example.txt")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\extest.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var example = section.Symbols.Where(t => t.Definition.Type == SymbolDefinitionType.MustBeFromAnExtension).Single();
                Assert.Equal("Foo", example.Id?.Id);
                Assert.Equal("Bar", example[0].AsString());
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

                var result = WixRunner.Execute(new[]
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

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\extest.wixpdb"));
                var section = intermediate.Sections.Single();

                var property = section.Symbols.OfType<PropertySymbol>().Where(p => p.Id.Id == "ExampleProperty").Single();
                Assert.Equal("ExampleProperty", property.Id.Id);
                Assert.Equal("test", property.Value);
            }
        }

        [Fact]
        public void CannotBuildWithMissingExtension()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var exception = Assert.Throws<WixException>(() =>
                    WixRunner.Execute(new[]
                    {
                        "build",
                        Path.Combine(folder, "Package.wxs"),
                        "-ext", "ExampleExtension.DoesNotExist"
                    }));

                Assert.StartsWith("The extension 'ExampleExtension.DoesNotExist' could not be found. Checked paths: ", exception.Message);
            }
        }

        [Fact]
        public void CannotBuildWithMissingVersionedExtension()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var exception = Assert.Throws<WixException>(() =>
                    WixRunner.Execute(new[]
                    {
                        "build",
                        Path.Combine(folder, "Package.wxs"),
                        "-ext", "ExampleExtension.DoesNotExist/1.0.0"
                    }));

                Assert.StartsWith("The extension 'ExampleExtension.DoesNotExist/1.0.0' could not be found. Checked paths: ", exception.Message);
            }
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
