// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using WixTestTools;
    using WixToolset.Mba.Core;
    using Xunit;
    using Xunit.Abstractions;

    public class DependencyTests : BurnE2ETests
    {
        public DependencyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanKeepSameExactPackageAfterUpgradingBundle()
        {
            var packageFv1 = this.CreatePackageInstaller("PackageFv1");
            var bundleKv1 = this.CreateBundleInstaller("BundleKv1");
            var bundleKv2 = this.CreateBundleInstaller("BundleKv2");

            packageFv1.VerifyInstalled(false);

            bundleKv1.Install();
            bundleKv1.VerifyRegisteredAndInPackageCache();

            packageFv1.VerifyInstalled(true);

            bundleKv2.Install();
            bundleKv2.VerifyRegisteredAndInPackageCache();
            bundleKv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageFv1.VerifyInstalled(true);

            bundleKv2.VerifyPackageIsCached("PackageF");

            bundleKv2.Uninstall();
            bundleKv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageFv1.VerifyInstalled(false);
        }

        [Fact (Skip = "https://github.com/wixtoolset/issues/issues/6401")]
        public void CanKeepSameExactPackageAfterUpgradingBundleWithSlipstreamedPatch()
        {
            var originalVersion = "1.0.0.0";
            var patchedVersion = "1.0.1.0";
            var testRegistryValue = "PackageA";
            var testRegistryValueExe = "ExeA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleAv1");
            var bundleC = this.CreateBundleInstaller("BundleC");

            packageA.VerifyInstalled(false);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageA.VerifyTestRegistryValue(testRegistryValue, originalVersion);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, originalVersion);

            // Verify https://github.com/wixtoolset/issues/issues/3294 - Uninstalling bundle registers a dependency on a package
            bundleC.Install();
            bundleC.VerifyRegisteredAndInPackageCache();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(true);
            packageA.VerifyTestRegistryValue(testRegistryValue, patchedVersion);
            bundleA.VerifyExeTestRegistryRootDeleted(testRegistryValueExe);

            // Verify https://github.com/wixtoolset/issues/issues/2915 - Update bundle removes previously cached MSIs
            bundleC.Repair();

            bundleC.Uninstall();
            bundleC.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
        }

        [Fact]
        public void CanKeepUpgradedPackageAfterUninstallUpgradedBundle()
        {
            var testRegistryValueExe = "ExeA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv101 = this.CreatePackageInstaller("PackageAv1_0_1");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv101 = this.CreateBundleInstaller("BundleAv1_0_1");
            var bundleB = this.CreateBundleInstaller("BundleB");

            packageAv1.VerifyInstalledWithVersion(false);
            packageAv101.VerifyInstalledWithVersion(false);
            packageB.VerifyInstalled(false);

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalledWithVersion(true);
            bundleAv1.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalledWithVersion(true);
            bundleAv1.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
            packageB.VerifyInstalled(true);

            bundleAv101.Install();
            bundleAv101.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalledWithVersion(false);
            packageAv101.VerifyInstalledWithVersion(true);
            bundleAv1.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.1.0");

            bundleAv101.Uninstall();
            bundleAv101.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv101.VerifyInstalledWithVersion(true);
            bundleAv1.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.1.0");
        }

        [Fact]
        public void UninstallsOrphanCompatiblePackages()
        {
            var testRegistryValueExe = "ExeA";

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");
            var bundleB = this.CreateBundleInstaller("BundleB");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            bundleAv1.VerifyPackageIsCached("PackageA");
            bundleAv1.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            bundleAv1.VerifyPackageIsCached("PackageA");
            bundleAv1.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
            packageB.VerifyInstalled(true);

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            bundleAv1.VerifyPackageIsCached("PackageA", false);
            packageAv2.VerifyInstalled(true);
            bundleAv2.VerifyPackageIsCached("PackageA");
            bundleAv1.VerifyExeTestRegistryValue(testRegistryValueExe, "2.0.0.0");

            bundleAv2.Uninstall();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv2.VerifyInstalled(true);
            bundleAv2.VerifyPackageIsCached("PackageA");
            bundleAv1.VerifyExeTestRegistryValue(testRegistryValueExe, "2.0.0.0");
            
            // Verify https://github.com/wixtoolset/issues/issues/3190
            bundleB.Uninstall();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);
            bundleAv2.VerifyPackageIsCached("PackageA", false);
            packageB.VerifyInstalled(false);
            bundleAv1.VerifyExeTestRegistryRootDeleted(testRegistryValueExe);
        }

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6401")]
        public void CanMinorUpgradeDependencyPackageFromPatchBundle()
        {
            var originalVersion = "1.0.0.0";
            var patchedVersion = "1.0.1.0";
            var testRegistryValue = "PackageA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageEv1 = this.CreatePackageInstaller("PackageEv1");
            var packageEv101 = this.CreatePackageInstaller("PackageEv1_0_1");
            var bundleJ = this.CreateBundleInstaller("BundleJ");
            var bundleJ_Patch = this.CreateBundleInstaller("BundleJ_Patch");

            packageA.VerifyInstalled(false);
            packageEv1.VerifyInstalledWithVersion(false);
            packageEv101.VerifyInstalledWithVersion(false);

            bundleJ.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleJ.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageA.VerifyTestRegistryValue(testRegistryValue, originalVersion);
                packageEv1.VerifyInstalledWithVersion(true);
            }

            bundleJ_Patch.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleJ_Patch.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageA.VerifyTestRegistryValue(testRegistryValue, patchedVersion);
                packageEv1.VerifyInstalledWithVersion(false);
                packageEv101.VerifyInstalledWithVersion(true);
            }

            bundleJ.Uninstall();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleJ.VerifyUnregisteredAndRemovedFromPackageCache();
                bundleJ_Patch.VerifyUnregisteredAndRemovedFromPackageCache();

                packageA.VerifyInstalled(false);
                packageEv1.VerifyInstalledWithVersion(false);
                packageEv101.VerifyInstalledWithVersion(false);
            }
        }

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6401")]
        public void CanMinorUpgradeDependencyPackageFromPatchBundleThenUninstallToRestoreBase()
        {
            var originalVersion = "1.0.0.0";
            var patchedVersion = "1.0.1.0";
            var testRegistryValue = "PackageA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageEv1 = this.CreatePackageInstaller("PackageEv1");
            var packageEv101 = this.CreatePackageInstaller("PackageEv1_0_1");
            var bundleJ = this.CreateBundleInstaller("BundleJ");
            var bundleJ_Patch = this.CreateBundleInstaller("BundleJ_Patch");

            packageA.VerifyInstalled(false);
            packageEv1.VerifyInstalledWithVersion(false);
            packageEv101.VerifyInstalledWithVersion(false);

            bundleJ.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleJ.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageA.VerifyTestRegistryValue(testRegistryValue, originalVersion);
                packageEv1.VerifyInstalledWithVersion(true);
            }

            bundleJ_Patch.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleJ_Patch.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageA.VerifyTestRegistryValue(testRegistryValue, patchedVersion);
                packageEv1.VerifyInstalledWithVersion(false);
                packageEv101.VerifyInstalledWithVersion(true);
            }

            bundleJ_Patch.Uninstall();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleJ_Patch.VerifyUnregisteredAndRemovedFromPackageCache();

                packageA.VerifyInstalled(true);
                packageA.VerifyTestRegistryValue(testRegistryValue, originalVersion);
                packageEv1.VerifyInstalledWithVersion(true);
                packageEv101.VerifyInstalledWithVersion(false);
            }

            bundleJ.Uninstall();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleJ.VerifyUnregisteredAndRemovedFromPackageCache();

                packageA.VerifyInstalled(false);
                packageEv1.VerifyInstalledWithVersion(false);
                packageEv101.VerifyInstalledWithVersion(false);
            }
        }

        [Fact]
        public void CanUninstallBaseWithAddOnsWhenAllSharePackages()
        {
            var testRegistryValueExe = "ExeA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleF = this.CreateBundleInstaller("BundleF");
            var bundleF_AddOnA = this.CreateBundleInstaller("BundleF_AddOnA");
            var bundleF_AddOnB = this.CreateBundleInstaller("BundleF_AddOnB");

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleF.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageB.VerifyInstalled(true);
            }

            bundleF_AddOnA.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF_AddOnA.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                bundleF.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
                packageB.VerifyInstalled(true);
            }

            bundleF_AddOnB.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF_AddOnB.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                bundleF.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
                packageB.VerifyInstalled(true);
            }

            bundleF.Uninstall();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF.VerifyUnregisteredAndRemovedFromPackageCache();
                bundleF_AddOnA.VerifyUnregisteredAndRemovedFromPackageCache();
                bundleF_AddOnB.VerifyUnregisteredAndRemovedFromPackageCache();

                packageA.VerifyInstalled(false);
                bundleF.VerifyExeTestRegistryRootDeleted(testRegistryValueExe);
                packageB.VerifyInstalled(false);
            }
        }

        [Fact]
        public void CanUpgradeBaseWithAddOns()
        {
            var testRegistryValueExe = "ExeA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleF = this.CreateBundleInstaller("BundleF");
            var bundleF_AddOnA = this.CreateBundleInstaller("BundleF_AddOnA");
            var bundleF_AddOnB = this.CreateBundleInstaller("BundleF_AddOnB");
            var bundleFv2 = this.CreateBundleInstaller("BundleFv2");

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleF.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageB.VerifyInstalled(true);
            }

            bundleF_AddOnA.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF_AddOnA.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                bundleF.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
                packageB.VerifyInstalled(true);
            }

            bundleF_AddOnB.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF_AddOnB.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                bundleF.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
                packageB.VerifyInstalled(true);
            }

            bundleFv2.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleFv2.VerifyRegisteredAndInPackageCache();
                bundleF.VerifyUnregisteredAndRemovedFromPackageCache();
                bundleF_AddOnA.VerifyRegisteredAndInPackageCache();
                bundleF_AddOnB.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                bundleF.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
                packageB.VerifyInstalled(true);
            }
        }

        [Fact]
        public void CanUninstallDependencyPackagesWithBundlesUninstalledInFifoOrder()
        {
            var testRegistryValueExe = "ExeA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleAv1");
            var bundleB = this.CreateBundleInstaller("BundleB");

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
            packageB.VerifyInstalled(true);

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(true);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
            packageB.VerifyInstalled(true);

            bundleB.Uninstall();
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            bundleA.VerifyExeTestRegistryRootDeleted(testRegistryValueExe);
            packageB.VerifyInstalled(false);
        }

        [Fact]
        public void CanUninstallDependencyPackagesWithBundlesUninstalledInReverseOrder()
        {
            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleAv1");
            var bundleB = this.CreateBundleInstaller("BundleB");

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageB.VerifyInstalled(true);

            bundleB.Uninstall();
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(true);

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);
        }

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6401")]
        public void CanUpgradePatchBundleWithAdditionalPatch()
        {
            var originalVersion = "1.0.0.0";
            var patchedVersion = "1.0.1.0";
            var patchedVersion2 = "1.0.2.0";
            var testRegistryValue = "PackageA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageB = this.CreatePackageInstaller("PackageBv1");
            var bundleF = this.CreateBundleInstaller("BundleF");
            var bundleF_PatchAv101 = this.CreateBundleInstaller("BundleF_PatchAv1_0_1");
            var bundleF_PatchAv102 = this.CreateBundleInstaller("BundleF_PatchAv1_0_2");

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleF.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageA.VerifyTestRegistryValue(testRegistryValue, originalVersion);
                packageB.VerifyInstalled(true);
            }

            bundleF_PatchAv101.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF_PatchAv101.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageA.VerifyTestRegistryValue(testRegistryValue, patchedVersion);
                packageB.VerifyInstalled(false);
            }

            bundleF_PatchAv102.Install();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF_PatchAv102.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageA.VerifyTestRegistryValue(testRegistryValue, patchedVersion2);
                packageB.VerifyInstalled(false);
            }

            bundleF.Uninstall();
            if (this.SupportAddonAndPatchRelatedBundles)
            {
                bundleF.VerifyUnregisteredAndRemovedFromPackageCache();
                bundleF_PatchAv101.VerifyUnregisteredAndRemovedFromPackageCache();
                bundleF_PatchAv102.VerifyUnregisteredAndRemovedFromPackageCache();

                packageA.VerifyInstalled(false);
                packageB.VerifyInstalled(false);
            }
        }

        [Fact]
        public void DoesntLoseDependenciesOnFailedMajorUpgradeBundleFromMajorUpdateMsiFifo()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageC = this.CreatePackageInstaller("PackageC");
            var packageFv1 = this.CreatePackageInstaller("PackageFv1");
            var packageFv2 = this.CreatePackageInstaller("PackageFv2");
            var packageGv1 = this.CreatePackageInstaller("PackageGv1");
            var packageGv2 = this.CreatePackageInstaller("PackageGv2");
            var bundleM = this.CreateBundleInstaller("BundleM");
            var bundleNv1 = this.CreateBundleInstaller("BundleNv1");
            var bundleNv2 = this.CreateBundleInstaller("BundleNv2");
            var testBAController = this.CreateTestBAController();

            packageAv1.VerifyInstalled(false);
            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalled(false);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(false);

            bundleM.Install();
            bundleM.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(false);

            bundleNv1.Install();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(true);
            packageGv2.VerifyInstalled(false);

            // Make PackageC fail.
            testBAController.SetPackageCancelExecuteAtProgress("PackageC", 10);

            bundleNv2.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleNv2.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(true);
            packageGv2.VerifyInstalled(false);

            bundleM.Uninstall();
            bundleM.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(true);
            packageGv2.VerifyInstalled(false);

            bundleNv1.Uninstall();
            bundleNv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageFv1.VerifyInstalled(false);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(false);
        }

        [Fact]
        public void DoesntLoseDependenciesOnFailedMajorUpgradeBundleFromMajorUpdateMsiLifo()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageC = this.CreatePackageInstaller("PackageC");
            var packageFv1 = this.CreatePackageInstaller("PackageFv1");
            var packageFv2 = this.CreatePackageInstaller("PackageFv2");
            var packageGv1 = this.CreatePackageInstaller("PackageGv1");
            var packageGv2 = this.CreatePackageInstaller("PackageGv2");
            var bundleM = this.CreateBundleInstaller("BundleM");
            var bundleNv1 = this.CreateBundleInstaller("BundleNv1");
            var bundleNv2 = this.CreateBundleInstaller("BundleNv2");
            var testBAController = this.CreateTestBAController();

            packageAv1.VerifyInstalled(false);
            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalled(false);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(false);

            bundleM.Install();
            bundleM.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(false);

            bundleNv1.Install();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(true);
            packageGv2.VerifyInstalled(false);

            // Make PackageC fail.
            testBAController.SetPackageCancelExecuteAtProgress("PackageC", 10);

            bundleNv2.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleNv2.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(true);
            packageGv2.VerifyInstalled(false);

            bundleNv1.Uninstall();
            bundleNv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(false);

            bundleM.Uninstall();
            bundleM.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageFv1.VerifyInstalled(false);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(false);
        }

        [Fact]
        public void DoesntLoseDependenciesOnFailedMajorUpgradeBundleFromMinorUpdateMsiFifo()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageC = this.CreatePackageInstaller("PackageC");
            var packageFv1 = this.CreatePackageInstaller("PackageFv1");
            var packageFv101 = this.CreatePackageInstaller("PackageFv1_0_1");
            var packageGv1 = this.CreatePackageInstaller("PackageGv1");
            var packageGv101 = this.CreatePackageInstaller("PackageGv1_0_1");
            var bundleM = this.CreateBundleInstaller("BundleM");
            var bundleNv1 = this.CreateBundleInstaller("BundleNv1");
            var bundleNv101 = this.CreateBundleInstaller("BundleNv1_0_1");
            var testBAController = this.CreateTestBAController();

            packageAv1.VerifyInstalled(false);
            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalledWithVersion(false);
            packageFv101.VerifyInstalledWithVersion(false);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(false);

            bundleM.Install();
            bundleM.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalledWithVersion(true);
            packageFv101.VerifyInstalledWithVersion(false);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(false);

            bundleNv1.Install();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalledWithVersion(true);
            packageFv101.VerifyInstalledWithVersion(false);
            packageGv1.VerifyInstalledWithVersion(true);
            packageGv101.VerifyInstalledWithVersion(false);

            // Make PackageC fail.
            testBAController.SetPackageCancelExecuteAtProgress("PackageC", 10);

            // Verify https://github.com/wixtoolset/issues/issues/6510 - Dependency provider removed on rollback even though package is not rolled back
            bundleNv101.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleNv101.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            // The expected values will change after implementing https://github.com/wixtoolset/issues/issues/6535 and https://github.com/wixtoolset/issues/issues/3421
            packageAv1.VerifyInstalled(true);
            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalledWithVersion(false);
            packageFv101.VerifyInstalledWithVersion(true);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(true);

            bundleM.Uninstall();
            bundleM.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageFv1.VerifyInstalledWithVersion(false);
            packageFv101.VerifyInstalledWithVersion(true);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(true);

            bundleNv1.Uninstall();
            bundleNv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageFv1.VerifyInstalledWithVersion(false);
            packageFv101.VerifyInstalledWithVersion(false);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(false);
        }

        [Fact]
        public void DoesntLoseDependenciesOnFailedMajorUpgradeBundleFromMinorUpdateMsiLifo()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageC = this.CreatePackageInstaller("PackageC");
            var packageFv1 = this.CreatePackageInstaller("PackageFv1");
            var packageFv101 = this.CreatePackageInstaller("PackageFv1_0_1");
            var packageGv1 = this.CreatePackageInstaller("PackageGv1");
            var packageGv101 = this.CreatePackageInstaller("PackageGv1_0_1");
            var bundleM = this.CreateBundleInstaller("BundleM");
            var bundleNv1 = this.CreateBundleInstaller("BundleNv1");
            var bundleNv101 = this.CreateBundleInstaller("BundleNv1_0_1");
            var testBAController = this.CreateTestBAController();

            packageAv1.VerifyInstalled(false);
            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalledWithVersion(false);
            packageFv101.VerifyInstalledWithVersion(false);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(false);

            bundleM.Install();
            bundleM.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalledWithVersion(true);
            packageFv101.VerifyInstalledWithVersion(false);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(false);

            bundleNv1.Install();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalledWithVersion(true);
            packageFv101.VerifyInstalledWithVersion(false);
            packageGv1.VerifyInstalledWithVersion(true);
            packageGv101.VerifyInstalledWithVersion(false);

            // Make PackageC fail.
            testBAController.SetPackageCancelExecuteAtProgress("PackageC", 10);

            // Verify https://github.com/wixtoolset/issues/issues/6510 - Dependency provider removed on rollback even though package is not rolled back
            bundleNv101.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleNv101.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            // The expected values will change after implementing https://github.com/wixtoolset/issues/issues/6535 and https://github.com/wixtoolset/issues/issues/3421
            packageAv1.VerifyInstalled(true);
            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalledWithVersion(false);
            packageFv101.VerifyInstalledWithVersion(true);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(true);

            bundleNv1.Uninstall();
            bundleNv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(true);
            packageFv1.VerifyInstalledWithVersion(false);
            packageFv101.VerifyInstalledWithVersion(true);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(false);

            bundleM.Uninstall();
            bundleM.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageFv1.VerifyInstalledWithVersion(false);
            packageFv101.VerifyInstalledWithVersion(false);
            packageGv1.VerifyInstalledWithVersion(false);
            packageGv101.VerifyInstalledWithVersion(false);
        }

        [Fact]
        public void DoesntRegisterDependencyOnPackageNotSelectedForInstall()
        {
            var testRegistryValueExe = "ExeA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleAv1");
            var bundleB = this.CreateBundleInstaller("BundleB");
            var testBAController = this.CreateTestBAController();

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");

            // Verify https://github.com/wixtoolset/issues/issues/3456 - Dependency registered on package though unselected to instal
            testBAController.SetPackageRequestedState("PackageA", RequestState.None);
            testBAController.SetPackageRequestedState("PackageB", RequestState.None);

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
            packageB.VerifyInstalled(false);

            testBAController.ResetPackageStates("PackageA");
            testBAController.ResetPackageStates("PackageB");

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");
            packageB.VerifyInstalled(false);

            bundleB.Uninstall();
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            bundleA.VerifyExeTestRegistryRootDeleted(testRegistryValueExe);
            packageB.VerifyInstalled(false);
        }

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/3516")]
        public void DoesntRollbackPackageInstallIfPreexistingDependents()
        {
            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageC = this.CreatePackageInstaller("PackageC");
            var bundleE = this.CreateBundleInstaller("BundleE");
            var bundleL = this.CreateBundleInstaller("BundleL");
            var testBAController = this.CreateTestBAController();

            packageA.VerifyInstalled(false);
            packageC.VerifyInstalled(false);

            // Make PackageC fail.
            testBAController.SetPackageCancelExecuteAtProgress("PackageC", 10);

            bundleE.Install();
            bundleE.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageC.VerifyInstalled(false);

            // Make PackageC install then rollback.
            testBAController.SetPackageCancelExecuteAtProgress("PackageC", null);
            testBAController.SetPackageCancelOnProgressAtProgress("PackageC", 10);

            bundleL.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_USEREXIT);
            bundleL.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(true);
            packageC.VerifyInstalled(true);

            testBAController.SetPackageCancelOnProgressAtProgress("PackageC", null);

            bundleE.Uninstall();
            bundleE.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            packageC.VerifyInstalled(false);
        }

        [Fact]
        public void RegistersDependencyOnFailedNonVitalPackages()
        {
            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageC = this.CreatePackageInstaller("PackageC");
            var bundleE = this.CreateBundleInstaller("BundleE");
            var bundleL = this.CreateBundleInstaller("BundleL");
            var testBAController = this.CreateTestBAController();

            packageA.VerifyInstalled(false);
            packageC.VerifyInstalled(false);

            // Make PackageC fail.
            testBAController.SetPackageCancelExecuteAtProgress("PackageC", 10);

            // Verify https://github.com/wixtoolset/issues/issues/3406 - Non-vital failure result in bundle failure (install)
            bundleE.Install();
            bundleE.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageC.VerifyInstalled(false);

            // Verify https://github.com/wixtoolset/issues/issues/3406 - Non-vital failure result in bundle failure (repair)
            bundleE.Repair();
            bundleE.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageC.VerifyInstalled(false);

            testBAController.SetPackageCancelExecuteAtProgress("PackageC", null);

            bundleL.Install();
            bundleL.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageC.VerifyInstalled(true);

            // Verify https://github.com/wixtoolset/issues/issues/3516 - Burn registers dependency on failed packages
            bundleL.Uninstall();
            bundleL.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(true);
            packageC.VerifyInstalled(true);

            bundleE.Uninstall();
            bundleE.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            packageC.VerifyInstalled(false);
        }

        [Fact]
        public void RemovesDependencyDuringUpgradeRollback()
        {
            var testRegistryValueExe = "ExeA";

            var packageA = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleAv1");
            var bundleD = this.CreateBundleInstaller("BundleD");

            packageA.VerifyInstalled(false);
            bundleA.VerifyExeTestRegistryRootDeleted(testRegistryValueExe);

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");

            // Verify https://github.com/wixtoolset/issues/issues/3341 - pkg dependecy not removed in rollback if pkg already present
            bundleD.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);
            bundleD.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(true);
            bundleA.VerifyExeTestRegistryValue(testRegistryValueExe, "1.0.0.0");

            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(false);
            bundleA.VerifyExeTestRegistryRootDeleted(testRegistryValueExe);
        }

        [Fact]
        public void RemovesDependencyProviderFromUpgradedPackageDuringUninstall()
        {
            var packageC = this.CreatePackageInstaller("PackageC");
            var packageFv1 = this.CreatePackageInstaller("PackageFv1");
            var packageFv2 = this.CreatePackageInstaller("PackageFv2");
            var packageGv1 = this.CreatePackageInstaller("PackageGv1");
            var packageGv2 = this.CreatePackageInstaller("PackageGv2");
            var bundleNv1 = this.CreateBundleInstaller("BundleNv1");
            var bundleNv2 = this.CreateBundleInstaller("BundleNv2");

            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalled(false);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(false);

            bundleNv1.Install();
            bundleNv1.VerifyRegisteredAndInPackageCache();

            packageC.VerifyInstalled(false);
            packageFv1.VerifyInstalled(true);
            packageFv2.VerifyInstalled(false);
            packageGv1.VerifyInstalled(true);
            packageGv2.VerifyInstalled(false);

            // Verify https://github.com/wixtoolset/issues/issues/3850 - Dependency provider not removed on uninstall from upgrade
            bundleNv2.Install();
            bundleNv2.VerifyRegisteredAndInPackageCache();
            bundleNv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageC.VerifyInstalled(true);
            packageFv1.VerifyInstalled(false);
            packageFv2.VerifyInstalled(true);
            packageGv1.VerifyInstalled(false);
            packageGv2.VerifyInstalled(true);

            bundleNv1.VerifyPackageIsCached("PackageF", false);
            bundleNv1.VerifyPackageIsCached("PackageG", false);
            bundleNv1.VerifyPackageProviderRemoved("PackageF");
            bundleNv1.VerifyPackageProviderRemoved("PackageG");
        }

        [Fact]
        public void SkipsCrossScopeDependencyRegistration()
        {
            var packageA = this.CreatePackageInstaller("PackageAv1");
            var packageDv1 = this.CreatePackageInstaller("PackageDv1");
            var packageDv2 = this.CreatePackageInstaller("PackageDv2");
            var bundleHv1 = this.CreateBundleInstaller("BundleHv1");
            var bundleHv2 = this.CreateBundleInstaller("BundleHv2");

            packageA.VerifyInstalled(false);
            packageDv1.VerifyInstalled(false);
            packageDv2.VerifyInstalled(false);

            var bundleHv1InstallLogFilePath = bundleHv1.Install();
            bundleHv1.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageDv1.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFileRegex(bundleHv1InstallLogFilePath, @"Skipping cross-scope dependency registration on package: PackageA, bundle scope: PerUser, package scope: PerMachine"));

            var bundleHv2InstallLogFilePath = bundleHv2.Install();
            bundleHv2.VerifyRegisteredAndInPackageCache();
            bundleHv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageA.VerifyInstalled(true);
            packageDv1.VerifyInstalled(false);
            packageDv2.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFileRegex(bundleHv2InstallLogFilePath, @"Skipping cross-scope dependency registration on package: PackageA, bundle scope: PerUser, package scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFileRegex(bundleHv2InstallLogFilePath, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Upgrade, scope: PerUser, version: 1\.0\.0\.0, cached: Yes"));
            Assert.True(LogVerifier.MessageInLogFileRegex(bundleHv2InstallLogFilePath, @"Detected related package: \{[0-9A-Za-z\-]{36}\}, scope: PerUser, version: 1.0.0.0, language: 0 operation: MajorUpgrade"));

            bundleHv2.Uninstall();
            bundleHv2.VerifyUnregisteredAndRemovedFromPackageCache();

            // Verify that permanent packageA is still installed and then remove.
            packageA.VerifyInstalled(true);
            packageDv2.VerifyInstalled(false);
            packageA.UninstallProduct();
            packageA.VerifyInstalled(false);
        }
    }
}
