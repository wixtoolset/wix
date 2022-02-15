// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Example.Extension;
    using Xunit;

    public class WixiplFixture
    {
        [Fact(Skip = "xxxxx CodeBase Issue: We can't determine the file path from which we were loaded. xxxxx")]
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

                var fileSymbol = section.Symbols.OfType<FileSymbol>().First();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);
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

        [Fact(Skip = "xxxxx CodeBase Issue: We can't determine the file path from which we were loaded. xxxxx")]
        public void CanBuildMsiUsingExtensionLibrary()
#if !(NET461 || NET472 || NET48 || NETCOREAPP3_1 || NET5_0)
        {
            throw new System.NotImplementedException();
        }
#else
        {
            var folder = TestData.Get(@"TestData\Wixipl");
#if NET461 || NET472 || NET48
            var extensionPath = (new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase)).LocalPath;
#else // NETCOREAPP3_1 || NET5_0
            var extensionPath = typeof(ExampleExtensionFactory).Assembly.Location;
#endif

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
                    var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                    Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                    Assert.Equal(@"test.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);
                }

                {
                    var binary = section.Symbols.OfType<BinarySymbol>().Single();
                    var path = binary[BinarySymbolFields.Data].AsPath().Path;
                    Assert.StartsWith(Path.Combine(baseFolder, @"obj\Example.Extension"), path);
                    Assert.EndsWith(@"wix-ir\example.txt", path);
                    Assert.Equal(@"BinFromWir", binary.Id.Id);
                }
            }
        }
#endif

        [Fact(Skip = "xxxxx CodeBase Issue: We can't determine the file path from which we were loaded. xxxxx")]
        public void CanBuildWixiplUsingExtensionLibrary()
#if !(NET461 || NET472 || NET48 || NETCOREAPP3_1 || NET5_0)
        {
            throw new System.NotImplementedException();
        }
#else
        {
            var folder = TestData.Get(@"TestData\Wixipl");
#if NET461 || NET472 || NET48
            var extensionPath = (new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase)).LocalPath;
#else // NETCOREAPP3_1 || NET5_0
            var extensionPath = typeof(ExampleExtensionFactory).Assembly.Location;
#endif

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
                    var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                    Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                    Assert.Equal(@"test.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);
                }

                {
                    var binary = section.Symbols.OfType<BinarySymbol>().Single();
                    var path = binary[BinarySymbolFields.Data].AsPath().Path;
                    Assert.StartsWith(Path.Combine(baseFolder, @"obj\test"), path);
                    Assert.EndsWith(@"wix-ir\example.txt", path);
                    Assert.Equal(@"BinFromWir", binary.Id.Id);
                }
            }
        }
#endif
    }
}
