// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System.Collections.Generic;
    using System.IO;
    using WixInternal.TestSupport;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class LayoutTests : BurnE2ETests
    {
        public LayoutTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
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

        [RuntimeFact]
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

        [RuntimeFact]
        public void CanSkipOverCorruptLocalFileForDownloadableFile()
        {
            var bundleB = this.CreateBundleInstaller("BundleB");
            var webServer = this.CreateWebServer();

            webServer.AddFiles(new Dictionary<string, string>
            {
                { "/BundleB/PackageA.msi", Path.Combine(this.TestContext.TestDataFolder, "PackageA.msi") },
            });
            webServer.Start();

            using var dfs = new DisposableFileSystem();
            var baseDirectory = dfs.GetFolder(true);
            var sourceDirectory = Path.Combine(baseDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            var layoutDirectory = Path.Combine(baseDirectory, "layout");
            Directory.CreateDirectory(layoutDirectory);

            // Manually copy bundle to empty directory and then run from there so it can't find the uncorrupted file.
            var bundleBFileInfo = new FileInfo(bundleB.Bundle);
            var bundleBCopiedPath = Path.Combine(sourceDirectory, bundleBFileInfo.Name);
            bundleBFileInfo.CopyTo(bundleBCopiedPath);
            
            // Copy a corrupted version of PackageA.msi next to the bundle.
            var packageAFileInfo = new FileInfo(Path.Combine(bundleBFileInfo.DirectoryName, "PackageA.msi"));
            var packageACorruptedFileInfo = new FileInfo(Path.Combine(sourceDirectory, packageAFileInfo.Name));
            packageAFileInfo.CopyTo(packageACorruptedFileInfo.FullName);
            SubtlyCorruptFile(packageACorruptedFileInfo);

            var layoutLogPath = bundleB.Layout(bundleBCopiedPath, layoutDirectory);
            bundleB.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(File.Exists(Path.Combine(layoutDirectory, bundleBFileInfo.Name)));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, packageAFileInfo.Name)));
            Assert.True(LogVerifier.MessageInLogFile(layoutLogPath, "Verification failed on payload group item: PackageA"));
        }

        [RuntimeFact]
        public void CanSkipOverCorruptLocalFileForOtherLocalFile()
        {
            var bundleA = this.CreateBundleInstaller("BundleA");
            var testBAController = this.CreateTestBAController();

            using var dfs = new DisposableFileSystem();
            var baseDirectory = dfs.GetFolder(true);
            var sourceDirectory = Path.Combine(baseDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);
            var layoutDirectory = Path.Combine(baseDirectory, "layout");
            Directory.CreateDirectory(layoutDirectory);

            // Copy a corrupted version of packages.cab and BundleA.wxs.
            var bundleAFileInfo = new FileInfo(bundleA.Bundle);
            var packagesCabFileInfo = new FileInfo(Path.Combine(bundleAFileInfo.DirectoryName, "packages.cab"));
            var packagesCabCorruptedFileInfo = new FileInfo(Path.Combine(sourceDirectory, packagesCabFileInfo.Name));
            packagesCabFileInfo.CopyTo(packagesCabCorruptedFileInfo.FullName);
            SubtlyCorruptFile(packagesCabCorruptedFileInfo);

            var layoutOnlyPayloadFileInfo = new FileInfo(Path.Combine(bundleAFileInfo.DirectoryName, "BundleA.wxs"));
            var layoutOnlyPayloadCorruptedFileInfo = new FileInfo(Path.Combine(sourceDirectory, layoutOnlyPayloadFileInfo.Name));
            layoutOnlyPayloadFileInfo.CopyTo(layoutOnlyPayloadCorruptedFileInfo.FullName);
            SubtlyCorruptFile(layoutOnlyPayloadCorruptedFileInfo);

            // Set the source to absolute path so the engine tries the corrupted files first.
            testBAController.SetContainerInitialLocalSource("PackagesContainer", packagesCabCorruptedFileInfo.FullName);
            testBAController.SetPayloadInitialLocalSource("LayoutOnlyPayload", layoutOnlyPayloadCorruptedFileInfo.FullName);

            var layoutLogPath = bundleA.Layout(layoutDirectory);
            bundleA.VerifyUnregisteredAndRemovedFromPackageCache();

            Assert.True(File.Exists(Path.Combine(layoutDirectory, bundleAFileInfo.Name)));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, packagesCabFileInfo.Name)));
            Assert.True(File.Exists(Path.Combine(layoutDirectory, "BundleA.wxs")));
            Assert.True(LogVerifier.MessageInLogFile(layoutLogPath, "Verification failed on container: PackagesContainer"));
            Assert.True(LogVerifier.MessageInLogFile(layoutLogPath, "Verification failed on payload group item: LayoutOnlyPayload"));
        }

        private static void SubtlyCorruptFile(FileInfo fileInfo)
        {
            using (var v = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite))
            {
                // Change one byte of information in the middle of the file to corrupt it.
                // Hopefully this ensures that these tests will continue to work even if optimizations are added later,
                // such as checking the file header, product code, or product version during acquisition.
                var bytePosition = v.Length / 2;
                v.Position = bytePosition;
                var byteValue = v.ReadByte();
                byteValue = byteValue == 0 ? 1 : 0;
                v.Position = bytePosition;
                v.WriteByte((byte)byteValue);
            }
        }
    }
}
