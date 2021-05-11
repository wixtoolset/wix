// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System.Collections.Generic;
    using System.IO;
    using WixBuildTools.TestSupport;
    using Xunit;
    using Xunit.Abstractions;

    public class LayoutTests : BurnE2ETests
    {
        public LayoutTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanLayoutBundleInPlaceWithMissingPayloads()
        {
            var bundleA = this.CreateBundleInstaller("BundleA");
            var webServer = this.CreateWebServer();

            webServer.AddFiles(new Dictionary<string, string>
            {
                { "/BundleA/LayoutOnlyPayload", Path.Combine(this.TestContext.TestDataFolder, "BundleA.wxs") },
                { "/BundleA/packages.cab", Path.Combine(this.TestContext.TestDataFolder, "packages.cab") },
            });
            webServer.Start();

            using var dfs = new DisposableFileSystem();
            var layoutDirectory = dfs.GetFolder(true);

            // Manually copy bundle to layout directory and then run from there so the non-compressed payloads have to be resolved.
            var bundleAFileInfo = new FileInfo(bundleA.Bundle);
            var bundleACopiedPath = Path.Combine(layoutDirectory, bundleAFileInfo.Name);
            bundleAFileInfo.CopyTo(bundleACopiedPath);

            bundleA.Layout(bundleACopiedPath, layoutDirectory);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(File.Exists(bundleACopiedPath));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, "packages.cab")));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, "BundleA.wxs")));
        }

        [Fact]
        public void CanLayoutBundleToNewDirectory()
        {
            var bundleA = this.CreateBundleInstaller("BundleA");
            var webServer = this.CreateWebServer();

            webServer.AddFiles(new Dictionary<string, string>
            {
                { "/BundleA/LayoutOnlyPayload", Path.Combine(this.TestContext.TestDataFolder, "BundleA.wxs") },
                { "/BundleA/packages.cab", Path.Combine(this.TestContext.TestDataFolder, "packages.cab") },
            });
            webServer.Start();

            using var dfs = new DisposableFileSystem();
            var layoutDirectory = dfs.GetFolder();

            bundleA.Layout(layoutDirectory);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(File.Exists(Path.Combine(layoutDirectory, "BundleA.exe")));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, "packages.cab")));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, "BundleA.wxs")));
        }
    }
}
