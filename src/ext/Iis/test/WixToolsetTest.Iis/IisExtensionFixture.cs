// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Iis
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Iis;
    using Xunit;

    public class IisExtensionFixture
    {
        [Fact]
        public void CanBuildUsingIIsWebAddress()
        {
            var folder = TestData.Get(@"TestData\UsingIis");
            var build = new Builder(folder, typeof(IisExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "IIsWebAddress");
            Assert.Equal(new[]
            {
                "IIsWebAddress:TestAddress\tTest\t\t[PORT]\t\t0",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
