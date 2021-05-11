// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Xunit;
    using Xunit.Abstractions;

    public class UpdateBundleTests : BurnE2ETests
    {
        public UpdateBundleTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanLaunchUpdateBundleFromLocalSourceInsteadOfInstall()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            var updateBundleSwitch = String.Concat("\"", "-updatebundle:", bundleAv2.Bundle, "\"");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            // Install the v2 bundle by getting v1 to launch it as an update bundle.
            bundleAv1.Install(arguments: updateBundleSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);

            bundleAv2.Uninstall();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();
            packageAv2.VerifyInstalled(false);
        }

        [Fact]
        public void CanLaunchUpdateBundleFromLocalSourceInsteadOfModify()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            var updateBundleSwitch = String.Concat("\"", "-updatebundle:", bundleAv2.Bundle, "\"");

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(true);
            packageAv2.VerifyInstalled(false);

            // Install the v2 bundle by getting v1 to launch it as an update bundle.
            bundleAv1.Modify(arguments: updateBundleSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);

            bundleAv2.Uninstall();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();
            packageAv2.VerifyInstalled(false);
        }

        [Fact]
        public void ForwardsArgumentsToUpdateBundle()
        {
            var packageAv1 = this.CreatePackageInstaller("PackageAv1");
            var packageAv2 = this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");
            var testBAController = this.CreateTestBAController();

            const string verifyArguments = "these arguments should exist";
            var updateBundleSwitch = String.Concat("\"", "-updatebundle:", bundleAv2.Bundle, "\" ", verifyArguments);

            testBAController.SetVerifyArguments(verifyArguments);

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(false);

            // Install the v2 bundle by getting v1 to launch it as an update bundle.
            bundleAv1.Install(arguments: updateBundleSwitch);
            bundleAv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            packageAv1.VerifyInstalled(false);
            packageAv2.VerifyInstalled(true);

            // Attempt to uninstall bundleA2 without the verify arguments passed and expect failure code.
            bundleAv2.Uninstall(expectedExitCode: -1);

            // Remove the required arguments and uninstall again.
            testBAController.SetVerifyArguments(null);
            bundleAv2.Uninstall();
            bundleAv2.VerifyUnregisteredAndRemovedFromPackageCache();
            packageAv2.VerifyInstalled(false);
        }

        // Installs bundle Bv1.0 then tries to update to latest version during modify (but no server exists).
        [Fact]
        public void CanCheckUpdateServerDuringModifyAndDoNothingWhenServerIsntResponsive()
        {
            var packageB = this.CreatePackageInstaller("PackageBv1");
            var bundleB = this.CreateBundleInstaller("BundleBv1");

            packageB.VerifyInstalled(false);

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();

            packageB.VerifyInstalled(true);

            // Run the v1 bundle requesting an update bundle.
            bundleB.Modify(arguments: "-checkupdate");
            bundleB.VerifyRegisteredAndInPackageCache();

            // Verify nothing changed.
            packageB.VerifyInstalled(true);

            bundleB.Uninstall();
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();
            packageB.VerifyInstalled(false);
        }

        // Installs bundle Bv1.0 then tries to update to latest version during modify (server exists, no feed).
        [Fact]
        public void CanCheckUpdateServerDuringModifyAndDoNothingWhenFeedIsMissing()
        {
            var packageB = this.CreatePackageInstaller("PackageBv1");
            var bundleB = this.CreateBundleInstaller("BundleBv1");
            var webServer = this.CreateWebServer();

            webServer.Start();

            packageB.VerifyInstalled(false);

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();

            packageB.VerifyInstalled(true);

            // Run the v1 bundle requesting an update bundle.
            bundleB.Modify(arguments: "-checkupdate");
            bundleB.VerifyRegisteredAndInPackageCache();

            // Verify nothing changed.
            packageB.VerifyInstalled(true);

            bundleB.Uninstall();
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();
            packageB.VerifyInstalled(false);
        }

        // Installs bundle Bv1.0 then tries to update to latest version during modify (server exists, v1.0 feed).
        [Fact]
        public void CanCheckUpdateServerDuringModifyAndDoNothingWhenAlreadyLatestVersion()
        {
            var packageB = this.CreatePackageInstaller("PackageBv1");
            var bundleB = this.CreateBundleInstaller("BundleBv1");
            var webServer = this.CreateWebServer();

            webServer.AddFiles(new Dictionary<string, string>
            {
                { "/BundleB/feed", Path.Combine(this.TestContext.TestDataFolder, "FeedBv1.0.xml") },
            });
            webServer.Start();

            packageB.VerifyInstalled(false);

            bundleB.Install();
            bundleB.VerifyRegisteredAndInPackageCache();

            packageB.VerifyInstalled(true);

            // Run the v1 bundle requesting an update bundle.
            bundleB.Modify(arguments: "-checkupdate");
            bundleB.VerifyRegisteredAndInPackageCache();

            // Verify nothing changed.
            packageB.VerifyInstalled(true);

            bundleB.Uninstall();
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();
            packageB.VerifyInstalled(false);
        }

        // Installs bundle Bv1.0 then does an update to bundle Bv2.0 during modify (server exists, v2.0 feed).
        [Fact]
        public void CanLaunchUpdateBundleFromDownloadInsteadOfModify()
        {
            var packageBv1 = this.CreatePackageInstaller("PackageBv1");
            var packageBv2 = this.CreatePackageInstaller("PackageBv2");
            var bundleBv1 = this.CreateBundleInstaller("BundleBv1");
            var bundleBv2 = this.CreateBundleInstaller("BundleBv2");
            var webServer = this.CreateWebServer();

            webServer.AddFiles(new Dictionary<string, string>
            {
                { "/BundleB/feed", Path.Combine(this.TestContext.TestDataFolder, "FeedBv2.0.xml") },
                { "/BundleB/2.0/BundleB.exe", bundleBv2.Bundle },
            });
            webServer.Start();

            packageBv1.VerifyInstalled(false);
            packageBv2.VerifyInstalled(false);

            bundleBv1.Install();
            bundleBv1.VerifyRegisteredAndInPackageCache();

            packageBv1.VerifyInstalled(true);
            packageBv2.VerifyInstalled(false);

            // Run the v1 bundle requesting an update bundle.
            bundleBv1.Modify(arguments: "-checkupdate");

            // The modify -> update is asynchronous, so we need to wait until the real BundleB is done
            var childBundles = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(bundleBv2.Bundle));
            foreach (var childBundle in childBundles)
            {
                childBundle.WaitForExit();
            }

            bundleBv1.VerifyUnregisteredAndRemovedFromPackageCache();
            bundleBv2.VerifyRegisteredAndInPackageCache();

            packageBv1.VerifyInstalled(false);
            packageBv2.VerifyInstalled(true);

            bundleBv2.Uninstall();
            bundleBv2.VerifyUnregisteredAndRemovedFromPackageCache();
            packageBv1.VerifyInstalled(false);
            packageBv2.VerifyInstalled(false);
        }
    }
}
