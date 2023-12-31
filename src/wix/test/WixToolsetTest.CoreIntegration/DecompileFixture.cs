// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Core;
    using WixToolset.Core.WindowsInstaller;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using Xunit;
    using static NuGet.Packaging.PackagingConstants;

    public class DecompileFixture
    {
        private static void DecompileAndCompare(string msiName, bool extract, string expectedWxsName, params string[] sourceFolder)
        {
            var folder = TestData.Get(sourceFolder);

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var extractPath = Path.Combine(intermediateFolder, "$extracted");
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "msi", "decompile",
                    Path.Combine(folder, msiName),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath,
                    extract ? "-x" : String.Empty,
                    extract ? extractPath : String.Empty,
                }, out var messages);

                Assert.Equal(0, result);
                Assert.Empty(messages);

                if (extract)
                {
                    Assert.NotEmpty(Directory.EnumerateFiles(extractPath, "*", SearchOption.AllDirectories));
                }

                WixAssert.CompareXml(Path.Combine(folder, expectedWxsName), outputPath);
            }
        }

        [Fact]
        public void CanDecompileSingleFileCompressed()
        {
            DecompileAndCompare("example.msi", extract: true, "Expected.wxs", "TestData", "DecompileSingleFileCompressed");
        }

        [Fact]
        public void CanDecompile64BitSingleFileCompressed()
        {
            DecompileAndCompare("example.msi", extract: true, "Expected.wxs", "TestData", "DecompileSingleFileCompressed64");
        }

        [Fact]
        public void CanDecompileNestedDirSearchUnderRegSearch()
        {
            DecompileAndCompare("NestedDirSearchUnderRegSearch.msi", extract: false, "DecompiledNestedDirSearchUnderRegSearch.wxs", "TestData", "AppSearch");
        }

        [Fact]
        public void CanDecompileOldClassTableDefinition()
        {
            // The input MSI was not created using standard methods, it is an example of a real world database that needs to be decompiled.
            // The Class/@Feature_ column has length of 32, the File/@Attributes has length of 2,
            // and numerous foreign key relationships are missing.
            DecompileAndCompare("OldClassTableDef.msi", extract: false, "DecompiledOldClassTableDef.wxs", "TestData", "Class");
        }

        [Fact]
        public void CanDecompileSequenceTables()
        {
            DecompileAndCompare("SequenceTables.msi", extract: false, "DecompiledSequenceTables.wxs", "TestData", "SequenceTables");
        }

        [Fact]
        public void CanDecompileShortcuts()
        {
            DecompileAndCompare("shortcuts.msi", extract: false, "DecompiledShortcuts.wxs", "TestData", "Shortcut");
        }

        [Fact]
        public void CanDecompileNullComponent()
        {
            DecompileAndCompare("example.msi", extract: true, "Expected.wxs", "TestData", "DecompileNullComponent");
        }

        [Fact]
        public void CanDecompileMergeModuleWithTargetDirComponent()
        {
            DecompileAndCompare("MergeModule1.msm", extract: true, "Expected.wxs", "TestData", "DecompileTargetDirMergeModule");
        }

        [Fact]
        public void CanDecompileUI()
        {
            DecompileAndCompare("ui.msi", extract: false, "ExpectedUI.wxs", "TestData", "Decompile");
        }

        [Fact]
        public void CanDecompileMergeModuleWithKeepModularizationIds()
        {
            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputFolder = fs.GetFolder();
                var extractPath = Path.Combine(intermediateFolder, "$extracted");
                var outputPath = Path.Combine(intermediateFolder, @"Actual.wxs");
                var sourceFolder = TestData.Get("TestData", "DecompileTargetDirMergeModule");

                var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
                serviceProvider.AddWindowsInstallerBackend();
                var extensionManager = serviceProvider.GetService<IExtensionManager>();
                var context = serviceProvider.GetService<IWindowsInstallerDecompileContext>();

                context.Extensions = Array.Empty<BaseWindowsInstallerDecompilerExtension>();
                context.ExtensionData = extensionManager.GetServices<IExtensionData>();
                context.DecompilePath = Path.Combine(sourceFolder, "MergeModule1.msm");
                context.DecompileType = OutputType.Module;
                context.KeepModularizationIds = true;
                context.IntermediateFolder = intermediateFolder;
                context.ExtractFolder = outputFolder;
                context.CabinetExtractFolder = outputFolder;

                var decompiler = serviceProvider.GetService<IWindowsInstallerDecompiler>();
                var result = decompiler.Decompile(context);

                result.Document.Save(outputPath);
                WixAssert.CompareXml(Path.Combine(sourceFolder, "ExpectedModularizationGuids.wxs"), outputPath);
            }
        }
    }
}
