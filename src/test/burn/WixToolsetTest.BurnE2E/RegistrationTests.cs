// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using Xunit;
    using Xunit.Abstractions;

    public class RegistrationTests : BurnE2ETests
    {
        public RegistrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void AutomaticallyUncachesBundleWhenNotInstalled()
        {
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            var cachedBundlePath = bundleA.ManuallyCache();

            testBAController.SetQuitAfterDetect();

            bundleA.Install(cachedBundlePath);

            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        [Fact]
        public void AutomaticallyUninstallsBundleWithoutBADoingApply()
        {
            this.InstallBundleThenManuallyUninstallPackageAndRemovePackageFromCacheThenRunAndQuitWithoutApply(true);
        }

        [Fact]
        public void AutomaticallyUninstallsBundleWithoutBADoingDetect()
        {
            this.InstallBundleThenManuallyUninstallPackageAndRemovePackageFromCacheThenRunAndQuitWithoutApply(false);
        }

        [Fact]
        public void RegistersInARPIfPrecached()
        {
            var bundleA = this.CreateBundleInstaller("BundleA");

            bundleA.ManuallyCache();

            // Verifies https://github.com/wixtoolset/issues/issues/5702
            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
        }

        private void InstallBundleThenManuallyUninstallPackageAndRemovePackageFromCacheThenRunAndQuitWithoutApply(bool detect)
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
            packageA.VerifyInstalled(true);

            packageA.UninstallProduct();
            bundleA.RemovePackageFromCache("PackageA");

            if (detect)
            {
                testBAController.SetQuitAfterDetect();
            }
            else
            {
                testBAController.SetImmediatelyQuit();
            }
            bundleA.Install();
            packageA.VerifyInstalled(false);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
        }
    }
}
