// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using WixTestTools;
    using WixToolset.Mba.Core;
    using Xunit;
    using Xunit.Abstractions;

    public class SlipstreamTests : BurnE2ETests
    {
        public SlipstreamTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private const string V090 = "0.9.0.0";
        private const string V100 = "1.0.0.0";
        private const string V101 = "1.0.1.0";

        [Fact]
        public void CanInstallBundleWithSlipstreamedPatchThenRemoveIt()
        {
            var testRegistryValue = "PackageA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleA");

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), String.Concat("PackageAv1 payload should have been removed by uninstall from: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        /// <summary>
        /// BundleA installs PackageA with slipstreamed PatchA.
        /// BundleOnlyPatchA is installed which contains PatchA (which should be a no-op).
        /// BundleOnlyPatchA in uninstalled which should do nothing since BundleA has a dependency on it.
        /// Bundle is installed which should remove everything.
        /// </summary>
        [Fact]
        public void ReferenceCountsSlipstreamedPatch()
        {
            var testRegistryValue = "PackageA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundleOnlyPatchA = this.CreateBundleInstaller("BundleOnlyPatchA");
            var bundleA = this.CreateBundleInstaller("BundleA");

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            bundleOnlyPatchA.Install();
            bundleOnlyPatchA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            bundleOnlyPatchA.Uninstall();
            bundleOnlyPatchA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), String.Concat("PackageAv1 payload should have been removed by uninstall from: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6350")]
        public void CanInstallBundleWithSlipstreamedPatchThenRepairIt()
        {
            this.InstallBundleWithSlipstreamedPatchThenRepairIt(false);
        }

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6350")]
        public void CanInstallReversedBundleWithSlipstreamedPatchThenRepairIt()
        {
            this.InstallBundleWithSlipstreamedPatchThenRepairIt(true);
        }

        private void InstallBundleWithSlipstreamedPatchThenRepairIt(bool isReversed)
        {
            var bundleName = isReversed ? "BundleAReverse" : "BundleA";
            var testRegistryValue = "PackageA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller(bundleName);

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            // Delete the installed file and registry key so we have something to repair.
            File.Delete(packageAv1SourceCodeInstalled);
            packageAv1.DeleteTestRegistryValue(testRegistryValue);

            bundleA.Repair();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), String.Concat("PackageAv1 payload should have been removed by uninstall from: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        [Fact]
        public void CanInstallSlipstreamedPatchThroughForcedRepair()
        {
            this.InstallSlipstreamedPatchThroughForcedRepair(false);
        }

        [Fact]
        public void CanInstallSlipstreamedPatchThroughReversedForcedRepair()
        {
            this.InstallSlipstreamedPatchThroughForcedRepair(true);
        }

        private void InstallSlipstreamedPatchThroughForcedRepair(bool isReversed)
        {
            var bundleName = isReversed ? "BundleAReverse" : "BundleA";
            var testRegistryValue = "PackageA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller(bundleName);
            var bundleOnlyA = this.CreateBundleInstaller("BundleOnlyA");
            var testBAController = this.CreateTestBAController();

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");

            bundleOnlyA.Install();
            bundleOnlyA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V100);

            // Delete the installed file and registry key so we have something to repair.
            File.Delete(packageAv1SourceCodeInstalled);
            packageAv1.DeleteTestRegistryValue(testRegistryValue);

            testBAController.SetPackageRequestedState("PackageA", RequestState.Repair);
            testBAController.SetPackageRequestedState("PatchA", RequestState.Repair);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            testBAController.ResetPackageStates("PackageA");
            testBAController.ResetPackageStates("PatchA");

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V100);

            bundleOnlyA.Uninstall();
            bundleOnlyA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), String.Concat("PackageAv1 payload should have been removed by uninstall from: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        [Fact]
        public void CanUninstallSlipstreamedPatchAlone()
        {
            var testRegistryValue = "PackageA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            testBAController.SetPackageRequestedState("PatchA", RequestState.Absent);

            bundleA.Modify();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V100);

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), String.Concat("PackageAv1 payload should have been removed by uninstall from: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        [Fact]
        public void CanModifyToUninstallPackageWithSlipstreamedPatch()
        {
            var testRegistryValue = "PackageA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageBv1 = this.CreatePackageInstaller("PackageBv1");
            var bundleB = this.CreateBundleInstaller("BundleB");
            var testBAController = this.CreateTestBAController();

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            var packageBv1SourceCodeInstalled = packageBv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), $"PackageBv1 payload should not be there on test start: {packageBv1SourceCodeInstalled}");

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);
            Assert.True(File.Exists(packageBv1SourceCodeInstalled), String.Concat("Should have found PackageBv1 payload installed at: ", packageBv1SourceCodeInstalled));

            testBAController.SetPackageRequestedState("PackageA", RequestState.Absent);
            testBAController.SetPackageRequestedState("PatchA", RequestState.Absent);

            bundleB.Modify();
            bundleB.VerifyRegisteredAndInPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should have been removed by modify from: {packageAv1SourceCodeInstalled}");

            testBAController.ResetPackageStates("PackageA");
            testBAController.ResetPackageStates("PatchA");

            bundleB.Uninstall();
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), String.Concat("PackageBv1 payload should have been removed by uninstall from: ", packageBv1SourceCodeInstalled));
            packageBv1.VerifyTestRegistryRootDeleted();
        }

        [Fact]
        public void UninstallsPackageWithSlipstreamedPatchDuringRollback()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageBv1 = this.CreatePackageInstaller("PackageBv1");
            var bundleB = this.CreateBundleInstaller("BundleB");
            var testBAController = this.CreateTestBAController();

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            var packageBv1SourceCodeInstalled = packageBv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), $"PackageBv1 payload should not be there on test start: {packageBv1SourceCodeInstalled}");

            testBAController.SetPackageCancelExecuteAtProgress("PackageB", 50);

            bundleB.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should have been removed by rollback from: {packageAv1SourceCodeInstalled}");
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), String.Concat("PackageBv1 payload should not have been installed from: ", packageBv1SourceCodeInstalled));
            packageBv1.VerifyTestRegistryRootDeleted();
        }

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6359")]
        public void CanAutomaticallyPredetermineSlipstreamPatchesAtBuildTime()
        {
            var testRegistryValueA = "PackageA";
            var testRegistryValueA2 = "PackageA2";
            var testRegistryValueB = "PackageB";
            var testRegistryValueB2 = "PackageB2";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageBv1 = this.CreatePackageInstaller("PackageBv1");
            var bundleC = this.CreateBundleInstaller("BundleC");

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            var packageBv1SourceCodeInstalled = packageBv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), $"PackageBv1 payload should not be there on test start: {packageBv1SourceCodeInstalled}");

            bundleC.Install();
            bundleC.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            // Product A should've slipstreamed both patches.
            packageAv1.VerifyTestRegistryValue(testRegistryValueA, V101);
            packageAv1.VerifyTestRegistryValue(testRegistryValueA2, V101);
            // Product B should've only slipstreamed patch AB2.
            packageBv1.VerifyTestRegistryValue(testRegistryValueB, V100);
            packageBv1.VerifyTestRegistryValue(testRegistryValueB2, V101);

            bundleC.Uninstall();
            bundleC.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), String.Concat("PackageAv1 payload should have been removed by uninstall from: ", packageAv1SourceCodeInstalled));
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), String.Concat("PackageBv1 payload should have been removed by uninstall from: ", packageBv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        [Fact]
        public void CanInstallSlipstreamedPatchWithPackageDuringMajorUpgrade()
        {
            var testRegistryValue = "PackageA";

            var packageAv0 = this.CreatePackageInstaller("PackageAv0_9_0");
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleA");

            packageAv1.VerifyInstalled(false);

            packageAv0.InstallProduct();
            packageAv0.VerifyInstalled(true);
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V090);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
            packageAv0.VerifyInstalled(false);
            packageAv1.VerifyInstalled(true);
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V101);

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            packageAv1.VerifyInstalled(false);
            packageAv1.VerifyTestRegistryRootDeleted();
        }

        [Fact]
        public void RespectsSlipstreamedPatchInstallCondition()
        {
            var testRegistryValue = "PackageA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var bundleD = this.CreateBundleInstaller("BundleD");

            var packageAv1SourceCodeInstalled = packageAv1.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), $"PackageAv1 payload should not be there on test start: {packageAv1SourceCodeInstalled}");

            bundleD.Install();
            bundleD.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageAv1SourceCodeInstalled), String.Concat("Should have found PackageAv1 payload installed at: ", packageAv1SourceCodeInstalled));
            // The patch was not supposed to be installed.
            packageAv1.VerifyTestRegistryValue(testRegistryValue, V100);

            bundleD.Uninstall();
            bundleD.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageAv1SourceCodeInstalled), String.Concat("PackageAv1 payload should have been removed by uninstall from: ", packageAv1SourceCodeInstalled));
            packageAv1.VerifyTestRegistryRootDeleted();
        }
    }
}
