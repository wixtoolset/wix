// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using Xunit.Abstractions;

    public class ContainerTests : BurnE2ETests
    {
        public ContainerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanSupportMultipleAttachedContainers()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleA");

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageB.VerifyInstalled(true);
        }

        [RuntimeFact]
        public void CanSupportMultipleDetachedContainers()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleB");

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageB.VerifyInstalled(true);
        }
    }
}
