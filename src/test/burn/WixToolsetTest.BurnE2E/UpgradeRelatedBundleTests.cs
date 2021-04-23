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
            Assert.True(LogVerifier.MessageInLogFileRegex(bundleAv2InstallLogFilePath, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Upgrade, scope: PerMachine, version: 1\.0\.0\.0, operation: MajorUpgrade, cached: No"));
        }
    }
}
