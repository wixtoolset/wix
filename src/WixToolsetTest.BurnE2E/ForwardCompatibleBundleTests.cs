// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class ForwardCompatibleBundleTests : BurnE2ETests
    {
        public ForwardCompatibleBundleTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private const string BundleAProviderId = "~" + nameof(ForwardCompatibleBundleTests) + "_BundleA";
        private const string BundleCProviderId = "~" + nameof(ForwardCompatibleBundleTests) + "_BundleC";
        private const string V100 = "1.0.0.0";
        private const string V200 = "2.0.0.0";

        [Fact]
        public void CanTrack1ForwardCompatibleDependentThroughMajorUpgrade()
        {
            string providerId = BundleAProviderId;
            string parent = "~BundleAv1";
            string parentSwitch = String.Concat("-parent ", parent);

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            // Install the v1 bundle with a parent.
            bundleAv1.Install(arguments: parentSwitch);
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageAv2.VerifyInstalled(false);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out var actualProviderVersion));
            Assert.Equal(V100, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Upgrade with the v2 bundle.
            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Uninstall the v2 bundle and nothing should happen because there is still a parent.
            bundleAv2.Uninstall();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Uninstall the v1 bundle with passthrough and all should be removed.
            bundleAv1.Uninstall(arguments: parentSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);
            Assert.False(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
        }

        [Fact]
        public void CanTrack1ForwardCompatibleDependentThroughMajorUpgradeWithParentNone()
        {
            string providerId = BundleAProviderId;
            string parent = "~BundleAv1";
            string parentSwitch = String.Concat("-parent ", parent);

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            // Install the v1 bundle with a parent.
            bundleAv1.Install(arguments: parentSwitch);
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageAv2.VerifyInstalled(false);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out var actualProviderVersion));
            Assert.Equal(V100, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Upgrade with the v2 bundle but prevent self parent being registered.
            bundleAv2.Install(arguments: "-parent:none");
            bundleAv2.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Uninstall the v1 bundle with passthrough and all should be removed.
            bundleAv1.Uninstall(arguments: parentSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);
            Assert.False(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
        }

        [Fact]
        public void CanTrack2ForwardCompatibleDependentsThroughMajorUpgrade()
        {
            string providerId = BundleAProviderId;
            string parent = "~BundleAv1";
            string parent2 = "~BundleAv1_Parent2";
            string parentSwitch = String.Concat("-parent ", parent);
            string parent2Switch = String.Concat("-parent ", parent2);

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            // Install the v1 bundle with a parent.
            bundleAv1.Install(arguments: parentSwitch);
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageAv2.VerifyInstalled(false);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out var actualProviderVersion));
            Assert.Equal(V100, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Install the v1 bundle with a second parent.
            bundleAv1.Install(arguments: parent2Switch);
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageAv2.VerifyInstalled(false);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V100, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Upgrade with the v2 bundle.
            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Uninstall the v2 bundle and nothing should happen because there is still a parent.
            bundleAv2.Uninstall();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Uninstall one parent of the v1 bundle and nothing should happen because there is still a parent.
            bundleAv1.Uninstall(arguments: parentSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.False(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Uninstall the v1 bundle with passthrough with second parent and all should be removed.
            bundleAv1.Uninstall(arguments: parent2Switch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);
            Assert.False(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
        }

        [Fact]
        public void CanTrack2ForwardCompatibleDependentsThroughMajorUpgradePerUser()
        {
            string providerId = BundleCProviderId;
            string parent = "~BundleCv1";
            string parent2 = "~BundleCv1_Parent2";
            string parentSwitch = String.Concat("-parent ", parent);
            string parent2Switch = String.Concat("-parent ", parent2);

            var packageCv1 = this.CreatePackageInstaller("PackageCv1");
            var packageCv2 = this.CreatePackageInstaller("PackageCv2");
            var bundleCv1 = this.CreateBundleInstaller("BundleCv1");
            var bundleCv2 = this.CreateBundleInstaller("BundleCv2");

            packageCv1.VerifyInstalled(false);
            packageCv2.VerifyInstalled(false);

            // Install the v1 bundle with a parent.
            bundleCv1.Install(arguments: parentSwitch);
            bundleCv1.VerifyRegisteredAndInPackageCache();

            packageCv1.VerifyInstalled(true);
            packageCv2.VerifyInstalled(false);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out var actualProviderVersion));
            Assert.Equal(V100, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Install the v1 bundle with a second parent.
            bundleCv1.Install(arguments: parent2Switch);
            bundleCv1.VerifyRegisteredAndInPackageCache();

            packageCv1.VerifyInstalled(true);
            packageCv2.VerifyInstalled(false);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V100, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Upgrade with the v2 bundle.
            bundleCv2.Install();
            bundleCv2.VerifyRegisteredAndInPackageCache();
            bundleCv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageCv1.VerifyInstalled(false);
            packageCv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Uninstall the v2 bundle and nothing should happen because there is still a parent.
            bundleCv2.Uninstall();
            bundleCv2.VerifyRegisteredAndInPackageCache();

            packageCv1.VerifyInstalled(false);
            packageCv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Uninstall one parent of the v1 bundle and nothing should happen because there is still a parent.
            bundleCv1.Uninstall(arguments: parentSwitch);
            bundleCv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleCv2.VerifyRegisteredAndInPackageCache();

            packageCv1.VerifyInstalled(false);
            packageCv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.False(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Uninstall the v1 bundle with passthrough with second parent and all should be removed.
            bundleCv1.Uninstall(arguments: parent2Switch);
            bundleCv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleCv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageCv1.VerifyInstalled(false);
            packageCv2.VerifyInstalled(false);
            Assert.False(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
        }

        [Fact]
        public void CanTrack2ForwardCompatibleDependentsThroughMajorUpgradeWithParent()
        {
            string providerId = BundleAProviderId;
            string parent = "~BundleAv1";
            string parent2 = "~BundleAv1_Parent2";
            string parent3 = "~BundleAv1_Parent3";
            string parentSwitch = String.Concat("-parent ", parent);
            string parent2Switch = String.Concat("-parent ", parent2);
            string parent3Switch = String.Concat("-parent ", parent3);

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            // Install the v1 bundle with a parent.
            bundleAv1.Install(arguments: parentSwitch);
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageAv2.VerifyInstalled(false);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out var actualProviderVersion));
            Assert.Equal(V100, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Install the v1 bundle with a second parent.
            bundleAv1.Install(arguments: parent2Switch);
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageAv2.VerifyInstalled(false);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V100, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Upgrade with the v2 bundle.
            bundleAv2.Install(arguments: parent3Switch);
            bundleAv2.VerifyRegisteredAndInPackageCache();
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent3));

            // Uninstall the v2 bundle and nothing should happen because there is still a parent.
            bundleAv2.Uninstall(arguments: parent3Switch);
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));
            Assert.False(BundleRegistration.DependencyDependentExists(providerId, parent3));

            // Uninstall one parent of the v1 bundle and nothing should happen because there is still a parent.
            bundleAv1.Uninstall(arguments: parentSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);
            Assert.False(BundleRegistration.DependencyDependentExists(providerId, parent));
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent2));

            // Uninstall the v1 bundle with passthrough with second parent and all should be removed.
            bundleAv1.Uninstall(arguments: parent2Switch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);
            Assert.False(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
        }

        [Fact]
        public void CanUninstallForwardCompatibleWithBundlesUninstalledInFifoOrder()
        {
            string providerId = BundleAProviderId;
            string parent = "~BundleAv1";
            string parentSwitch = String.Concat("-parent ", parent);

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out var actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);

            // Install the v1 bundle with a parent which should passthrough to v2.
            bundleAv1.Install(arguments: parentSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            bundleAv2.Uninstall();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Uninstall the v1 bundle with passthrough and all should be removed.
            bundleAv1.Uninstall(arguments: parentSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);
            Assert.False(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
        }

        [Fact]
        public void CanUninstallForwardCompatibleWithBundlesUninstalledInReverseOrder()
        {
            string providerId = BundleAProviderId;
            string parent = "~BundleAv1";
            string parentSwitch = String.Concat("-parent ", parent);

            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out var actualProviderVersion));
            Assert.Equal(V200, actualProviderVersion);

            // Install the v1 bundle with a parent which should passthrough to v2.
            bundleAv1.Install(arguments: parentSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.True(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Uninstall the v1 bundle with the same parent which should passthrough to v2 and remove parent.
            bundleAv1.Uninstall(arguments: parentSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);
            Assert.False(BundleRegistration.DependencyDependentExists(providerId, parent));

            // Uninstall the v2 bundle and all should be removed.
            bundleAv2.Uninstall();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);
            Assert.False(BundleRegistration.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
        }
    }
}
