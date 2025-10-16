// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace WixToolsetTest.DirectX
{
    using System.Linq;
    using WixInternal.MSTestSupport;
    using WixInternal.Core.MSTestPackage;
    using WixToolset.DirectX;

    [TestClass]
    public class DirectXExtensionFixture
    {
        [TestMethod]
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
