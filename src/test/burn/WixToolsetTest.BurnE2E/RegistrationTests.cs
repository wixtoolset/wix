// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using WixTestTools;
    using WixToolset.BootstrapperApplicationApi;
    using Xunit;
    using Xunit.Abstractions;

    public class RegistrationTests : BurnE2ETests
    {
        public RegistrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void AllowsBAToKeepRegistration()
        {
            const string fakeInstallDay = "20230101";
            this.CreatePackageInstaller("PackageA");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            testBAController.SetPackageRequestedState("PackageA", RequestState.Absent);
            testBAController.SetForceKeepRegistration();

            bundleA.Install();
            var initialRegistration = bundleA.VerifyRegisteredAndInPackageCache();

            var now = DateTime.Now;
            var today = now.ToString("yyyyMMdd");
            var yesterday = now.AddDays(-1).ToString("yyyyMMdd"); // check yesterday in case the bundle install crossed the midnight hour.

            Assert.NotNull(initialRegistration.EstimatedSize);
            Assert.True(initialRegistration.InstallDate == today || initialRegistration.InstallDate == yesterday, $"Installed date should have been {today} or {yesterday}");

            testBAController.SetForceKeepRegistration(null);
            testBAController.ResetPackageStates("PackageA");

            // Pretend the bundle was installed on this day to be able to verify that it is only written on the initial install.
            using (var registrationKey = initialRegistration.BaseKey.OpenSubKey(initialRegistration.KeyPath, writable: true))
            {
                registrationKey.SetValue(GenericArpRegistration.REGISTRY_ARP_INSTALL_DATE, fakeInstallDay);
            }

            bundleA.Install();
            var finalRegistration = bundleA.VerifyRegisteredAndInPackageCache();

            // Verifies https://github.com/wixtoolset/issues/issues/4039
            Assert.NotNull(finalRegistration.EstimatedSize);
            Assert.InRange(finalRegistration.EstimatedSize.Value, initialRegistration.EstimatedSize.Value + 1, Int32.MaxValue);

            Assert.Equal(fakeInstallDay, finalRegistration.InstallDate);
        }

        [RuntimeFact]
        public void AutomaticallyUncachesBundleWhenNotInstalled()
        {
            this.CreatePackageInstaller("PackageA");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            var cachedBundlePath = bundleA.ManuallyCache();

            testBAController.SetQuitAfterDetect();

            bundleA.Install(cachedBundlePath);

            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();
        }

        [RuntimeFact]
        public void AutomaticallyUninstallsBundleWithoutBADoingApply()
        {
            this.InstallBundleThenManuallyUninstallPackageAndRemovePackageFromCacheThenRunAndQuitWithoutApply(true);
        }

        [RuntimeFact]
        public void AutomaticallyUninstallsBundleWithoutBADoingDetect()
        {
            this.InstallBundleThenManuallyUninstallPackageAndRemovePackageFromCacheThenRunAndQuitWithoutApply(false);
        }

        [RuntimeFact]
        public void RegistersInARPIfPrecached()
        {
            this.CreatePackageInstaller("PackageA");
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
