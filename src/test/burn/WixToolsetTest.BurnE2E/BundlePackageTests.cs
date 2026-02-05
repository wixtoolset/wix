// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using WixTestTools;
    using WixToolset.BootstrapperApplicationApi;
    using Xunit;
    using Xunit.Abstractions;

    public class BundlePackageTests : BurnE2ETests
    {
        public BundlePackageTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanBuildBundlePackagesWithRemoteBundles()
        {
            var multipleBundlePackagesWithRemoteBundle = this.CreateBundleInstaller("MultipleBundlePackagesWithRemoteBundle");

            multipleBundlePackagesWithRemoteBundle.Install();
            // Bundle only contains permanent packages, so it isn't registered
            multipleBundlePackagesWithRemoteBundle.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        [RuntimeFact]
        public void CanInstallAndUninstallBundlePackages()
        {
            var packageA = this.CreatePackageInstaller(@"..\BasicFunctionalityTests\PackageA");
            var packageA_x64 = this.CreatePackageInstaller(@"..\BasicFunctionalityTests\PackageA_x64");
            var bundleA = this.CreateBundleInstaller(@"..\BasicFunctionalityTests\BundleA");
            var bundleB_x64 = this.CreateBundleInstaller(@"..\BasicFunctionalityTests\BundleB_x64");
            var multipleBundlePackagesBundle = this.CreateBundleInstaller(@"MultipleBundlePackagesBundle");

            var packageA32SourceCodeFilePath = packageA.GetInstalledFilePath("Package.wxs");
            var packageA64SourceCodeFilePath = packageA_x64.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageA32SourceCodeFilePath), $"PackageA payload should not be there on test start: {packageA32SourceCodeFilePath}");
            Assert.False(File.Exists(packageA64SourceCodeFilePath), $"PackageA_x64 payload should not be there on test start: {packageA64SourceCodeFilePath}");

            multipleBundlePackagesBundle.Install();
            multipleBundlePackagesBundle.VerifyRegisteredAndInPackageCache();

            bundleA.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);
            bundleB_x64.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);

            // Source file should be installed
            Assert.True(File.Exists(packageA32SourceCodeFilePath), $"Should have found PackageA payload installed at: {packageA32SourceCodeFilePath}");
            Assert.True(File.Exists(packageA64SourceCodeFilePath), $"Should have found PackageA_x64 payload installed at: {packageA64SourceCodeFilePath}");

            multipleBundlePackagesBundle.Uninstall();
            multipleBundlePackagesBundle.VerifyUnregisteredAndRemovedFromPackageCache();

            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleB_x64.VerifyUnregisteredAndRemovedFromPackageCache();

            // Source file should *not* be installed
            Assert.False(File.Exists(packageA32SourceCodeFilePath), $"PackageA payload should have been removed by uninstall from: {packageA32SourceCodeFilePath}");
            Assert.False(File.Exists(packageA64SourceCodeFilePath), $"PackageA_x64 payload should have been removed by uninstall from: {packageA64SourceCodeFilePath}");
        }

        [RuntimeFact]
        public void CanInstallUpgradeBundlePackage()
        {
            var bundleAv1 = this.CreateBundleInstaller(@"..\UpgradeRelatedBundleTests\BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller(@"..\UpgradeRelatedBundleTests\BundleAv2");
            var upgradeBundlePackageBundlev2 = this.CreateBundleInstaller("UpgradeBundlePackageBundlev2");

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            upgradeBundlePackageBundlev2.Install();
            upgradeBundlePackageBundlev2.VerifyRegisteredAndInPackageCache();
            bundleAv2.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        [RuntimeFact]
        public void CanInstallV3BundlePackage()
        {
            var v3BundleId = "{215a70db-ab35-48c7-be51-d66eaac87177}";
            var v3BundleName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Package Cache", v3BundleId, "CustomV3Theme");
            var v3Bundle = new BundleInstaller(this.TestContext, v3BundleName);
            this.AddBundleInstaller(v3Bundle);
            var v3BundlePackageBundle = this.CreateBundleInstaller("V3BundlePackageBundle");

            Assert.False(File.Exists(v3Bundle.Bundle), "v3bundle.exe was already installed");

            var logPath = v3BundlePackageBundle.Install();
            v3BundlePackageBundle.VerifyRegisteredAndInPackageCache();

            Assert.True(LogVerifier.MessageInLogFile(logPath, "Applied execute package: v3bundle.exe, result: 0x0, restart: None"));

            Assert.True(BundleRegistration.TryGetPerMachineBundleRegistrationById(v3BundleId, false, this.TestContext.TestOutputHelper, out var v3Registration));
            Assert.Null(v3Registration.SystemComponent);
        }

        [RuntimeFact]
        public void CanLeaveBundlePackageVisible()
        {
            var bundleAv1 = this.CreateBundleInstaller(@"..\UpgradeRelatedBundleTests\BundleAv1");
            var upgradeBundlePackageBundlev1 = this.CreateBundleInstaller("UpgradeBundlePackageBundlev1");

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            upgradeBundlePackageBundlev1.Install();
            upgradeBundlePackageBundlev1.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            upgradeBundlePackageBundlev1.Uninstall();
            upgradeBundlePackageBundlev1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv1.VerifyRegisteredAndInPackageCache();
        }

        [RuntimeFact]
        public void CanReferenceCountBundlePackage()
        {
            var bundleAv1 = this.CreateBundleInstaller(@"..\UpgradeRelatedBundleTests\BundleAv1");
            var upgradeBundlePackageBundlev1 = this.CreateBundleInstaller("UpgradeBundlePackageBundlev1");

            upgradeBundlePackageBundlev1.Install();
            upgradeBundlePackageBundlev1.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);

            // Repair bundle so it adds itself as a reference to itself.
            bundleAv1.Repair();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            upgradeBundlePackageBundlev1.Uninstall();
            upgradeBundlePackageBundlev1.VerifyUnregisteredAndRemovedFromPackageCache();

            bundleAv1.VerifyRegisteredAndInPackageCache();
        }

        [RuntimeFact]
        public void CanSkipObsoleteBundlePackage()
        {
            var bundleAv1 = this.CreateBundleInstaller(@"..\UpgradeRelatedBundleTests\BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller(@"..\UpgradeRelatedBundleTests\BundleAv2");
            var upgradeBundlePackageBundlev1 = this.CreateBundleInstaller("UpgradeBundlePackageBundlev1");

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            upgradeBundlePackageBundlev1.Install();
            upgradeBundlePackageBundlev1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        [RuntimeFact]
        public void CanRecacheAndReinstallBundlePackageOnUninstallRollback()
        {
            var packageFail = this.CreatePackageInstaller("PackageFail");
            var packageA = this.CreatePackageInstaller(@"..\BasicFunctionalityTests\PackageA");
            var bundleA = this.CreateBundleInstaller(@"..\BasicFunctionalityTests\BundleA");
            var bundlePackageUninstallFailureBundle = this.CreateBundleInstaller("BundlePackageUninstallFailureBundle");
            var testBAController = this.CreateTestBAController();
            var bundleASelfCachedPath = bundleA.GetExpectedCachedBundlePath();
            var bundleAEmbeddedCachedPath = bundlePackageUninstallFailureBundle.GetPackageEntryPointCachePath("PackageA");

            packageA.VerifyInstalled(false);
            packageFail.VerifyInstalled(false);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            testBAController.SetPackageRequestedCacheType("PackageA", BOOTSTRAPPER_CACHE_TYPE.Remove);

            var installLogPath = bundlePackageUninstallFailureBundle.Install();
            bundlePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            packageFail.VerifyInstalled(true);
            bundleA.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);

            Assert.False(LogVerifier.MessageInLogFile(installLogPath, $"Applying execute package: PackageA, action: Install, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"Applying execute package: PackageA, action: Install, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);

            testBAController.ResetPackageStates("PackageA");
            testBAController.SetAllowAcquireAfterValidationFailure();

            var uninstallLogPath = bundlePackageUninstallFailureBundle.Uninstall((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "FAILWHENDEFERRED=1");
            bundlePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            bundleA.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);
            packageFail.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, "TESTBA: OnCachePackageNonVitalValidationFailure() - id: PackageA, default: None, requested: Acquire"));
            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying execute package: PackageA, action: Uninstall, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying execute package: PackageA, action: Uninstall, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);
            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying rollback package: PackageA, action: Install, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying rollback package: PackageA, action: Install, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);
        }

        [RuntimeFact]
        public void CanReinstallBundlePackageOnUninstallRollback()
        {
            var packageFail = this.CreatePackageInstaller("PackageFail");
            var packageA = this.CreatePackageInstaller(@"..\BasicFunctionalityTests\PackageA");
            var bundleA = this.CreateBundleInstaller(@"..\BasicFunctionalityTests\BundleA");
            var bundlePackageUninstallFailureBundle = this.CreateBundleInstaller("BundlePackageUninstallFailureBundle");
            var bundleASelfCachedPath = bundleA.GetExpectedCachedBundlePath();
            var bundleAEmbeddedCachedPath = bundlePackageUninstallFailureBundle.GetPackageEntryPointCachePath("PackageA");

            packageA.VerifyInstalled(false);
            packageFail.VerifyInstalled(false);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            var installLogPath = bundlePackageUninstallFailureBundle.Install();
            bundlePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            packageFail.VerifyInstalled(true);
            bundleA.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);

            Assert.False(LogVerifier.MessageInLogFile(installLogPath, $"Applying execute package: PackageA, action: Install, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"Applying execute package: PackageA, action: Install, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);

            var uninstallLogPath = bundlePackageUninstallFailureBundle.Uninstall((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "FAILWHENDEFERRED=1");
            bundlePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            bundleA.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);
            packageFail.VerifyInstalled(true);

            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, "TESTBA: OnCachePackageNonVitalValidationFailure() - id: PackageA"));
            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying execute package: PackageA, action: Uninstall, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying execute package: PackageA, action: Uninstall, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);
            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying rollback package: PackageA, action: Install, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying rollback package: PackageA, action: Install, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);
        }

        [RuntimeFact]
        public void CanSkipReinstallBundlePackageOnUninstallRollback()
        {
            var packageFail = this.CreatePackageInstaller("PackageFail");
            var packageA = this.CreatePackageInstaller(@"..\BasicFunctionalityTests\PackageA");
            var bundleA = this.CreateBundleInstaller(@"..\BasicFunctionalityTests\BundleA");
            var bundlePackageUninstallFailureBundle = this.CreateBundleInstaller("BundlePackageUninstallFailureBundle");
            var testBAController = this.CreateTestBAController();
            var bundleASelfCachedPath = bundleA.GetExpectedCachedBundlePath();
            var bundleAEmbeddedCachedPath = bundlePackageUninstallFailureBundle.GetPackageEntryPointCachePath("PackageA");

            packageA.VerifyInstalled(false);
            packageFail.VerifyInstalled(false);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            testBAController.SetPackageRequestedCacheType("PackageA", BOOTSTRAPPER_CACHE_TYPE.Remove);

            var installLogPath = bundlePackageUninstallFailureBundle.Install();
            bundlePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            packageFail.VerifyInstalled(true);
            bundleA.VerifyRegisteredAndInPackageCache(expectedSystemComponent: 1);

            Assert.False(LogVerifier.MessageInLogFile(installLogPath, $"Applying execute package: PackageA, action: Install, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"Applying execute package: PackageA, action: Install, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);

            var uninstallLogPath = bundlePackageUninstallFailureBundle.Uninstall((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "FAILWHENDEFERRED=1");
            bundlePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            packageFail.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, "TESTBA: OnCachePackageNonVitalValidationFailure() - id: PackageA, default: None, requested: None"));
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying execute package: PackageA, action: Uninstall, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying execute package: PackageA, action: Uninstall, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);
            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying rollback package: PackageA, action: Install, path: {bundleASelfCachedPath}"), bundleASelfCachedPath);
            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, $"Applying rollback package: PackageA, action: Install, path: {bundleAEmbeddedCachedPath}"), bundleAEmbeddedCachedPath);
        }
    }
}
