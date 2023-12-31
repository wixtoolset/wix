// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class CustomActionFixture
    {
        [Fact]
        public void CanBuildSetProperty()
        {
            var folder = TestData.Get("TestData", "SetProperty");

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

                var output = WindowsInstallerData.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"), false);
                var caRows = output.Tables["CustomAction"].Rows.Single();
                WixAssert.StringEqual("SetINSTALLLOCATION", caRows.FieldAsString(0));
                WixAssert.StringEqual("51", caRows.FieldAsString(1));
                WixAssert.StringEqual("INSTALLLOCATION", caRows.FieldAsString(2));
                WixAssert.StringEqual("[INSTALLFOLDER]", caRows.FieldAsString(3));
            }
        }

        [Fact]
        public void CannotBuildWhenSetPropertyReferencesMissingAction()
        {
            var folder = TestData.Get("TestData", "SetProperty");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "CannotBuildWhenSetPropertyReferencesMissingAction.wxs"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                var messages = result.Messages.Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "The identifier 'WixAction:InstallUISequence/OnlyScheduledInExecuteSequence' could not be found. Ensure you have typed the reference correctly and that all the necessary inputs are provided to the linker."
                }, messages);
            }
        }

        [Fact]
        public void CanDetectCustomActionCycle()
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
                    Path.Combine(folder, "CustomAction", "CustomActionCycle.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                Assert.Equal(176, result.ExitCode);
                WixAssert.StringEqual("The InstallExecuteSequence table contains an action 'Action1' that is scheduled to come before or after action 'Action3', which is also scheduled to come before or after action 'Action1'. Please remove this circular dependency by changing the Before or After attribute for one of the actions.", result.Messages[0].ToString());
            }
        }

        [Fact]
        public void WarnsOnVBScriptCustomAction()
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
                    Path.Combine(folder, "CustomAction", "VBScriptCustomAction.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                Assert.Equal(1163, result.ExitCode);
                Assert.Equal(3, result.Messages.Length);
                Assert.Equal(3, result.Messages.Where(m => m.Id == 1163).Count());
            }
        }

        [Fact]
        public void CanDetectCustomActionCycleWithTail()
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
                    Path.Combine(folder, "CustomAction", "CustomActionCycleWithTail.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                Assert.Equal(176, result.ExitCode);
                WixAssert.StringEqual("The InstallExecuteSequence table contains an action 'Action2' that is scheduled to come before or after action 'Action4', which is also scheduled to come before or after action 'Action2'. Please remove this circular dependency by changing the Before or After attribute for one of the actions.", result.Messages[0].ToString());
            }
        }

        [Fact]
        public void CanScheduleCustomActionInModule()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msmPath = Path.Combine(baseFolder, "bin", "test.msm");

                var result = WixRunner.Execute(new[]
                    {
                    "build",
                    Path.Combine(folder, "CustomAction", "MsmCustomAction.wxs"),
                    Path.Combine(folder, "CustomAction", "SimpleCustomAction.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msmPath
                });

                result.AssertSuccess();

                var rows = Query.QueryDatabase(msmPath, new[] { "CustomAction", "ModuleInstallExecuteSequence" });
                WixAssert.CompareLineByLine(new[]
                {
                    "CustomAction:Action1.243FB739_4D05_472F_9CFB_EF6B1017B6DE\t1\tBinary1.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tEntryPoint1\t",
                    "ModuleInstallExecuteSequence:Action1.243FB739_4D05_472F_9CFB_EF6B1017B6DE\t\tInstallFiles\t1\t",
                    "ModuleInstallExecuteSequence:CreateFolders\t3700\t\t\t",
                    "ModuleInstallExecuteSequence:InstallFiles\t4000\t\t\t",
                    "ModuleInstallExecuteSequence:RemoveFiles\t3500\t\t\t",
                    "ModuleInstallExecuteSequence:RemoveFolders\t3600\t\t\t"
                }, rows);
            }
        }

        [Fact]
        public void CanScheduleSetPropertyInModule()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msmPath = Path.Combine(baseFolder, "bin", "test.msm");

                var result = WixRunner.Execute(new[]
                    {
                    "build",
                    Path.Combine(folder, "SetProperty", "MsmSetProperty.wxs"),
                    "-bindpath", Path.Combine(folder, "SetProperty", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msmPath
                });

                result.AssertSuccess();

                var rows = Query.QueryDatabase(msmPath, new[] { "CustomAction", "ModuleInstallExecuteSequence" });
                WixAssert.CompareLineByLine(new[]
                {
                    "CustomAction:SetINSTALLLOCATION.243FB739_4D05_472F_9CFB_EF6B1017B6DE\t51\tINSTALLLOCATION.243FB739_4D05_472F_9CFB_EF6B1017B6DE\t[INSTALLFOLDER.243FB739_4D05_472F_9CFB_EF6B1017B6DE]\t",
                    "ModuleInstallExecuteSequence:CostFinalize\t1000\t\t\t",
                    "ModuleInstallExecuteSequence:CreateFolders\t3700\t\t\t",
                    "ModuleInstallExecuteSequence:InstallFiles\t4000\t\t\t",
                    "ModuleInstallExecuteSequence:RemoveFiles\t3500\t\t\t",
                    "ModuleInstallExecuteSequence:RemoveFolders\t3600\t\t\t","" +
                    "ModuleInstallExecuteSequence:SetINSTALLLOCATION.243FB739_4D05_472F_9CFB_EF6B1017B6DE\t\tCostFinalize\t1\t"
                }, rows);
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
                WixAssert.CompareLineByLine(new[]
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
                    "InstallExecuteSequence:CreateFolders\t\t3700",
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
                    "InstallExecuteSequence:RemoveFolders\t\t3600",
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
    }
}
