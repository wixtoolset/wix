// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class OptionalUpdateRegistrationTests : BurnE2ETests
    {
        public OptionalUpdateRegistrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void BundleUpdateRegistrationIsStickyAndAccurateAcrossUpgrades()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();
            var gotV1Registration = bundleAv1.TryGetUpdateRegistration(plannedPerMachine: null, out var v1Registration);

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();
            var gotV2Registration = bundleAv2.TryGetUpdateRegistration(plannedPerMachine: null, out var v2Registration);

            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(gotV1Registration, "Missing update registration after v1 install.");
            Assert.True(gotV2Registration, "Missing update registration after v2 upgrade.");

            Assert.Equal("Acme", v1Registration.Publisher);
            Assert.Equal("Acme", v2Registration.Publisher);
            Assert.Equal("Setup Geeks", v1Registration.PublishingGroup);
            Assert.Equal("Setup Geeks", v2Registration.PublishingGroup);
            Assert.Equal("~OptionalUpdateRegistrationTests", v1Registration.PackageName);
            Assert.Equal("~OptionalUpdateRegistrationTests", v2Registration.PackageName);
            Assert.Equal("1.0.0.0", v1Registration.PackageVersion);
            Assert.Equal("2.0.0.0", v2Registration.PackageVersion);
        }
    }
}
