// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;

    public class TestEngine
    {
        private static readonly string TestEngineFile = TestData.Get(@"..\Win32\examples\Example.TestEngine\Example.TestEngine.exe");
        public static readonly string BurnStubFile = TestData.Get(@"runtimes\win-x86\native\burn.x86.exe");

        public TestEngineResult RunShutdownEngine(string bundleFilePath, string tempFolderPath)
        {
            var baFolderPath = Path.Combine(tempFolderPath, "ba");
            var extractFolderPath = Path.Combine(tempFolderPath, "extract");
            var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFilePath, baFolderPath, extractFolderPath);
            extractResult.AssertSuccess();

            var args = new string[] {
                '"' + bundleFilePath + '"',
                '"' + extractResult.GetBAFilePath(baFolderPath) + '"',
            };
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
