// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using Xunit;
    using Xunit.Abstractions;

    public class PrereqBaTests : BurnE2ETests
    {
        public PrereqBaTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// This bundle purposely provides a .runtimeconfig.json file that requires a version of .NET Core that doesn't exist,
        /// with an MSI package to represent the prerequisite package.
        /// This verifies that:
        ///   The preqba doesn't infinitely reload itself after failing to load the managed BA.
        ///   The engine automatically uninstalls the bundle since only permanent packages were installed.
        /// </summary>
        [Fact]
        public void DncPreqBaDetectsInfiniteLoop()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            this.CreatePackageInstaller("PackageF");

            var bundleA = this.CreateBundleInstaller("BundleA");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");

            bundleA.Install();

            // Part of the test is Install actually completing.

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));

            // No non-permanent packages should have ended up installed or cached so it should have unregistered.
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        /// <summary>
        /// This bundle purposely provides a WixToolset.Mba.Host.config file that requires a version of .NET Framework that doesn't exist,
        /// with an MSI package to represent the prerequisite package.
        /// This verifies that:
        ///   The preqba doesn't infinitely reload itself after failing to load the managed BA.
        ///   The engine automatically uninstalls the bundle since only permanent packages were installed.
        /// </summary>
        [Fact]
        public void MbaPreqBaDetectsInfiniteLoop()
        {
            var packageB = this.CreatePackageInstaller("PackageB");
            this.CreatePackageInstaller("PackageF");

            var bundleB = this.CreateBundleInstaller("BundleB");

            var packageBSourceCodeInstalled = packageB.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageBSourceCodeInstalled), $"Package B payload should not be there on test start: {packageBSourceCodeInstalled}");

            bundleB.Install();

            // Part of the test is Install actually completing.

            // Source file should be installed
            Assert.True(File.Exists(packageBSourceCodeInstalled), String.Concat("Should have found Package B payload installed at: ", packageBSourceCodeInstalled));

            // No non-permanent packages should have ended up installed or cached so it should have unregistered.
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();
        }
    }
}
