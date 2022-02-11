// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class UpgradeRelatedBundleTests : BurnE2ETests
    {
        public UpgradeRelatedBundleTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void ReinstallsOlderBundleAfterFailure()
        {
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var packageAv3 = this.CreatePackageInstaller("PackageAv3");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");
            var bundleAv3 = this.CreateBundleInstaller("BundleAv3");

            packageAv2.VerifyInstalled(false);
            packageAv3.VerifyInstalled(false);

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv2.VerifyInstalled(true);
            packageAv3.VerifyInstalled(false);

            // Verify https://github.com/wixtoolset/issues/issues/3421
            var bundleAv3InstallLogFilePath = bundleAv3.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);
            bundleAv3.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(LogVerifier.MessageInLogFileRegex(bundleAv3InstallLogFilePath, @"Applied execute package: PackageA, result: 0x0, restart: None"));

            packageAv2.VerifyInstalled(true);
            packageAv3.VerifyInstalled(false);
        }

        [Fact]
        public void ReportsRelatedBundleMissingFromCache()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            bundleAv1.ManuallyUncache();

            // Verify https://github.com/wixtoolset/issues/issues/4991
            var bundleAv2InstallLogFilePath = bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            Assert.True(LogVerifier.MessageInLogFileRegex(bundleAv2InstallLogFilePath, @"OnDetectRelatedBundle\(\) - id: \{[0-9A-Za-z\-]{36}\}, missing from cache: True"));
            Assert.True(LogVerifier.MessageInLogFileRegex(bundleAv2InstallLogFilePath, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Upgrade, scope: PerMachine, version: 1\.0\.0\.0, cached: No"));
        }

        [Fact]
        public void Bundle64UpgradesBundle32()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2x64 = this.CreateBundleInstaller("BundleAv2x64");

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            var bundleAv2x64InstallLogFilePath = bundleAv2x64.Install();
            bundleAv2x64.VerifyRegisteredAndInPackageCache();

            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            
            Assert.True(LogVerifier.MessageInLogFileRegex(bundleAv2x64InstallLogFilePath, @"Detected related package: \{[0-9A-Za-z\-]{36}\}, scope: PerMachine, version: 1.0.0.0, language: 1033 operation: MajorUpgrade"));
        }

        [Fact]
        public void Bundle32UpgradesBundle64()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1x64 = this.CreateBundleInstaller("BundleAv1x64");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            bundleAv1x64.Install();
            bundleAv1x64.VerifyRegisteredAndInPackageCache();

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();
             
            bundleAv1x64.VerifyUnregisteredAndRemovedFromPackageCache();
        }
    }
}
