// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using Example.Extension;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
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
        public void CanBuildMultiarchWixlib()
        {
            var folder = TestData.Get(@"TestData", "WixlibMultiarch");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MultiarchFile.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    "-arch", "x64",
                    Path.Combine(folder, "MultiarchFile.wxs"),
                    wixlibPath,
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath
                });

                result.AssertSuccess();

                var wixlib = Intermediate.Load(wixlibPath);
                var componentSymbols = wixlib.Sections.SelectMany(s => s.Symbols).OfType<ComponentSymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "x64 filcV1yrx0x8wJWj4qMzcH21jwkPko",
                    "x86 filcV1yrx0x8wJWj4qMzcH21jwkPko",
                }, componentSymbols.Select(c => (c.Win64 ? "x64 " : "x86 ") + c.Id.Id).OrderBy(s => s).ToArray());
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
                Assert.Single(binarySymbols, t => t.Data.Path == "wix-ir/foo.dll");
                Assert.Single(binarySymbols, t => t.Data.Path == "wix-ir/foo.dll-1");
                Assert.Single(binarySymbols, t => t.Data.Path == "wix-ir/foo.dll-2");
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
                WixAssert.StringEqual(Path.Combine(folder, @"data\test.txt"), wixFile[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"test.txt", wixFile[FileSymbolFields.Source].PreviousValue.AsPath().Path);
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
                WixAssert.StringEqual(Path.Combine(folder, @"data\test2.txt"), wixFile.Data.Path);
            }
        }

        [Fact]
        public void CanBuildWithExtensionUsingWixlib()
        {
            var folder = TestData.Get(@"TestData\ExampleExtension");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
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
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(Path.Combine(folder, @"data\example.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"example.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var example = section.Symbols.Where(t => t.Definition.Type == SymbolDefinitionType.MustBeFromAnExtension).Single();
                WixAssert.StringEqual("Foo", example.Id?.Id);
                WixAssert.StringEqual("Bar <optimized>", example[1].AsString());
            }
        }

        [Fact]
        public void CanBuildWithExtensionUsingMultipleWixlibs()
        {
            var folder = TestData.Get(@"TestData\ComplexExampleExtension");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"components.wixlib")
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "OtherComponents.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
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
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileSymbols = section.Symbols.OfType<FileSymbol>().OrderBy(t => Path.GetFileName(t.Source.Path)).ToArray();
                WixAssert.StringEqual(Path.Combine(folder, @"data\example.txt"), fileSymbols[0][FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"example.txt", fileSymbols[0][FileSymbolFields.Source].PreviousValue.AsPath().Path);
                WixAssert.StringEqual(Path.Combine(folder, @"data\other.txt"), fileSymbols[1][FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"other.txt", fileSymbols[1][FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var examples = section.Symbols.Where(t => t.Definition.Type == SymbolDefinitionType.MustBeFromAnExtension).ToArray();
                WixAssert.CompareLineByLine(new[] { "Foo", "Other" }, examples.Select(t => t.Id?.Id).ToArray());
                WixAssert.CompareLineByLine(new[] { "filF5_pLhBuF5b4N9XEo52g_hUM5Lo", "filvxdStJhRE_M5kbpLsTZJXbs34Sg" }, examples.Select(t => t[0].AsString()).ToArray());
                WixAssert.CompareLineByLine(new[] { "Bar <optimized>", "Value <optimized>" }, examples.Select(t => t[1].AsString()).ToArray());
            }
        }
    }
}
