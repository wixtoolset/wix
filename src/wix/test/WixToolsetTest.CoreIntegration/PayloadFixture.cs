// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
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
                WixAssert.StringEqual(@"dir\file.ext", fields[(int)WixBundlePayloadSymbolFields.Name]);
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

                Assert.Single(result.Messages, m => m.Id == 1152); // CompilerWarnings.PathCanonicalized

                var intermediate = Intermediate.Load(wixlibPath);
                var allSymbols = intermediate.Sections.SelectMany(s => s.Symbols);
                var payloadSymbol = allSymbols.OfType<WixBundlePayloadSymbol>()
                                              .SingleOrDefault();
                Assert.NotNull(payloadSymbol);

                var fields = payloadSymbol.Fields.Select(field => field?.Type == IntermediateFieldType.Bool
                                                        ? field.AsNullableNumber()?.ToString()
                                                        : field?.AsString())
                                                .ToList();
                WixAssert.StringEqual(@"c\d\e\f.exe", fields[(int)WixBundlePayloadSymbolFields.Name]);
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

                Assert.InRange(result.ExitCode, 2, Int32.MaxValue);

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
                var bundlePath = Path.Combine(baseFolder, @"bin", "test.exe");

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

                var messages = result.Messages.Select(WixMessageFormatter.FormatMessage).OrderBy(m => m).ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "Error 7002: The Payload 'paybrzYNo9tnpTN4NnUuXkoauGUDe8' is shared with the BootstrapperApplication. This is not currently supported.",
                }, messages);
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

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "Container", new List<string> { "FileSize", "Hash" } },
                    { "Payload", new List<string> { "FileSize", "Hash" } },
                };
                var payloads = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Payload", ignoreAttributesByElementName);
                WixAssert.CompareLineByLine(new[]
                {
                    "<Payload Id='burn.exe' FilePath='burn.exe' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a0' Container='PackagesContainer' />",
                    "<Payload Id='test.msi' FilePath='test.msi' FileSize='*' Hash='*' DownloadUrl='http://example.com/id/test.msi/test.msi' Packaging='external' SourcePath='test.msi' />",
                    "<Payload Id='LayoutOnlyPayload' FilePath='DownloadUrlPlaceholdersBundle.wxs' FileSize='*' Hash='*' LayoutOnly='yes' DownloadUrl='http://example.com/id/LayoutOnlyPayload/DownloadUrlPlaceholdersBundle.wxs' Packaging='external' SourcePath='DownloadUrlPlaceholdersBundle.wxs' />",
                   @"<Payload Id='fhuZsOcBDTuIX8rF96kswqI6SnuI' FilePath='MsiPackage\test.txt' FileSize='*' Hash='*' DownloadUrl='http://example.com/test.msiid/fhuZsOcBDTuIX8rF96kswqI6SnuI/MsiPackage/test.txt' Packaging='external' SourcePath='MsiPackage\test.txt' />",
                   @"<Payload Id='faf_OZ741BG7SJ6ZkcIvivZ2Yzo8' FilePath='MsiPackage\Shared.dll' FileSize='*' Hash='*' DownloadUrl='http://example.com/test.msiid/faf_OZ741BG7SJ6ZkcIvivZ2Yzo8/MsiPackage/Shared.dll' Packaging='external' SourcePath='MsiPackage\Shared.dll' />",
                }, payloads);

                var containers = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Container", ignoreAttributesByElementName);
                WixAssert.CompareLineByLine(new[]
                {
                    "<Container Id='PackagesContainer' FileSize='*' Hash='*' DownloadUrl='http://example.com/id/PackagesContainer/packages.cab' FilePath='packages.cab' />",
                }, containers);
            }
        }

        [Fact]
        public void CanBuildBundleWithRemotePackagePaylod()
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
                    Path.Combine(folder, "Payload", "RemotePayloadInPackage.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var payloadElements = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Payload");
                WixAssert.CompareLineByLine(new[]
                {
                    "<Payload Id='payU3zeF5QWZvsWVEfgPVoFiuo65qQ' FilePath='reallybig.dat' FileSize='100000000' Hash='4312abcef' DownloadUrl='example.com/reallybig.dat' Packaging='external' SourcePath='reallybig.dat' />",
                    "<Payload Id='burn.exe' FilePath='burn.exe' FileSize='463360' Hash='F6E722518AC3AB7E31C70099368D5770788C179AA23226110DCF07319B1E1964E246A1E8AE72E2CF23E0138AFC281BAFDE45969204405E114EB20C8195DA7E5E' Packaging='embedded' SourcePath='a0' Container='WixAttachedContainer' />",
                    "<Payload Id='payIswsNTCI1qS7UYl_lydLSgHt2Aw' FilePath='fake.txt' FileSize='1' Hash='bcadef' DownloadUrl='example.com/fake.txt' Packaging='external' SourcePath='fake.txt' />",
                    "<Payload Id='RemotePayloadExe' FilePath='fake.exe' FileSize='1' Hash='a' DownloadUrl='example.com' Packaging='external' SourcePath='fake.exe' />",
                }, payloadElements);

                var payloadRefElements = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Chain/burn:ExePackage/burn:PayloadRef");
                WixAssert.CompareLineByLine(new[]
                {
                    "<PayloadRef Id='burn.exe' />",
                    "<PayloadRef Id='payU3zeF5QWZvsWVEfgPVoFiuo65qQ' />",
                    "<PayloadRef Id='RemotePayloadExe' />",
                    "<PayloadRef Id='payIswsNTCI1qS7UYl_lydLSgHt2Aw' />"
                }, payloadRefElements);
            }
        }

        [Fact]
        public void CannotBuildRemotePayloadInBootstrapperApplication()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, "bin", "test.exe");

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "Payload", "RemotePayloadInBootstrapperApplication.wxs"),
                    Path.Combine(folder, "SimpleBundle", "MultiFileBootstrapperApplication.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "Bootstrapper application and bundle extension payloads must be embedded in the bundle. The payload 'someremotefile.txt' is remote thus cannot be found for embedding. Provide a full path to the payload via the Payload/@SourceFile attribute."
                }, result.Messages.Select(m => m.ToString()).ToArray());
            }
        }
    }
}
