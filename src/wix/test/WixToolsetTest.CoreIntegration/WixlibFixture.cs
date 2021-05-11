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

    public class WixlibFixture
    {
        [Fact]
        public void CanBuildSimpleBundleUsingWixlib()
        {
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
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildWixlibWithBinariesFromNamedBindPaths()
        {
            var folder = TestData.Get(@"TestData\WixlibWithBinaries");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-bf",
                    "-bindpath", Path.Combine(folder, "data"),
                    // Use names that aren't excluded in default .gitignores.
                    "-bindpath", $"AlphaBits={Path.Combine(folder, "data", "alpha")}",
                    "-bindpath", $"MipsBits={Path.Combine(folder, "data", "mips")}",
                    "-bindpath", $"PowerBits={Path.Combine(folder, "data", "powerpc")}",
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();

                var wixlib = Intermediate.Load(wixlibPath);
                var binarySymbols = wixlib.Sections.SelectMany(s => s.Symbols).OfType<BinarySymbol>().ToList();
                Assert.Equal(3, binarySymbols.Count);
                Assert.Single(binarySymbols.Where(t => t.Data.Path == "wix-ir/foo.dll"));
                Assert.Single(binarySymbols.Where(t => t.Data.Path == "wix-ir/foo.dll-1"));
                Assert.Single(binarySymbols.Where(t => t.Data.Path == "wix-ir/foo.dll-2"));
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

                var wixFile = section.Symbols.OfType<FileSymbol>().First();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[FileSymbolFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[FileSymbolFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanOverridePathWixVariable()
        {
            var folder = TestData.Get(@"TestData\WixVariableOverride");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-bf",
                    "-bindpath", Path.Combine(folder, "data"),
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

                var wixFile = section.Symbols.OfType<BinarySymbol>().First();
                Assert.Equal(Path.Combine(folder, @"data\test2.txt"), wixFile.Data.Path);
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

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var example = section.Symbols.Where(t => t.Definition.Type == SymbolDefinitionType.MustBeFromAnExtension).Single();
                Assert.Equal("Foo", example.Id?.Id);
                Assert.Equal("Bar", example[0].AsString());
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

                var fileSymbols = section.Symbols.OfType<FileSymbol>().OrderBy(t => Path.GetFileName(t.Source.Path)).ToArray();
                Assert.Equal(Path.Combine(folder, @"data\example.txt"), fileSymbols[0][FileSymbolFields.Source].AsPath().Path);
                Assert.Equal(@"example.txt", fileSymbols[0][FileSymbolFields.Source].PreviousValue.AsPath().Path);
                Assert.Equal(Path.Combine(folder, @"data\other.txt"), fileSymbols[1][FileSymbolFields.Source].AsPath().Path);
                Assert.Equal(@"other.txt", fileSymbols[1][FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var examples = section.Symbols.Where(t => t.Definition.Type == SymbolDefinitionType.MustBeFromAnExtension).ToArray();
                Assert.Equal(new string[] { "Foo", "Other" }, examples.Select(t => t.Id?.Id).ToArray());
                Assert.Equal(new[] { "Bar", "Value" }, examples.Select(t => t[0].AsString()).ToArray());
            }
        }
    }
}
