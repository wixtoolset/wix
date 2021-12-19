// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixE2E
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using Xunit;

    public class WixE2EFixture
    {
        [Fact]
        public void CanBuildWixlibWithNativeDll()
        {
            var projectPath = TestData.Get("TestData", "WixprojLibraryVcxprojDll", "WixprojLibraryVcxprojDll.wixproj");

            CleanEverything();

            var result = RestoreAndBuild(projectPath);
            result.AssertSuccess();
        }

        [Fact]
        public void CanBuildModuleWithWinFormsApp()
        {
            var projectPath = TestData.Get("TestData", "WixprojModuleCsprojWinFormsNetFx", "WixprojModuleCsprojWinFormsNetFx.wixproj");

            CleanEverything();

            var result = RestoreAndBuild(projectPath);
            result.AssertSuccess();
        }

        [Fact]
        public void CanBuildPackageWithWebApp()
        {
            var projectPath = TestData.Get("TestData", "WixprojPackageCsprojWebApplicationNetCore", "WixprojPackageCsprojWebApplicationNetCore.wixproj");

            CleanEverything();

            var result = RestoreAndBuild(projectPath);
            result.AssertSuccess();
        }

        [Fact]
        public void CanBuildPackageWithNativeWindowsApp()
        {
            var projectPath = TestData.Get("TestData", "WixprojPackageVcxprojWindowsApp", "WixprojPackageVcxprojWindowsApp.wixproj");

            CleanEverything();

            var result = RestoreAndBuild(projectPath);
            result.AssertSuccess();
        }

        private static void CleanEverything()
        {
            var rootFolder = TestData.Get("TestData");
            var deleteFolders = new[] { "Debug", "bin", "obj" };

            foreach (var projectFolder in Directory.GetDirectories(rootFolder))
            {
                foreach (var deleteFolder in deleteFolders)
                {
                    var folder = Path.Combine(projectFolder, deleteFolder);

                    if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder, true);
                    }
                }
            }
        }

        private static MsbuildRunnerResult RestoreAndBuild(string projectPath, bool x64 = true)
        {
            return MsbuildRunner.Execute(projectPath, new[] { "-Restore", "-v:m", "-bl" }, x64);
        }
    }
}
