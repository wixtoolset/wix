// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ComPlus
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.ComPlus;
    using Xunit;

    public class ComPlusExtensionFixture
    {
        [Fact(Skip = "xxxxx CodeBase Issue: We can't determine the file path from which we were loaded. xxxxx")]
        public void CanBuildUsingComPlusPartition()
        {
            var folder = TestData.Get(@"TestData\UsingComPlusPartition");
            var build = new Builder(folder, typeof(ComPlusExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Wix4ComPlusPartition", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix4ComPlusInstallCommit_A64\t11777\tWix4cpca_A64\tComPlusCleanup\t",
                "CustomAction:Wix4ComPlusInstallExecute_A64\t11265\tWix4cpca_A64\tComPlusInstallExecute\t",
                "CustomAction:Wix4ComPlusInstallExecuteCommit_A64\t11777\tWix4cpca_A64\tComPlusInstallExecuteCommit\t",
                "CustomAction:Wix4ComPlusInstallPrepare_A64\t11265\tWix4cpca_A64\tComPlusPrepare\t",
                "CustomAction:Wix4ComPlusRollbackInstallExecute_A64\t11521\tWix4cpca_A64\tComPlusRollbackInstallExecute\t",
                "CustomAction:Wix4ComPlusRollbackInstallPrepare_A64\t11521\tWix4cpca_A64\tComPlusCleanup\t",
                "CustomAction:Wix4ComPlusRollbackUninstallExecute_A64\t11521\tWix4cpca_A64\tComPlusInstallExecute\t",
                "CustomAction:Wix4ComPlusRollbackUninstallPrepare_A64\t11521\tWix4cpca_A64\tComPlusCleanup\t",
                "CustomAction:Wix4ComPlusUninstallCommit_A64\t11777\tWix4cpca_A64\tComPlusCleanup\t",
                "CustomAction:Wix4ComPlusUninstallExecute_A64\t11265\tWix4cpca_A64\tComPlusUninstallExecute\t",
                "CustomAction:Wix4ComPlusUninstallPrepare_A64\t11265\tWix4cpca_A64\tComPlusPrepare\t",
                "CustomAction:Wix4ConfigureComPlusInstall_A64\t1\tWix4cpca_A64\tConfigureComPlusInstall\t",
                "CustomAction:Wix4ConfigureComPlusUninstall_A64\t1\tWix4cpca_A64\tConfigureComPlusUninstall\t",
                "Wix4ComPlusPartition:MyPartition\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tMyPartitionId\tMyPartition",
            }, results);
        }

        private static void Build(string[] args)
        {
            args = args.Concat(new[] { "-arch", "arm64" }).ToArray();

            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }
    }
}
