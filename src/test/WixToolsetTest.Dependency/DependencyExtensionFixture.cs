// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Dependency
{
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Dependency;
    using Xunit;

    public class DependencyExtensionFixture
    {
        [Fact]
        public void CanBuildUsingProvides()
        {
            var folder = TestData.Get(@"TestData\UsingProvides");
            var build = new Builder(folder, typeof(DependencyExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction", "WixDependencyProvider");
            Assert.Equal(new[]
            {
                "CustomAction:Wix4DependencyCheck_X86\t1\tDependencyCA_X86\tWixDependencyCheck\t",
                "WixDependencyProvider:dep74OfIcniaqxA7EprRGBw4Oyy3r8\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tUsingProvides\t\t\t",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
