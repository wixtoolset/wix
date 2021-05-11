// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.UI
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.UI;
    using Xunit;

    public class UIExtensionFixture
    {
        [Fact]
        public void CanBuildUsingWixUIAdvanced()
        {
            var folder = TestData.Get(@"TestData\WixUI_Advanced");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Property");
            Assert.Equal(new[]
            {
                "Property:WixUI_Mode\tAdvanced",
            }, results.Where(s => s.StartsWith("Property:WixUI_Mode")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIFeatureTree()
        {
            var folder = TestData.Get(@"TestData\WixUI_FeatureTree");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Property");
            Assert.Equal(new[]
            {
                "Property:WixUI_Mode\tFeatureTree",
            }, results.Where(s => s.StartsWith("Property:WixUI_Mode")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIInstallDir()
        {
            var folder = TestData.Get(@"TestData\WixUI_InstallDir");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Property");
            Assert.Equal(new[]
            {
                "Property:WixUI_Mode\tInstallDir",
            }, results.Where(s => s.StartsWith("Property:WixUI_Mode")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIMinimal()
        {
            var folder = TestData.Get(@"TestData\WixUI_Minimal");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Property");
            Assert.Equal(new[]
            {
                "Property:WixUI_Mode\tMinimal",
            }, results.Where(s => s.StartsWith("Property:WixUI_Mode")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIMondo()
        {
            var folder = TestData.Get(@"TestData\WixUI_Mondo");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Property");
            Assert.Equal(new[]
            {
                "Property:WixUI_Mode\tMondo",
            }, results.Where(s => s.StartsWith("Property:WixUI_Mode")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIMondoLocalized()
        {
            var folder = TestData.Get(@"TestData\WixUI_Mondo");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildInGerman, "Control");
            Assert.Equal(new[]
            {
                "&Ja",
            }, results.Where(s => s.StartsWith("Control:ErrorDlg\tY")).Select(s => s.Split('\t')[9]).ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }

        private static void BuildInGerman(string[] args)
        {
            var localizedArgs = args.Append("-culture").Append("de-DE").ToArray();

            var result = WixRunner.Execute(localizedArgs)
                                  .AssertSuccess();
        }
    }
}
