// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Netfx
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Netfx;
    using Xunit;

    public class NetfxExtensionFixture
    {
        [Fact]
        public void CanBuildUsingNativeImage()
        {
            var folder = TestData.Get(@"TestData\UsingNativeImage");
            var build = new Builder(folder, typeof(NetfxExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Wix4NetFxNativeImage");
            Assert.Equal(new[]
            {
                "Wix4NetFxNativeImage:ExampleNgen\tfil6349_KNDJhqShNzVdHX3ihhvA6Y\t3\t8\t\t",
            }, results.OrderBy(s => s).ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
