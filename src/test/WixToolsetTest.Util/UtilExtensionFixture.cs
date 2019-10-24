// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Util
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Util;
    using Xunit;

    public class UtilExtensionFixture
    {
        [Fact]
        public void CanBuildUsingFileShare()
        {
            var folder = TestData.Get(@"TestData\UsingFileShare");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "FileShare", "FileSharePermissions");
            Assert.Equal(new[]
            {
                "FileShare:ExampleFileShare\texample\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example file share\tINSTALLFOLDER\t\t",
                "FileSharePermissions:ExampleFileShare\tEveryone\t1",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildBundleWithSearches()
        {
            var burnStubPath = TestData.Get(@"TestData\.Data\burn.exe");
            var folder = TestData.Get(@"TestData\BundleWithSearches");
            var rootFolder = TestData.Get();
            var wixext = Path.Combine(rootFolder, "WixToolset.Util.wixext.dll");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-ext", wixext,
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-burnStub", burnStubPath,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
#if TODO
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
#endif

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"test.wir"));
                var section = intermediate.Sections.Single();

                var searchTuples = section.Tuples.OfType<WixSearchTuple>().OrderBy(t => t.Id.Id).ToList();
                Assert.Equal(3, searchTuples.Count);
                Assert.Equal("FileSearchId", searchTuples[0].Id.Id);
                Assert.Equal("FileSearchVariable", searchTuples[0].Variable);
                Assert.Equal("ProductSearchId", searchTuples[1].Id.Id);
                Assert.Equal("ProductSearchVariable", searchTuples[1].Variable);
                Assert.Equal("1 & 2 < 3", searchTuples[1].Condition);
                Assert.Equal("RegistrySearchId", searchTuples[2].Id.Id);
                Assert.Equal("RegistrySearchVariable", searchTuples[2].Variable);

                var fileSearchTuple = section.Tuples.OfType<WixFileSearchTuple>().Single();
                Assert.Equal("FileSearchId", fileSearchTuple.Id.Id);
                Assert.Equal(@"%windir%\System32\mscoree.dll", fileSearchTuple.Path);
                Assert.Equal(WixFileSearchAttributes.Default | WixFileSearchAttributes.WantExists, fileSearchTuple.Attributes);

                var productSearchTuple = section.Tuples.OfType<WixProductSearchTuple>().Single();
                Assert.Equal("ProductSearchId", productSearchTuple.Id.Id);
                Assert.Equal("{738D02BF-E231-4370-8209-E9FD4E1BE2A1}", productSearchTuple.Guid);
                Assert.Equal(WixProductSearchAttributes.Version | WixProductSearchAttributes.UpgradeCode, productSearchTuple.Attributes);

                var registrySearchTuple = section.Tuples.OfType<WixRegistrySearchTuple>().Single();
                Assert.Equal("RegistrySearchId", registrySearchTuple.Id.Id);
                Assert.Equal(RegistryRootType.LocalMachine, registrySearchTuple.Root);
                Assert.Equal(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", registrySearchTuple.Key);
                Assert.Equal("Release", registrySearchTuple.Value);
                Assert.Equal(WixRegistrySearchAttributes.WantValue | WixRegistrySearchAttributes.Raw, registrySearchTuple.Attributes);
            }
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
