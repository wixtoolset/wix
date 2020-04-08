// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.DirectX
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.DirectX;
    using Xunit;

    public class DirectXExtensionFixture
    {
        [Fact]
        public void CanBuildUsingPixelShaderVersion()
        {
            var folder = TestData.Get(@"TestData\UsingPixelShaderVersion");
            var build = new Builder(folder, typeof(DirectXExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction");
            Assert.Equal(new[]
            {
                "CustomAction:WixQueryDirectXCaps\t65\tDirectXCA\tWixQueryDirectXCaps\t",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
