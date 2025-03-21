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
            var gotV1Registration = bundleAv1.TryGetUpdateRegistration(out var v1Registration);

            bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();
            var gotV2Registration = bundleAv2.TryGetUpdateRegistration(out var v2Registration);

            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(gotV1Registration, "Missing v1 update registration.");
            Assert.True(gotV2Registration, "Missing v2 update registration.");

            Assert.Equal(v1Registration?.Publisher, v2Registration?.Publisher);
            Assert.Equal(v1Registration?.PublishingGroup, v2Registration?.PublishingGroup);
            Assert.Equal(v1Registration?.PackageName, v2Registration?.PackageName);
            Assert.NotEqual(v1Registration?.PackageVersion, v2Registration?.PackageVersion);
        }
    }
}
