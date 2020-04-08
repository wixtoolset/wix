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
        [Fact(Skip = "Currently fails due to duplicate symbols")]
        public void CanBuildUsingComPlusPartition()
        {
            var folder = TestData.Get(@"TestData\UsingComPlusPartition");
            var build = new Builder(folder, typeof(ComPlusExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "ComPlusPartition");
            Assert.Equal(new[]
            {
                "ComPlusPartition:",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
