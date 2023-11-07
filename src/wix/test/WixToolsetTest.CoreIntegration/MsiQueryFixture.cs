// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using Example.Extension;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using Xunit;

    public class MsiQueryFixture
    {
        [Fact]
        public void PopulatesAppSearchTablesFromComponentSearch()
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
                    Path.Combine(folder, "AppSearch", "ComponentSearch.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "AppSearch", "CompLocator" });
                WixAssert.CompareLineByLine(new[]
                {
                    "AppSearch:SAMPLECOMPFOUND\tSampleCompSearch",
                    "CompLocator:SampleCompSearch\t{4D9A0D20-D0CC-40DE-B580-EAD38B985217}\t1",
                }, results);
            }
        }

        [Fact]
        public void PopulatesAppSearchTablesFromDirectorySearch()
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
                    Path.Combine(folder, "AppSearch", "DirectorySearch.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "AppSearch", "DrLocator" });
                WixAssert.CompareLineByLine(new[]
                {
                    "AppSearch:SAMPLEDIRFOUND\tSampleDirSearch",
                    "DrLocator:SampleDirSearch\t\tC:\\SampleDir\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesAppSearchTablesFromFileSearch()
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
                    Path.Combine(folder, "AppSearch", "FileSearch.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "AppSearch", "DrLocator", "IniLocator" });
                WixAssert.CompareLineByLine(new[]
                {
                    "AppSearch:SAMPLEFILEFOUND\tSampleFileSearch",
                    "DrLocator:SampleFileSearch\tSampleIniFileSearch\t\t",
                    "IniLocator:SampleFileSearch\tsample.fil\tMySection\tMyKey\t\t1",
                }, results);
            }
        }

        [Fact]
        public void PopulatesAppSearchTablesFromRegistrySearch()
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
                    Path.Combine(folder, "AppSearch", "RegistrySearch.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "AppSearch", "RegLocator" });
                WixAssert.CompareLineByLine(new[]
                {
                    "AppSearch:SAMPLEREGFOUND\tSampleRegSearch",
                    "RegLocator:SampleRegSearch\t2\tSampleReg\t\t2",
                }, results);
            }
        }

        [Fact]
        public void PopulatesAppSearchTablesFromRegistrySearch64()
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
                    Path.Combine(folder, "AppSearch", "RegistrySearch64.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "AppSearch", "RegLocator" });
                WixAssert.CompareLineByLine(new[]
                {
                    "AppSearch:SAMPLEREGFOUND\tSampleRegSearch",
                    "RegLocator:SampleRegSearch\t2\tSampleReg\t\t18",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCreateFolderTableForNullKeypathComponents()
        {
            var folder = TestData.Get(@"TestData\Components");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CreateFolder" });
                WixAssert.CompareLineByLine(new[]
                {
                    "CreateFolder:INSTALLFOLDER\tNullKeypathComponent",
                }, results);
            }
        }

        [Fact]
        public void PopulatesDirectoryTableWithValidDefaultDir()
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
                    "-sw1031", // this is expected for this test
                    Path.Combine(folder, "DefaultDir", "DefaultDir.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Directory" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Directory:DUPLICATENAMEANDSHORTNAME\tINSTALLFOLDER\tduplicat",
                    "Directory:Folder1\tINSTALLFOLDER\tFolder.1",
                    "Directory:Folder12\tINSTALLFOLDER\tFolder.12",
                    "Directory:Folder123\tINSTALLFOLDER\tFolder.123",
                    "Directory:Folder1234\tINSTALLFOLDER\tyakwclwy|Folder.1234",
                    "Directory:GitFolder\tINSTALLFOLDER\t69sdfw2d|.git",
                    "Directory:INSTALLFOLDER\tProgramFiles6432Folder\t1egc1laj|MsiPackage",
                    "Directory:NAMEANDSHORTNAME\tINSTALLFOLDER\tSHORTNAM|NameAndShortName",
                    "Directory:NAMEANDSHORTSOURCENAME\tINSTALLFOLDER\tNAMEASSN|NameAndShortSourceName",
                    "Directory:NAMEWITHSHORTVALUE\tINSTALLFOLDER\tSHORTVAL",
                    "Directory:ProgramFiles6432Folder\tProgramFilesFolder\t.",
                    "Directory:ProgramFilesFolder\tTARGETDIR\tPFiles",
                    "Directory:SHORTNAMEANDLONGSOURCENAME\tINSTALLFOLDER\tSHNALSNM:6ukthv5q|ShortNameAndLongSourceName",
                    "Directory:SHORTNAMEONLY\tINSTALLFOLDER\tSHORTONL",
                    "Directory:SOURCENAME\tINSTALLFOLDER\ts2s5bq-i|NameAndSourceName:dhnqygng|SourceNameWithName",
                    "Directory:SOURCENAMESONLY\tINSTALLFOLDER\t.:SRCNAMON|SourceNameOnly",
                    "Directory:SOURCENAMEWITHSHORTVALUE\tINSTALLFOLDER\t.:SRTSRCVL",
                    "Directory:TARGETDIR\t\tSourceDir",
                }, results);
            }
        }

        [Fact]
        public void PopulatesEnvironmentTable()
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
                    Path.Combine(folder, "Environment", "Environment.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Environment" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Environment:PATH\t=-*PATH\t[INSTALLFOLDER]; ;[~]\tWixEnvironmentTest",
                    "Environment:WixEnvironmentTest1\t=-WixEnvTest1\t\tWixEnvironmentTest",
                    "Environment:WixEnvironmentTest2\t+-WixEnvTest1\t\tWixEnvironmentTest",
                    "Environment:WixEnvironmentTest3\t!-WixEnvTest1\t\tWixEnvironmentTest",
                    "Environment:WixEnvironmentTest4\t=-*WIX\t[INSTALLFOLDER]\tWixEnvironmentTest",
                }, results);
            }
        }

        [Fact]
        public void CanBuildMsiWithEmptyCustomTableBecauseOfCustomTableRef()
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
                    Path.Combine(folder, "EnsureTable", "EnsureCustomTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabaseByTable(msiPath, new[] { "SomeCustomTable" });
                WixAssert.StringCollectionEmpty(results["SomeCustomTable"]);
            }
        }

        [Fact]
        public void CanBuildMsiWithEmptyStandardTableBecauseOfEnsureTable()
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
                    Path.Combine(folder, "EnsureTable", "EnsureModuleSignature.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabaseByTable(msiPath, new[] { "ModuleSignature" });
                WixAssert.StringCollectionEmpty(results["ModuleSignature"]);
            }
        }

        [Fact]
        public void CanBuildMsiWithEmptyTableFromExtensionBecauseOfEnsureTable()
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
                    Path.Combine(folder, "EnsureTable", "EnsureExtensionTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabaseByTable(msiPath, new[] { "Wix4Example" });
                WixAssert.StringCollectionEmpty(results["Wix4Example"]);
            }
        }

        [Fact]
        public void PopulatesFeatureTableWithParent()
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
                    Path.Combine(folder, "FeatureGroup", "FeatureGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Feature" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Feature:ChildFeature\tParentFeature\tChildFeatureTitle\t\t2\t1\t\t0",
                    "Feature:ParentFeature\t\tParentFeatureTitle\t\t2\t1\t\t0",
                    "Feature:ProductFeature\t\tMsiPackageTitle\t\t2\t1\t\t0",
                }, results);
            }
        }

        [Fact]
        public void PopulatesFontTableFromFontTitle()
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
                    Path.Combine(folder, "Font", "FontTitle.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Font", "InstallExecuteSequence" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Font:test.txt\tFakeFont",
                    "InstallExecuteSequence:RegisterFonts\t\t5300",
                    "InstallExecuteSequence:UnregisterFonts\t\t2500",
                }, results.Where(l => l.Contains("Font")).ToArray());
            }
        }

        [Fact]
        public void PopulatesFontTableFromTrueType()
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
                    Path.Combine(folder, "Font", "TrueType.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Font", "InstallExecuteSequence" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Font:TrueTypeFontFile\t",
                    "InstallExecuteSequence:RegisterFonts\t\t5300",
                    "InstallExecuteSequence:UnregisterFonts\t\t2500",
                }, results.Where(l => l.Contains("Font")).ToArray());
            }
        }

        [Fact]
        public void PopulatesIniFile()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "IniFile", "IniFile.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "IniFile" });
                WixAssert.CompareLineByLine(new[]
                {
                    "IniFile:iniRVwYTVbDGRcXg7ckoDxDHV1iRaQ\ttest.txt\tINSTALLFOLDER\tTestSection\tSomeKey\tSomeValue\t2\tIniComp",
                }, results);
            }
        }

        [Fact]
        public void PopulatesInstallExecuteSequenceTable()
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
                    Path.Combine(folder, "Upgrade", "DetectOnly.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "InstallExecuteSequence" });
                WixAssert.CompareLineByLine(new[]
                {
                    "InstallExecuteSequence:CostFinalize\t\t1000",
                    "InstallExecuteSequence:CostInitialize\t\t800",
                    "InstallExecuteSequence:CreateFolders\t\t3700",
                    "InstallExecuteSequence:FileCost\t\t900",
                    "InstallExecuteSequence:FindRelatedProducts\t\t25",
                    "InstallExecuteSequence:InstallFiles\t\t4000",
                    "InstallExecuteSequence:InstallFinalize\t\t6600",
                    "InstallExecuteSequence:InstallInitialize\t\t1500",
                    "InstallExecuteSequence:InstallValidate\t\t1400",
                    "InstallExecuteSequence:LaunchConditions\t\t100",
                    "InstallExecuteSequence:MigrateFeatureStates\t\t1200",
                    "InstallExecuteSequence:ProcessComponents\t\t1600",
                    "InstallExecuteSequence:PublishFeatures\t\t6300",
                    "InstallExecuteSequence:PublishProduct\t\t6400",
                    "InstallExecuteSequence:RegisterProduct\t\t6100",
                    "InstallExecuteSequence:RegisterUser\t\t6000",
                    "InstallExecuteSequence:RemoveExistingProducts\t\t1401",
                    "InstallExecuteSequence:RemoveFiles\t\t3500",
                    "InstallExecuteSequence:RemoveFolders\t\t3600",
                    "InstallExecuteSequence:UnpublishFeatures\t\t1800",
                    "InstallExecuteSequence:ValidateProductID\t\t700",
                }, results);
            }
        }

        [Fact]
        public void PopulatesLockPermissionsTableWithEmptyPermissions()
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
                    Path.Combine(folder, "LockPermissions", "EmptyPermissions.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "LockPermissions" });
                WixAssert.CompareLineByLine(new[]
                {
                    "LockPermissions:INSTALLFOLDER\tCreateFolder\t\tAdministrator\t0",
                }, results);
            }
        }

        [Fact]
        public void PopulatesMsiAssemblyTables()
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
                    Path.Combine(folder, "Assembly", "Win32Assembly.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "Assembly", "data"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "MsiAssembly", "MsiAssemblyName" });
                WixAssert.CompareLineByLine(new[]
                {
                    "MsiAssembly:test.txt\tProductFeature\ttest.dll.manifest\t\t1",
                    "MsiAssemblyName:test.txt\tname\tMyApplication.app",
                    "MsiAssemblyName:test.txt\tversion\t1.0.0.0",
                }, results);
            }
        }

        [Fact]
        public void PopulatesReserveCostTable()
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
                    Path.Combine(folder, "ReserveCost", "ReserveCost.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "ReserveCost" });
                WixAssert.CompareLineByLine(new[]
                {
                    "ReserveCost:TestCost\tReserveCostComp\tINSTALLFOLDER\t100\t200",
                }, results);
            }
        }

        [Fact]
        public void PopulatesServiceTables()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "build",
                    Path.Combine(folder, "ServiceInstall", "OwnProcess.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "ServiceInstall", "ServiceControl", "MsiServiceConfig", "MsiServiceConfigFailureActions" });
                WixAssert.CompareLineByLine(new[]
                {
                    "MsiServiceConfig:SampleService.DS\tSampleService\t1\t3\t1\ttest.txt",
                    "MsiServiceConfig:SampleService.SS\tSampleService\t1\t5\t1\ttest.txt",
                    "MsiServiceConfigFailureActions:SampleService\tSampleService\t1\t120\tRestart required because service failed.\t[~]\t\t\ttest.txt",
                    "ServiceControl:SampleService\tSampleService\t161\t\t1\ttest.txt",
                    "ServiceInstall:SampleService\tSampleService\t\t16\t4\t0\t\t\t\t\t\ttest.txt\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesTextStyleTableWhenColorIsNull()
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
                    Path.Combine(folder, "TextStyle", "ColorNull.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "TextStyle" });
                WixAssert.CompareLineByLine(new[]
                {
                    "TextStyle:FirstTextStyle\tArial\t2\t\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesTextStyleTableWhenSizeIsLocalized()
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
                    Path.Combine(folder, "TextStyle", "SizeLocalized.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-loc", Path.Combine(folder, "TextStyle", "SizeLocalized.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "TextStyle" });
                WixAssert.CompareLineByLine(new[]
                {
                    "TextStyle:CustomFont\tTahoma\t8\t\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesTypeLibTableWhenLanguageIsZero()
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
                    Path.Combine(folder, "TypeLib", "Language0.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "TypeLib" });
                WixAssert.CompareLineByLine(new[]
                {
                    "TypeLib:{765BE8EE-BD7F-491E-90D2-C5A972462B50}\t0\tTypeLibComp\t\t\t\tProductFeature\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesUpgradeTableFromManualUpgrade()
        {
            var folder = TestData.Get(@"TestData\ManualUpgrade");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var msiPath = Path.Combine(intermediateFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                }, out var messages);

                Assert.Equal(0, result);

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Upgrade" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Upgrade:{01120000-00E0-0000-0000-0000000FF1CE}\t12.0.0\t13.0.0\t\t260\t\tBLAHBLAHBLAH",
                }, results);
            }
        }

        [Fact]
        public void PopulatesUpgradeTableFromDetectOnlyUpgrade()
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
                    Path.Combine(folder, "Upgrade", "DetectOnly.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Upgrade" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Upgrade:{12E4699F-E774-4D05-8A01-5BDD41BBA127}\t\t1.0.0.0\t1033\t1\t\tWIX_UPGRADE_DETECTED",
                    "Upgrade:{12E4699F-E774-4D05-8A01-5BDD41BBA127}\t1.0.0.0\t\t1033\t2\t\tWIX_DOWNGRADE_DETECTED",
                    "Upgrade:{B05772EA-82B8-4DE0-B7EB-45B5F0CCFE6D}\t1.0.0\t\t\t256\t\tRELPRODFOUND",
                }, results);

                var prefix = "Property:SecureCustomProperties\t";
                var secureProperties = Query.QueryDatabase(msiPath, new[] { "Property" }).Where(p => p.StartsWith(prefix)).Single();
                WixAssert.CompareLineByLine(new[]
                {
                    "RELPRODFOUND",
                    "WIX_DOWNGRADE_DETECTED",
                    "WIX_UPGRADE_DETECTED",
                }, secureProperties.Substring(prefix.Length).Split(';').OrderBy(p => p).ToArray());
            }
        }

        [Fact]
        public void CanPublishComponentWithMultipleFeatureComponents()
        {
            var folder = TestData.Get(@"TestData\PublishComponent");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "PublishComponent" });
                WixAssert.CompareLineByLine(new[]
                {
                    "PublishComponent:{0A82C8F6-9CE9-4336-B8BE-91A39B5F7081}	Qualifier2	Component2	AppData2	ProductFeature2",
                    "PublishComponent:{BD245B5A-EC33-46ED-98FF-E9D3D416AD04}	Qualifier1	Component1	AppData1	ProductFeature1",
                }, results);
            }
        }
    }
}
