// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public class MsbuildRunner
    {
        private static readonly string VswhereRelativePath = @"Microsoft Visual Studio\Installer\vswhere.exe";
        private static readonly string[] VswhereFindArguments = new[] { "-property", "installationPath", "-latest" };
        private static readonly string Msbuild15RelativePath = @"MSBuild\15.0\bin\MSBuild.exe";

        public MsbuildRunner()
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

            this.Msbuild15Path = Path.Combine(result.Output[0], Msbuild15RelativePath);
            if (!File.Exists(this.Msbuild15Path))
            {
                throw new InvalidOperationException($"Failed to find MSBuild v15 at: {this.Msbuild15Path}");
            }
        }

        private string Msbuild15Path { get; }

        public MsbuildRunnerResult Execute(string projectPath, string[] arguments = null)
        {
            var total = new List<string>
            {
                projectPath
            };

            if (arguments != null)
            {
                total.AddRange(arguments);
            }

            var workingFolder = Path.GetDirectoryName(projectPath);
            return RunProcessCaptureOutput(this.Msbuild15Path, total.ToArray(), workingFolder);
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
                process.OutputDataReceived += (s, e) => { if (e.Data != null) output.Add(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) output.Add(e.Data); };

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
