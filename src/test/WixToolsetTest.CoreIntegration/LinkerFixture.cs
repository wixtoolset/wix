
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

    public class LinkerFixture
    {
        [Fact(Skip = "Test demonstrates failure")]
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

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"test.wir"));
                var section = intermediate.Sections.Single();

                var actions = section.Tuples.OfType<WixActionTuple>().Where(wat => wat.Action.StartsWith("Set")).ToList();
                Assert.Equal(2, actions.Count);
                //Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                //Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }
    }
}
