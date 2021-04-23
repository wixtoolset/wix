// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
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
            var packageA = this.CreatePackageInstaller("PackageA");
            var bundleC = this.CreateBundleInstaller("BundleC");

            packageA.VerifyInstalled(false);

            // Recreate the 5GB payload to avoid having to copy it to the VM to run the tests.
            var targetFilePath = Path.Combine(this.TestContext.TestDataFolder, "fivegb.file");
            if (!File.Exists(targetFilePath))
            {
                var testTool = new TestTool(Path.Combine(TestData.Get(), "win-x86", "TestExe.exe"))
                {
                    Arguments = "/lf \"" + targetFilePath + "|5368709120\"",
                    ExpectedExitCode = 0,
                };
                testTool.Run(true);
            }

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
