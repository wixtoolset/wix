// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class CustomActionFixture
    {
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
                Assert.Equal("The InstallExecuteSequence table contains an action 'Action1' that is scheduled to come before or after action 'Action3', which is also scheduled to come before or after action 'Action1'.  Please remove this circular dependency by changing the Before or After attribute for one of the actions.", result.Messages[0].ToString());
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
                Assert.Equal("The InstallExecuteSequence table contains an action 'Action2' that is scheduled to come before or after action 'Action4', which is also scheduled to come before or after action 'Action2'.  Please remove this circular dependency by changing the Before or After attribute for one of the actions.", result.Messages[0].ToString());
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
