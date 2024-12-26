// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BootstrapperApplications
{
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class InternalUIBAFixture
    {
        [Fact]
        public void CanBuildUsingWixIuiBa()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "SinglePrimaryPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balPackageInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixBalPackageInfo");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixBalPackageInfo PackageId='test.msi' PrimaryPackageType='default' />",
                }, balPackageInfos);

                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.thm")));
                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.wxl")));
            }
        }

        [Fact]
        public void CanBuildUsingWixIuiBaWithUrlPrereqPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "UrlPrereqPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balPackageInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixBalPackageInfo");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixBalPackageInfo PackageId='test.msi' PrimaryPackageType='default' />",
                }, balPackageInfos);

                var mbaPrereqInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPrereqInformation");
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixPrereqInformation PackageId='wixnative.exe' LicenseUrl='https://www.mysite.com/prereqterms' />",
                }, mbaPrereqInfos);

                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.thm")));
                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.wxl")));
            }
        }

        [Fact]
        public void CanBuildUsingWixIuiBaWithImplicitPrimaryPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "ImplicitPrimaryPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balPackageInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixBalPackageInfo");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixBalPackageInfo PackageId='test.msi' PrimaryPackageType='default' />",
                }, balPackageInfos);

                var mbaPrereqInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPrereqInformation");
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixPrereqInformation PackageId='wixnative.exe' />",
                }, mbaPrereqInfos);

                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.thm")));
                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.wxl")));
            }
        }

        [Fact]
        public void CanBuildUsingWixIuiBaWithWarnings()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var compileResult = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "IuiBaWarnings.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "WixInternalUIBootstrapperApplication does not support the value of 'force' for Cache on prereq packages. Prereq packages are only cached when they need to be installed.",
                    "WixInternalUIBootstrapperApplication ignores InstallCondition for the primary package so that the MSI UI is always shown.",
                    "WixInternalUIBootstrapperApplication ignores DisplayInternalUICondition for the primary package so that the MSI UI is always shown.",
                    "WixInternalUIBootstrapperApplication ignores DisplayFilesInUseDialogCondition for the primary package so that the MSI UI is always shown.",
                    "When using WixInternalUIBootstrapperApplication, all prereq packages should be before the primary package in the chain. The prereq packages are always installed before the primary package.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balPackageInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixBalPackageInfo");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixBalPackageInfo PackageId='test.msi' DisplayInternalUICondition='DISPLAYTEST' DisplayFilesInUseDialogCondition='DISPLAYTEST' PrimaryPackageType='default' />",
                }, balPackageInfos);

                var mbaPrereqInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPrereqInformation");
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixPrereqInformation PackageId='wixnative.exe' />",
                }, mbaPrereqInfos);

                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.thm")));
                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.wxl")));
            }
        }

        [Fact]
        public void CanBuildUsingWixIuiBaAndForcedCachePrimaryPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var compileResult = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "CanForceCachePrimaryPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var wixPackageProperties = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixPackageProperties");
                AssertCacheType(wixPackageProperties[0]);
                AssertCacheType(wixPackageProperties[1]);

                var balPackageInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixBalPackageInfo");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixBalPackageInfo PackageId='test.msi' PrimaryPackageType='default' />",
                }, balPackageInfos);

                var mbaPrereqInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPrereqInformation");
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixPrereqInformation PackageId='wixnative.exe' />",
                }, mbaPrereqInfos);

                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.thm")));
                Assert.True(File.Exists(Path.Combine(baFolderPath, "wixpreq.wxl")));
            }

            static void AssertCacheType(XmlNode node)
            {
                var element = node as XmlElement;
                var package = element?.GetAttribute("Package");
                var cache = element?.GetAttribute("Cache");

                if (package == "test.msi")
                {
                    Assert.Equal("force", cache);
                }
                else if (package == "wixnative.exe")
                {
                    Assert.Equal("keep", cache);
                }
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithAllPrereqPackages()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "AllPrereqPackages.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication, there must be one package with bal:PrimaryPackageType=\"default\".",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6808, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithImplicitNonMsiPrimaryPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "ImplicitNonMsiPrimaryPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication, packages must either be non-permanent and have the bal:PrimaryPackageType attribute, or be permanent and have the bal:PrereqPackage attribute set to 'yes'.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6811, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithImplicitPrimaryPackageEnableFeatureSelection()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "ImplicitPrimaryPackageEnableFeatureSelection.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication, packages must either be non-permanent and have the bal:PrimaryPackageType attribute, or be permanent and have the bal:PrereqPackage attribute set to 'yes'.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6811, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithMultipleNonPermanentNonPrimaryPackages()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "MultipleNonPermanentNonPrimaryPackages.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication, packages must either be non-permanent and have the bal:PrimaryPackageType attribute, or be permanent and have the bal:PrereqPackage attribute set to 'yes'.",
                    "When using WixInternalUIBootstrapperApplication, packages must either be non-permanent and have the bal:PrimaryPackageType attribute, or be permanent and have the bal:PrereqPackage attribute set to 'yes'.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6811, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithMultiplePrimaryPackagesOfSameType()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "MultipleDefaultPrimaryPackages.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "There may only be one package in the bundle with PrimaryPackageType of 'default'.",
                    "The location of the package related to the previous error.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6810, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithNoDefaultPrimaryPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "NoDefaultPrimaryPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication, there must be one package with bal:PrimaryPackageType=\"default\".",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6808, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithNonMsiPrimaryPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "NonMsiPrimaryPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication, each primary package must be an MsiPackage.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6814, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithNonPermanentPrereqPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "NonPermanentPrereqPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication and bal:PrereqPackage is set to 'yes', the package must be permanent.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6812, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithPermanentPrimaryPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "PermanentPrimaryPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication, packages with the bal:PrimaryPackageType attribute must not be permanent.",
                    "When using WixInternalUIBootstrapperApplication, there must be one package with bal:PrimaryPackageType=\"default\".",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6808, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithPrimaryPackageEnableFeatureSelection()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "PrimaryPackageEnableFeatureSelection.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", TestData.Get(@"TestData\WixStdBa\Data"),
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "When using WixInternalUIBootstrapperApplication, primary packages must not have feature selection enabled because it interferes with the user selecting feature through the MSI UI.",
                    "When using WixInternalUIBootstrapperApplication, there must be one package with bal:PrimaryPackageType=\"default\".",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(6808, compileResult.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildUsingWixIuiBaWithPrimaryPrereqPackage()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var wixlibFile = Path.Combine(baseFolder, "bin", "test.wixlib");
                var bundleSourceFolder = TestData.Get(@"TestData\WixIuiBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "PrimaryPrereqPackage.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The MsiPackage/@PrereqPackage attribute's value, 'yes', cannot be specified with attribute PrimaryPackageType present.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());

                Assert.Equal(193, compileResult.ExitCode);
            }
        }
    }
}
