// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Dtf.WindowsInstaller;
    using Xunit;

    public class LanguageFixture
    {
        [Fact]
        public void CanBuildWithDefaultProductLanguage()
        {
            var folder = TestData.Get(@"TestData", "Language");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var directorySymbols = section.Symbols.OfType<DirectorySymbol>();
                WixAssert.CompareLineByLine(new[]
                {
                    "INSTALLFOLDER:Example Corporation\\MsiPackage",
                    "ProgramFilesFolder:PFiles",
                    "TARGETDIR:SourceDir"
                }, directorySymbols.OrderBy(s => s.Id.Id).Select(s => s.Id.Id + ":" + s.Name).ToArray());

                var propertySymbol = section.Symbols.OfType<PropertySymbol>().Single(p => p.Id.Id == "ProductLanguage");
                Assert.Equal("0", propertySymbol.Value);

                var summaryPlatform = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.PlatformAndLanguage);
                Assert.Equal("Intel;0", summaryPlatform.Value);

                var summaryCodepage = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.Codepage);
                Assert.Equal("1252", summaryCodepage.Value);

                var data = WindowsInstallerData.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var directoryRows = data.Tables["Directory"].Rows;
                WixAssert.CompareLineByLine(
                [
                    "dwGveZhe5wcMbbRyRAkRwm2sqnE4:ProgramFilesFolder:u7-b4gch|Example Corporation",
                    "INSTALLFOLDER:dwGveZhe5wcMbbRyRAkRwm2sqnE4:oekcr5lq|MsiPackage",
                    "ProgramFilesFolder:TARGETDIR:PFiles",
                    "TARGETDIR::SourceDir"
                ], [.. directoryRows.Select(r => r.FieldAsString(0) + ":" + r.FieldAsString(1) + ":" + r.FieldAsString(2))]);
            }
        }

        [Fact]
        public void CanBuildEnuWxl()
        {
            var folder = TestData.Get(@"TestData", "Language");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var propertySymbol = section.Symbols.OfType<PropertySymbol>().Single(p => p.Id.Id == "ProductLanguage");
                Assert.Equal("1033", propertySymbol.Value);

                var summaryPlatform = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.PlatformAndLanguage);
                Assert.Equal("Intel;1033", summaryPlatform.Value);

                var summaryCodepage = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.Codepage);
                Assert.Equal("1252", summaryCodepage.Value);
            }
        }

        [Fact]
        public void CanBuildJpnWxl()
        {
            var folder = TestData.Get(@"TestData", "Language");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.ja-jp.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var propertySymbol = section.Symbols.OfType<PropertySymbol>().Single(p => p.Id.Id == "ProductLanguage");
                Assert.Equal("1041", propertySymbol.Value);

                var summaryPlatform = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.PlatformAndLanguage);
                Assert.Equal("Intel;1041", summaryPlatform.Value);

                var summaryCodepage = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.Codepage);
                Assert.Equal("932", summaryCodepage.Value);
            }
        }

        [Fact]
        public void CanBuildJpnWxlWithEnuSummaryInfo()
        {
            var folder = TestData.Get(@"TestData", "Language");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "PackageWithEnSummaryInfo.ja-jp.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var propertySymbol = section.Symbols.OfType<PropertySymbol>().Single(p => p.Id.Id == "ProductLanguage");
                Assert.Equal("1041", propertySymbol.Value);

                var summaryPlatform = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.PlatformAndLanguage);
                Assert.Equal("Intel;1041", summaryPlatform.Value);

                var summaryCodepage = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.Codepage);
                Assert.Equal("1252", summaryCodepage.Value);
            }
        }

        [Theory]
        [InlineData("de-DE,en-US")]
        [InlineData("de-DE;en-US")]
        public void CanBuildSingleLanguageWithFallbackCultures(string cultureArg)
        {
            var folder = TestData.Get(@"TestData", "Language");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-loc", Path.Combine(folder, "Package.ja-jp.wxl"),
                    "-culture", cultureArg,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var propertySymbol = section.Symbols.OfType<PropertySymbol>().Single(p => p.Id.Id == "ProductLanguage");
                Assert.Equal("1033", propertySymbol.Value);

                var summaryPlatform = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.PlatformAndLanguage);
                Assert.Equal("Intel;1033", summaryPlatform.Value);

                var names = Query.GetSubStorageNames(msiPath);
                Assert.Empty(names);
            }
        }

        [Fact]
        public void CanBuildMultilingualMsiPackage()
        {
            var folder = TestData.Get(@"TestData", "Language");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");
                var wixpdbPath = Path.Combine(baseFolder, @"bin\test.wixpdb");
                var mstPath = Path.Combine(baseFolder, "1041.mst");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-loc", Path.Combine(folder, "Package.ja-jp.wxl"),
                    "-culture", "en-US",
                    "-culture", "ja-JP",
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(wixpdbPath);
                var section = intermediate.Sections.Single();

                var propertySymbol = section.Symbols.OfType<PropertySymbol>().Single(p => p.Id.Id == "ProductLanguage");
                Assert.Equal("1033", propertySymbol.Value);

                var summaryPlatform = section.Symbols.OfType<SummaryInformationSymbol>().Single(s => s.PropertyId == SummaryInformationType.PlatformAndLanguage);
                Assert.Equal("Intel;1033,1041", summaryPlatform.Value);

                var output = WindowsInstallerData.Load(wixpdbPath, false);
                var substorage = Assert.Single(output.SubStorages);
                Assert.Equal("1041", substorage.Name);

                var names = Query.GetSubStorageNames(msiPath);
                Assert.Equal(new[] { "1041" }, names);

                using (var summaryInfo = new SummaryInfo(msiPath, enableWrite: false))
                {
                    Assert.Equal("Intel;1033,1041", summaryInfo.Template);
                }

                Query.ExtractSubStorage(msiPath, "1041", mstPath);

                using (var db = new Database(msiPath, DatabaseOpenMode.Transact))
                {
                    var initialFeatureTitle = db.ExecuteScalar("SELECT `Title` FROM `Feature` WHERE `Feature` = 'ProductFeature'") as string;
                    var initialProductLanguage = db.ExecuteScalar("SELECT `Value` FROM `Property` WHERE `Property` = 'ProductLanguage'") as string;
                    Assert.Equal("MsiPackage en-us", initialFeatureTitle);
                    Assert.Equal("1033", initialProductLanguage);

                    db.ApplyTransform(mstPath);

                    var transformedFeatureTitle = db.ExecuteScalar("SELECT `Title` FROM `Feature` WHERE `Feature` = 'ProductFeature'") as string;
                    var transformedProductLanguage = db.ExecuteScalar("SELECT `Value` FROM `Property` WHERE `Property` = 'ProductLanguage'") as string;
                    Assert.Equal("MsiPackage ja-jp", transformedFeatureTitle);
                    Assert.Equal("1041", transformedProductLanguage);
                }
            }
        }
    }
}
