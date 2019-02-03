// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Dependency
{
    using System.Linq;
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

            var results = build.BuildAndQuery(Build, "WixDependencyProvider");
            Assert.Equal(new[]
            {
                "WixDependencyProvider:depJQsOasf1FRUsKxq8THB9sXk8yws\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tUsingProvides\t\t\t0",
            }, results.OrderBy(s => s).ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
