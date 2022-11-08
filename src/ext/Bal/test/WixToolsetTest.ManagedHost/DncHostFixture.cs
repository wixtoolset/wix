// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using System;
    using WixInternal.TestSupport;
    using WixInternal.TestSupport.XunitExtensions;
    using Xunit;

    public class DncHostFixture
    {
        static readonly string bundleBasePath = TestData.Get("..", "examples");

        [Fact]
        public void CanLoadFDDEarliestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "EarliestCoreBundleFDD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core FDD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "EarliestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
            }
        }

        [SkippableFact]
        public void CanLoadFDDx86EarliestCoreMBA()
        {
            // https://github.com/microsoft/vstest/issues/3586
            Environment.SetEnvironmentVariable("DOTNET_ROOT", null);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "EarliestCoreBundleFDDx86.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder, x86: true);
                var resultOutput = result.Output.ToArray();

                if (resultOutput.Length > 0 && (resultOutput[0] == "error from hostfxr: It was not possible to find any compatible framework version" ||
                    resultOutput[0] == "error from hostfxr: You must install or update .NET to run this application."))
                {
                    WixAssert.Skip(String.Join(Environment.NewLine, resultOutput));
                }

                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core FDD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "EarliestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, resultOutput);
            }
        }

        [Fact]
        public void CanLoadSCDEarliestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "EarliestCoreBundleSCD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core SCD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "EarliestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
            }
        }

        [Fact]
        public void CanLoadTrimmedSCDEarliestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "EarliestCoreBundleTrimmedSCD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core SCD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "EarliestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
            }
        }

        [Fact]
        public void CanReloadSCDEarliestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "EarliestCoreBundleSCD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunReloadEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core SCD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "EarliestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                    "Loading .NET Core SCD bootstrapper application.",
                    "Reloaded 1 time(s)", // dnchost doesn't currently support unloading
                    "Creating BA thread to run asynchronously.",
                    "EarliestCoreBA",
                    "Shutdown,Restart,0",

                }, result.Output.ToArray());
            }
        }

        [Fact]
        public void CanLoadFDDLatestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "LatestCoreBundleFDD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core FDD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "LatestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
            }
        }

        [Fact]
        public void CanLoadFDDx86LatestCoreMBA()
        {
            // https://github.com/microsoft/vstest/issues/3586
            Environment.SetEnvironmentVariable("DOTNET_ROOT", null);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "LatestCoreBundleFDDx86.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder, x86: true);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core FDD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "LatestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
            }
        }

        [Fact]
        public void CanReloadFDDLatestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "LatestCoreBundleFDD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunReloadEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core FDD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "LatestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                    "Loading .NET Core FDD bootstrapper application.",
                    "Reloaded 1 time(s)", // dnchost doesn't currently support unloading
                    "Creating BA thread to run asynchronously.",
                    "LatestCoreBA",
                    "Shutdown,Restart,0",
                }, result.Output.ToArray());
            }
        }

        [Fact]
        public void CanLoadSCDLatestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "LatestCoreBundleSCD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core SCD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "LatestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
                var logMessages = result.Output;
            }
        }

        [Fact]
        public void CanLoadTrimmedSCDLatestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "LatestCoreBundleTrimmedSCD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core SCD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "LatestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
            }
        }

        [Fact]
        public void CanReloadSCDLatestCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "LatestCoreBundleSCD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunReloadEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core SCD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "LatestCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                    "Loading .NET Core SCD bootstrapper application.",
                    "Reloaded 1 time(s)", // dnchost doesn't currently support unloading
                    "Creating BA thread to run asynchronously.",
                    "LatestCoreBA",
                    "Shutdown,Restart,0",
                }, result.Output.ToArray());
            }
        }

        [Fact]
        public void CanLoadFDDWPFCoreMBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "WPFCoreBundleFDD.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading .NET Core FDD bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "WPFCoreBA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
            }
        }
    }
}
