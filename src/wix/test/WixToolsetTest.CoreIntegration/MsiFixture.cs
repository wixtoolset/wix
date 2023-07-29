// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class MsiFixture
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
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\PFiles\Example Corporation MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));

                Assert.False(intermediate.HasLevel(WixToolset.Data.IntermediateLevels.Compiled));
                Assert.True(intermediate.HasLevel(WixToolset.Data.IntermediateLevels.Linked));
                Assert.True(intermediate.HasLevel(WixToolset.Data.IntermediateLevels.Resolved));
                Assert.True(intermediate.HasLevel(WixToolset.Data.WindowsInstaller.IntermediateLevels.FullyBound));

                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().First();
                WixAssert.StringEqual(Path.Combine(folder, @"data\test.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"test.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CannotBuildMissingFile()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "does-not-exist"),
                    "-bindpath", Path.Combine(folder, "also-does-not-exist"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(binFolder, "test.msi")
                }, out var messages);
                Assert.Equal(103, result);

                var errorMessages = messages.Select(m => m.ToString().Replace(folder, "<folder>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"Cannot find the File file 'test.txt'. The following paths were checked: test.txt, <folder>\does-not-exist\test.txt, <folder>\also-does-not-exist\test.txt",
                    @"Cannot find the File file 'test.txt'. The following paths were checked: test.txt, <folder>\does-not-exist\test.txt, <folder>\also-does-not-exist\test.txt",
                }, errorMessages);

                var errorMessage = errorMessages.First();
                var checkedPaths = errorMessage.Substring(errorMessage.IndexOf(':') + 1).Split(new[] { ',' }).Select(s => s.Trim()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "test.txt",
                    Path.Combine("<folder>", "does-not-exist", "test.txt"),
                    Path.Combine("<folder>", "also-does-not-exist", "test.txt"),
                }, checkedPaths);
            }
        }

        [Fact]
        public void CanBuildWithErrorTable()
        {
            var folder = TestData.Get(@"TestData\ErrorsInUI");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\PFiles\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var errors = section.Symbols.OfType<ErrorSymbol>().ToDictionary(t => t.Id.Id);
                WixAssert.StringEqual("Category 55 Emergency Doomsday Crisis", errors["1234"].Message.Trim());
                WixAssert.StringEqual(" ", errors["5678"].Message);

                var customAction1 = section.Symbols.OfType<CustomActionSymbol>().Where(t => t.Id.Id == "CanWeReferenceAnError_YesWeCan").Single();
                WixAssert.StringEqual("1234", customAction1.Target);

                var customAction2 = section.Symbols.OfType<CustomActionSymbol>().Where(t => t.Id.Id == "TextErrorsWorkOKToo").Single();
                WixAssert.StringEqual("If you see this, something went wrong.", customAction2.Target);
            }
        }

        [Fact]
        public void CanLoadPdbGeneratedByBuild()
        {
            var folder = TestData.Get(@"TestData\MultiFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-d", "MediaTemplateCompressionLevel",
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\cab1.cab")));

                var pdbPath = Path.Combine(intermediateFolder, @"bin\test.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var output = WindowsInstallerData.Load(pdbPath, suppressVersionCheck: true);
                Assert.NotNull(output);
            }
        }

        [Fact]
        public void CanLoadPdbGeneratedByBuildViaWixOutput()
        {
            var folder = TestData.Get(@"TestData\MultiFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-d", "MediaTemplateCompressionLevel",
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\cab1.cab")));

                var pdbPath = Path.Combine(intermediateFolder, @"bin\test.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var wixOutput = WixOutput.Read(pdbPath);
                var output = WindowsInstallerData.Load(wixOutput, suppressVersionCheck: true);
                Assert.NotNull(output);
            }
        }

        [Fact]
        public void CanBuildManualUpgrade()
        {
            var folder = TestData.Get(@"TestData\ManualUpgrade");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                }, out var messages);

                Assert.Equal(0, result);

                var pdbPath = Path.Combine(intermediateFolder, @"bin\test.wixpdb");
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(pdbPath));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\PFiles\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var upgradeSymbol = section.Symbols.OfType<UpgradeSymbol>().Single();
                Assert.False(upgradeSymbol.ExcludeLanguages);
                Assert.True(upgradeSymbol.IgnoreRemoveFailures);
                Assert.False(upgradeSymbol.VersionMaxInclusive);
                Assert.True(upgradeSymbol.VersionMinInclusive);
                Assert.Equal("13.0.0", upgradeSymbol.VersionMax);
                Assert.Equal("12.0.0", upgradeSymbol.VersionMin);
                Assert.False(upgradeSymbol.OnlyDetect);
                Assert.Equal("BLAHBLAHBLAH", upgradeSymbol.ActionProperty);

                var pdb = WindowsInstallerData.Load(pdbPath, suppressVersionCheck: false);
                var secureProperties = pdb.Tables["Property"].Rows.Where(row => row.GetKey() == "SecureCustomProperties").Single();
                Assert.Contains("BLAHBLAHBLAH", secureProperties.FieldAsString(1));
            }
        }

        [Fact]
        public void CanBuildWixipl()
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
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.wixipl")
                }, out var messages);

                Assert.Equal(0, result);

                var builtFiles = Directory.GetFiles(Path.Combine(baseFolder, @"bin"));

                WixAssert.CompareLineByLine(new[]{
                    "test.wixipl"
                }, builtFiles.Select(Path.GetFileName).ToArray());
            }
        }

        [Fact]
        public void CanBuildWixlib()
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
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.wixlib")
                }, out var messages);

                Assert.Equal(0, result);

                var builtFiles = Directory.GetFiles(Path.Combine(baseFolder, @"bin"));

                WixAssert.CompareLineByLine(new[]{
                    "test.wixlib"
                }, builtFiles.Select(Path.GetFileName).ToArray());
            }
        }

        [Fact]
        public void CanBuildBinaryWixlib()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindfiles",
                    "-o", Path.Combine(baseFolder, @"bin\test.wixlib"));

                result.AssertSuccess();

                using (var wixout = WixOutput.Read(Path.Combine(baseFolder, @"bin\test.wixlib")))
                {
                    Assert.NotNull(wixout.GetDataStream("wix-ir.json"));

                    var text = wixout.GetData("wix-ir/test.txt");
                    WixAssert.StringEqual("This is test.txt.", text);
                }
            }
        }

        [Fact]
        public void CanBuildBinaryWixlibWithCollidingFilenames()
        {
            var folder = TestData.Get(@"TestData\SameFileFolders");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(
                    "build",
                    Path.Combine(folder, "TestComponents.wxs"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindfiles",
                    "-o", Path.Combine(baseFolder, @"bin\test.wixlib"));

                result.AssertSuccess();

                using (var wixout = WixOutput.Read(Path.Combine(baseFolder, @"bin\test.wixlib")))
                {
                    Assert.NotNull(wixout.GetDataStream("wix-ir.json"));

                    var text = wixout.GetData("wix-ir/test.txt");
                    WixAssert.StringEqual(@"This is a\test.txt.", text);

                    var text2 = wixout.GetData("wix-ir/test.txt-1");
                    WixAssert.StringEqual(@"This is b\test.txt.", text2);

                    var text3 = wixout.GetData("wix-ir/test.txt-2");
                    WixAssert.StringEqual(@"This is c\test.txt.", text3);
                }
            }
        }

        [Fact]
        public void CanBuildWithIncludePath()
        {
            var folder = TestData.Get(@"TestData\IncludePath");
            var bindpath = Path.Combine(folder, "data");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", bindpath,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi"),
                    "-i", bindpath);

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\PFiles\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(Path.Combine(folder, @"data\test.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"test.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var featureSymbol = Assert.Single(section.Symbols.OfType<FeatureSymbol>());
                WixAssert.StringEqual("MsiPackage", featureSymbol.Title);
            }
        }

        [Fact]
        public void CanBuild64bit()
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
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-arch", "x64",
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var platformSummary = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.PlatformAndLanguage);
                Assert.Equal("x64;1033", platformSummary.Value);
            }
        }

        [Fact]
        public void CanBuildSharedComponent()
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
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-arch", "x64",
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                // Only one component is shared.
                var sharedComponentSymbols = section.Symbols.OfType<ComponentSymbol>();
                Assert.Equal(1, sharedComponentSymbols.Sum(t => t.Shared ? 1 : 0));

                // And it is this one.
                var sharedComponentSymbol = sharedComponentSymbols.Single(t => t.Id.Id == "Shared.dll");
                Assert.True(sharedComponentSymbol.Shared);
            }
        }

        [Fact]
        public void CannotBuildBadProperty()
        {
            var folder = TestData.Get(@"TestData", "Property");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BadProperty.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, "bin", "test.msi")
                });

                var messages = result.Messages.Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "The 'Break' Property contains '[X]' in its value which is an illegal reference to another property. If this value is a string literal, not a property reference, please ignore this warning. To set a property with the value of another property, use a CustomAction with Property and Value attributes.",
                    "The 'Break' Property contains '[Y]' in its value which is an illegal reference to another property. If this value is a string literal, not a property reference, please ignore this warning. To set a property with the value of another property, use a CustomAction with Property and Value attributes.",
                }, messages);
            }
        }

        [Fact]
        public void CanBuildVersionIndependentProgId()
        {
            var folder = TestData.Get(@"TestData\ProgId");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\PFiles\MsiPackage\Foo.exe")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var progids = section.Symbols.OfType<ProgIdSymbol>().OrderBy(symbol => symbol.ProgId).ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "Foo.File.hol",
                    "Foo.File.hol.15"
                }, progids.Select(p => p.ProgId).ToArray());

                WixAssert.CompareLineByLine(new[]
                {
                    "Foo.File.hol.15",
                    null
                }, progids.Select(p => p.ParentProgIdRef).ToArray());
            }
        }

        [Fact]
        public void FailsBuildAtBindTimeForMissingEnsureTable()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BadEnsureTable", "BadEnsureTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });
                Assert.Collection(result.Messages,
                    first =>
                    {
                        Assert.Equal(MessageLevel.Error, first.Level);
                        WixAssert.StringEqual("Cannot find the table definitions for the 'TableDefinitionNotExposedByExtension' table. This is likely due to a typing error or missing extension. Please ensure all the necessary extensions are supplied on the command line with the -ext parameter.", first.ToString());
                    });

                Assert.False(File.Exists(msiPath));
            }
        }
    }
}
