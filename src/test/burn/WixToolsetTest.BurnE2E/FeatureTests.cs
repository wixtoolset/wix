// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using WixTestTools;
    using WixToolset.BootstrapperApplicationApi;
    using Xunit;
    using Xunit.Abstractions;

    public class FeatureTests : BurnE2ETests
    {
        public FeatureTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanControlFeatureSelectionDuringInstallAndModify()
        {
            var packageA = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleAv1");
            var testBAController = this.CreateTestBAController();

            // Install the bundle without the optional feature present
            testBAController.SetPackageFeatureState("PackageA", "Complete", FeatureState.Local);
            testBAController.SetPackageFeatureState("PackageA", "Test", FeatureState.Absent);
            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            string packageSourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Should not have found Package A payload installed at: ", packageSourceCodeInstalled));
            packageA.VerifyTestRegistryValue("PackageA", "1.0.0.0");

            // Now turn on the feature.
            testBAController.SetPackageFeatureState("PackageA", "Test", FeatureState.Local);

            bundleA.Modify();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));

            // Turn the feature back off.
            testBAController.SetPackageFeatureState("PackageA", "Test", FeatureState.Absent);
            bundleA.Modify();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Should have removed Package A payload from: ", packageSourceCodeInstalled));

            // Uninstall everything.
            testBAController.ResetPackageStates("PackageA");
            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            packageA.VerifyTestRegistryRootDeleted();
        }

        [RuntimeFact]
        public void CanControlFeatureSelectionDuringRepair()
        {
            var packageA = this.CreatePackageInstaller("PackageAv1");
            var bundleA = this.CreateBundleInstaller("BundleAv1");
            var testBAController = this.CreateTestBAController();

            // Install the bundle with the optional feature present
            testBAController.SetPackageFeatureState("PackageA", "Complete", FeatureState.Local);
            testBAController.SetPackageFeatureState("PackageA", "Test", FeatureState.Local);
            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();

            string packageSourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");
            string packageNotKeyPathFile = packageA.GetInstalledFilePath("notkeypath.file");

            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A keyfile installed at: ", packageSourceCodeInstalled));
            Assert.True(File.Exists(packageNotKeyPathFile), String.Concat("Should have found Package A non-keyfile installed at: ", packageNotKeyPathFile));

            // Delete the non-keypath source file.
            File.Delete(packageNotKeyPathFile);

            // Now repair without repairing the feature to verify the non-keyfile doesn't come back.
            testBAController.SetPackageFeatureState("PackageA", "Test", FeatureState.Unknown);
            bundleA.Repair();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A keyfile installed at: ", packageSourceCodeInstalled));
            Assert.False(File.Exists(packageNotKeyPathFile), String.Concat("Should have not found Package A non-keyfile installed at: ", packageNotKeyPathFile));

            // Now repair and include the feature this time.
            testBAController.SetPackageFeatureState("PackageA", "Test", FeatureState.Local);
            bundleA.Repair();
            bundleA.VerifyRegisteredAndInPackageCache();
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A keyfile installed at: ", packageSourceCodeInstalled));
            Assert.True(File.Exists(packageNotKeyPathFile), String.Concat("Should have repaired Package A non-keyfile installed at: ", packageNotKeyPathFile));

            // Uninstall everything.
            testBAController.ResetPackageStates("PackageA");
            bundleA.Uninstall();
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            packageA.VerifyTestRegistryRootDeleted();
        }

        [RuntimeFact]
        public void CanControlFeatureSelectionDuringMinorUpgrade()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv1_0_1 = this.CreatePackageInstaller("PackageAv1_0_1");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv1_0_1 = this.CreateBundleInstaller("BundleAv1_0_1");
            var testBAController = this.CreateTestBAController();

            // Install v1 with the optional feature turned on.
            testBAController.SetPackageFeatureState("PackageA", "Complete", FeatureState.Local);
            testBAController.SetPackageFeatureState("PackageA", "Test", FeatureState.Local);
            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalledWithVersion(true);
            packageAv1.VerifyTestRegistryValue("PackageA", "1.0.0.0");

            // Install v1.0.1 with the optional feature still turned on.
            bundleAv1_0_1.Install();
            bundleAv1_0_1.VerifyRegisteredAndInPackageCache();

            packageAv1_0_1.VerifyInstalledWithVersion(true);
            packageAv1_0_1.VerifyTestRegistryValue("PackageA", "1.0.1.0");
        }
    }
}
