// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Data;
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class HarvestFilesFixture
    {
        [Fact]
        public void MustIncludeSomeFiles()
        {
            var messages = BuildAndQueryComponentAndFileTables("BadAuthoring.wxs", isPackage: true, 10);
            Assert.Equal(new[]
            {
                "10",
            }, messages);
        }

        [Fact]
        public void ZeroFilesHarvestedIsAWarning()
        {
            var messages = BuildAndQueryComponentAndFileTables("ZeroFiles.wxs", isPackage: true, 8600);
            Assert.Equal(new[]
            {
                "8600",
            }, messages);
        }

        [Fact]
        public void MissingHarvestDirectoryIsAWarning()
        {
            var messages = BuildAndQueryComponentAndFileTables("BadDirectory.wxs", isPackage: true, 8601);
            Assert.Equal(new[]
            {
                "8601",
                "8601",
            }, messages);
        }

        [Fact]
        public void DuplicateFilesSomethingSomething()
        {
            var messages = BuildAndQueryComponentAndFileTables("DuplicateFiles.wxs", isPackage: true, 8602);
            Assert.Equal(new[]
            {
                "8602",
                "8602",
                "8602",
                "8602",
            }, messages);
        }

        [Fact]
        public void CanHarvestFilesInComponentGroup()
        {
            BuildQueryAssertFiles("ComponentGroup.wxs",  new[]
            {
                "FileName.Extension",
                "test20.txt",
                "test21.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        [Fact]
        public void CanHarvestFilesInDirectory()
        {
            BuildQueryAssertFiles("Directory.wxs",  new[]
            {
                "test10.txt",
                "test120.txt",
                "test2.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        [Fact]
        public void CanHarvestFilesInDirectoryRef()
        {
            BuildQueryAssertFiles("DirectoryRef.wxs", new[]
            {
                "notatest.txt",
                "pleasedontincludeme.dat",
                "test1.txt",
                "test120.txt",
                "test2.txt",
                "test20.txt",
                "test21.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        [Fact]
        public void CanHarvestFilesInFeature()
        {
            var rows = BuildAndQueryComponentAndFileTables("Feature.wxs");

            AssertFileComponentIds(3, rows);
        }

        [Fact]
        public void CanHarvestFilesInFeatureGroup()
        {
            BuildQueryAssertFiles("FeatureGroup.wxs", new[]
            {
                "FileName.Extension",
                "notatest.txt",
                "pleasedontincludeme.dat",
                "test1.txt",
                "test2.txt",
                "test20.txt",
                "test21.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        [Fact]
        public void CanHarvestFilesInFeatureRef()
        {
            BuildQueryAssertFiles("FeatureRef.wxs", new[]
            {
                "FileName.Extension",
                "notatest.txt",
                "pleasedontincludeme.dat",
                "test1.txt",
                "test2.txt",
                "test20.txt",
                "test21.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        [Fact]
        public void CanHarvestFilesInFragments()
        {
            BuildQueryAssertFiles("Fragment.wxs", new[]
            {
                "notatest.txt",
                "test1.txt",
                "test2.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        [Fact]
        public void CanHarvestFilesInModules()
        {
            BuildQueryAssertFiles("Module.wxs", new[]
            {
                "notatest.txt",
                "test1.txt",
                "test2.txt",
                "test3.txt",
                "test4.txt",
            }, isPackage: false);
        }

        [Fact]
        public void CanHarvestFilesWithBindPaths()
        {
            BuildQueryAssertFiles("BindPaths.wxs", new[]
            {
                "FileName.Extension",
                "test1.txt",
                "test10.txt",
                "test120.txt",
                "test2.txt",
                "test20.txt",
                "test21.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        [Fact]
        public void HarvestedFilesUnderPackageWithAuthoredFeatureAreOrphaned()
        {
            var messages = BuildAndQueryComponentAndFileTables("PackageWithoutDefaultFeature.wxs", isPackage: true, 267);
            Assert.Equal(new[]
            {
                "267",
                "267",
                "267",
                "267",
            }, messages);
        }

        [Fact]
        public void CanHarvestFilesInStandardDirectory()
        {
            BuildQueryAssertFiles("StandardDirectory.wxs", new[]
            {
                "FileName.Extension",
                "notatest.txt",
                "pleasedontincludeme.dat",
                "test1.txt",
                "test10.txt",
                "test120.txt",
                "test2.txt",
                "test20.txt",
                "test21.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        [Fact]
        public void CanHarvestFilesInFiveLines()
        {
            BuildQueryAssertFiles("PackageFiveLiner.wxs", new[]
            {
                "FileName.Extension",
                "notatest.txt",
                "pleasedontincludeme.dat",
                "test20.txt",
                "test21.txt",
                "test3.txt",
                "test4.txt",
            });
        }

        private static void BuildQueryAssertFiles(string file, string[] expectedFileNames, bool isPackage = true, int? exitCode = null)
        {
            var rows = BuildAndQueryComponentAndFileTables(file, isPackage, exitCode);

            var fileNames = AssertFileComponentIds(expectedFileNames.Length, rows);

            Assert.Equal(expectedFileNames, fileNames);
        }

        private static string[] BuildAndQueryComponentAndFileTables(string file, bool isPackage = true, int? exitCode = null)
        {
            var folder = TestData.Get("TestData", "HarvestFiles");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var msiPath = Path.Combine(binFolder, isPackage ? "test.msi" : "test.msm");

                var arguments = new[]
                {
                    "build",
                    Path.Combine(folder, file),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", folder,
                    "-bindpath", @$"ToBeHarvested={folder}\files1",
                    "-bindpath", @$"ToBeHarvested={folder}\files2",
                    "-o", msiPath,
                };

                var result = WixRunner.Execute(arguments);

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

        private static string[] AssertFileComponentIds(int fileCount, string[] rows)
        {
            var componentRows = rows.Where(row => row.StartsWith("Component:")).ToArray();
            var fileRows = rows.Where(row => row.StartsWith("File:")).ToArray();

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

            Assert.Equal(fileCount, componentRows.Length);

            var files = fileRows.Select(row => row.Split('\t')[2]);
            var lfns = files.Select(name => name.Split('|'));

            return fileRows
                .Select(row => row.Split('\t')[2])
                .Select(GetLFN)
                .OrderBy(name => name).ToArray();

            static string GetLFN(string possibleSfnLfnPair)
            {
                var parts = possibleSfnLfnPair.Split('|');
                return parts[parts.Length - 1];
            }
        }
    }
}
