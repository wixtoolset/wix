// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
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
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().First();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildSingleFileCompressed()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");

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
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\example.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildSingleFileCompressedWithMediaTemplate()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");

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
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildSingleFileCompressedWithMediaTemplateWithLowCompression()
        {
            var folder = TestData.Get(@"TestData\SingleFileCompressed");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-d", "MediaTemplateCompressionLevel=low",
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\low1.cab")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildMultipleFilesCompressed()
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
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanFailBuildMissingFile()
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
                    "-bindpath", Path.Combine(folder, "does-not-exist"),
                    "-bindpath", Path.Combine(folder, "also-does-not-exist"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                }, out var messages);
                Assert.Equal(103, result);

                var error = messages.Single(m => m.Level == MessageLevel.Error);
                var errorMessage = error.ToString();
                var checkedPaths = errorMessage.Substring(errorMessage.IndexOf(':') + 1).Split(new[] { ',' }).Select(s => s.Trim()).ToArray();
                Assert.Equal(new[]
                {
                    "test.txt",
                    Path.Combine(folder, "does-not-exist", "test.txt"),
                    Path.Combine(folder, "also-does-not-exist", "test.txt"),
                }, checkedPaths);
            }
        }

        [Fact(Skip = "Currently fails")]
        public void CanBuildDialogsInInstallUISequence()
        {
            var folder = TestData.Get(@"TestData\DialogsInInstallUISequence");

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
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"test.wir"));
                var section = intermediate.Sections.Single();

                var textStyle = section.Tuples.OfType<TextStyleTuple>().Single();
                Assert.Equal("Tahoma", textStyle.FaceName);
                Assert.Equal(8, textStyle.Size);

                var installUIActions = section.Tuples.OfType<WixActionTuple>()
                                                     .Where(t => t.SequenceTable == SequenceTable.InstallUISequence)
                                                     .ToList();
                Assert.Equal(10, installUIActions.Count);
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
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var errors = section.Tuples.OfType<ErrorTuple>().ToDictionary(t => t.Error);
                Assert.Equal("Category 55 Emergency Doomsday Crisis", errors[1234].Message.Trim());
                Assert.Equal(" ", errors[5678].Message);
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
        public void CanBuildSimpleModule()
        {
            var folder = TestData.Get(@"TestData\SimpleModule");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Module.wxs"),
                    "-loc", Path.Combine(folder, "Module.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msm")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msm")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
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
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var upgradeTuple = section.Tuples.OfType<UpgradeTuple>().Single();
                Assert.False(upgradeTuple.ExcludeLanguages);
                Assert.True(upgradeTuple.IgnoreRemoveFailures);
                Assert.False(upgradeTuple.VersionMaxInclusive);
                Assert.True(upgradeTuple.VersionMinInclusive);
                Assert.Equal("13.0.0", upgradeTuple.VersionMax);
                Assert.Equal("12.0.0", upgradeTuple.VersionMin);
                Assert.False(upgradeTuple.OnlyDetect);
                Assert.Equal("BLAHBLAHBLAH", upgradeTuple.ActionProperty);

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

                Assert.Equal(new[]{
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

                Assert.Equal(new[]{
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
                    Assert.Equal("This is test.txt.", text);
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
                    Assert.Equal(@"This is a\test.txt.", text);

                    var text2 = wixout.GetData("wix-ir/test.txt-1");
                    Assert.Equal(@"This is b\test.txt.", text2);

                    var text3 = wixout.GetData("wix-ir/test.txt-2");
                    Assert.Equal(@"This is c\test.txt.", text3);
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
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildWithAssembly()
        {
            var folder = TestData.Get(@"TestData\Assembly");

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
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\AssemblyMsiPackage\candle.exe")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\candle.exe"), fileTuple[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"candle.exe", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);

                var msiAssemblyNameTuples = section.Tuples.OfType<MsiAssemblyNameTuple>();
                Assert.Equal(new[]
                {
                    "culture",
                    "fileVersion",
                    "name",
                    "processorArchitecture",
                    "publicKeyToken",
                    "version"
                }, msiAssemblyNameTuples.OrderBy(a => a.Name).Select(a => a.Name).ToArray());

                Assert.Equal(new[]
                {
                    "neutral",
                    "3.11.11810.0",
                    "candle",
                    "x86",
                    "256B3414DFA97718",
                    "3.0.0.0"
                }, msiAssemblyNameTuples.OrderBy(a => a.Name).Select(a => a.Value).ToArray());
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

                var platformSummary = section.Tuples.OfType<SummaryInformationTuple>().Single(s => s.PropertyId == SumaryInformationType.PlatformAndLanguage);
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
                var sharedComponentTuples = section.Tuples.OfType<ComponentTuple>();
                Assert.Equal(1, sharedComponentTuples.Sum(t => t.Shared ? 1 : 0));

                // And it is this one.
                var sharedComponentTuple = sharedComponentTuples.Single(t => t.Id.Id == "Shared.dll");
                Assert.True(sharedComponentTuple.Shared);
            }
        }

        [Fact]
        public void CanBuildSetProperty()
        {
            var folder = TestData.Get(@"TestData\SetProperty");

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

                var output = WindowsInstallerData.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"), false);
                var caRows = output.Tables["CustomAction"].Rows.Single();
                Assert.Equal("SetINSTALLLOCATION", caRows.FieldAsString(0));
                Assert.Equal("51", caRows.FieldAsString(1));
                Assert.Equal("INSTALLLOCATION", caRows.FieldAsString(2));
                Assert.Equal("[INSTALLFOLDER]", caRows.FieldAsString(3));
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
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\MsiPackage\Foo.exe")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var progids = section.Tuples.OfType<ProgIdTuple>().OrderBy(tuple => tuple.ProgId).ToList();
                Assert.Equal(new[]
                {
                    "Foo.File.hol",
                    "Foo.File.hol.15"
                }, progids.Select(p => p.ProgId).ToArray());

                Assert.Equal(new[]
                {
                    "Foo.File.hol.15",
                    null
                }, progids.Select(p => p.ParentProgIdRef).ToArray());
            }
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CanBuildInstanceTransform()
        {
            var folder = TestData.Get(@"TestData\InstanceTransform");

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
                });

                result.AssertSuccess();

                var output = WindowsInstallerData.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"), false);
                Assert.NotEmpty(output.SubStorages);
            }
        }
    }
}
