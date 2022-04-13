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

    public class RollbackBoundaryFixture
    {
        [Fact]
        public void CanStartChainWithRollbackBoundary()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "RollbackBoundary", "BeginningOfChain.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
            }
        }

        [Fact]
        public void CannotHaveRollbackBoundaryAndChainPackageWithSameId()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "RollbackBoundary", "SharedIdWithPackage.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                Assert.Equal(92, result.ExitCode);

                WixAssert.CompareLineByLine(new[]
                {
                    "Duplicate symbol 'WixChainItem:collision' found. This typically means that an Id is duplicated. Access modifiers (internal, protected, private) cannot prevent these conflicts. Ensure all your identifiers of a given type (File, Component, Feature) are unique.",
                    "Location of symbol related to previous error.",
                }, result.Messages.Select(m => m.ToString()).ToArray());

                Assert.False(File.Exists(exePath));
            }
        }

        [Fact]
        public void DiscardsConsecutiveRollbackBoundaries()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "RollbackBoundary", "ConsecutiveRollbackBoundaries.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                WixAssert.CompareLineByLine(new[]
                {
                    "The RollbackBoundary 'Second' was discarded because it was not followed by a package. Without a package the rollback boundary doesn't do anything. Verify that the RollbackBoundary element is not followed by another RollbackBoundary and that the element is not at the end of the chain.",
                    "Location of rollback boundary related to previous warning.",
                }, result.Messages.Select(m => m.ToString()).ToArray());

                Assert.True(File.Exists(exePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, exePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var rollbackBoundaries = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:RollbackBoundary")
                                                      .Cast<XmlElement>()
                                                      .Select(e => e.GetTestXml())
                                                      .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<RollbackBoundary Id='First' Vital='yes' Transaction='no' />",
                }, rollbackBoundaries);

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "MsiPackage", new List<string> { "Size" } },
                };
                var chainPackages = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/*")
                                                      .Cast<XmlElement>()
                                                      .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                                      .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<MsiPackage Id='test.msi' Cache='keep' CacheId='{040011E1-F84C-4927-AD62-50A5EC19CA32}v1.0.0.0' InstallSize='34' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='First' RollbackBoundaryBackward='First' LogPathVariable='WixBundleLog_test.msi' RollbackLogPathVariable='WixBundleRollbackLog_test.msi' ProductCode='{040011E1-F84C-4927-AD62-50A5EC19CA32}' Language='1033' Version='1.0.0.0' UpgradeCode='{047730A5-30FE-4A62-A520-DA9381B8226A}'>" +
                    "<MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />" +
                    "<MsiProperty Id='MSIFASTINSTALL' Value='7' />" +
                    "<Provides Key='{040011E1-F84C-4927-AD62-50A5EC19CA32}_v1.0.0.0' Version='1.0.0.0' DisplayName='MsiPackage' />" +
                    "<RelatedPackage Id='{047730A5-30FE-4A62-A520-DA9381B8226A}' MaxVersion='1.0.0.0' MaxInclusive='no' OnlyDetect='no' LangInclusive='yes'><Language Id='1033' /></RelatedPackage>" +
                    "<RelatedPackage Id='{047730A5-30FE-4A62-A520-DA9381B8226A}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='yes'><Language Id='1033' /></RelatedPackage>" +
                    "<PayloadRef Id='test.msi' />" +
                    "<PayloadRef Id='fhuZsOcBDTuIX8rF96kswqI6SnuI' />" +
                    "<PayloadRef Id='faf_OZ741BG7SJ6ZkcIvivZ2Yzo8' />" +
                    "</MsiPackage>",
                }, chainPackages);
            }
        }
    }
}
