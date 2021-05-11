// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ComPlus
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.ComPlus;
    using Xunit;

    public class ComPlusExtensionFixture
    {
        [Fact]
        public void CanBuildUsingComPlusPartition()
        {
            var folder = TestData.Get(@"TestData\UsingComPlusPartition");
            var build = new Builder(folder, typeof(ComPlusExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "ComPlusPartition");
            WixAssert.CompareLineByLine(new[]
            {
                "ComPlusPartition:MyPartition\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tMyPartitionId\tMyPartition",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
