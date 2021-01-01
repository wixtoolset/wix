// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Win32;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class BundleInstaller : IDisposable
    {
        public const string BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY = "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        public const string BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH = "BundleCachePath";
        public const string FULL_BURN_POLICY_REGISTRY_PATH = "SOFTWARE\\WOW6432Node\\Policies\\WiX\\Burn";
        public const string PACKAGE_CACHE_FOLDER_NAME = "Package Cache";

        public BundleInstaller(WixTestContext testContext, string name)
        {
            this.Bundle = Path.Combine(testContext.TestDataFolder, $"{name}.exe");
            this.BundlePdb = Path.Combine(testContext.TestDataFolder, $"{name}.wixpdb");
            this.TestGroupName = testContext.TestGroupName;
            this.TestName = testContext.TestName;
        }

        public string Bundle { get; }

        public string BundlePdb { get; }

        private WixBundleSymbol BundleSymbol { get; set; }

        public string TestGroupName { get; }

        public string TestName { get; }

        /// <summary>
        /// Installs the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Install(int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Install, arguments);
        }

        /// <summary>
        /// Modify the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Modify(int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Modify, arguments);
        }

        /// <summary>
        /// Repairs the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Repair(int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Repair, arguments);
        }

        /// <summary>
        /// Uninstalls the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Uninstall(int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Uninstall, arguments);
        }

        /// <summary>
        /// Uninstalls the bundle at the given path with optional arguments.
        /// </summary>
        /// <param name="bundlePath">This should be the bundle in the package cache.</param>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Uninstall(string bundlePath, int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Uninstall, arguments, bundlePath: bundlePath);
        }

        /// <summary>
        /// Executes the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code.</param>
        /// <param name="mode">Install mode.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        private string RunBundleWithArguments(int expectedExitCode, MSIExec.MSIExecMode mode, string[] arguments, bool assertOnError = true, string bundlePath = null)
        {
            TestTool bundle = new TestTool(bundlePath ?? this.Bundle);
            var sb = new StringBuilder();

            // Be sure to run silent.
            sb.Append(" -quiet");
            
            // Generate the log file name.
            string logFile = Path.Combine(Path.GetTempPath(), String.Format("{0}_{1}_{2:yyyyMMddhhmmss}_{4}_{3}.log", this.TestGroupName, this.TestName, DateTime.UtcNow, Path.GetFileNameWithoutExtension(this.Bundle), mode));
            sb.AppendFormat(" -log \"{0}\"", logFile);

            // Set operation.
            switch (mode)
            {
                case MSIExec.MSIExecMode.Modify:
                    sb.Append(" -modify");
                    break;

                case MSIExec.MSIExecMode.Repair:
                    sb.Append(" -repair");
                    break;

                case MSIExec.MSIExecMode.Cleanup:
                case MSIExec.MSIExecMode.Uninstall:
                    sb.Append(" -uninstall");
                    break;
            }

            // Add additional arguments.
            if (null != arguments)
            {
                sb.Append(" ");
                sb.Append(String.Join(" ", arguments));
            }

            // Set the arguments.
            bundle.Arguments = sb.ToString();

            // Run the tool and assert the expected code.
            bundle.ExpectedExitCode = expectedExitCode;
            bundle.Run(assertOnError);

            // Return the log file name.
            return logFile;
        }

        private WixBundleSymbol GetBundleSymbol()
        {
            if (this.BundleSymbol == null)
            {
                using var wixOutput = WixOutput.Read(this.BundlePdb);
                var intermediate = Intermediate.Load(wixOutput);
                var section = intermediate.Sections.Single();
                this.BundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
            }

            return this.BundleSymbol;
        }

        public string GetExpectedCachedBundlePath()
        {
            var bundleSymbol = this.GetBundleSymbol();

            using var policyKey = Registry.LocalMachine.OpenSubKey(FULL_BURN_POLICY_REGISTRY_PATH);
            var redirectedCachePath = policyKey?.GetValue("PackageCache") as string;
            var cachePath = redirectedCachePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), PACKAGE_CACHE_FOLDER_NAME);
            return Path.Combine(cachePath, bundleSymbol.BundleId, Path.GetFileName(this.Bundle));
        }

        public string VerifyRegisteredAndInPackageCache()
        {
            var bundleSymbol = this.GetBundleSymbol();
            var bundleId = bundleSymbol.BundleId;
            var registrationKeyPath = $"{BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY}\\{bundleId}";

            using var registrationKey = Registry.LocalMachine.OpenSubKey(registrationKeyPath);
            Assert.NotNull(registrationKey);

            var cachePathValue = registrationKey.GetValue(BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH);
            Assert.NotNull(cachePathValue);
            var cachePath = Assert.IsType<string>(cachePathValue);
            Assert.True(File.Exists(cachePath));

            var expectedCachePath = this.GetExpectedCachedBundlePath();
            Assert.Equal(expectedCachePath, cachePath, StringComparer.OrdinalIgnoreCase);

            return cachePath;
        }

        public void VerifyUnregisteredAndRemovedFromPackageCache()
        {
            var cachedBundlePath = this.GetExpectedCachedBundlePath();
            this.VerifyUnregisteredAndRemovedFromPackageCache(cachedBundlePath);
        }

        public void VerifyUnregisteredAndRemovedFromPackageCache(string cachedBundlePath)
        {
            var bundleSymbol = this.GetBundleSymbol();
            var bundleId = bundleSymbol.BundleId;
            var registrationKeyPath = $"{BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY}\\{bundleId}";

            using var registrationKey = Registry.LocalMachine.OpenSubKey(registrationKeyPath);
            Assert.Null(registrationKey);

            Assert.False(File.Exists(cachedBundlePath));
        }

        public void Dispose()
        {
            string[] args = { "-burn.ignoredependencies=ALL" };
            this.RunBundleWithArguments((int)MSIExec.MSIExecReturnCode.SUCCESS, MSIExec.MSIExecMode.Cleanup, args, assertOnError: false);
        }
    }
}
