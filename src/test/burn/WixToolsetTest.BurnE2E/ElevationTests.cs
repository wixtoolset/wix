// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using Xunit;
    using Xunit.Abstractions;

    public class ElevationTests : BurnE2ETests
    {
        public ElevationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <summary>
        /// This test calls Elevate after Detect, and then calls Plan in OnElevateBegin.
        /// After calling Plan, it pumps some messages to simulate UI like the UAC callback.
        /// </summary>
        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6349")] // CAUTION: this test currently hangs because the Plan request gets dropped.
        public void CanExplicitlyElevateAndPlanFromOnElevateBegin()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            testBAController.SetExplicitlyElevateAndPlanFromOnElevateBegin();

            bundleA.Install();
            bundleA.VerifyRegisteredAndInPackageCache();
            packageA.VerifyInstalled(true);
        }
    }
}
