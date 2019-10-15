// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class WixlibQueryFixture
    {
        [Fact(Skip = "Test demonstrates failure")]
        public void DetectOnlyUpgradeProducesReferenceToRemoveExistingProducts()
        {
            var folder = TestData.Get(@"TestData\Upgrade");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "DetectOnly.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(wixlibPath);
                var allTuples = intermediate.Sections.SelectMany(s => s.Tuples);
                var wixSimpleRefTuples = allTuples.OfType<WixSimpleReferenceTuple>();
                var repRef = wixSimpleRefTuples.Where(t => t.Table == "WixAction" &&
                                                           t.PrimaryKeys == "InstallExecuteSequence/RemoveExistingProducts")
                                               .SingleOrDefault();
                Assert.NotNull(repRef);
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
        public void TypeLibLanguageAsStringReturnsZero()
        {
            var folder = TestData.Get(@"TestData\TypeLib");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Language0.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(wixlibPath);
                var allTuples = intermediate.Sections.SelectMany(s => s.Tuples);
                var typeLibTuple = allTuples.OfType<TypeLibTuple>()
                                            .SingleOrDefault();
                Assert.NotNull(typeLibTuple);

                var fields = typeLibTuple.Fields.Select(field => field?.Type == IntermediateFieldType.Bool
                                                        ? field.AsNullableNumber()?.ToString()
                                                        : field?.AsString())
                                                .ToList();
                Assert.Equal("0", fields[1]);
            }
        }
    }
}
