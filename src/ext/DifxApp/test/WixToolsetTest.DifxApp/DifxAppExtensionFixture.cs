// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.DifxApp
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.DifxApp;
    using Xunit;

    public class DifxAppExtensionFixture
    {
        [Fact]
        public void CanBuildUsingDriver()
        {
            var folder = TestData.Get(@"TestData\UsingDriver");
            var build = new Builder(folder, typeof(DifxAppExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:MsiCleanupOnSuccess\t1\tDIFxAppx64\tCleanupOnSuccess\t",
                "CustomAction:MsiInstallDrivers\t3073\tDIFxAppAx64\tInstallDriverPackages\t",
                "CustomAction:MsiProcessDrivers\t1\tDIFxAppx64\tProcessDriverPackages\t",
                "CustomAction:MsiRollbackInstall\t3329\tDIFxAppAx64\tRollbackInstall\t",
                "CustomAction:MsiUninstallDrivers\t3073\tDIFxAppAx64\tUninstallDriverPackages\t",
            }, results);
        }

        private static void Build(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("x64");

            var extDir = Path.GetDirectoryName(new Uri(typeof(DifxAppExtensionFactory).Assembly.CodeBase).LocalPath);
            newArgs.Add(Path.Combine(extDir, "..", "difxapp_x64.wixlib"));

            var result = WixRunner.Execute(warningsAsErrors: false, newArgs.ToArray()).AssertSuccess();

            Assert.Single(result.Messages.Where(m => m.Id == (int)WixToolset.Data.WarningMessages.Ids.DeprecatedElement));
        }
    }
}
