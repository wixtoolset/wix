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
        [Fact(Skip = "xxxxx CodeBase Issue: We can't determine the file path from which we were loaded. xxxxx")]
        public void CanBuildUsingPixelShaderVersion()
        {
            var folder = TestData.Get(@"TestData\UsingPixelShaderVersion");
            var build = new Builder(folder, typeof(DirectXExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction", "Binary");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4DXCA_X86\t[Binary data]",
                "CustomAction:Wix4QueryDirectXCaps_X86\t65\tWix4DXCA_X86\tWixQueryDirectXCaps\t",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
