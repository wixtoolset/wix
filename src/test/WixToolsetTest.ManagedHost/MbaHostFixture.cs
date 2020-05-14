// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class MbaHostFixture
    {
        [Fact]
        public void CanLoadFullFramework2MBA()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundleFile = Path.Combine(binFolder, "FullFramework2MBA.exe");
                var baSourceFolder = TestData.Get(@"..\examples");
                var bundleSourceFolder = TestData.Get(@"TestData\FullFramework2MBA");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-ext", TestData.Get(@"WixToolset.NetFx.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", baSourceFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();
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
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundleFile = Path.Combine(binFolder, "FullFramework4MBA.exe");
                var baSourceFolder = TestData.Get(@"..\examples");
                var bundleSourceFolder = TestData.Get(@"TestData\FullFramework4MBA");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-ext", TestData.Get(@"WixToolset.NetFx.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", baSourceFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();
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
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundleFile = Path.Combine(binFolder, "FullFramework2MBA.exe");
                var baSourceFolder = TestData.Get(@"..\examples");
                var bundleSourceFolder = TestData.Get(@"TestData\FullFramework2MBA");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-ext", TestData.Get(@"WixToolset.NetFx.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", baSourceFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();
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
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundleFile = Path.Combine(binFolder, "FullFramework4MBA.exe");
                var baSourceFolder = TestData.Get(@"..\examples");
                var bundleSourceFolder = TestData.Get(@"TestData\FullFramework4MBA");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-ext", TestData.Get(@"WixToolset.NetFx.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", baSourceFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();
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
