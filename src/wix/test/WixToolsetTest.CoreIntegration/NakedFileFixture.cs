// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Data;
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class NakedFileFixture
    {
        [Fact]
        public void CanBuildNakedFilesInComponentGroup()
        {
            var rows = BuildAndQueryComponentAndFileTables("ComponentGroup.wxs");

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesInFeature()
        {
            var rows = BuildAndQueryComponentAndFileTables("Feature.wxs");

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesInDirectory()
        {
            var rows = BuildAndQueryComponentAndFileTables("Directory.wxs");

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesInDirectoryRef()
        {
            var rows = BuildAndQueryComponentAndFileTables("DirectoryRef.wxs");

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesInFeatureRef()
        {
            var rows = BuildAndQueryComponentAndFileTables("FeatureRef.wxs");

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesInFeatureGroup()
        {
            var rows = BuildAndQueryComponentAndFileTables("FeatureGroup.wxs");

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesInFragments()
        {
            var rows = BuildAndQueryComponentAndFileTables("Fragment.wxs");

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesInStandardDirectory()
        {
            var rows = BuildAndQueryComponentAndFileTables("StandardDirectory.wxs");

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesInModule()
        {
            var rows = BuildAndQueryComponentAndFileTables("Module.wxs", isPackage: false);

            AssertFileComponentIds(2, rows);
        }

        [Fact]
        public void CanBuildNakedFilesWithConditions()
        {
            var rows = BuildAndQueryComponentAndFileTables("Condition.wxs");
            var componentRows = rows.Where(row => row.StartsWith("Component:")).ToArray();

            // Coincidentally, the files' ids are the same as the component conditions.
            foreach (var componentRow in componentRows)
            {
                var columns = componentRow.Split(':', '\t');
                Assert.Equal(columns[1], columns[5]);
            }
        }

        [Fact]
        public void CanBuildNakedFilesUnderPackage()
        {
            var rows = BuildAndQueryComponentAndFileTables("Package.wxs");
            AssertFileComponentIds(4, rows);
        }

        [Fact]
        public void CanBuildNakedFilesUnderPackageWithDefaultInstallFolder()
        {
            var rows = BuildAndQueryComponentAndFileTables("PackageWithDefaultInstallFolder.wxs");
            AssertFileComponentIds(4, rows);
        }

        [Fact]
        public void NakedFilesUnderPackageWithAuthoredFeatureAreOrphaned()
        {
            var messages = BuildAndQueryComponentAndFileTables("PackageWithoutDefaultFeature.wxs", isPackage: true, 267);
            Assert.Equal(new[]
            {
                "267",
                "267",
            }, messages);
        }

        [Fact]
        public void IllegalAttributesWhenNonNakedFailTheBuild()
        {
            var messages = BuildAndQueryComponentAndFileTables("BadAttributes.wxs", isPackage: true, 62);
            Assert.Equal(new[]
            {
                "62",
                "62",
                "62",
                "62",
            }, messages);
        }

        [Fact]
        public void CanBuildNakedFileFromWixlibComponentGroup()
        {
            var rows = BuildPackageWithWixlib("WixlibComponentGroup.wxs", "WixlibComponentGroupPackage.wxs");

            AssertFileComponentIds(2, rows);
        }

        private static string[] BuildPackageWithWixlib(string wixlibSourcePath, string msiSourcePath)
        {
            var folder = TestData.Get("TestData", "NakedFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var wixlibPath = Path.Combine(binFolder, Path.ChangeExtension(wixlibSourcePath, ".wixlib"));

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, wixlibSourcePath),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", folder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();

                var msiPath = Path.Combine(binFolder, "test.msi");

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, msiSourcePath),
                    wixlibPath,
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", folder,
                    "-o", msiPath,
                });
                result.AssertSuccess();

                return Query.QueryDatabase(msiPath, new[] { "Component", "File" })
                    .OrderBy(s => s)
                    .ToArray();
            }
        }

        private static string[] BuildAndQueryComponentAndFileTables(string file, bool isPackage = true, int? exitCode = null)
        {
            var folder = TestData.Get("TestData", "NakedFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var msiPath = Path.Combine(binFolder, isPackage ? "test.msi" : "test.msm");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, file),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", folder,
                    "-o", msiPath,
                });

                if (exitCode.HasValue)
                {
                    Assert.Equal(exitCode.Value, result.ExitCode);

                    return result.Messages.Select(m => m.Id.ToString()).ToArray();
                }
                else
                {
                    result.AssertSuccess();

                    return Query.QueryDatabase(msiPath, new[] { "Component", "File" })
                        .OrderBy(s => s)
                        .ToArray();
                }
            }
        }

        private static void AssertFileComponentIds(int fileCount, string[] rows)
        {
            var componentRows = rows.Where(row => row.StartsWith("Component:")).ToArray();
            var fileRows = rows.Where(row => row.StartsWith("File:")).ToArray();

            Assert.Equal(fileCount, componentRows.Length);
            Assert.Equal(componentRows.Length, fileRows.Length);

            // Component id == Component keypath == File id
            foreach (var componentRow in componentRows)
            {
                var columns = componentRow.Split(':', '\t');
                Assert.Equal(columns[1], columns[6]);
            }

            foreach (var fileRow in fileRows)
            {
                var columns = fileRow.Split(':', '\t');
                Assert.Equal(columns[1], columns[2]);
            }
        }
    }
}
