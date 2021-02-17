// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class FailureTests : BurnE2ETests
    {
        public FailureTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanCancelMsiPackageVeryEarly()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            // Cancel package B right away.
            testBAController.SetPackageCancelExecuteAtProgress("PackageB", 1);

            bundleA.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);
        }

        [Fact]
        public void CanCancelMsiPackageVeryLate()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            // Cancel package B at the last moment possible.
            testBAController.SetPackageCancelExecuteAtProgress("PackageB", 100);

            bundleA.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);
        }

        [Fact]
        public void CanCancelMsiPackageInOnProgress()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            // Cancel package B during its OnProgress message.
            testBAController.SetPackageCancelOnProgressAtProgress("PackageB", 100);

            bundleA.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);
        }
    }
}
