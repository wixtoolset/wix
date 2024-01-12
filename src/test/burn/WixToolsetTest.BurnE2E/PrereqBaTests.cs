// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class PrereqBaTests : BurnE2ETests
    {
        public PrereqBaTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        const int E_PREREQBA_INFINITE_LOOP = -2_114_714_646;

        /// <summary>
        /// This bundle purposely provides a .runtimeconfig.json file that requires a version of .NET Core that doesn't exist,
        /// with an MSI package to represent the prerequisite package.
        /// This verifies that:
        ///   The preqba doesn't infinitely try to install prereqs.
        ///   The engine automatically uninstalls the bundle since only permanent packages were installed.
        /// </summary>
        [RuntimeFact(Skip = ".NET displays a message box when runtime is not present on the machine which hangs on CI systems. Skip this test until we can get a different behavior from .NET")]
        public void DncAlwaysPreqBaDetectsInfiniteLoop()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageC = this.CreatePackageInstaller("PackageC");

            var bundleC = this.CreateBundleInstaller("BundleC");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");
            packageC.VerifyInstalled(false);

            bundleC.Install(E_PREREQBA_INFINITE_LOOP, "CAUSEINFINITELOOP=1");

            // Part of the test is Install actually completing.

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));
            packageC.VerifyInstalled(false);

            // No non-permanent packages should have ended up installed or cached so it should have unregistered.
            bundleC.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        /// <summary>
        /// This bundle purposely provides a .runtimeconfig.json file that requires a version of .NET Core that doesn't exist,
        /// with an MSI package to represent the prerequisite package.
        /// This verifies that:
        ///   The preqba doesn't infinitely reload itself after failing to load the managed BA.
        ///   The engine automatically uninstalls the bundle since only permanent packages were installed.
        /// </summary>
        [RuntimeFact(Skip = ".NET displays a message box when runtime is not present on the machine which hangs on CI systems. Skip this test until we can get a different behavior from .NET")]
        public void DncPreqBaDetectsInfiniteLoop()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageC = this.CreatePackageInstaller("PackageC");

            var bundleA = this.CreateBundleInstaller("BundleA");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");
            packageC.VerifyInstalled(false);

            bundleA.Install(E_PREREQBA_INFINITE_LOOP, "CAUSEINFINITELOOP=1");

            // Part of the test is Install actually completing.

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));
            packageC.VerifyInstalled(false);

            // No non-permanent packages should have ended up installed or cached so it should have unregistered.
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        /// <summary>
        /// This bundle purposely provides a .runtimeconfig.json file that requires a version of .NET Core that doesn't exist,
        /// with an EXE prereq package to swap it out with a good one.
        /// This verifies that:
        ///   The preqba doesn't infinitely try to install prereqs.
        ///   The managed BA gets loaded after installing prereqs.
        /// </summary>
        [RuntimeFact]
        public void DncAlwaysPreqBaLoadsManagedBaAfterInstallingPrereqs()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageC = this.CreatePackageInstaller("PackageC");

            var bundleC = this.CreateBundleInstaller("BundleC");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");
            packageC.VerifyInstalled(false);

            bundleC.Install();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));
            packageC.VerifyInstalled(true);

            bundleC.VerifyRegisteredAndInPackageCache();

            bundleC.Uninstall();

            bundleC.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        /// <summary>
        /// This bundle purposely provides a .runtimeconfig.json file that requires a version of .NET Core that doesn't exist,
        /// with an EXE prereq package to swap it out with a good one.
        /// This verifies that:
        ///   The preqba doesn't infinitely reload itself after failing to load the managed BA.
        ///   The managed BA gets loaded after installing prereqs.
        /// </summary>
        [RuntimeFact]
        public void DncPreqBaLoadsManagedBaAfterInstallingPrereqs()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageC = this.CreatePackageInstaller("PackageC");

            var bundleA = this.CreateBundleInstaller("BundleA");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");
            packageC.VerifyInstalled(false);

            bundleA.Install();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));
            packageC.VerifyInstalled(true);

            bundleA.VerifyRegisteredAndInPackageCache();

            bundleA.Uninstall();

            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        [RuntimeFact]
        public void DncAlwaysPreqBaForwardsHelpToManagedBa()
        {
            var bundleE = this.CreateBundleInstaller("BundleE");

            var bundleLog = bundleE.Help();

            Assert.True(LogVerifier.MessageInLogFile(bundleLog, "This is a BA for automated testing"));
        }

        /// <summary>
        /// This bundle purposely provides a WixToolset.Mba.Host.config file that requires a version of .NET Framework that doesn't exist,
        /// with an MSI package to represent the prerequisite package.
        /// This verifies that:
        ///   The preqba doesn't infinitely try to install prereqs.
        ///   The engine automatically uninstalls the bundle since only permanent packages were installed.
        /// </summary>
        [RuntimeFact(Skip = ".NET displays a message box when runtime is not present on the machine which hangs on CI systems. Skip this test until we can get a different behavior from .NET")]
        public void MbaAlwaysPreqBaDetectsInfiniteLoop()
        {
            var packageB = this.CreatePackageInstaller("PackageB");
            var packageC = this.CreatePackageInstaller("PackageC");

            var bundleD = this.CreateBundleInstaller("BundleD");

            var packageBSourceCodeInstalled = packageB.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageBSourceCodeInstalled), $"Package B payload should not be there on test start: {packageBSourceCodeInstalled}");
            packageC.VerifyInstalled(false);

            bundleD.Install(E_PREREQBA_INFINITE_LOOP, "CAUSEINFINITELOOP=1");

            // Part of the test is Install actually completing.

            // Source file should be installed
            Assert.True(File.Exists(packageBSourceCodeInstalled), String.Concat("Should have found Package B payload installed at: ", packageBSourceCodeInstalled));
            packageC.VerifyInstalled(false);

            // No non-permanent packages should have ended up installed or cached so it should have unregistered.
            bundleD.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        /// <summary>
        /// This bundle purposely provides a WixToolset.Mba.Host.config file that requires a version of .NET Framework that doesn't exist,
        /// with an MSI package to represent the prerequisite package.
        /// This verifies that:
        ///   The preqba doesn't infinitely reload itself after failing to load the managed BA.
        ///   The engine automatically uninstalls the bundle since only permanent packages were installed.
        /// </summary>
        [RuntimeFact(Skip = ".NET displays a message box when runtime is not present on the machine which hangs on CI systems. Skip this test until we can get a different behavior from .NET")]
        public void MbaPreqBaDetectsInfiniteLoop()
        {
            var packageB = this.CreatePackageInstaller("PackageB");
            var packageC = this.CreatePackageInstaller("PackageC");

            var bundleB = this.CreateBundleInstaller("BundleB");

            var packageBSourceCodeInstalled = packageB.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageBSourceCodeInstalled), $"Package B payload should not be there on test start: {packageBSourceCodeInstalled}");
            packageC.VerifyInstalled(false);

            bundleB.Install(E_PREREQBA_INFINITE_LOOP, "CAUSEINFINITELOOP=1");

            // Part of the test is Install actually completing.

            // Source file should be installed
            Assert.True(File.Exists(packageBSourceCodeInstalled), String.Concat("Should have found Package B payload installed at: ", packageBSourceCodeInstalled));
            packageC.VerifyInstalled(false);

            // No non-permanent packages should have ended up installed or cached so it should have unregistered.
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        /// <summary>
        /// This bundle purposely provides a WixToolset.Mba.Host.config file that requires a version of .NET Framework that doesn't exist,
        /// with an EXE prereq package to swap it out with a good one.
        /// This verifies that:
        ///   The preqba doesn't infinitely try to install prereqs.
        ///   The managed BA gets loaded after installing prereqs.
        /// </summary>
        [RuntimeFact]
        public void MbaAlwaysPreqBaLoadsManagedBaAfterInstallingPrereqs()
        {
            var packageB = this.CreatePackageInstaller("PackageB");
            var packageC = this.CreatePackageInstaller("PackageC");

            var bundleD = this.CreateBundleInstaller("BundleD");

            var packageBSourceCodeInstalled = packageB.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageBSourceCodeInstalled), $"Package B payload should not be there on test start: {packageBSourceCodeInstalled}");
            packageC.VerifyInstalled(false);

            bundleD.Install();

            // Source file should be installed
            Assert.True(File.Exists(packageBSourceCodeInstalled), String.Concat("Should have found Package B payload installed at: ", packageBSourceCodeInstalled));
            packageC.VerifyInstalled(true);

            bundleD.VerifyRegisteredAndInPackageCache();

            bundleD.Uninstall();

            bundleD.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        /// <summary>
        /// This bundle purposely provides a WixToolset.Mba.Host.config file that requires a version of .NET Framework that doesn't exist,
        /// with an EXE prereq package to swap it out with a good one.
        /// This verifies that:
        ///   The preqba doesn't infinitely reload itself after failing to load the managed BA.
        ///   The managed BA gets loaded after installing prereqs.
        /// </summary>
        [RuntimeFact]
        public void MbaPreqBaLoadsManagedBaAfterInstallingPrereqs()
        {
            var packageB = this.CreatePackageInstaller("PackageB");
            var packageC = this.CreatePackageInstaller("PackageC");

            var bundleB = this.CreateBundleInstaller("BundleB");

            var packageBSourceCodeInstalled = packageB.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageBSourceCodeInstalled), $"Package B payload should not be there on test start: {packageBSourceCodeInstalled}");
            packageC.VerifyInstalled(false);

            bundleB.Install();

            // Source file should be installed
            Assert.True(File.Exists(packageBSourceCodeInstalled), String.Concat("Should have found Package B payload installed at: ", packageBSourceCodeInstalled));
            packageC.VerifyInstalled(true);

            bundleB.VerifyRegisteredAndInPackageCache();

            bundleB.Uninstall();

            // No non-permanent packages should have ended up installed or cached so it should have unregistered.
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();
        }
    }
}
