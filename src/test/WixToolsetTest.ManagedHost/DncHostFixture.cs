// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core FDD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("EarliestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core SCD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("EarliestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core SCD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("EarliestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core SCD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("EarliestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
                Assert.Equal("Loading .NET Core SCD bootstrapper application.", logMessages[4]);
                Assert.Equal("Reloaded 1 time(s)", logMessages[5]); // dnchost doesn't currently support unloading
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[6]);
                Assert.Equal("EarliestCoreBA", logMessages[7]);
                Assert.Equal("Shutdown,Restart,0", logMessages[8]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core FDD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("LatestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core FDD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("LatestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
                Assert.Equal("Loading .NET Core FDD bootstrapper application.", logMessages[4]);
                Assert.Equal("Reloaded 1 time(s)", logMessages[5]); // dnchost doesn't currently support unloading
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[6]);
                Assert.Equal("LatestCoreBA", logMessages[7]);
                Assert.Equal("Shutdown,Restart,0", logMessages[8]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core SCD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("LatestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core SCD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("LatestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core SCD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("LatestCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
                Assert.Equal("Loading .NET Core SCD bootstrapper application.", logMessages[4]);
                Assert.Equal("Reloaded 1 time(s)", logMessages[5]); // dnchost doesn't currently support unloading
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[6]);
                Assert.Equal("LatestCoreBA", logMessages[7]);
                Assert.Equal("Shutdown,Restart,0", logMessages[8]);
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
                var logMessages = result.Output;
                Assert.Equal("Loading .NET Core FDD bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("WPFCoreBA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
            }
        }
    }
}
