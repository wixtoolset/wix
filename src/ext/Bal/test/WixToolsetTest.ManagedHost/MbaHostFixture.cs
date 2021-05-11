// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class MbaHostFixture
    {
        static readonly string bundleBasePath = TestData.Get("..", "examples");

        [Fact]
        public void CanLoadFullFramework2MBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "FullFramework2Bundle.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                var logMessages = result.Output;
                Assert.Equal("Loading managed bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("FullFramework2BA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
            }
        }

        [Fact]
        public void CanLoadFullFramework4MBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "FullFramework4Bundle.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunShutdownEngine(bundleFile, baseFolder);
                var logMessages = result.Output;
                Assert.Equal("Loading managed bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("FullFramework4BA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
            }
        }

        [Fact]
        public void CanReloadFullFramework2MBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "FullFramework2Bundle.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunReloadEngine(bundleFile, baseFolder);
                var logMessages = result.Output;
                Assert.Equal("Loading managed bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("FullFramework2BA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
                Assert.Equal("Loading managed bootstrapper application.", logMessages[4]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[5]);
                Assert.Equal("FullFramework2BA", logMessages[6]);
                Assert.Equal("Shutdown,Restart,0", logMessages[7]);
            }
        }

        [Fact]
        public void CanReloadFullFramework4MBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = TestData.Get(bundleBasePath, "FullFramework4Bundle.exe");
                var testEngine = new TestEngine();

                var result = testEngine.RunReloadEngine(bundleFile, baseFolder);
                var logMessages = result.Output;
                Assert.Equal("Loading managed bootstrapper application.", logMessages[0]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[1]);
                Assert.Equal("FullFramework4BA", logMessages[2]);
                Assert.Equal("Shutdown,ReloadBootstrapper,0", logMessages[3]);
                Assert.Equal("Loading managed bootstrapper application.", logMessages[4]);
                Assert.Equal("Creating BA thread to run asynchronously.", logMessages[5]);
                Assert.Equal("FullFramework4BA", logMessages[6]);
                Assert.Equal("Shutdown,Restart,0", logMessages[7]);
            }
        }
    }
}
