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
    using WixToolset.Data.Tuples;
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
                {
                    "AppSearch:SAMPLEREGFOUND\tSampleRegSearch",
                    "RegLocator:SampleRegSearch\t2\tSampleReg\t\t2",
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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

                var results = Query.QueryDatabase(msiPath, new[] { "CheckBox", "Control", "InstallUISequence" });
                Assert.Equal(new[]
                {
                    "CheckBox:WIXUI_EXITDIALOGOPTIONALCHECKBOX\t1",
                    "Control:FirstDialog\tHeader\tText\t0\t13\t90\t13\t3\tFirstDialogHeader\tTitle\t\t",
                    "Control:FirstDialog\tTitle\tText\t0\t0\t90\t13\t3\tFirstDialogTitle\tHeader\t\t",
                    "Control:SecondDialog\tOptionalCheckBox\tCheckBox\t0\t13\t100\t40\t2\t[WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT]\tTitle\t\t",
                    "Control:SecondDialog\tTitle\tText\t0\t0\t90\t13\t3\tSecondDialogTitle\tOptionalCheckBox\t\t",
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
                Assert.Equal(new[]
                {
                    "CreateFolder:INSTALLFOLDER\tNullKeypathComponent",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCustomActionTable()
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
                    Path.Combine(folder, "CustomAction", "UnscheduledCustomAction.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] {
                    "ActionText",
                    "AdminExecuteSequence",
                    "AdminUISequence",
                    "AdvtExecuteSequence",
                    "Binary",
                    "CustomAction",
                    "InstallExecuteSequence",
                    "InstallUISequence",
                    "Property",
                }).Where(x => !x.StartsWith("Property:") || x.StartsWith("Property:MsiHiddenProperties\t")).ToArray();
                Assert.Equal(new[]
                {
                    "ActionText:CustomAction2\tProgess2Text\t",
                    "AdminExecuteSequence:CostFinalize\t\t1000",
                    "AdminExecuteSequence:CostInitialize\t\t800",
                    "AdminExecuteSequence:CustomAction2\t\t801",
                    "AdminExecuteSequence:FileCost\t\t900",
                    "AdminExecuteSequence:InstallAdminPackage\t\t3900",
                    "AdminExecuteSequence:InstallFiles\t\t4000",
                    "AdminExecuteSequence:InstallFinalize\t\t6600",
                    "AdminExecuteSequence:InstallInitialize\t\t1500",
                    "AdminExecuteSequence:InstallValidate\t\t1400",
                    "AdminUISequence:CostFinalize\t\t1000",
                    "AdminUISequence:CostInitialize\t\t800",
                    "AdminUISequence:CustomAction2\t\t801",
                    "AdminUISequence:ExecuteAction\t\t1300",
                    "AdminUISequence:FileCost\t\t900",
                    "AdvtExecuteSequence:CostFinalize\t\t1000",
                    "AdvtExecuteSequence:CostInitialize\t\t800",
                    "AdvtExecuteSequence:CustomAction2\t\t801",
                    "AdvtExecuteSequence:InstallFinalize\t\t6600",
                    "AdvtExecuteSequence:InstallInitialize\t\t1500",
                    "AdvtExecuteSequence:InstallValidate\t\t1400",
                    "AdvtExecuteSequence:PublishFeatures\t\t6300",
                    "AdvtExecuteSequence:PublishProduct\t\t6400",
                    "Binary:Binary1\t[Binary data]",
                    "CustomAction:CustomAction1\t1\tBinary1\tInvalidEntryPoint\t",
                    "CustomAction:CustomAction2\t51\tTestAdvtExecuteSequenceProperty\t1\t",
                    "CustomAction:CustomActionWithHiddenTarget\t9217\tBinary1\tInvalidEntryPoint\t",
                    "CustomAction:DiscardOptimismAllBeingsWhoProceed\t19\t\tAbandon hope all ye who enter here.\t",
                    "InstallExecuteSequence:CostFinalize\t\t1000",
                    "InstallExecuteSequence:CostInitialize\t\t800",
                    "InstallExecuteSequence:CustomAction2\t\t801",
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
                    "InstallExecuteSequence:UnpublishFeatures\t\t1800",
                    "InstallExecuteSequence:ValidateProductID\t\t700",
                    "InstallUISequence:CostFinalize\t\t1000",
                    "InstallUISequence:CostInitialize\t\t800",
                    "InstallUISequence:CustomAction2\t\t801",
                    "InstallUISequence:ExecuteAction\t\t1300",
                    "InstallUISequence:FileCost\t\t900",
                    "InstallUISequence:FindRelatedProducts\t\t25",
                    "InstallUISequence:LaunchConditions\t\t100",
                    "InstallUISequence:MigrateFeatureStates\t\t1200",
                    "InstallUISequence:ValidateProductID\t\t700",
                    "Property:MsiHiddenProperties\tCustomActionWithHiddenTarget",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCustomTable1()
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
                    Path.Combine(folder, "CustomTable", "CustomTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTable1" });
                Assert.Equal(new[]
                {
                    "CustomTable1:Row1\ttest.txt",
                    "CustomTable1:Row2\ttest.txt",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCustomTableWithLocalization()
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
                    Path.Combine(folder, "CustomTable", "LocalizedCustomTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-loc", Path.Combine(folder, "CustomTable", "LocalizedCustomTable.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTableLocalized" });
                Assert.Equal(new[]
                {
                    "CustomTableLocalized:Row1\tThis is row one",
                    "CustomTableLocalized:Row2\tThis is row two",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCustomTableWithFilePath()
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
                    Path.Combine(folder, "CustomTable", "CustomTableWithFile.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "CustomTable", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTableWithFile" });
                Assert.Equal(new[]
                {
                    "CustomTableWithFile:Row1\t[Binary data]",
                    "CustomTableWithFile:Row2\t[Binary data]",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCustomTableWithFilePathSerialized()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(baseFolder, @"bin\test.wixlib");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "CustomTable", "CustomTableWithFile.wxs"),
                    "-bindpath", Path.Combine(folder, "CustomTable", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-lib", wixlibPath,
                    "-bindpath", Path.Combine(folder, "CustomTable", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTableWithFile" });
                Assert.Equal(new[]
                {
                    "CustomTableWithFile:Row1\t[Binary data]",
                    "CustomTableWithFile:Row2\t[Binary data]",
                }, results);
            }
        }

        [Fact]
        public void UnrealCustomTableIsNotPresentInMsi()
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
                    Path.Combine(folder, "CustomTable", "CustomTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTable2" });
                Assert.Empty(results);
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
                    Path.Combine(folder, "DefaultDir", "DefaultDir.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Directory" });
                Assert.Equal(new[]
                {
                    "Directory:DUPLICATENAMEANDSHORTNAME\tINSTALLFOLDER\tduplicat",
                    "Directory:INSTALLFOLDER\tProgramFilesFolder\toekcr5lq|MsiPackage",
                    "Directory:NAMEANDSHORTNAME\tINSTALLFOLDER\tSHORTNAM|NameAndShortName",
                    "Directory:NAMEANDSHORTSOURCENAME\tINSTALLFOLDER\tNAMEASSN|NameAndShortSourceName",
                    "Directory:NAMEWITHSHORTVALUE\tINSTALLFOLDER\tSHORTVAL",
                    "Directory:ProgramFilesFolder\tTARGETDIR\t.",
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
                {
                    "InstallExecuteSequence:CostFinalize\t\t1000",
                    "InstallExecuteSequence:CostInitialize\t\t800",
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
                {
                    "MsiShortcutProperty:scp4GOCIx4Eskci4nBG1MV_vSUOZt4\tTheShortcut\tCustomShortcutKey\tCustomShortcutValue",
                    "Shortcut:TheShortcut\tINSTALLFOLDER\td\tShortcutComp\t[#filcV1yrx0x8wJWj4qMzcH21jwkPko]\t\t\t\t\t\t\t\t\t\t\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesRegistryTableFromRegistryValue()
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
                    Path.Combine(folder, "Registry", "RegistryValue.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Registry" });
                Assert.Equal(new[]
                {
                    "Registry:reg04OIwIchl.9ZTjisTT6NzGSsQSM\t2\tPath\\To\\AnotherKey\tSecret\t#x\tMiscComponent",
                    "Registry:regEblTuusqFNSUQNy88zaP_UA5kIY\t2\tPath\\To\\Key\t\t1.0.1234.123\tMiscComponent",
                }, results);
            }
        }

        [Fact]
        public void PopulatesRegistryTableFromRemoveRegistryKey()
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
                    Path.Combine(folder, "Registry", "RemoveRegistryKey.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Registry" });
                Assert.Equal(new[]
                {
                    "Registry:RemoveAKeyName\t2\tAKeyName\t-\t\tRemoveRegistryKeyComp",
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
                {
                    "TextStyle:FirstTextStyle\tArial\t2\t\t",
                }, results);
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
                {
                    "Upgrade:{12E4699F-E774-4D05-8A01-5BDD41BBA127}\t\t1.0.0.0\t1033\t1\t\tWIX_UPGRADE_DETECTED",
                    "Upgrade:{12E4699F-E774-4D05-8A01-5BDD41BBA127}\t1.0.0.0\t\t1033\t2\t\tWIX_DOWNGRADE_DETECTED",
                    "Upgrade:{B05772EA-82B8-4DE0-B7EB-45B5F0CCFE6D}\t1.0.0\t\t\t256\t\tRELPRODFOUND",
                }, results);

                var prefix = "Property:SecureCustomProperties\t";
                var secureProperties = Query.QueryDatabase(msiPath, new[] { "Property" }).Where(p => p.StartsWith(prefix)).Single();
                Assert.Equal(new[]
                {
                    "RELPRODFOUND",
                    "WIX_DOWNGRADE_DETECTED",
                    "WIX_UPGRADE_DETECTED",
                }, secureProperties.Substring(prefix.Length).Split(';').OrderBy(p => p));
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
                Assert.Empty(section.Tuples.OfType<FileTuple>());

                var data = WindowsInstallerData.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                Assert.Null(data.Tables["File"]);

                var results = Query.QueryDatabase(msiPath, new[] { "File" });
                Assert.Equal(new[]
                {
                    "File:filyIq8rqcxxf903Hsn5K9L0SWV73g.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tModuleComponent.243FB739_4D05_472F_9CFB_EF6B1017B6DE\ttest.txt\t17\t\t\t512\t0"
                }, results);

                var files = Query.GetCabinetFiles(cabPath);
                Assert.Equal(new[]
                {
                    "filyIq8rqcxxf903Hsn5K9L0SWV73g.243FB739_4D05_472F_9CFB_EF6B1017B6DE"
                }, files.Select(f => f.Name).ToArray());
            }
        }
    }
}
