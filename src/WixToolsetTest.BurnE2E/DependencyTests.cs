// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using Xunit;
    using Xunit.Abstractions;

    public class DependencyTests : BurnE2ETests
    {
        public DependencyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanKeepSameExactPackageAfterUpgradingBundle()
        {
            var packageA = this.CreatePackageInstaller("PackageF");
            var bundleAv1 = this.CreateBundleInstaller("BundleKv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleKv2");

            packageA.VerifyInstalled(false);

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(true);

            bundleAv2.Uninstall();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
        }
    }
}
