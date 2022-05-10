// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Dependency
{
    using System.Linq;
    using System.Text.RegularExpressions;
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

            var results = build.BuildAndQuery(Build, "CustomAction", "Wix4DependencyProvider", "Wix4Dependency")
                               .Select(r => Regex.Replace(r, "{[^}]*}", "{*}"))
                               .ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:Wix4DependencyCheck_X86\t1\tDependencyCA_X86\tWixDependencyCheck\t",
                "CustomAction:Wix4DependencyRequire_X86\t1\tDependencyCA_X86\tWixDependencyRequire\t",
                "Wix4Dependency:depL8BNflcqZaN5CQEWh2U3SBHFDdg\tUsingRequires\t1.0.0-beta.9\t\t0",
                "Wix4DependencyProvider:dep74OfIcniaqxA7EprRGBw4Oyy3r8\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tUsingProvides\t\t\t",
                "Wix4DependencyProvider:depTpv28q7slcxvXPWmU4Z0GfbiI.4\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\t{*}\t\t\t",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
