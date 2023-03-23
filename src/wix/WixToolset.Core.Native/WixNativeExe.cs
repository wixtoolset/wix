// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    internal class WixNativeExe
    {
        private const string WixNativeExeFileName = "wixnative.exe";
        private static string PathToWixNativeExe;

        private readonly string commandLine;
        private readonly List<string> stdinLines = new List<string>();

        public WixNativeExe(params object[] args)
        {
            this.commandLine = String.Join(" ", QuoteArgumentsAsNecesary(args));
        }

        public void AddStdinLine(string line)
        {
            this.stdinLines.Add(line);
        }

        public void AddStdinLines(IEnumerable<string> lines)
        {
            this.stdinLines.AddRange(lines);
        }

        public IReadOnlyCollection<string> Run()
        {
            EnsurePathToWixNativeExeSet();

            var wixNativeInfo = new ProcessStartInfo(PathToWixNativeExe, this.commandLine)
            {
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false
            };

            var outputLines = new List<string>();

            using (var process = Process.Start(wixNativeInfo))
            {
                process.OutputDataReceived += (s, a) => { if (a.Data != null) { outputLines.Add(a.Data); } };
                process.ErrorDataReceived += (s, a) => { if (a.Data != null) { outputLines.Add(a.Data); } };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Send the stdin preamble.
                process.StandardInput.WriteLine(":");

                if (this.stdinLines.Count > 0)
                {
                    foreach (var line in this.stdinLines)
                    {
                        var bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
                        process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                    }

                    // Trailing blank line indicates stdin complete.
                    process.StandardInput.WriteLine();
                }

                // If the process successfully exits documentation says we need to wait again
                // without a timeout to ensure that all of the redirected output is captured.
                //
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw WixNativeException.FromOutputLines(process.ExitCode, outputLines);
                }
            }

            return outputLines;
        }

        private static void EnsurePathToWixNativeExeSet()
        {
            if (String.IsNullOrEmpty(PathToWixNativeExe))
            {
                var result = typeof(WixNativeExe).Assembly.FindFileRelativeToAssembly(WixNativeExeFileName, searchNativeDllDirectories: true);

                if (!result.Found)
                {
                    throw new PlatformNotSupportedException(
                        $"Could not find platform specific '{WixNativeExeFileName}'",
                        new FileNotFoundException($"Could not find internal piece of WiX Toolset from: {result.PossiblePaths}", WixNativeExeFileName));
                }

                PathToWixNativeExe = result.Path;
            }
        }

        private static IEnumerable<string> QuoteArgumentsAsNecesary(object[] args)
        {
            foreach (var arg in args)
            {
                if (arg is string str)
                {
                    if (String.IsNullOrEmpty(str))
                    {
                    }
                    else if (str.Contains(" ") && !str.StartsWith("\""))
                    {
                        // Escape a trailing backslash with another backslash if quoting the path.
                        if (str.EndsWith("\\", StringComparison.Ordinal))
                        {
                            str += "\\";
                        }

                        yield return $"\"{str}\"";
                    }
                    else
                    {
                        yield return str;
                    }
                }
                else if (arg is int i)
                {
                    yield return i.ToString();
                }
                else
                {
                    throw new ArgumentException(nameof(args));
                }
            }
        }
    }
}
