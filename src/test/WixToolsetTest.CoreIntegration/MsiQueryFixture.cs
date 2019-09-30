// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class MsiQueryFixture
    {
        [Fact(Skip = "Test demonstrates failure")]
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

        [Fact(Skip = "Test demonstrates failure")]
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
                    "AppSearch:SAMPLECOMPFOUND\tSampleCompSearch",
                    "DrLocator:SampleDirSearch\t\tC:\\SampleDir\t",
                }, results);
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
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

        [Fact(Skip = "Test demonstrates failure")]
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

        [Fact(Skip = "Test demonstrates failure")]
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
                var results = Query.QueryDatabase(msiPath, new[] { "Binary", "CustomAction" });
                Assert.Equal(new[]
                {
                    "Binary:Binary1\t[Binary data]",
                    "CustomAction:CustomAction1\t1\tBinary1\tInvalidEntryPoint\t",
                }, results);
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
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

        [Fact(Skip = "Test demonstrates failure")]
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

        [Fact(Skip = "Test demonstrates failure")]
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

        [Fact(Skip = "Test demonstrates failure")]
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

        [Fact(Skip = "Test demonstrates failure")]
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
                    "Upgrade:{12E4699F-E774-4D05-8A01-5BDD41BBA127}\t1.0.0.0\t1033\t\t2\t\tWIX_DOWNGRADE_DETECTED",
                    "Upgrade:{B05772EA-82B8-4DE0-B7EB-45B5F0CCFE6D}\t1.0.0\t\t\t256\t\tRELPRODFOUND",
                }, results);
            }
        }
    }
}
