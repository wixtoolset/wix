
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class LinkerFixture
    {
        [Fact]
        public void MustCompileBeforeLinking()
        {
            var intermediate1 = new Intermediate("TestIntermediate1", new[] { new IntermediateSection("test1", SectionType.Product, 65001) }, null);
            var intermediate2 = new Intermediate("TestIntermediate2", new[] { new IntermediateSection("test2", SectionType.Fragment, 65001) }, null);
            var serviceProvider = new WixToolsetServiceProvider();

            var listener = new TestMessageListener();
            var messaging = serviceProvider.GetService<IMessaging>();
            messaging.SetListener(listener);

            var creator = serviceProvider.GetService<ITupleDefinitionCreator>();
            var context = serviceProvider.GetService<ILinkContext>();
            context.Extensions = Enumerable.Empty<WixToolset.Extensibility.ILinkerExtension>();
            context.ExtensionData = Enumerable.Empty<WixToolset.Extensibility.IExtensionData>();
            context.Intermediates = new[] { intermediate1, intermediate2 };
            context.TupleDefinitionCreator = creator;

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
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\MsiPackage\test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var actions = section.Tuples.OfType<WixActionTuple>().Where(wat => wat.Action.StartsWith("Set")).ToList();
                Assert.Equal(2, actions.Count);
                //Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                //Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }
    }
}
