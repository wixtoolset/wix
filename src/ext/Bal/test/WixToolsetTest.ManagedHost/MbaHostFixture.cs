// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using WixBuildTools.TestSupport;
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
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading managed bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "FullFramework2BA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
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
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading managed bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "FullFramework4BA",
                    "Shutdown,ReloadBootstrapper,0",
                }, result.Output.ToArray());
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
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading managed bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "FullFramework2BA",
                    "Shutdown,ReloadBootstrapper,0",
                    "Loading managed bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "FullFramework2BA",
                    "Shutdown,Restart,0",
                }, result.Output.ToArray());
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
                WixAssert.CompareLineByLine(new[]
                {
                    "Loading managed bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "FullFramework4BA",
                    "Shutdown,ReloadBootstrapper,0",
                    "Loading managed bootstrapper application.",
                    "Creating BA thread to run asynchronously.",
                    "FullFramework4BA",
                    "Shutdown,Restart,0",
                }, result.Output.ToArray());
            }
        }
    }
}
