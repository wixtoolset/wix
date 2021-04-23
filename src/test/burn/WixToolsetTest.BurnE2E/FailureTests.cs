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

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/5750")]
        public void CanCancelExecuteWhileCaching()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleB = this.CreateBundleInstaller("BundleB");
            var testBAController = this.CreateTestBAController();

            // Slow the caching of package B to ensure that package A starts installing and cancels.
            testBAController.SetPackageCancelExecuteAtProgress("PackageA", 50);
            testBAController.SetPackageSlowCache("PackageB", 2000);

            bundleB.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);
        }

        /// <summary>
        /// BundleC has non-vital PackageA and vital PackageB.
        /// PackageA is not compressed in the bundle and has a Name different from the source file. The Name points to a file that does not exist.
        /// BundleC should be able to install successfully by ignoring the missing PackageA and installing PackageB.
        /// </summary>
        [Fact]
        public void CanInstallWhenMissingNonVitalPackage()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleC = this.CreateBundleInstaller("BundleC");

            var bundleCInstallLogFilePath = bundleC.Install();
            bundleC.VerifyRegisteredAndInPackageCache();
            Assert.True(LogVerifier.MessageInLogFileRegex(bundleCInstallLogFilePath, "Skipping apply of package: PackageA due to cache error: 0x80070002. Continuing..."));

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(true);

            bundleC.Uninstall();
            bundleC.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);
        }
    }
}
