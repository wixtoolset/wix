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
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleA");
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x86_testba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleB");
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x86_dnctestba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleC");
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x86_wixba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleD");
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x64_wixstdba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleA_x64");
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x64_testba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleB_x64");
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x64_dnctestba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleC_x64");
        }

        [Fact]
        public void CanInstallAndUninstallSimpleBundle_x64_dncwixba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleD_x64");
        }

        private void CanInstallAndUninstallSimpleBundle(string packageName, string bundleName)
        {
            var package = this.CreatePackageInstaller(packageName);

            var bundle = this.CreateBundleInstaller(bundleName);

            var packageSourceCodeInstalled = package.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageSourceCodeInstalled), $"{packageName} payload should not be there on test start: {packageSourceCodeInstalled}");

            bundle.Install();

            var cachedBundlePath = bundle.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageSourceCodeInstalled), $"Should have found {packageName} payload installed at: {packageSourceCodeInstalled}");

            bundle.Uninstall(cachedBundlePath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageSourceCodeInstalled), $"{packageName} payload should have been removed by uninstall from: {packageSourceCodeInstalled}");

            bundle.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }
    }
}
