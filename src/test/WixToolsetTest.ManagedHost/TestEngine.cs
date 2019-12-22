// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using WixBuildTools.TestSupport;

    public class TestEngine
    {
        private static readonly string TestEngineFile = TestData.Get(@"..\Win32\examples\Example.TestEngine\Example.TestEngine.exe");

        public TestEngineResult RunShutdownEngine(string baFile)
        {
            var args = new string[] { '"' + baFile + '"' };
            return RunProcessCaptureOutput(TestEngineFile, args);
        }

        private static TestEngineResult RunProcessCaptureOutput(string executablePath, string[] arguments = null, string workingFolder = null)
        {
            var startInfo = new ProcessStartInfo(executablePath)
            {
                Arguments = string.Join(' ', arguments),
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

            return new TestEngineResult
            {
                ExitCode = exitCode,
                Output = output,
            };
        }
    }
}
