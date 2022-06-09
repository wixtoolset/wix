// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class ExePackageFixture
    {
        [Fact]
        public void CanBuildWithArpEntry()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ExePackage", "ArpEntry.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>>
                {
                    { "ExePackage", new List<string> { "CacheId", "Size" } },
                };
                var exePackages = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:ExePackage")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributes))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<ExePackage Id='burn.exe' Cache='keep' CacheId='*' InstallSize='463360' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_burn.exe' RollbackLogPathVariable='WixBundleRollbackLog_burn.exe' InstallArguments='-install' RepairArguments='-repair' Repairable='yes' DetectionType='arp' ArpId='id' ArpDisplayVersion='1.0.0.0'>" +
                      "<PayloadRef Id='burn.exe' />" +
                    "</ExePackage>",
                }, exePackages);
            }
        }

        [Fact]
        public void WarningWhenInvalidArpEntryVersion()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "ExePackage", "InvalidArpEntryVersion.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "Invalid WixVersion '1.0.0.abc' in ArpEntry/@'Version'. Comparisons may yield unexpected results."
                }, result.Messages.Select(m => m.ToString()).ToArray());
                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>>
                {
                    { "ExePackage", new List<string> { "CacheId", "Size" } },
                };
                var exePackages = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:ExePackage")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributes))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<ExePackage Id='burn.exe' Cache='keep' CacheId='*' InstallSize='463360' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_burn.exe' RollbackLogPathVariable='WixBundleRollbackLog_burn.exe' InstallArguments='-install' RepairArguments='' Repairable='no' DetectionType='arp' ArpId='id' ArpDisplayVersion='1.0.0.abc'>" +
                      "<PayloadRef Id='burn.exe' />" +
                    "</ExePackage>",
                }, exePackages);
            }
        }

        [Fact]
        public void WarningWhenPermanentWithoutDetectConditionOrUninstallArgumentsOrArpEntry()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "ExePackage", "PermanentWithoutDetectConditionOrUninstallArgumentsOrArpEntry.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage/@DetectCondition attribute or child element ArpEntry is recommended so the package is only installed when absent."
                }, result.Messages.Select(m => m.ToString()).ToArray());
                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>>
                {
                    { "ExePackage", new List<string> { "CacheId", "Size" } },
                };
                var exePackages = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:ExePackage")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributes))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<ExePackage Id='burn.exe' Cache='keep' CacheId='*' InstallSize='463360' Size='*' PerMachine='yes' Permanent='yes' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_burn.exe' RollbackLogPathVariable='WixBundleRollbackLog_burn.exe' InstallArguments='-install' RepairArguments='' Repairable='no' DetectionType='none'>" +
                      "<PayloadRef Id='burn.exe' />" +
                    "</ExePackage>",
                }, exePackages);
            }
        }

        [Fact]
        public void NoWarningWhenPermanentWithEmptyDetectCondition()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ExePackage", "PermanentWithEmptyDetectCondition.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>>
                {
                    { "ExePackage", new List<string> { "CacheId", "Size" } },
                };
                var exePackages = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:ExePackage")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributes))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<ExePackage Id='burn.exe' Cache='keep' CacheId='*' InstallSize='463360' Size='*' PerMachine='yes' Permanent='yes' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_burn.exe' RollbackLogPathVariable='WixBundleRollbackLog_burn.exe' InstallArguments='-install' RepairArguments='' Repairable='no' DetectionType='none'>" +
                      "<PayloadRef Id='burn.exe' />" +
                    "</ExePackage>",
                }, exePackages);
            }
        }

        [Fact]
        public void ErrorWhenArpEntryWithDetectCondition()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ArpEntryWithDetectCondition.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage element cannot have a child element 'ArpEntry' when attribute 'DetectCondition' is set.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(372, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenArpEntryWithUninstallArguments()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ArpEntryWithUninstallArguments.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage element cannot have a child element 'ArpEntry' when attribute 'UninstallArguments' is set.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(372, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenArpEntryWithInvalidId()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "InvalidArpEntryId.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ArpEntry/@Id attribute's value, '..\\id', is not a valid filename because it contains illegal characters. Legal filenames contain no more than 260 characters and must contain at least one non-period character. Any character except for the follow may be used: \\ ? | > < : / * \".",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(27, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenNonPermanentWithOnlyDetectCondition()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "NonPermanentWithOnlyDetectCondition.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage element's UninstallArguments attribute was not found; it is required without attribute Permanent present.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(408, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenNonPermanentWithOnlyUninstallArguments()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "NonPermanentWithOnlyUninstallArguments.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage element's DetectCondition attribute was not found; it is required without attribute Permanent present.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(408, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenRepairablePermanentWithoutDetectCondition()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "RepairablePermanentWithoutDetectCondition.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage/@DetectCondition attribute is required to have a value when attribute RepairArguments is present.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(401, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenRepairablePermanentWithoutDetectConditionOrUninstallArgumentsOrArpEntry()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "RepairablePermanentWithoutDetectConditionOrUninstallArgumentsOrArpEntry.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "Element 'ExePackage' missing attribute 'DetectCondition' or child element 'ArpEntry'. Exactly one of those is required when attribute 'RepairArguments' is specified.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(413, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenNonPermanentWithoutDetectConditionOrUninstallArgumentsOrArpEntry()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "NonPermanentWithoutDetectConditionOrUninstallArgumentsOrArpEntry.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "Element 'ExePackage' missing attribute 'DetectCondition' or child element 'ArpEntry'. Exactly one of those is required when attribute 'Permanent' is not specified.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(414, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenRepairConditionWithoutRepairArguments()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "RepairConditionWithoutRepairArguments.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage/@RepairArguments attribute is required to have a value when attribute RepairCondition is present.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(401, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenUninstallArgumentsWithoutDetectCondition()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "UninstallArgumentsWithoutDetectCondition.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage/@DetectCondition attribute is required to have a value when attribute UninstallArguments is present.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(401, result.ExitCode);
            }
        }
    }
}
