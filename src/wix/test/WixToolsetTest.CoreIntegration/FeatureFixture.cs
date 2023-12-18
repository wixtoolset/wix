// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using Xunit;

    public class FeatureFixture
    {
        [Fact]
        public void CanDetectMissingFeatureComponentMapping()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Feature", "PackageMissingFeatureComponentMapping.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                var messages = result.Messages.Select(m => m.ToString()).ToList();
                messages.Sort();

                WixAssert.CompareLineByLine(new[]
                {
                    "Found orphaned Component 'filit6MyH46zIGKsPPPXDZDfeNrfVY'. If this is a Package, every Component must have at least one parent Feature. To include a Component in a Module, you must include it directly as a Component element of the Module element or indirectly via ComponentRef, ComponentGroup, or ComponentGroupRef elements.",
                }, messages.ToArray());

                Assert.Equal(267, result.ExitCode);
            }
        }

        [Fact]
        public void CanAutomaticallyCreateDefaultFeature()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Feature", "PackageDefaultFeature.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                Assert.Empty(result.Messages);

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Feature", "FeatureComponents", "Shortcut" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Feature:WixDefaultFeature\t\t\t\t0\t1\t\t0",
                    "FeatureComponents:WixDefaultFeature\tAnotherComponentInAFragment",
                    "FeatureComponents:WixDefaultFeature\tComponentInAFragment",
                    "FeatureComponents:WixDefaultFeature\tfil6J6CHYPBCOMYclNjnqn0afimmzM",
                    "FeatureComponents:WixDefaultFeature\tfilcV1yrx0x8wJWj4qMzcH21jwkPko",
                    "FeatureComponents:WixDefaultFeature\tfilj.cb0sFWqIPHPFSKJSEEaPDuAQ4",
                    "Shortcut:AdvertisedShortcut\tINSTALLFOLDER\tShortcut\tAnotherComponentInAFragment\tWixDefaultFeature\t\t\t\t\t\t\t\t\t\t\t",
                }, results);
            }
        }

        [Fact]
        public void WontAutomaticallyCreateDefaultFeature()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Feature", "PackageBadDefaultFeature.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                var messages = result.Messages.Select(m => m.ToString()).ToList();
                messages.Sort();

                WixAssert.CompareLineByLine(new[]
                {
                    "Found orphaned Component 'fil6J6CHYPBCOMYclNjnqn0afimmzM'. If this is a Package, every Component must have at least one parent Feature. To include a Component in a Module, you must include it directly as a Component element of the Module element or indirectly via ComponentRef, ComponentGroup, or ComponentGroupRef elements.",
                    "Found orphaned Component 'filcV1yrx0x8wJWj4qMzcH21jwkPko'. If this is a Package, every Component must have at least one parent Feature. To include a Component in a Module, you must include it directly as a Component element of the Module element or indirectly via ComponentRef, ComponentGroup, or ComponentGroupRef elements.",
                    "Found orphaned Component 'filj.cb0sFWqIPHPFSKJSEEaPDuAQ4'. If this is a Package, every Component must have at least one parent Feature. To include a Component in a Module, you must include it directly as a Component element of the Module element or indirectly via ComponentRef, ComponentGroup, or ComponentGroupRef elements.",
                }, messages.ToArray());

                Assert.Equal(267, result.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildMsiWithTooLargeFeatureDepth()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Feature", "PackageWithExcessiveFeatureDepth.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                Assert.Equal(7503, result.ExitCode);

                var errors = result.Messages.Where(m => m.Level == MessageLevel.Error);
                Assert.Equal(new[]
                {
                    7503
                }, errors.Select(e => e.Id).ToArray());
                WixAssert.CompareLineByLine(new[]
                {
                    "Maximum depth of the Feature tree allowed in an MSI was exceeded. An MSI does not support a Feature tree with depth greater than 16. The Feature 'Depth17' is at depth 17.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
            }
        }
    }
}
