// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class WixStdBaTests : BurnE2ETests
    {
        public WixStdBaTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void ExitsWithErrorWhenDowngradingWithoutSuppression()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var bundle1v10 = this.CreateBundleInstaller("WixStdBaTest1_v10");
            var bundle1v11 = this.CreateBundleInstaller("WixStdBaTest1_v11");

            packageA.VerifyInstalled(false);

            bundle1v11.Install();
            bundle1v11.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);

            bundle1v10.Install((int)MSIExec.MSIExecReturnCode.ERROR_PRODUCT_VERSION);
            bundle1v10.VerifyUnregisteredAndRemovedFromPackageCache();
            bundle1v11.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
        }

        [Fact]
        public void ExitsWithoutErrorWhenDowngradingWithSuppression()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var bundle1v11 = this.CreateBundleInstaller("WixStdBaTest1_v11");
            var bundle1v12 = this.CreateBundleInstaller("WixStdBaTest1_v12");

            packageA.VerifyInstalled(false);

            bundle1v12.Install();
            bundle1v12.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);

            bundle1v11.Install();
            bundle1v11.VerifyUnregisteredAndRemovedFromPackageCache();
            bundle1v12.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
        }
    }
}
