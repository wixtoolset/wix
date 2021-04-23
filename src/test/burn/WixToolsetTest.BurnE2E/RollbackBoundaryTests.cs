// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using Xunit;
    using Xunit.Abstractions;

    public class RollbackBoundaryTests : BurnE2ETests
    {
        public RollbackBoundaryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// Installs 1 bundle:
        ///   chain - non-vital rollback boundary, package F, package A, vital rollback boundary, package B
        ///     package F fails
        ///     package A and B are permanent
        ///   Execution is supposed to be:
        ///     package F (fails)
        ///     rollback to non-vital rollback boundary which ignores the error and skips over package A
        ///     install package B
        ///     unregister since no non-permanent packages should be installed or cached.
        /// </summary>
        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6309")]
        public void NonVitalRollbackBoundarySkipsToNextRollbackBoundary()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            this.CreatePackageInstaller("PackageC");
            this.CreatePackageInstaller("PackageF");

            var bundleA = this.CreateBundleInstaller("BundleA");

            var packageASourceCodeInstalled = packageA.GetInstalledFilePath("Package.wxs");
            var packageBSourceCodeInstalled = packageB.GetInstalledFilePath("Package.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), $"Package A payload should not be there on test start: {packageASourceCodeInstalled}");
            Assert.False(File.Exists(packageBSourceCodeInstalled), $"Package B payload should not be there on test start: {packageBSourceCodeInstalled}");

            bundleA.Install();

            // No non-permanent packages should have ended up installed or cached so it should have unregistered.
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            // Only PackageB source file should be installed
            Assert.True(File.Exists(packageBSourceCodeInstalled), String.Concat("Should have found Package B payload installed at: ", packageBSourceCodeInstalled));
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Should not have found Package A payload installed at: ", packageASourceCodeInstalled));
        }
    }
}
