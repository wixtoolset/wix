// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Iis
{
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Iis;
    using Xunit;

    public class IisExtensionFixture
    {
        [Fact]
        public void CanBuildUsingIIsWebAddress()
        {
            var folder = TestData.Get(@"TestData\UsingIis");
            var build = new Builder(folder, typeof(IisExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, validate: true, "Wix4IIsWebSite", "Wix4IIsWebAddress");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4IIsWebAddress:TestAddress\tTest\t\t[PORT]\t\t0",
                "Wix4IIsWebSite:Test\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tTest web server\t\tTestWebSiteProductDirectory\t2\t2\tTestAddress\tReadAndExecute\t\t\t\t",
            }, results);
        }

        private static void Build(string[] args)
        {
            WixRunner.Execute(args).AssertSuccess();
        }
    }
}
