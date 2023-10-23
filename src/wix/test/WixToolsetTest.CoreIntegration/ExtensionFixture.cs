// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Example.Extension;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
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
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4Example:Foo\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tBar <optimized>"
            }, results);
        }

        [Fact]
        public void CanRoundtripExampleExtension()
        {
            var folder = TestData.Get(@"TestData", "ExampleExtension");
            var expectedOutputPath = Path.Combine(folder, "Decompiled-Expected.xml");

            var build = new Builder(folder, typeof(ExampleExtensionFactory), new[] { Path.Combine(folder, "data") });
            using (var fs = new DisposableFileSystem())
            {
                var decompileFolder = fs.GetFolder();
                var actualOutputPath = Path.Combine(decompileFolder, "decompiled.xml");

                build.BuildAndDecompileAndBuild(Build, Decompile, actualOutputPath);

                var expected = File.ReadAllLines(expectedOutputPath);
                var actual = File.ReadAllLines(actualOutputPath).Select(ReplaceGuids).ToArray();
                WixAssert.CompareLineByLine(expected, actual);
            }
        }

        private static string ReplaceGuids(string value)
        {
            value = String.IsNullOrWhiteSpace(value) ? value : Regex.Replace(value, @" ProductCode=\""\{[a-fA-F0-9\-]+\}\""", " ProductCode=\"{GUID}\"");
            return String.IsNullOrWhiteSpace(value) ? value : Regex.Replace(value, @" Guid=\""\{[a-fA-F0-9\-]+\}\""", " Guid=\"{GUID}\"");

        }

        [Fact]
        public void CanBuildWithExampleExtension()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
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
                WixAssert.StringEqual(Path.Combine(folder, @"data\example.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"example.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var example = section.Symbols.Where(t => t.Definition.Type == SymbolDefinitionType.MustBeFromAnExtension).Single();
                WixAssert.StringEqual("Foo", example.Id?.Id);
                WixAssert.StringEqual("filF5_pLhBuF5b4N9XEo52g_hUM5Lo", example[0].AsString());
                WixAssert.StringEqual("Bar <optimized>", example[1].AsString());
            }
        }

        [Fact]
        public void CanBuildWithExampleExtensionWithFilePossibleKeyPath()
        {
            var folder = TestData.Get("TestData", "ExampleExtensionUsingPossibleKeyPath");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageWithFile.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, "bin", "extest.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, "bin", "extest.wixpdb"));
                var section = intermediate.Sections.Single();

                var componentSymbol = section.Symbols.OfType<ComponentSymbol>().Single();
                Assert.Equal("ExampleFile", componentSymbol.KeyPath);
                Assert.Equal(ComponentKeyPathType.File, componentSymbol.KeyPathType);
            }
        }

        [Fact]
        public void CanBuildWithExampleExtensionWithRegistryPossibleKeyPath()
        {
            var folder = TestData.Get("TestData", "ExampleExtensionUsingPossibleKeyPath");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageWithRegistry.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, "bin", "extest.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, "bin", "extest.wixpdb"));
                var section = intermediate.Sections.Single();

                var componentSymbol = section.Symbols.OfType<ComponentSymbol>().Single();
                Assert.Equal("RegMadeKeyPath", componentSymbol.KeyPath);
                Assert.Equal(ComponentKeyPathType.Registry, componentSymbol.KeyPathType);
            }
        }

        [Fact]
        public void CanParseCommandLineWithExtension()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-example", "test",
                    "-o", Path.Combine(intermediateFolder, @"bin\extest.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\extest.wixpdb"));
                var section = intermediate.Sections.Single();

                var property = section.Symbols.OfType<PropertySymbol>().Where(p => p.Id.Id == "ExampleProperty").Single();
                WixAssert.StringEqual("ExampleProperty", property.Id.Id);
                WixAssert.StringEqual("test", property.Value);
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

        [Fact]
        public void CanManipulateExtensionCache()
        {
            var currentFolder = Environment.CurrentDirectory;

            try
            {
                using (var fs = new DisposableFileSystem())
                {
                    var folder = fs.GetFolder(true);
                    Environment.CurrentDirectory = folder;

                    var result = WixRunner.Execute(new[]
                    {
                        "extension", "add", "WixToolset.UI.wixext"
                    });

                    result.AssertSuccess();

                    var cacheFolder = Path.Combine(folder, ".wix", "extensions", "WixToolset.UI.wixext");
                    Assert.True(Directory.Exists(cacheFolder), $"Expected folder '{cacheFolder}' to exist");

                    result = WixRunner.Execute(new[]
                    {
                        "extension", "list"
                    });

                    result.AssertSuccess();
                    var output = result.Messages.Select(m => m.ToString()).Single();
                    Assert.StartsWith("WixToolset.UI.wixext 4.", output);
                    Assert.DoesNotContain("damaged", output);

                    result = WixRunner.Execute(new[]
                    {
                        "extension", "remove", "WixToolset.UI.wixext"
                    });

                    result.AssertSuccess();
                    Assert.False(Directory.Exists(cacheFolder), $"Expected folder '{cacheFolder}' to NOT exist");
                }
            }
            finally
            {
                Environment.CurrentDirectory = currentFolder;
            }
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }

        private static void Decompile(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }
    }
}
