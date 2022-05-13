// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System.Threading;
    using WixTestTools;
    using WixToolset.Mba.Core;
    using Xunit;
    using Xunit.Abstractions;

    public class FailureTests : BurnE2ETests
    {
        public FailureTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanCancelExePackageAndAbandonIt()
        {
            var bundleD = this.CreateBundleInstaller("BundleD");
            var testBAController = this.CreateTestBAController();

            // Cancel package ExeA after it starts.
            testBAController.SetPackageCancelExecuteAtProgress("ExeA", 1);
            testBAController.SetPackageRecordTestRegistryValue("ExeA");

            var logPath = bundleD.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleD.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(LogVerifier.MessageInLogFile(logPath, "TestRegistryValue: Execute, ExeA, Version, ''"));
            Assert.False(LogVerifier.MessageInLogFile(logPath, "TestRegistryValue: Rollback, ExeA, Version"));
        }

        [RuntimeFact]
        public void CanCancelExePackageAndWaitUntilItCompletes()
        {
            var bundleD = this.CreateBundleInstaller("BundleD");
            var testBAController = this.CreateTestBAController();

            // Cancel package ExeA after it starts.
            testBAController.SetPackageCancelExecuteAtProgress("ExeA", 1);
            testBAController.SetPackageProcessCancelAction("ExeA", BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION.Wait);
            testBAController.SetPackageRecordTestRegistryValue("ExeA");

            var logPath = bundleD.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleD.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(LogVerifier.MessageInLogFile(logPath, "TestRegistryValue: Execute, ExeA, Version, '1.0.0.0'"));
            Assert.True(LogVerifier.MessageInLogFile(logPath, "TestRegistryValue: Rollback, ExeA, Version, ''"));

            // The package should have rolled back.
            bundleD.VerifyExeTestRegistryRootDeleted("ExeA");
        }

        [RuntimeFact]
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

        [RuntimeFact]
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

        [RuntimeFact]
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

        [RuntimeFact]
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
        [RuntimeFact]
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
