// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
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

                var result = WixRunner.Execute(new[]
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
    }
}
