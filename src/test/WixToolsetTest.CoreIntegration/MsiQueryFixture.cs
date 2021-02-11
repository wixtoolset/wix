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
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class MsiQueryFixture
    {
        [Fact]
        public void PopulatesAppIdTableWhenAdvertised()
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
                    Path.Combine(folder, "AppId", "Advertised.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "AppId" });
                WixAssert.CompareLineByLine(new[]
                {
                    "AppId:{D6040299-B15C-4C94-AE26-0C9B60D14C35}\t\t\t\t\t\t",
                }, results);
            }
        }

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
        public void PopulatesClassTablesWhenIconIndexIsZero()
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
                    Path.Combine(folder, "Class", "IconIndex0.wxs"),
                    Path.Combine(folder, "Icon", "SampleIcon.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Class" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Class:{3FAED4CC-C473-4B8A-BE8B-303871377A4A}\tLocalServer32\tClassComp\t\tFakeClass3FAE\t\t\tSampleIcon\t0\t\t\tProductFeature\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesClassTablesWhenProgIdIsNestedUnderAdvertisedClass()
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
                    Path.Combine(folder, "ProgId", "NestedUnderClass.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Class", "ProgId", "Registry" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Class:{F12A6F69-117F-471F-AE73-F8E74218F498}\tLocalServer32\tProgIdComp\t73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\tFakeClassF12A\t\t\t\t\t\t\tProductFeature\t",
                    "ProgId:73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\t\t{F12A6F69-117F-471F-AE73-F8E74218F498}\tFakeClassF12A\t\t",
                    "Registry:regUIIK326nDZpkWHuexeF58EikQvA\t0\t73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\tNoOpen\tNoOpen73E7\tProgIdComp",
                    "Registry:regvrhMurMp98anbQJkpgA8yJCefdM\t0\tCLSID\\{F12A6F69-117F-471F-AE73-F8E74218F498}\\Version\t\t0.0.0.1\tProgIdComp",
                    "Registry:regY1F4E2lvu_Up6gV6c3jeN5ukn8s\t0\tCLSID\\{F12A6F69-117F-471F-AE73-F8E74218F498}\\LocalServer32\tThreadingModel\tApartment\tProgIdComp",
                }, results);
            }
        }

        [Fact]
        public void PopulatesControlTables()
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
                    Path.Combine(folder, "DialogsInInstallUISequence", "PackageComponents.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));

                var results = Query.QueryDatabase(msiPath, new[] { "CheckBox", "Control", "ControlCondition", "InstallUISequence" });
                WixAssert.CompareLineByLine(new[]
                {
                    "CheckBox:WIXUI_EXITDIALOGOPTIONALCHECKBOX\t1",
                    "Control:FirstDialog\tHeader\tText\t0\t13\t90\t13\t3\t\tFirstDialogHeader\tTitle\t",
                    "Control:FirstDialog\tTitle\tText\t0\t0\t90\t13\t3\t\tFirstDialogTitle\tHeader\t",
                    "Control:SecondDialog\tOptionalCheckBox\tCheckBox\t0\t13\t100\t40\t2\tWIXUI_EXITDIALOGOPTIONALCHECKBOX\t[WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT]\tTitle\tOptional checkbox|Check this box for fun",
                    "Control:SecondDialog\tTitle\tText\t0\t0\t90\t13\t3\t\tSecondDialogTitle\tOptionalCheckBox\t",
                    "ControlCondition:FirstDialog\tHeader\tDisable\tInstalled",
                    "ControlCondition:FirstDialog\tHeader\tHide\tInstalled",
                    "ControlCondition:SecondDialog\tOptionalCheckBox\tShow\tWIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT AND NOT Installed",
                    "InstallUISequence:CostFinalize\t\t1000",
                    "InstallUISequence:CostInitialize\t\t800",
                    "InstallUISequence:ExecuteAction\t\t1300",
                    "InstallUISequence:FileCost\t\t900",
                    "InstallUISequence:FindRelatedProducts\t\t25",
                    "InstallUISequence:FirstDialog\tInstalled AND PATCH\t1298",
                    "InstallUISequence:LaunchConditions\t\t100",
                    "InstallUISequence:MigrateFeatureStates\t\t1200",
                    "InstallUISequence:SecondDialog\tNOT Installed\t1299",
                    "InstallUISequence:ValidateProductID\t\t700",
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

        [Fact(Skip = "Test demonstrates failure")]
        public void PopulatesExampleTableBecauseOfEnsureTable()
        {
            var folder = TestData.Get(@"TestData");
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "EnsureTable", "EnsureTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-ext", extensionPath,
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabaseByTable(msiPath, new[] { "Wix4Example" });
                Assert.Empty(results["Wix4Example"]);
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
                var results = Query.QueryDatabase(msiPath, new[] { "Font" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Font:test.txt\tFakeFont",
                }, results);
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
                var results = Query.QueryDatabase(msiPath, new[] { "Font" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Font:TrueTypeFontFile\t",
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
        public void PopulatesMsiShortcutPropertyTable()
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
                    Path.Combine(folder, "Shortcut", "ShortcutProperty.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "MsiShortcutProperty", "Shortcut" });
                WixAssert.CompareLineByLine(new[]
                {
                    "MsiShortcutProperty:scp4GOCIx4Eskci4nBG1MV_vSUOZt4\tTheShortcut\tCustomShortcutKey\tCustomShortcutValue",
                    "Shortcut:TheShortcut\tINSTALLFOLDER\td\tShortcutComp\t[#filcV1yrx0x8wJWj4qMzcH21jwkPko]\t\t\t\t\t\t\t\t\t\t\t",
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

                var result = WixRunner.Execute(new[]
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
                var results = Query.QueryDatabase(msiPath, new[] { "ServiceInstall", "ServiceControl" });
                WixAssert.CompareLineByLine(new[]
                {
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
        public void CanMergeModule()
        {
            var folder = TestData.Get(@"TestData\SimpleMerge");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var msiPath = Path.Combine(intermediateFolder, @"bin\test.msi");
                var cabPath = Path.Combine(intermediateFolder, @"bin\cab1.cab");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, ".data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();
                Assert.Empty(section.Symbols.OfType<FileSymbol>());

                var data = WindowsInstallerData.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                Assert.Null(data.Tables["File"]);

                var results = Query.QueryDatabase(msiPath, new[] { "File" });
                WixAssert.CompareLineByLine(new[]
                {
                    "File:filyIq8rqcxxf903Hsn5K9L0SWV73g.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tModuleComponent.243FB739_4D05_472F_9CFB_EF6B1017B6DE\ttest.txt\t17\t\t\t512\t0"
                }, results);

                var files = Query.GetCabinetFiles(cabPath);
                WixAssert.CompareLineByLine(new[]
                {
                    "filyIq8rqcxxf903Hsn5K9L0SWV73g.243FB739_4D05_472F_9CFB_EF6B1017B6DE"
                }, files.Select(f => f.Name).ToArray());
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
