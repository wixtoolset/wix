// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using WixBuildTools.TestSupport;
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
