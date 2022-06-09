// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Win32;
    using WixBuildTools.TestSupport;
    using WixTestTools;
    using WixToolset.Mba.Core;
    using Xunit;
    using Xunit.Abstractions;

    public class CacheTests : BurnE2ETests
    {
        public CacheTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        private void SkipIf5GBFileUnavailable()
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
                WixAssert.Skip($"Skipping {this.TestContext.TestName} because there is not enough disk space available to run the test.");
            }

            if (!File.Exists(targetFilePath))
            {
                var testExeTool = new TestExeTool
                {
                    Arguments = "/lf \"" + targetFilePath + $"|{FiveGB}\"",
                    ExpectedExitCode = 0,
                };
                testExeTool.Run(true);
            }
        }

        [LongRuntimeFact]
        public void CanCache5GBFile()
        {
            this.SkipIf5GBFileUnavailable();

            var packageA = this.CreatePackageInstaller("PackageA");
            var bundleC = this.CreateBundleInstaller("BundleC");

            packageA.VerifyInstalled(false);

            bundleC.Install();
            bundleC.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
        }

        private string Cache5GBFileFromDownload(bool disableRangeRequests)
        {
            this.SkipIf5GBFileUnavailable();

            var packageA = this.CreatePackageInstaller("PackageA");
            var bundleC = this.CreateBundleInstaller("BundleC");
            var webServer = this.CreateWebServer();

            webServer.AddFiles(new Dictionary<string, string>
            {
                { "/BundleC/fivegb.file", Path.Combine(this.TestContext.TestDataFolder, "fivegb.file") },
                { "/BundleC/PackageA.msi", Path.Combine(this.TestContext.TestDataFolder, "PackageA.msi") },
            });
            webServer.DisableRangeRequests = disableRangeRequests;
            webServer.Start();

            using var dfs = new DisposableFileSystem();
            var separateDirectory = dfs.GetFolder(true);

            // Manually copy bundle to separate directory and then run from there so the non-compressed payloads have to be resolved.
            var bundleCFileInfo = new FileInfo(bundleC.Bundle);
            var bundleCCopiedPath = Path.Combine(separateDirectory, bundleCFileInfo.Name);
            bundleCFileInfo.CopyTo(bundleCCopiedPath);

            packageA.VerifyInstalled(false);

            var installLogPath = bundleC.Install(bundleCCopiedPath);
            bundleC.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);

            return installLogPath;
        }

        [LongRuntimeFact]
        public void CanCache5GBFileFromDownloadWithRangeRequestSupport()
        {
            var logPath = this.Cache5GBFileFromDownload(false);

            Assert.False(LogVerifier.MessageInLogFile(logPath, "Range request not supported for URL: http://localhost:9999/e2e/BundleC/fivegb.file"));
            Assert.False(LogVerifier.MessageInLogFile(logPath, "Content-Length not returned for URL: http://localhost:9999/e2e/BundleC/fivegb.file"));
        }

        [LongRuntimeFact]
        public void CanCache5GBFileFromDownloadWithoutRangeRequestSupport()
        {
            var logPath = this.Cache5GBFileFromDownload(true);

            Assert.True(LogVerifier.MessageInLogFile(logPath, "Range request not supported for URL: http://localhost:9999/e2e/BundleC/fivegb.file"));
            Assert.False(LogVerifier.MessageInLogFile(logPath, "Content-Length not returned for URL: http://localhost:9999/e2e/BundleC/fivegb.file"));
        }

        [RuntimeFact]
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
            webServer.DisableHeadResponses = true;
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

            var bundlePackageRegistration = bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageB.VerifyInstalled(false);

            testBAController.SetPackageRequestedState("PackageB", RequestState.Present);

            var modifyLogPath = bundleA.Modify(bundlePackageRegistration.CachePath);
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);
            packageB.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFile(modifyLogPath, "Ignoring failure to get size and time for URL: http://localhost:9999/e2e/BundleA/PackageB.msi (error 0x80070002)"));
        }

        [RuntimeFact]
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

                var bundlePackageRegistration = bundleB.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageB.VerifyInstalled(false);

                testBAController.SetPackageRequestedState("PackageB", RequestState.Present);

                bundleB.Modify(bundlePackageRegistration.CachePath);
                bundleB.VerifyRegisteredAndInPackageCache();

                packageA.VerifyInstalled(true);
                packageB.VerifyInstalled(true);
            }
        }

        [RuntimeFact]
        public void CanGetEngineWorkingDirectoryFromCommandLine()
        {
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            testBAController.SetImmediatelyQuit();

            using (var dfs = new DisposableFileSystem())
            {
                var baseTempPath = dfs.GetFolder(true);
                var logPath = bundleA.Install(0, $"-burn.engine.working.directory=\"{baseTempPath}\"");
                LogVerifier.MessageInLogFileRegex(logPath, $"Burn x86 v4.*, Windows v.* \\(Build .*: Service Pack .*\\), path: {baseTempPath.Replace("\\", "\\\\")}\\\\.*\\\\.cr\\\\BundleA.exe");
            }
        }

        [RuntimeFact]
        public void CanGetEngineWorkingDirectoryFromPolicy()
        {
            var deletePolicyKey = false;
            string originalPolicyValue = null;

            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();
            var policyPath = bundleA.GetFullBurnPolicyRegistryPath();

            testBAController.SetImmediatelyQuit();

            try
            {
                using (var dfs = new DisposableFileSystem())
                {
                    var baseTempPath = dfs.GetFolder(true);

                    var policyKey = Registry.LocalMachine.OpenSubKey(policyPath, writable: true);
                    if (policyKey == null)
                    {
                        policyKey = Registry.LocalMachine.CreateSubKey(policyPath, writable: true);
                        deletePolicyKey = true;
                    }

                    using (policyKey)
                    {
                        originalPolicyValue = policyKey.GetValue("EngineWorkingDirectory") as string;
                        policyKey.SetValue("EngineWorkingDirectory", baseTempPath);
                    }

                    var logPath = bundleA.Install();
                    LogVerifier.MessageInLogFileRegex(logPath, $"Burn x86 v4.*, Windows v.* \\(Build .*: Service Pack .*\\), path: {baseTempPath.Replace("\\", "\\\\")}\\\\.*\\\\.cr\\\\BundleA.exe");
                }
            }
            finally
            {
                if (deletePolicyKey)
                {
                    Registry.LocalMachine.DeleteSubKeyTree(policyPath);
                }
                else if (originalPolicyValue != null)
                {
                    using (var policyKey = Registry.LocalMachine.CreateSubKey(policyPath, writable: true))
                    {
                        policyKey.SetValue("EngineWorkingDirectory", originalPolicyValue);
                    }
                }
            }
        }
    }
}
