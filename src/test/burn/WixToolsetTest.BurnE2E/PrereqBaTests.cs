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

        /// <summary>
        /// This bundle purposely misnames its .runtimeconfig.json file to force the PreqBA to kick in
        /// and use its EXE prereq package to swap in a good one.
        /// This verifies that:
        ///   The mangaged BA fails to load due to the missing file.
        ///   The preqba kicks in to copy in the file.
        ///   The managed BA gets loaded.
        /// </summary>
        [RuntimeFact]
        public void DncLoadsOnlyAfterInstallingPrereqs()
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

        /// <summary>
        /// This bundle purposely misnames its .runtimeconfig.json file to force the PreqBA to kick in
        /// and use its EXE prereq package to swap in a good one.
        /// This verifies that:
        ///   The preqba runs first to fix the file.
        ///   The managed BA gets loaded on first try.
        /// </summary>
        [RuntimeFact]
        public void DncPreqsFirstThenBaLoadsManagedBa()
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

        /// This bundle purposely misnames its WixToolset.Mba.Host.config file to force the PreqBA to kick in
        /// and use its EXE prereq package to swap in a good one.
        /// This verifies that:
        ///   The mangaged BA fails to load due to the missing file.
        ///   The preqba kicks in to copy in the file.
        ///   The managed BA gets loaded.
        /// </summary>
        [RuntimeFact(Skip = "It is no longer possible to replace the bad.config with a good config, these tests do not work for .NET Framework the way they can for .NET above.")]
        public void MbaLoadsOnlyAfterInstallingPrereqs()
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

        /// This bundle purposely misnames its WixToolset.Mba.Host.config file to force the PreqBA to kick in
        /// and use its EXE prereq package to swap in a good one.
        /// This verifies that:
        ///   The preqba runs first to fix the file.
        ///   The managed BA gets loaded on first try.
        /// </summary>
        [RuntimeFact(Skip = "It is no longer possible to replace the bad.config with a good config, these tests do not work for .NET Framework the way they can for .NET above.")]
        public void MbaPreqsFirstThenBaLoadsManagedBa()
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

        [RuntimeFact]
        public void DncAlwaysPreqBaForwardsHelpToManagedBa()
        {
            var bundleE = this.CreateBundleInstaller("BundleE");

            var bundleLog = bundleE.Help();

            Assert.True(LogVerifier.MessageInLogFile(bundleLog, "This is a BA for automated testing"));
        }
    }
}
