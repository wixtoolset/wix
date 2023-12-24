// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class UpgradeFixture
    {
        [Fact]
        public void FailsOnInvalidVersion()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Upgrade", "UpgradeInvalidMinVersion.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                var errorMessages = result.Messages.Where(m => m.Level == MessageLevel.Error)
                                                   .Select(m => m.ToString())
                                                   .ToArray();
                Assert.StartsWith("Invalid MSI package version: '1.256.0'.", errorMessages.Single());
                Assert.Equal(1148, result.ExitCode);
            }
        }

        [Fact]
        public void MajorUpgradeDowngradeMessagePopulatesRowsAsExpected()
        {
            var folder = TestData.Get("TestData", "Upgrade");
            var build = new Builder(folder, new Type[] { }, new[] { folder });

            var results = build.BuildAndQuery(Build, "Upgrade", "LaunchCondition");
            WixAssert.CompareLineByLine(new[]
            {
                "LaunchCondition:NOT WIX_DOWNGRADE_DETECTED\tNo downgrades allowed!",
                "LaunchCondition:NOT WIX_UPGRADE_DETECTED\tNo upgrades allowed!",
                "Upgrade:{7AB24276-C628-43DB-9E65-A184D052909B}\t\t2.0.0\t1033\t1\t\tWIX_UPGRADE_DETECTED",
                "Upgrade:{7AB24276-C628-43DB-9E65-A184D052909B}\t2.0.0\t\t1033\t2\t\tWIX_DOWNGRADE_DETECTED",
            }, results);
        }

        [Fact]
        public void DefaultMajorUpgradePopulatesUpgradeRowsAsExpected()
        {
            var folder = TestData.Get("TestData", "DefaultMajorUpgrade");
            var build = new Builder(folder, new Type[] { }, new[] { folder });

            var results = build.BuildAndQuery(Build, "Upgrade", "LaunchCondition");
            WixAssert.CompareLineByLine(new[]
            {
                "LaunchCondition:NOT WIX_DOWNGRADE_DETECTED\tA newer version of [ProductName] is already installed.",
                "Upgrade:{7AB24276-C628-43DB-9E65-A184D052909B}\t\t2.0.0\t1033\t1\t\tWIX_UPGRADE_DETECTED",
                "Upgrade:{7AB24276-C628-43DB-9E65-A184D052909B}\t2.0.0\t\t1033\t2\t\tWIX_DOWNGRADE_DETECTED",
            }, results);
        }

        [Fact]
        public void UpgradeStrategyNoneDoesNotCreateDefaultMajorUpgrade()
        {
            var folder = TestData.Get("TestData", "DefaultMajorUpgradeNone");
            var build = new Builder(folder, new Type[] { }, new[] { folder });

            var results = build.BuildAndQuery(Build, "Upgrade", "LaunchCondition");
            Assert.Empty(results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }
    }
}
