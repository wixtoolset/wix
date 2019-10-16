// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Util
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
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

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
