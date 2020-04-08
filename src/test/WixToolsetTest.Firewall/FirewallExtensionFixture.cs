// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Firewall
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Firewall;
    using Xunit;

    public class FirewallExtensionFixture
    {
        [Fact]
        public void CanBuildUsingFileShare()
        {
            var folder = TestData.Get(@"TestData\UsingFirewall");
            var build = new Builder(folder, typeof(FirewallExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "WixFirewallException");
            Assert.Equal(new[]
            {
                "WixFirewallException:ExampleFirewall\texample\t*\t42\t6\t\t0\t2147483647\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example firewall",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
