// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using static WixTestTools.MSIExec;

    public partial class PackageInstaller : IDisposable
    {
        public PackageInstaller(WixTestContext testContext, string filename)
        {
            this.Package = Path.Combine(testContext.TestDataFolder, $"{filename}.msi");
            this.PackagePdb = Path.Combine(testContext.TestDataFolder, $"{filename}.wixpdb");
            this.TestContext = testContext;

            using var wixOutput = WixOutput.Read(this.PackagePdb);

            var intermediate = Intermediate.Load(wixOutput);
            var section = intermediate.Sections.Single();
            var platformSummary = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.PlatformAndLanguage);
            var platformString = platformSummary.Value.Split(new char[] { ';' }, 2)[0];
            this.IsX64 = platformString != "Intel";

            this.WiData = WindowsInstallerData.Load(wixOutput);
        }

        public string Package { get; }

        private WixTestContext TestContext { get; }

        public string TestGroupName => this.TestContext.TestGroupName;

        public string TestName => this.TestContext.TestName;

        /// <summary>
        /// Installs a .msi file
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code</param>
        /// <param name="otherArguments">Other arguments to pass to MSIExec.</param>
        /// <returns>MSIExec log File</returns>
        public string InstallProduct(MSIExecReturnCode expectedExitCode = MSIExecReturnCode.SUCCESS, params string[] otherArguments)
        {
            return this.RunMSIExec(MSIExecMode.Install, otherArguments, expectedExitCode);
        }

        /// <summary>
        /// Uninstalls a .msi file
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code</param>
        /// <param name="otherArguments">Other arguments to pass to MSIExec.</param>
        /// <returns>MSIExec log File</returns>
        public string UninstallProduct(MSIExecReturnCode expectedExitCode = MSIExecReturnCode.SUCCESS, params string[] otherArguments)
        {
            return this.RunMSIExec(MSIExecMode.Uninstall, otherArguments, expectedExitCode);
        }

        /// <summary>
        /// Repairs a .msi file
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code</param>
        /// <param name="otherArguments">Other arguments to pass to msiexe.exe.</param>
        /// <returns>MSIExec log File</returns>
        public string RepairProduct(MSIExecReturnCode expectedExitCode = MSIExecReturnCode.SUCCESS, params string[] otherArguments)
        {
            return this.RunMSIExec(MSIExecMode.Repair, otherArguments, expectedExitCode);
        }

        /// <summary>
        /// Executes MSIExec on a .msi file
        /// </summary>
        /// <param name="mode">Mode of execution for MSIExec</param>
        /// <param name="otherArguments">Other arguments to pass to MSIExec.</param>
        /// <param name="expectedExitCode">Expected exit code</param>
        /// <returns>MSIExec exit code</returns>
        private string RunMSIExec(MSIExecMode mode, string[] otherArguments, MSIExecReturnCode expectedExitCode, bool assertOnError = true)
        {
            // Generate the log file name.
            var logFile = Path.Combine(Path.GetTempPath(), String.Format("{0}_{1}_{2:yyyyMMddhhmmss}_{4}_{3}.log", this.TestGroupName, this.TestName, DateTime.UtcNow, Path.GetFileNameWithoutExtension(this.Package), mode));

            var msiexec = new MSIExec
            {
                Product = this.Package,
                ExecutionMode = mode,
                OtherArguments = null != otherArguments ? String.Join(" ", otherArguments) : null,
                ExpectedExitCode = expectedExitCode,
                LogFile = logFile,
            };

            msiexec.Run(assertOnError);
            return msiexec.LogFile;
        }

        public void Dispose()
        {
            string[] args = { "IGNOREDEPENDENCIES=ALL", "WIXFAILWHENDEFERRED=0" };
            this.RunMSIExec(MSIExecMode.Cleanup, args, MSIExecReturnCode.SUCCESS, assertOnError: false);
        }
    }
}
