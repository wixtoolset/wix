// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using WixBuildTools.TestSupport;
    using WixTestTools;
    using WixToolset.Mba.Core;
    using Xunit;
    using Xunit.Abstractions;

    public class LongPathTests : BurnE2ETests
    {
        public LongPathTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x86_wixstdba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleA");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x86_testba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleB");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x86_dnctestba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleC");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x86_wixba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA", "BundleD");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x64_wixstdba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleA_x64");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimplePerUserBundle_x64_wixstdba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageApu_x64", "BundleApu_x64", "PackagePerUser.wxs", unchecked((int)0xc0000005));
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x64_testba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleB_x64");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x64_dnctestba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleC_x64");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallSimpleBundle_x64_dncwixba()
        {
            this.CanInstallAndUninstallSimpleBundle("PackageA_x64", "BundleD_x64");
        }

        private void CanInstallAndUninstallSimpleBundle(string packageName, string bundleName, string fileName = "Package.wxs", int? alternateExitCode = null)
        {
            var package = this.CreatePackageInstaller(Path.Combine("..", "BasicFunctionalityTests", packageName));

            var bundle = this.CreateBundleInstaller(Path.Combine("..", "BasicFunctionalityTests", bundleName));
            bundle.AlternateExitCode = alternateExitCode;

            using var dfs = new DisposableFileSystem();
            var baseFolder = GetLongPath(dfs.GetFolder());

            var packageSourceCodeInstalled = package.GetInstalledFilePath(fileName);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageSourceCodeInstalled), $"{packageName} payload should not be there on test start: {packageSourceCodeInstalled}");

            var bundleFileInfo = new FileInfo(bundle.Bundle);
            var bundleCopiedPath = Path.Combine(baseFolder, bundleFileInfo.Name);
            bundleFileInfo.CopyTo(bundleCopiedPath);

            bundle.Install(bundleCopiedPath);
            bundle.VerifyRegisteredAndInPackageCache();

            // Source file should be installed
            Assert.True(File.Exists(packageSourceCodeInstalled), $"Should have found {packageName} payload installed at: {packageSourceCodeInstalled}");

            if (alternateExitCode == bundle.LastExitCode)
            {
                WixAssert.Skip($"Install exited with {bundle.LastExitCode}");
            }

            bundle.Uninstall(bundleCopiedPath);

            // Source file should *not* be installed
            Assert.False(File.Exists(packageSourceCodeInstalled), $"{packageName} payload should have been removed by uninstall from: {packageSourceCodeInstalled}");

            bundle.VerifyUnregisteredAndRemovedFromPackageCache();

            if (alternateExitCode == bundle.LastExitCode)
            {
                WixAssert.Skip($"Uninstall exited with {bundle.LastExitCode}");
            }
        }

        [RuntimeFact]
        public void CanLayoutNonCompressedBundleToLongPath()
        {
            var nonCompressedBundle = this.CreateBundleInstaller("NonCompressedBundle");
            var testBAController = this.CreateTestBAController();

            testBAController.SetPackageRequestedState("NetFx48Web", RequestState.None);

            using var dfs = new DisposableFileSystem();
            var layoutDirectory = GetLongPath(dfs.GetFolder());

            nonCompressedBundle.Layout(layoutDirectory);
            nonCompressedBundle.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(File.Exists(Path.Combine(layoutDirectory, "NonCompressedBundle.exe")));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, "PackageA.msi")));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, "1a.cab")));
            Assert.False(File.Exists(Path.Combine(layoutDirectory, @"redist\ndp48-web.exe")));
        }

        [RuntimeFact]
        public void CanInstallNonCompressedBundleWithLongTempPath()
        {
            this.InstallNonCompressedBundle(longTemp: true, useOriginalTempForLog: true);
        }

        [RuntimeFact]
        public void CannotInstallNonCompressedBundleWithLongPackageCachePath()
        {
            var installLogPath = this.InstallNonCompressedBundle((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, longPackageCache: true);
            Assert.True(LogVerifier.MessageInLogFile(installLogPath, @"Error 0x80070643: Failed to install MSI package"));
        }

        [RuntimeFact]
        public void CannotInstallNonCompressedBundleWithLongWorkingPath()
        {
            var installLogPath = this.InstallNonCompressedBundle((int)MSIExec.MSIExecReturnCode.ERROR_FILENAME_EXCED_RANGE | unchecked((int)0x80070000), longWorkingPath: true);
            Assert.True(LogVerifier.MessageInLogFile(installLogPath, @"Error 0x800700ce: Failed to load BA DLL"));
        }

        public string InstallNonCompressedBundle(int expectedExitCode = 0, bool longTemp = false, bool useOriginalTempForLog = false, bool longWorkingPath = false, bool longPackageCache = false, int? alternateExitCode = null)
        {
            var deletePolicyKey = false;
            string originalEngineWorkingDirectoryValue = null;
            string originalPackageCacheValue = null;
            var originalTemp = Environment.GetEnvironmentVariable("TMP");
            var packageA = this.CreatePackageInstaller("PackageA");
            var nonCompressedBundle = this.CreateBundleInstaller("NonCompressedBundle");
            var policyPath = nonCompressedBundle.GetFullBurnPolicyRegistryPath();
            string installLogPath = null;

            try
            {
                using var dfs = new DisposableFileSystem();
                var originalBaseFolder = dfs.GetFolder();
                var baseFolder = GetLongPath(originalBaseFolder);
                var sourceFolder = Path.Combine(baseFolder, "source");
                var workingFolder = Path.Combine(baseFolder, "working");
                var tempFolder = Path.Combine(originalBaseFolder, new string('d', 260 - originalBaseFolder.Length - 2));
                var packageCacheFolder = Path.Combine(baseFolder, "package cache");

                var copyResult = TestDataFolderFileSystem.RobocopyFolder(this.TestContext.TestDataFolder, sourceFolder);
                Assert.True(copyResult.ExitCode >= 0 && copyResult.ExitCode < 8, $"Exit code: {copyResult.ExitCode}\r\nOutput: {String.Join("\r\n", copyResult.StandardOutput)}\r\nError: {String.Join("\r\n", copyResult.StandardError)}");

                var bundleFileInfo = new FileInfo(nonCompressedBundle.Bundle);
                var bundleCopiedPath = Path.Combine(sourceFolder, bundleFileInfo.Name);

                var policyKey = Registry.LocalMachine.OpenSubKey(policyPath, writable: true);
                if (policyKey == null)
                {
                    policyKey = Registry.LocalMachine.CreateSubKey(policyPath, writable: true);
                    deletePolicyKey = true;
                }

                using (policyKey)
                {
                    originalEngineWorkingDirectoryValue = policyKey.GetValue("EngineWorkingDirectory") as string;
                    originalPackageCacheValue = policyKey.GetValue("PackageCache") as string;

                    if (longWorkingPath)
                    {
                        policyKey.SetValue("EngineWorkingDirectory", workingFolder);
                    }

                    if (longPackageCache)
                    {
                        policyKey.SetValue("PackageCache", packageCacheFolder);
                    }
                }

                if (longTemp)
                {
                    Environment.SetEnvironmentVariable("TMP", tempFolder);

                    if (useOriginalTempForLog)
                    {
                        nonCompressedBundle.LogDirectory = originalTemp;
                    }
                }

                try
                {
                    nonCompressedBundle.AlternateExitCode = alternateExitCode;
                    installLogPath = nonCompressedBundle.Install(bundleCopiedPath, expectedExitCode);

                    if (alternateExitCode == nonCompressedBundle.LastExitCode)
                    {
                        WixAssert.Skip($"Install exited with {nonCompressedBundle.LastExitCode}");
                    }
                }
                finally
                {
                    TestDataFolderFileSystem.RobocopyFolder(tempFolder, originalTemp);
                }

                installLogPath = Path.Combine(originalTemp, Path.GetFileName(installLogPath));

                if (expectedExitCode == 0)
                {
                    var registration = nonCompressedBundle.VerifyRegisteredAndInPackageCache();
                    packageA.VerifyInstalled(true);

                    nonCompressedBundle.Uninstall(registration.CachePath);

                    if (alternateExitCode == nonCompressedBundle.LastExitCode)
                    {
                        WixAssert.Skip($"Uninstall exited with {nonCompressedBundle.LastExitCode}");
                    }
                }

                nonCompressedBundle.VerifyUnregisteredAndRemovedFromPackageCache();
                packageA.VerifyInstalled(false);

                return installLogPath;
            }
            finally
            {
                Environment.SetEnvironmentVariable("TMP", originalTemp);

                if (deletePolicyKey)
                {
                    Registry.LocalMachine.DeleteSubKeyTree(policyPath);
                }
                else
                {
                    using (var policyKey = Registry.LocalMachine.OpenSubKey(policyPath, writable: true))
                    {
                        policyKey?.SetValue("EngineWorkingDirectory", originalEngineWorkingDirectoryValue);
                        policyKey?.SetValue("PackageCache", originalPackageCacheValue);
                    }
                }
            }
        }

        private static string GetLongPath(string baseFolder)
        {
            Directory.CreateDirectory(baseFolder);

            // Try to create a directory that is longer than MAX_PATH but without the \\?\ prefix to detect OS support for long paths.
            // Need to PInvoke CreateDirectoryW directly because .NET methods will append the \\?\ prefix.
            foreach (var c in new char[] { 'a', 'b', 'c' })
            {
                baseFolder = Path.Combine(baseFolder, new string(c, 100));
                if (!CreateDirectoryW(baseFolder, IntPtr.Zero))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    if (lastError == 206)
                    {
                        WixAssert.Skip($"MAX_PATH is being enforced ({baseFolder})");
                    }
                    throw new Win32Exception(lastError);
                }
            }

            return baseFolder;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool CreateDirectoryW(string lpPathName, IntPtr lpSecurityAttributes);
    }
}
