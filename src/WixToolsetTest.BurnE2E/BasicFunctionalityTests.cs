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
        public void CanInstallAndUninstallSimpleBundle()
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
    }
}
