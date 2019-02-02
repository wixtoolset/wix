// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.DifxApp
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
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
            Assert.Equal(new[]
            {
                "CustomAction:MsiCleanupOnSuccess\t1\tDIFxApp.dll\tCleanupOnSuccess\t0",
                "CustomAction:MsiInstallDrivers\t3073\tDIFxAppA.dll\tInstallDriverPackages\t0",
                "CustomAction:MsiProcessDrivers\t1\tDIFxApp.dll\tProcessDriverPackages\t0",
                "CustomAction:MsiRollbackInstall\t3329\tDIFxAppA.dll\tRollbackInstall\t0",
                "CustomAction:MsiUninstallDrivers\t3073\tDIFxAppA.dll\tUninstallDriverPackages\t0",
            }, results.OrderBy(s => s).ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
