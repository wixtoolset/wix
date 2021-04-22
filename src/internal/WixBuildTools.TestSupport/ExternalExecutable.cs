// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public abstract class ExternalExecutable
    {
        private readonly string exePath;

        protected ExternalExecutable(string exePath)
        {
            this.exePath = exePath;
        }

        protected ExternalExecutableResult Run(string args, bool mergeErrorIntoOutput = false, string workingDirectory = null)
        {
            var startInfo = new ProcessStartInfo(this.exePath, args)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(this.exePath),
            };

            using (var process = Process.Start(startInfo))
            {
                // This implementation of merging the streams does not guarantee that lines are retrieved in the same order that they were written.
                // If the process is simultaneously writing to both streams, this is impossible to do anyway.
                var standardOutput = new ConcurrentQueue<string>();
                var standardError = mergeErrorIntoOutput ? standardOutput : new ConcurrentQueue<string>();

                process.ErrorDataReceived += (s, e) => { if (e.Data != null) { standardError.Enqueue(e.Data); } };
                process.OutputDataReceived += (s, e) => { if (e.Data != null) { standardOutput.Enqueue(e.Data); } };

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();

                return new ExternalExecutableResult
                {
                    ExitCode = process.ExitCode,
                    StandardError = mergeErrorIntoOutput ? null : standardError.ToArray(),
                    StandardOutput = standardOutput.ToArray(),
                    StartInfo = startInfo,
                };
            }
        }

        // This is internal because it assumes backslashes aren't used as escape characters and there aren't any double quotes.
        internal static string CombineArguments(IEnumerable<string> arguments)
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
