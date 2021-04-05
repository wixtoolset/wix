// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
                {
                    "d4EceYatXTyy8HXPt5B6DT9Rj.wE:u7-b4gch|Example Corporation",
                    "INSTALLFOLDER:oekcr5lq|MsiPackage",
                    "ProgramFilesFolder:PFiles",
                    "TARGETDIR:SourceDir"
                }, directoryRows.Select(r => r.FieldAsString(0) + ":" + r.FieldAsString(2)).ToArray());
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
    }
}
