
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Core;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class LinkerFixture
    {
        [Fact]
        public void MustCompileBeforeLinking()
        {
            var intermediate1 = new Intermediate("TestIntermediate1", new[] { new IntermediateSection("test1", SectionType.Package) }, null);
            var intermediate2 = new Intermediate("TestIntermediate2", new[] { new IntermediateSection("test2", SectionType.Fragment) }, null);
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();

            var listener = new TestMessageListener();
            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var creator = serviceProvider.GetService<ISymbolDefinitionCreator>();
            var context = serviceProvider.GetService<ILinkContext>();
            context.Extensions = Array.Empty<WixToolset.Extensibility.ILinkerExtension>();
            context.ExtensionData = Array.Empty<WixToolset.Extensibility.IExtensionData>();
            context.IntermediateFolder = Path.GetTempPath();
            context.Intermediates = new[] { intermediate1, intermediate2 };
            context.OutputPath = Path.Combine(context.IntermediateFolder, "test.msi");
            context.SymbolDefinitionCreator = creator;

            var linker = serviceProvider.GetService<ILinker>();
            linker.Link(context);

            Assert.Equal((int)ErrorMessages.Ids.IntermediatesMustBeCompiled, messaging.LastErrorNumber);
            Assert.Single(listener.Messages);
            Assert.EndsWith("TestIntermediate1, TestIntermediate2", listener.Messages[0].ToString());
        }

        [Fact]
        public void CanBuildWithOverridableActions()
        {
            var folder = TestData.Get(@"TestData\OverridableActions");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    "-sw1008", // this is expected for this test
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\PFiles\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var actions = section.Symbols.OfType<WixActionSymbol>().Where(wat => wat.Action.StartsWith("Set")).ToList();
                Assert.Equal(2, actions.Count);
                //Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileSymbolFields.Source].AsPath().Path);
                //Assert.Equal(@"test.txt", wixFile[WixFileSymbolFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void MissingEntrySectionDetectedPackage()
        {
            var folder = TestData.Get("TestData", "OverridableActions");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                try
                {
                    WixRunner.Execute(new[]
                    {
                        "build",
                        Path.Combine(folder, "PackageComponents.wxs"),
                        "-intermediateFolder", intermediateFolder,
                        "-o", Path.Combine(baseFolder, "bin", "test.msi")
                    });
                }
                catch (WixException we)
                {
                    WixAssert.StringEqual("Could not find entry section in provided list of intermediates. Expected section of type 'Package'.", we.Message);
                    return;
                }

                Assert.Fail("Expected WixException for missing entry section but expectations were not met.");
            }
        }

        [Fact]
        public void MissingEntrySectionDetectedWixipl()
        {
            var folder = TestData.Get(@"TestData\OverridableActions");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                try
                {
                    WixRunner.Execute(new[]
                    {
                        "build",
                        Path.Combine(folder, "PackageComponents.wxs"),
                        "-intermediateFolder", intermediateFolder,
                        "-o", Path.Combine(baseFolder, @"bin\test.wixipl")
                    });
                }
                catch (WixException we)
                {
                    WixAssert.StringEqual("Could not find entry section in provided list of intermediates. Supported entry section types are: Package, Bundle, Patch, PatchCreation, Module.", we.Message);
                    return;
                }

                Assert.Fail("Expected WixException for missing entry section but expectations were not met.");
            }
        }

        [Fact]
        public void MissingEntrySectionDetectedUnknown()
        {
            var folder = TestData.Get(@"TestData\OverridableActions");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                try
                {
                    WixRunner.Execute(new[]
                    {
                        "build",
                        Path.Combine(folder, "PackageComponents.wxs"),
                        "-intermediateFolder", intermediateFolder,
                        "-o", Path.Combine(baseFolder, @"bin\test.bob")
                    });
                }
                catch (WixException we)
                {
                    WixAssert.StringEqual("Could not find entry section in provided list of intermediates. Supported entry section types are: Package, Bundle, Patch, PatchCreation, Module.", we.Message);
                    return;
                }

                Assert.Fail("Expected WixException for missing entry section but expectations were not met.");
            }
        }
    }
}
