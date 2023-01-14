// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixE2E
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using WixInternal.TestSupport;
    using Xunit;

    public class WixE2EFixture
    {
        [Fact]
        public void CanBuildWixlibMultiFramework()
        {
            var projectPath = TestData.Get("TestData", "WixprojLibraryMultiFramework", "WixprojLibraryMultiFramework.wixproj");

            CleanEverything();

            var result = RestoreAndBuild(projectPath);
            result.AssertSuccess();
        }

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

            var signingStatement = result.Output.Where(s => s.Contains("warning :"))
                                                .Select(s => s.Replace(Path.GetDirectoryName(projectPath), "<projectFolder>").Replace(@"\Debug\", @"\<configuration>\").Replace(@"\Release\", @"\<configuration>\"))
                                                .ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                @"<projectFolder>\WixprojPackageVcxprojWindowsApp.wixproj(18,5): warning : SignMsi = obj\<configuration>\en-US\WixprojPackageVcxprojWindowsApp.msi;obj\<configuration>\ja-JP\WixprojPackageVcxprojWindowsApp.msi"
            }, signingStatement);
        }

        [Fact]
        public void CanIncrementalBuildPackageWithNativeWindowsAppWithNoEdits()
        {
            var projectDirectory = TestData.Get("TestData", "WixprojPackageVcxprojWindowsApp");
            var projectPath = Path.Combine(projectDirectory, "WixprojPackageVcxprojWindowsApp.wixproj");
            var projectBinPath = Path.Combine(projectDirectory, "bin");

            CleanEverything();

            var result = RestoreAndBuild(projectPath);
            result.AssertSuccess();

            var firstBuiltFiles = Directory.GetFiles(projectBinPath, "*.*", SearchOption.AllDirectories).ToArray();
            var firstHashes = firstBuiltFiles.Select(s => $"{s.Substring(projectBinPath.Length).TrimStart('\\')} with hash: {GetFileHash(s)}").ToArray();

            // This should be an incremental build and not do any work.
            //
            result = RestoreAndBuild(projectPath);
            result.AssertSuccess();

            var secondBuiltFiles = Directory.GetFiles(projectBinPath, "*.*", SearchOption.AllDirectories).ToArray();
            var secondHashes = secondBuiltFiles.Select(s => $"{s.Substring(projectBinPath.Length).TrimStart('\\')} with hash: {GetFileHash(s)}").ToArray();

            WixAssert.CompareLineByLine(firstHashes, secondHashes);
        }

        [Fact]
        public void CanIncrementalBuildPackageWithNativeWindowsAppWithEdits()
        {
            var projectDirectory = TestData.Get("TestData", "WixprojPackageVcxprojWindowsApp");
            var projectPath = Path.Combine(projectDirectory, "WixprojPackageVcxprojWindowsApp.wixproj");
            var projectBinPath = Path.Combine(projectDirectory, "bin");

            CleanEverything();

            var result = RestoreAndBuild(projectPath);
            result.AssertSuccess();

            var firstBuiltFiles = Directory.GetFiles(projectBinPath, "*.*", SearchOption.AllDirectories).ToArray();
            var firstRelativePaths = firstBuiltFiles.Select(s => $"{s.Substring(projectBinPath.Length).TrimStart('\\')}").ToArray();
            var firstHashes = firstBuiltFiles.Select(s => $"{s.Substring(projectBinPath.Length).TrimStart('\\')} with hash: {GetFileHash(s)}").ToArray();

            var packageWxsPath = Path.Combine(projectDirectory, "Package.wxs");
            File.SetLastWriteTime(packageWxsPath, DateTime.Now);

            // This should be an incremental build that does work because a file was updated.
            //
            result = RestoreAndBuild(projectPath);
            result.AssertSuccess();

            var secondBuiltFiles = Directory.GetFiles(projectBinPath, "*.*", SearchOption.AllDirectories).ToArray();
            var secondRelativePaths = secondBuiltFiles.Select(s => $"{s.Substring(projectBinPath.Length).TrimStart('\\')}").ToArray();
            var secondHashes = secondBuiltFiles.Select(s => $"{s.Substring(projectBinPath.Length).TrimStart('\\')} with hash: {GetFileHash(s)}").ToArray();

            WixAssert.CompareLineByLine(firstRelativePaths, secondRelativePaths);
            Assert.NotEqual(firstHashes, secondHashes);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanBuildPackageWithHarvesting(bool x64)
        {
            var projectPath = TestData.Get("TestData", "WixprojPackageHarvesting", "WixprojPackageHarvesting.wixproj");

            CleanEverything();

            var result = RestoreAndBuild(projectPath, x64);
            result.AssertSuccess();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanBuildPackageWithHeatDir(bool x64)
        {
            var projectPath = TestData.Get("TestData", "WixprojPackageHeatDir", "WixprojPackageHeatDir.wixproj");

            CleanEverything();

            var result = RestoreAndBuild(projectPath, x64);
            result.AssertSuccess();
        }

        [Fact(Skip = "Investigate if .NET Core WebApplications can be incrementally built")]
        public void CanIncrementalBuildPackageWithNetCoreWebAppWithoutEdits()
        {
            var projectDirectory = TestData.Get("TestData", "WixprojPackageCsprojWebApplicationNetCore");
            var projectPath = Path.Combine(projectDirectory, "WixprojPackageCsprojWebApplicationNetCore.wixproj");
            var projectBinPath = Path.Combine(projectDirectory, "bin");

            CleanEverything();

            var result = RestoreAndBuild(projectPath);
            result.AssertSuccess();

            var firstBuiltFiles = Directory.GetFiles(projectBinPath, "*.*", SearchOption.AllDirectories).ToArray();
            var firstHashes = firstBuiltFiles.Select(s => $"{s.Substring(projectBinPath.Length).TrimStart('\\')} with hash: {GetFileHash(s)}").ToArray();

            //var packageWxsPath = Path.Combine(projectDirectory, "Package.wxs");
            //File.SetLastWriteTime(packageWxsPath, DateTime.Now);

            // This should be an incremental build that does work because a file was updated.
            //
            result = RestoreAndBuild(projectPath);
            result.AssertSuccess();

            var secondBuiltFiles = Directory.GetFiles(projectBinPath, "*.*", SearchOption.AllDirectories).ToArray();
            var secondHashes = secondBuiltFiles.Select(s => $"{s.Substring(projectBinPath.Length).TrimStart('\\')} with hash: {GetFileHash(s)}").ToArray();

            WixAssert.CompareLineByLine(firstHashes, secondHashes);
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

            foreach (var logFile in Directory.GetFiles(rootFolder, "*.binlog", SearchOption.AllDirectories))
            {
                File.Delete(logFile);
            }
        }

        private static string GetFileHash(string path)
        {
            using (var sha2 = SHA256.Create())
            {
                var bytes = File.ReadAllBytes(path);
                var hashBytes = sha2.ComputeHash(bytes);

                var sb = new StringBuilder();
                foreach (var hash in hashBytes)
                {
                    sb.AppendFormat("{0:X}", hash);
                }

                return sb.ToString();
            }
        }

        private static MsbuildRunnerResult RestoreAndBuild(string projectPath, bool x64 = true, bool suppressValidation = true)
        {
            return MsbuildRunner.Execute(projectPath, new[] { "-Restore", "-v:m", "-bl", $"-p:SuppressValidation={suppressValidation}" }, x64);
        }
    }
}
