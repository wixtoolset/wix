// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixBuildTools.TestSupport;
    using WixToolset.Core;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class PayloadFixture
    {
        [Fact]
        public void CanParseValidName()
        {
            var folder = TestData.Get(@"TestData\Payload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ValidName.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();

                Assert.Empty(result.Messages);

                var intermediate = Intermediate.Load(wixlibPath);
                var allSymbols = intermediate.Sections.SelectMany(s => s.Symbols);
                var payloadSymbol = allSymbols.OfType<WixBundlePayloadSymbol>()
                                              .SingleOrDefault();
                Assert.NotNull(payloadSymbol);

                var fields = payloadSymbol.Fields.Select(field => field?.Type == IntermediateFieldType.Bool
                                                        ? field.AsNullableNumber()?.ToString()
                                                        : field?.AsString())
                                                .ToList();
                Assert.Equal(@"dir\file.ext", fields[(int)WixBundlePayloadSymbolFields.Name]);
            }
        }

        [Fact]
        public void CanCanonicalizeName()
        {
            var folder = TestData.Get(@"TestData\Payload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "build",
                    Path.Combine(folder, "CanonicalizeName.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();

                Assert.Single(result.Messages, m => m.Id == (int)WarningMessages.Ids.PathCanonicalized);

                var intermediate = Intermediate.Load(wixlibPath);
                var allSymbols = intermediate.Sections.SelectMany(s => s.Symbols);
                var payloadSymbol = allSymbols.OfType<WixBundlePayloadSymbol>()
                                              .SingleOrDefault();
                Assert.NotNull(payloadSymbol);

                var fields = payloadSymbol.Fields.Select(field => field?.Type == IntermediateFieldType.Bool
                                                        ? field.AsNullableNumber()?.ToString()
                                                        : field?.AsString())
                                                .ToList();
                Assert.Equal(@"c\d.exe", fields[(int)WixBundlePayloadSymbolFields.Name]);
            }
        }

        [Fact]
        public void RejectsAbsoluteName()
        {
            var folder = TestData.Get(@"TestData\Payload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "AbsoluteName.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.InRange(result.ExitCode, 2, int.MaxValue);

                var expectedIllegalRelativeLongFileName = 1;
                var expectedPayloadMustBeRelativeToCache = 2;
                Assert.Equal(expectedIllegalRelativeLongFileName, result.Messages.Where(m => m.Id == (int)ErrorMessages.Ids.IllegalRelativeLongFilename).Count());
                Assert.Equal(expectedPayloadMustBeRelativeToCache, result.Messages.Where(m => m.Id == (int)ErrorMessages.Ids.PayloadMustBeRelativeToCache).Count());
            }
        }

        [Fact]
        public void RejectsPayloadSharedBetweenPackageAndBA()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Payload", "SharedBAAndPackagePayloadBundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                Assert.Equal((int)LinkerErrors.Ids.PayloadSharedWithBA, result.ExitCode);
            }
        }

        [Fact]
        public void ReplacesDownloadUrlPlaceholders()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "Payload", "DownloadUrlPlaceholdersBundle.wxs"),
                    Path.Combine(folder, "SimpleBundle", "MultiFileBootstrapperApplication.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                result.AssertSuccess();

                WixAssert.CompareLineByLine(new string[]
                {
                    "The Payload 'burn.exe' is being added to Container 'PackagesContainer', overriding its Compressed value of 'no'.",
                }, result.Messages.Select(m => m.ToString()).ToArray());

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "Container", new List<string> { "FileSize", "Hash" } },
                    { "Payload", new List<string> { "FileSize", "Hash" } },
                };
                var payloads = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Payload")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<Payload Id='burn.exe' FilePath='burn.exe' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a0' Container='PackagesContainer' />",
                    "<Payload Id='test.msi' FilePath='test.msi' FileSize='*' Hash='*' DownloadUrl='http://example.com/id/test.msi/test.msi' Packaging='external' SourcePath='test.msi' />",
                    "<Payload Id='LayoutOnlyPayload' FilePath='DownloadUrlPlaceholdersBundle.wxs' FileSize='*' Hash='*' LayoutOnly='yes' DownloadUrl='http://example.com/id/LayoutOnlyPayload/DownloadUrlPlaceholdersBundle.wxs' Packaging='external' SourcePath='DownloadUrlPlaceholdersBundle.wxs' />",
                   @"<Payload Id='fhuZsOcBDTuIX8rF96kswqI6SnuI' FilePath='MsiPackage\test.txt' FileSize='*' Hash='*' Packaging='external' SourcePath='MsiPackage\test.txt' />",
                   @"<Payload Id='faf_OZ741BG7SJ6ZkcIvivZ2Yzo8' FilePath='MsiPackage\Shared.dll' FileSize='*' Hash='*' Packaging='external' SourcePath='MsiPackage\Shared.dll' />",
                }, payloads);

                var containers = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Container")
                                              .Cast<XmlElement>()
                                              .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                              .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<Container Id='PackagesContainer' FileSize='*' Hash='*' DownloadUrl='http://example.com/id/PackagesContainer/packages.cab' FilePath='packages.cab' />",
                }, containers);
            }
        }
    }
}
