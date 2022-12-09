// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;
    using System.Linq;
    using WixToolset.Data.Symbols;

    public class AssemblyFixture
    {
        [Fact]
        public void CanBuildWithAssembly()
        {
            var folder = TestData.Get(@"TestData", "Assembly");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var msiPath = Path.Combine(baseFolder, "bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "PFiles", "AssemblyMsiPackage", "candle.exe")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(Path.Combine(folder, "data", "candle.exe"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual("candle.exe", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var msiAssemblyNameSymbols = section.Symbols.OfType<MsiAssemblyNameSymbol>();
                WixAssert.CompareLineByLine(new[]
                {
                    "culture = neutral",
                    "fileVersion = 3.11.11810.0",
                    "name = candle",
                    "processorArchitecture = x86",
                    "publicKeyToken = 256B3414DFA97718",
                    "version = 3.0.0.0"
                }, msiAssemblyNameSymbols.OrderBy(a => a.Name).Select(a => a.Name + " = " + a.Value).ToArray());

                var rows = Query.QueryDatabase(msiPath, new[] { "MsiAssembly", "MsiAssemblyName" });
                WixAssert.CompareLineByLine(new[]
                {
                    "MsiAssembly:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA\tProductFeature\t\t\t",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA\tculture\tneutral",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA\tfileVersion\t3.11.11810.0",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA\tname\tcandle",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA\tprocessorArchitecture\tx86",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA\tpublicKeyToken\t256B3414DFA97718",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA\tversion\t3.0.0.0"
                }, rows);
            }
        }

        [Fact]
        public void CanBuildWithAssemblyInModule()
        {
            var folder = TestData.Get(@"TestData", "Assembly");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var msmPath = Path.Combine(baseFolder, "bin", "test.msm");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Module.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                    "-o", msmPath
                });

                result.AssertSuccess();

                var rows = Query.QueryDatabase(msmPath, new[] { "MsiAssembly", "MsiAssemblyName" });
                WixAssert.CompareLineByLine(new[]
                {
                    "MsiAssembly:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA.147730A5_30FE_4A62_A520_DA9381B8226A\t{00000000-0000-0000-0000-000000000000}\t\t\t",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA.147730A5_30FE_4A62_A520_DA9381B8226A\tculture\tneutral",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA.147730A5_30FE_4A62_A520_DA9381B8226A\tfileVersion\t3.11.11810.0",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA.147730A5_30FE_4A62_A520_DA9381B8226A\tname\tcandle",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA.147730A5_30FE_4A62_A520_DA9381B8226A\tprocessorArchitecture\tx86",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA.147730A5_30FE_4A62_A520_DA9381B8226A\tpublicKeyToken\t256B3414DFA97718",
                    "MsiAssemblyName:fil1B9Dd3yyw7a4WVD.MROlfbKT_KA.147730A5_30FE_4A62_A520_DA9381B8226A\tversion\t3.0.0.0"
                }, rows);
            }
        }

        [Fact]
        public void CanBuildWithNet1xAssembly()
        {
            var folder = TestData.Get("TestData", "Assembly1x");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, "bin", "test.msi")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "PFiles", "AssemblyMsiPackage", "candle.exe")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(Path.Combine(folder, "data", "candle.exe"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual("candle.exe", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);

                var msiAssemblyNameSymbols = section.Symbols.OfType<MsiAssemblyNameSymbol>();
                WixAssert.CompareLineByLine(new[]
                {
                    "culture",
                    "fileVersion",
                    "name",
                    "publicKeyToken",
                    "version"
                }, msiAssemblyNameSymbols.OrderBy(a => a.Name).Select(a => a.Name).ToArray());

                WixAssert.CompareLineByLine(new[]
                {
                    "neutral",
                    "2.0.5805.0",
                    "candle",
                    "CE35F76FCDA82BAD",
                    "2.0.5805.0",
                }, msiAssemblyNameSymbols.OrderBy(a => a.Name).Select(a => a.Value).ToArray());
            }
        }
    }
}
