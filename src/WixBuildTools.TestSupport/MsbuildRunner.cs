// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public static class MsbuildRunner
    {
        private static readonly string VswhereRelativePath = @"Microsoft Visual Studio\Installer\vswhere.exe";
        private static readonly string[] VswhereFindArguments = new[] { "-property", "installationPath" };
        private static readonly string Msbuild15RelativePath = @"MSBuild\15.0\Bin\MSBuild.exe";
        private static readonly string Msbuild16RelativePath = @"MSBuild\Current\Bin\MSBuild.exe";

        private static readonly object InitLock = new object();

        private static string Msbuild15Path;
        private static string Msbuild16Path;

        public static MsbuildRunnerResult Execute(string projectPath, string[] arguments = null) => InitAndExecute(String.Empty, projectPath, arguments);

        public static MsbuildRunnerResult ExecuteWithMsbuild15(string projectPath, string[] arguments = null) => InitAndExecute("15", projectPath, arguments);

        public static MsbuildRunnerResult ExecuteWithMsbuild16(string projectPath, string[] arguments = null) => InitAndExecute("16", projectPath, arguments);

        private static MsbuildRunnerResult InitAndExecute(string msbuildVersion, string projectPath, string[] arguments)
        {
            lock (InitLock)
            {
                if (Msbuild15Path == null && Msbuild16Path == null)
                {
                    var vswherePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), VswhereRelativePath);
                    if (!File.Exists(vswherePath))
                    {
                        throw new InvalidOperationException($"Failed to find vswhere at: {vswherePath}");
                    }

                    var result = RunProcessCaptureOutput(vswherePath, VswhereFindArguments);
                    if (result.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Failed to execute vswhere.exe, exit code: {result.ExitCode}");
                    }

                    Msbuild15Path = String.Empty;
                    Msbuild16Path = String.Empty;

                    foreach (var installPath in result.Output)
                    {
                        if (String.IsNullOrEmpty(Msbuild16Path))
                        {
                            var path = Path.Combine(installPath, Msbuild16RelativePath);
                            if (File.Exists(path))
                            {
                                Msbuild16Path = path;
                            }
                        }

                        if (String.IsNullOrEmpty(Msbuild15Path))
                        {
                            var path = Path.Combine(installPath, Msbuild15RelativePath);
                            if (File.Exists(path))
                            {
                                Msbuild15Path = path;
                            }
                        }
                    }
                }
            }

            var msbuildPath = !String.IsNullOrEmpty(Msbuild15Path) ? Msbuild15Path : Msbuild16Path;

            if (msbuildVersion == "15")
            {
                msbuildPath = Msbuild15Path;
            }
            else if (msbuildVersion == "16")
            {
                msbuildPath = Msbuild16Path;
            }

            return ExecuteCore(msbuildVersion, msbuildPath, projectPath, arguments);
        }

        private static MsbuildRunnerResult ExecuteCore(string msbuildVersion, string msbuildPath, string projectPath, string[] arguments)
        {
            if (String.IsNullOrEmpty(msbuildPath))
            {
                throw new InvalidOperationException($"Failed to find an installed MSBuild{msbuildVersion}");
            }

            var total = new List<string>
            {
                projectPath
            };

            if (arguments != null)
            {
                total.AddRange(arguments);
            }

            var workingFolder = Path.GetDirectoryName(projectPath);
            return RunProcessCaptureOutput(msbuildPath, total.ToArray(), workingFolder);
        }

        private static MsbuildRunnerResult RunProcessCaptureOutput(string executablePath, string[] arguments = null, string workingFolder = null)
        {
            var startInfo = new ProcessStartInfo(executablePath)
            {
                Arguments = CombineArguments(arguments),
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = workingFolder,
            };

            var exitCode = 0;
            var output = new List<string>();

            using (var process = Process.Start(startInfo))
            {
                process.OutputDataReceived += (s, e) => { if (e.Data != null) { output.Add(e.Data); } };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) { output.Add(e.Data); } };

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            return new MsbuildRunnerResult { ExitCode = exitCode, Output = output.ToArray() };
        }

        private static string CombineArguments(string[] arguments)
        {
            if (arguments == null)
            {
                return null;
            }

            var sb = new StringBuilder();

            foreach (var arg in arguments)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                if (arg.IndexOf(' ') > -1)
                {
                    sb.Append("\"");
                    sb.Append(arg);
                    sb.Append("\"");
                }
                else
                {
                    sb.Append(arg);
                }
            }

            return sb.ToString();
        }
    }
}
