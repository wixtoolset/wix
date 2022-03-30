// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System.IO;
    using Xunit;
    using Xunit.Abstractions;

    public class BundlePackageTests : BurnE2ETests
    {
        public BundlePackageTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
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

            bundleA.VerifyRegisteredAndInPackageCache();
            bundleB_x64.VerifyRegisteredAndInPackageCache();

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
    }
}
