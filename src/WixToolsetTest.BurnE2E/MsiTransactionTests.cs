// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class MsiTransactionTests : BurnE2ETests
    {
        public MsiTransactionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanUpgradeBundleWithMsiTransaction()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageBv1 = this.CreatePackageInstaller("PackageBv1");
            var packageBv2 = this.CreatePackageInstaller("PackageBv2");
            var packageCv1 = this.CreatePackageInstaller("PackageCv1");
            var packageCv2 = this.CreatePackageInstaller("PackageCv2");
            var packageD = this.CreatePackageInstaller("PackageD");

            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");
            var packageBv1SourceCodeInstalled = packageBv1.GetInstalledFilePath("Package.wxs");
            var packageBv2SourceCodeInstalled = packageBv2.GetInstalledFilePath("Package.wxs");
            var packageCv1SourceCodeInstalled = packageCv1.GetInstalledFilePath("Package.wxs");
            var packageCv2SourceCodeInstalled = packageCv2.GetInstalledFilePath("Package.wxs");
            var packageDSourceCodeInstalled = packageD.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), $"Package Bv1 payload should not be there on test start: {packageBv1SourceCodeInstalled}");
            Assert.False(File.Exists(packageBv2SourceCodeInstalled), $"Package Bv2 payload should not be there on test start: {packageBv2SourceCodeInstalled}");
            Assert.False(File.Exists(packageCv1SourceCodeInstalled), $"Package Cv1 payload should not be there on test start: {packageCv1SourceCodeInstalled}");
            Assert.False(File.Exists(packageCv2SourceCodeInstalled), $"Package Cv2 payload should not be there on test start: {packageCv2SourceCodeInstalled}");
            Assert.False(File.Exists(packageDSourceCodeInstalled), $"Package D payload should not be there on test start: {packageDSourceCodeInstalled}");

            bundleAv1.Install();

            var bundleAv1CachedPath = bundleAv1.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));
            Assert.True(File.Exists(packageBv1SourceCodeInstalled), String.Concat("Should have found Package Bv1 payload installed at: ", packageBv1SourceCodeInstalled));
            Assert.True(File.Exists(packageCv1SourceCodeInstalled), String.Concat("Should have found Package Cv1 payload installed at: ", packageCv1SourceCodeInstalled));

            bundleAv2.Install();

            var bundleAv2CachedPath = bundleAv2.VerifyRegisteredAndInPackageCache();

            // Source file should be upgraded
            Assert.True(File.Exists(packageDSourceCodeInstalled), String.Concat("Should have found Package D payload installed at: ", packageDSourceCodeInstalled));
            Assert.True(File.Exists(packageBv2SourceCodeInstalled), String.Concat("Should have found Package Bv2 payload installed at: ", packageBv2SourceCodeInstalled));
            Assert.True(File.Exists(packageCv2SourceCodeInstalled), String.Concat("Should have found Package Cv2 payload installed at: ", packageCv2SourceCodeInstalled));
            Assert.False(File.Exists(packageCv1SourceCodeInstalled), String.Concat("Package Cv1 payload should have been removed by upgrade uninstall from: ", packageCv1SourceCodeInstalled));
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), String.Concat("Package Bv1 payload should have been removed by upgrade uninstall from: ", packageBv1SourceCodeInstalled));
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A payload should have been removed by upgrade uninstall from: ", packageASourceCodeInstalled));

            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache(bundleAv1CachedPath);

            // Uninstall everything.
            bundleAv2.Uninstall();

            // Source file should *not* be installed
            Assert.False(File.Exists(packageDSourceCodeInstalled), String.Concat("Package D payload should have been removed by uninstall from: ", packageDSourceCodeInstalled));
            Assert.False(File.Exists(packageBv2SourceCodeInstalled), String.Concat("Package Bv2 payload should have been removed by uninstall from: ", packageBv2SourceCodeInstalled));
            Assert.False(File.Exists(packageCv2SourceCodeInstalled), String.Concat("Package Cv2 payload should have been removed by uninstall from: ", packageCv2SourceCodeInstalled));

            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache(bundleAv2CachedPath);
        }

        /// <summary>
        /// Installs 2 bundles:
        ///   BundleBv1- installs package Bv1
        ///   BundleBv2- installs packages A, Bv2, F
        ///     package Bv2 performs a major upgrade of package Bv1
        ///     package F fails
        ///     Thus, rolling back the transaction should reinstall package Bv1
        /// </summary>
        [Fact]
        public void CanRelyOnMsiTransactionRollback()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageBv1 = this.CreatePackageInstaller("PackageBv1");
            var packageBv2 = this.CreatePackageInstaller("PackageBv2");
            this.CreatePackageInstaller("PackageF");

            var bundleBv1 = this.CreateBundleInstaller("BundleBv1");
            var bundleBv2 = this.CreateBundleInstaller("BundleBv2");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");
            var packageBv1SourceCodeInstalled = packageBv1.GetInstalledFilePath("Package.wxs");
            var packageBv2SourceCodeInstalled = packageBv2.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");
            Assert.False(File.Exists(packageBv1SourceCodeInstalled), $"Package Bv1 payload should not be there on test start: {packageBv1SourceCodeInstalled}");
            Assert.False(File.Exists(packageBv2SourceCodeInstalled), $"Package Bv2 payload should not be there on test start: {packageBv2SourceCodeInstalled}");

            bundleBv1.Install();

            bundleBv1.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageBv1SourceCodeInstalled), String.Concat("Should have found Package Bv1 payload installed at: ", packageBv1SourceCodeInstalled));

            bundleBv2.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Bundle v2 should be registered since it installed a non-permanent package.
            bundleBv2.VerifyRegisteredAndInPackageCache();

            // Bundle v1 should not have been removed since the install of v2 failed in the middle of the chain.
            bundleBv1.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));

            // Previous source file should be installed
            Assert.True(File.Exists(packageBv1SourceCodeInstalled), String.Concat("Should have found Package Bv1 payload installed at: ", packageBv1SourceCodeInstalled));
            Assert.False(File.Exists(packageBv2SourceCodeInstalled), String.Concat("Should not have found Package Bv2 payload installed at: ", packageBv2SourceCodeInstalled));
        }
    }
}
