// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class StandardActionFixture
    {
        [Fact]
        public void CanCompileSpecialActionWithOverride()
        {
            using var fs = new DisposableFileSystem();

            var results = BuildAndQueryMsi(fs, "SpecialActionOverride.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "InstallExecuteSequence:AppSearch\t\t99",
                "InstallExecuteSequence:CostFinalize\t\t1000",
                "InstallExecuteSequence:CostInitialize\t\t800",
                "InstallExecuteSequence:CreateFolders\t\t3700",
                "InstallExecuteSequence:FileCost\t\t900",
                "InstallExecuteSequence:FindRelatedProducts\t\t98",
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
                "InstallUISequence:ExecuteAction\t\t1300",
                "InstallUISequence:FileCost\t\t900",
                "InstallUISequence:FindRelatedProducts\t\t25",
                "InstallUISequence:LaunchConditions\t\t100",
                "InstallUISequence:MigrateFeatureStates\t\t1200",
                "InstallUISequence:ValidateProductID\t\t700",
            }, results);
        }

        [Fact]
        public void CanCompileStandardActionWithOverride()
        {
            using var fs = new DisposableFileSystem();

            var results = BuildAndQueryMsi(fs, "StandardActionOverride.wxs");

            WixAssert.CompareLineByLine(new[]
            {
                "InstallExecuteSequence:CostFinalize\t\t1000",
                "InstallExecuteSequence:CostInitialize\t\t800",
                "InstallExecuteSequence:CreateFolders\t\t3700",
                "InstallExecuteSequence:FileCost\t\t900",
                "InstallExecuteSequence:FindRelatedProducts\t\t25",
                "InstallExecuteSequence:InstallFiles\tTEST_CONDITION\t4000",
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
                "InstallUISequence:ExecuteAction\t\t1300",
                "InstallUISequence:FileCost\t\t900",
                "InstallUISequence:FindRelatedProducts\t\t25",
                "InstallUISequence:LaunchConditions\t\t100",
                "InstallUISequence:MigrateFeatureStates\t\t1200",
                "InstallUISequence:ValidateProductID\t\t700",
            }, results);
        }

        private static string[] BuildAndQueryMsi(DisposableFileSystem fs, string sourceFile)
        {
            var folder = TestData.Get(@"TestData");

            var baseFolder = fs.GetFolder();
            var intermediateFolder = Path.Combine(baseFolder, "obj");
            var msiPath = Path.Combine(baseFolder, "bin", "test.msi");

            var result = WixRunner.Execute(new[]
            {
                    "build",
                    Path.Combine(folder, "StandardAction", sourceFile),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

            result.AssertSuccess();

            var results = Query.QueryDatabase(msiPath, new[]
            {
                "InstallExecuteSequence",
                "InstallUISequence"
            }).ToArray();

            return results;
        }
    }
}
