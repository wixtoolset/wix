// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixTestTools;
    using WixToolset.Mba.Core;
    using Xunit;
    using Xunit.Abstractions;

    public class CacheTests : BurnE2ETests
    {
        public CacheTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanCache5GBFile()
        {
            // Recreate the 5GB payload to avoid having to copy it to the VM to run the tests.
            const long FiveGB = 5_368_709_120;
            const long OneGB = 1_073_741_824;
            var targetFilePath = Path.Combine(this.TestContext.TestDataFolder, "fivegb.file");

            // If the drive has less than 5GB (for the test file) plus 1GB (for working space), then
            // skip the test.
            var drive = new DriveInfo(targetFilePath.Substring(0, 1));
            if (drive.AvailableFreeSpace < FiveGB + OneGB)
            {
                Console.WriteLine("Skipping CanCache5GBFile() test because there is not enough disk space available to run the test.");
                return;
            }

            if (!File.Exists(targetFilePath))
            {
                var testTool = new TestTool(Path.Combine(TestData.Get(), "win-x86", "TestExe.exe"))
                {
                    Arguments = "/lf \"" + targetFilePath + $"|{FiveGB}\"",
                    ExpectedExitCode = 0,
                };
                testTool.Run(true);
            }

            var packageA = this.CreatePackageInstaller("PackageA");
            var bundleC = this.CreateBundleInstaller("BundleC");

            packageA.VerifyInstalled(false);

            bundleC.Install();
            bundleC.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
        }

        [Fact]
        public void CanDownloadPayloadsFromMissingAttachedContainer()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();
            var webServer = this.CreateWebServer();

            webServer.AddFiles(new Dictionary<string, string>
            {
                { "/BundleA/PackageA.msi", Path.Combine(this.TestContext.TestDataFolder, "PackageA.msi") },
                { "/BundleA/PackageB.msi", Path.Combine(this.TestContext.TestDataFolder, "PackageB.msi") },
            });
            webServer.Start();

            // Don't install PackageB initially so it will be installed when run from the package cache.
            testBAController.SetPackageRequestedState("PackageB", RequestState.Absent);

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            // Manually copy bundle to separate directory, install from there, and then delete it
            // so that when run from the package cache, it can't find the attached container.
            using (var dfs = new DisposableFileSystem())
            {
                var tempDirectory = dfs.GetFolder(true);

                var bundleAFileInfo = new FileInfo(bundleA.Bundle);
                var bundleACopiedPath = Path.Combine(tempDirectory, bundleAFileInfo.Name);
                bundleAFileInfo.CopyTo(bundleACopiedPath);

                bundleA.Install(bundleACopiedPath);
            }

            var bundlePackageCachePath = bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageB.VerifyInstalled(false);

            testBAController.SetPackageRequestedState("PackageB", RequestState.Present);

            bundleA.Modify(bundlePackageCachePath);
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageB.VerifyInstalled(true);
        }

        [Fact]
        public void CanFindAttachedContainerFromRenamedBundle()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var packageB = this.CreatePackageInstaller("PackageB");
            var bundleB = this.CreateBundleInstaller("BundleB");
            var testBAController = this.CreateTestBAController();

            // Don't install PackageB initially so it will be installed when run from the package cache.
            testBAController.SetPackageRequestedState("PackageB", RequestState.Absent);

            packageA.VerifyInstalled(false);
            packageB.VerifyInstalled(false);

            // Manually copy bundle to separate directory with new name and install from there
            // so that when run from the package cache, it has to get the attached container from the renamed bundle.
            using (var dfs = new DisposableFileSystem())
            {
                var tempDirectory = dfs.GetFolder(true);

                var bundleBFileInfo = new FileInfo(bundleB.Bundle);
                var bundleBCopiedPath = Path.Combine(tempDirectory, "RenamedBundle.exe");
                bundleBFileInfo.CopyTo(bundleBCopiedPath);

                bundleB.Install(bundleBCopiedPath);

                var bundlePackageCachePath = bundleB.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageB.VerifyInstalled(false);

                testBAController.SetPackageRequestedState("PackageB", RequestState.Present);

                bundleB.Modify(bundlePackageCachePath);
                bundleB.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageB.VerifyInstalled(true);
            }
        }
    }
}
