// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using Xunit;
    using Xunit.Abstractions;

    public class BasicFunctionalityTests : BurnE2ETests
    {
        public BasicFunctionalityTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x86_wixstdba()
        {
            var packageA = this.CreatePackageInstaller("PackageA");

            var bundleA = this.CreateBundleInstaller("BundleA");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");

            bundleA.Install();

            var cachedBundlePath = bundleA.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));

            bundleA.Uninstall(cachedBundlePath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageASourceCodeInstalled));

            bundleA.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x86_testba()
        {
            var packageA = this.CreatePackageInstaller("PackageA");

            var bundleB = this.CreateBundleInstaller("BundleB");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");

            bundleB.Install();

            var cachedBundlePath = bundleB.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));

            bundleB.Uninstall(cachedBundlePath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageASourceCodeInstalled));

            bundleB.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x86_dnctestba()
        {
            var packageA = this.CreatePackageInstaller("PackageA");

            var bundleC = this.CreateBundleInstaller("BundleC");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");

            bundleC.Install();

            var cachedBundlePath = bundleC.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));

            bundleC.Uninstall(cachedBundlePath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageASourceCodeInstalled));

            bundleC.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x64_wixstdba()
        {
            var packageA_x64 = this.CreatePackageInstaller("PackageA_x64");

            var bundleA_x64 = this.CreateBundleInstaller("BundleA_x64");

            var packageASourceCodeInstalled = packageA_x64.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A x64 payload should not be there on test start: {packageASourceCodeInstalled}");

            bundleA_x64.Install();

            var cachedBundlePath = bundleA_x64.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A x64 payload installed at: ", packageASourceCodeInstalled));

            bundleA_x64.Uninstall(cachedBundlePath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A x64 payload should have been removed by uninstall from: ", packageASourceCodeInstalled));

            bundleA_x64.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x64_testba()
        {
            var packageA_x64 = this.CreatePackageInstaller("PackageA_x64");

            var bundleB_x64 = this.CreateBundleInstaller("BundleB_x64");

            var packageASourceCodeInstalled = packageA_x64.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A x64 payload should not be there on test start: {packageASourceCodeInstalled}");

            bundleB_x64.Install();

            var cachedBundlePath = bundleB_x64.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A x64 payload installed at: ", packageASourceCodeInstalled));

            bundleB_x64.Uninstall(cachedBundlePath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A x64 payload should have been removed by uninstall from: ", packageASourceCodeInstalled));

            bundleB_x64.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x64_dnctestba()
        {
            var packageA_x64 = this.CreatePackageInstaller("PackageA_x64");

            var bundleC_x64 = this.CreateBundleInstaller("BundleC_x64");

            var packageASourceCodeInstalled = packageA_x64.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A x64 payload should not be there on test start: {packageASourceCodeInstalled}");

            bundleC_x64.Install();

            var cachedBundlePath = bundleC_x64.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A x64 payload installed at: ", packageASourceCodeInstalled));

            bundleC_x64.Uninstall(cachedBundlePath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A x64 payload should have been removed by uninstall from: ", packageASourceCodeInstalled));

            bundleC_x64.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }
    }
}
