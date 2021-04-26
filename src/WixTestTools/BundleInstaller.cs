// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.IO;
    using System.Text;

    public partial class BundleInstaller : IDisposable
    {
        public BundleInstaller(WixTestContext testContext, string name)
        {
            this.Bundle = Path.Combine(testContext.TestDataFolder, $"{name}.exe");
            this.BundlePdb = Path.Combine(testContext.TestDataFolder, $"{name}.wixpdb");
            this.TestContext = testContext;
            this.TestGroupName = testContext.TestGroupName;
            this.TestName = testContext.TestName;
        }

        public string Bundle { get; }

        private WixTestContext TestContext { get; }

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
        /// Installs the bundle with optional arguments.
        /// </summary>
        /// <param name="bundlePath">This should be the bundle in the package cache.</param>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Install(string bundlePath, int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Install, arguments, bundlePath: bundlePath);
        }

        /// <summary>
        /// Calls Layout for the bundle with optional arguments.
        /// </summary>
        /// <param name="layoutDirectory">The destination directory.</param>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Layout(string layoutDirectory, int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.AdministrativeInstall, arguments, layoutDirectory: layoutDirectory);
        }

        /// <summary>
        /// Calls Layout for the bundle with optional arguments.
        /// </summary>
        /// <param name="bundlePath">Path to the bundle to run.</param>
        /// <param name="layoutDirectory">The destination directory.</param>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Layout(string bundlePath, string layoutDirectory, int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.AdministrativeInstall, arguments, bundlePath: bundlePath, layoutDirectory: layoutDirectory);
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
        /// Modify the bundle with optional arguments.
        /// </summary>
        /// <param name="bundlePath">This should be the bundle in the package cache.</param>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public string Modify(string bundlePath, int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            return this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Modify, arguments, bundlePath: bundlePath);
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
        private string RunBundleWithArguments(int expectedExitCode, MSIExec.MSIExecMode mode, string[] arguments, bool assertOnError = true, string bundlePath = null, string layoutDirectory = null)
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
                case MSIExec.MSIExecMode.AdministrativeInstall:
                    sb.Append($" -layout \"{layoutDirectory}\"");
                    break;

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

        public void Dispose()
        {
            string[] args = { "-burn.ignoredependencies=ALL" };
            this.RunBundleWithArguments((int)MSIExec.MSIExecReturnCode.SUCCESS, MSIExec.MSIExecMode.Cleanup, args, assertOnError: false);
        }
    }
}
