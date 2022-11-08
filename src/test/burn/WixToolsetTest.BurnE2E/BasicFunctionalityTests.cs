// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using WixInternal.TestSupport;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class BasicFunctionalityTests : BurnE2ETests
    {
        public BasicFunctionalityTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x86_wixstdba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleA");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x86_testba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleB");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x86_dnctestba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleC");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x86_wixba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleD");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x64_wixstdba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleA_x64");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimplePerUserBundle_x64_wixstdba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageApu_x64", "BundleApu_x64", "PackagePerUser.wxs", unchecked((int)0xc0000005));
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x64_testba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleB_x64");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x64_dnctestba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleC_x64");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x64_dncwixba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleD_x64");
        }

        private void CanInstallAndUninstallSimpleBundle(string packageName, string bundleName, string fileName = "Package.wxs", int? alternateExitCode = null)
        {
            var package = this.CreatePackageInstaller(packageName);

            var bundle = this.CreateBundleInstaller(bundleName);
            bundle.AlternateExitCode = alternateExitCode;

            var packageSourceCodeInstalled = package.GetInstalledFilePath(fileName);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageSourceCodeInstalled), $"{packageName} payload should not be there on test start: {packageSourceCodeInstalled}");

            bundle.Install();

            var registration = bundle.VerifyRegisteredAndInPackageCache();
            var cachedBundlePath = registration.CachePath;

            // Source file should be installed
            Assert.True(File.Exists(packageSourceCodeInstalled), $"Should have found {packageName} payload installed at: {packageSourceCodeInstalled}");

            if (alternateExitCode == bundle.LastExitCode)
            {
                WixAssert.Skip($"Install exited with {bundle.LastExitCode}");
            }

            bundle.Uninstall(cachedBundlePath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageSourceCodeInstalled), $"{packageName} payload should have been removed by uninstall from: {packageSourceCodeInstalled}");

            bundle.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);

            if (alternateExitCode == bundle.LastExitCode)
            {
                WixAssert.Skip($"Uninstall exited with {bundle.LastExitCode}");
            }
        }
    }
}
