// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.MSTestSupport
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class MsbuildRunner : ExternalExecutable
    {
        private static readonly string VswhereFindArguments = "-property installationPath -version [17.0,18.0)";
        private static readonly string MsbuildCurrentRelativePath = @"MSBuild\Current\Bin\MSBuild.exe";
        private static readonly string MsbuildCurrentRelativePath64 = @"MSBuild\Current\Bin\amd64\MSBuild.exe";

        private static readonly object InitLock = new object();

        private static bool Initialized;
        private static MsbuildRunner MsbuildCurrentRunner;
        private static MsbuildRunner MsbuildCurrentRunner64;

        public static MsbuildRunnerResult Execute(string projectPath, string[] arguments = null, bool x64 = false) =>
            InitAndExecute(String.Empty, projectPath, arguments, x64);

        public static MsbuildRunnerResult ExecuteWithMsbuildCurrent(string projectPath, string[] arguments = null, bool x64 = false) =>
            InitAndExecute("Current", projectPath, arguments, x64);

        private static MsbuildRunnerResult InitAndExecute(string msbuildVersion, string projectPath, string[] arguments, bool x64)
        {
            lock (InitLock)
            {
                if (!Initialized)
                {
                    Initialized = true;
                    var vswhereResult = VswhereRunner.Execute(VswhereFindArguments, true);
                    if (vswhereResult.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Failed to execute vswhere.exe, exit code: {vswhereResult.ExitCode}. Output:\r\n{String.Join("\r\n", vswhereResult.StandardOutput)}");
                    }

                    string msbuildCurrentPath = null;
                    string msbuildCurrentPath64 = null;

                    foreach (var installPath in vswhereResult.StandardOutput)
                    {
                        if (msbuildCurrentPath == null)
                        {
                            var path = Path.Combine(installPath, MsbuildCurrentRelativePath);
                            if (File.Exists(path))
                            {
                                msbuildCurrentPath = path;
                            }
                        }

                        if (msbuildCurrentPath64 == null)
                        {
                            var path = Path.Combine(installPath, MsbuildCurrentRelativePath64);
                            if (File.Exists(path))
                            {
                                msbuildCurrentPath64 = path;
                            }
                        }
                    }

                    if (msbuildCurrentPath != null)
                    {
                        MsbuildCurrentRunner = new MsbuildRunner(msbuildCurrentPath);
                    }

                    if (msbuildCurrentPath64 != null)
                    {
                        MsbuildCurrentRunner64 = new MsbuildRunner(msbuildCurrentPath64);
                    }
                }
            }

            MsbuildRunner runner = x64 ? MsbuildCurrentRunner64 : MsbuildCurrentRunner;

            if (runner == null)
            {
                throw new InvalidOperationException($"Failed to find an installed{(x64 ? " 64-bit" : String.Empty)} MSBuild{msbuildVersion}");
            }

            return runner.ExecuteCore(projectPath, arguments);
        }

        private MsbuildRunner(string exePath) : base(exePath) { }

        private MsbuildRunnerResult ExecuteCore(string projectPath, string[] arguments)
        {
            var total = new List<string>
            {
                projectPath,
            };

            if (arguments != null)
            {
                total.AddRange(arguments);
            }

            var args = CombineArguments(total);
            var mergeErrorIntoOutput = true;
            var workingFolder = Path.GetDirectoryName(projectPath);
            var result = this.Run(args, mergeErrorIntoOutput, workingFolder);

            return new MsbuildRunnerResult
            {
                ExitCode = result.ExitCode,
                Output = result.StandardOutput,
            };
        }
    }
}
