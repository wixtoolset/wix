// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MSBuild
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using Xunit;

    public class MsbuildFixture
    {
        private static readonly string WixTargetsPath = Path.Combine(new Uri(typeof(MsbuildFixture).Assembly.CodeBase).AbsolutePath, "..", "..", "publish", "WixToolset.MSBuild", "tools", "wix.targets");

        [Fact]
        public void CanBuildSimpleBundle()
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "SimpleBundle");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "SimpleBundle.wixproj");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                });
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.TrimStart().StartsWith("wix.exe build -platform x86"));
                Assert.Single(platformSwitches);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Equal(new[]
                {
                    @"bin\x86\Debug\SimpleBundle.exe",
                    @"bin\x86\Debug\SimpleBundle.wixpdb",
                }, paths);
            }
        }

        [Fact]
        public void CanBuildSimpleMsiPackage()
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                });
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.TrimStart().StartsWith("wix.exe build -platform x86"));
                Assert.Single(platformSwitches);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Equal(4, warnings.Count());

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Equal(new[]
                {
                    @"bin\x86\Debug\en-US\cab1.cab",
                    @"bin\x86\Debug\en-US\MsiPackage.msi",
                    @"bin\x86\Debug\en-US\MsiPackage.wixpdb",
                }, paths);
            }
        }

        [Fact]
        public void CanBuildWithDefaultAndExplicitlyFullWixpdbs()
        {
            var expectedOutputs = new[]
                {
                    @"bin\x86\Debug\en-US\cab1.cab",
                    @"bin\x86\Debug\en-US\MsiPackage.msi",
                    @"bin\x86\Debug\en-US\MsiPackage.wixpdb",
                };

            this.AssertWixpdb(null, expectedOutputs);
            this.AssertWixpdb("Full", expectedOutputs);
        }

        [Fact]
        public void CanBuildWithNoWixpdb()
        {
            this.AssertWixpdb("NONE", new[]
                {
                    @"bin\x86\Debug\en-US\cab1.cab",
                    @"bin\x86\Debug\en-US\MsiPackage.msi",
                });
        }

        private void AssertWixpdb(string wixpdbType, string[] expectedOutputFiles)
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    wixpdbType == null ? String.Empty : $"-p:WixPdbType={wixpdbType}",
                    $"-p:WixTargetsPath={WixTargetsPath}",
                });
                result.AssertSuccess();

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Equal(expectedOutputFiles, paths);
            }
        }

        [Fact]
        public void CanBuild64BitMsiPackage()
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:InstallerPlatform=x64",
                });
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.TrimStart().StartsWith("wix.exe build -platform x64"));
                Assert.Single(platformSwitches);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Equal(new[]
                {
                    @"bin\x86\Debug\en-US\cab1.cab",
                    @"bin\x86\Debug\en-US\MsiPackage.msi",
                    @"bin\x86\Debug\en-US\MsiPackage.wixpdb",
                }, paths);
            }
        }

        [Fact(Skip = "Currently fails")]
        public void CanBuildSimpleMsiPackageWithIceSuppressions()
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    "-p:SuppressIces=\"ICE45;ICE46\""
                });
                result.AssertSuccess();
            }
        }

        [Fact]
        public void CanBuildSimpleMsiPackageWithWarningSuppressions()
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    "-p:SuppressSpecificWarnings=\"1118;1102\""
                });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);
            }
        }

        [Fact]
        public void CanBuildSimpleMsiPackageAsWixipl()
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    "-p:OutputType=IntermediatePostLink"
                });
                result.AssertSuccess();

                var path = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .Single();
                Assert.Equal(@"bin\x86\Debug\MsiPackage.wixipl", path);
            }
        }

        [Fact]
        public void CanBuildAndCleanSimpleMsiPackage()
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                // Build
                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    "-v:diag"
                });
                result.AssertSuccess();

                var buildOutput = String.Join("\r\n", result.Output);

                var createdPaths = Directory.EnumerateFiles(baseFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.NotEmpty(createdPaths);

                // Clean
                result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    "-t:Clean",
                    "-v:diag"
                });
                result.AssertSuccess();

                var cleanOutput = String.Join("\r\n", result.Output);

                // Clean is only expected to delete the files listed in {Project}.FileListAbsolute.txt,
                // so this is not quite right but close enough.
                var allowedFiles = new HashSet<string>
                {
                    "MsiPackage.wixproj",
                    "Package.en-us.wxl",
                    "Package.wxs",
                    "PackageComponents.wxs",
                    @"data\test.txt",
                    @"obj\x86\Debug\MsiPackage.wixproj.FileListAbsolute.txt",
                };

                var remainingPaths = Directory.EnumerateFiles(baseFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .Where(s => !allowedFiles.Contains(s))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Empty(remainingPaths);
            }
        }
    }
}
