// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class SoftwareTagFixture
    {
        private static readonly XNamespace BurnManifestNamespace = "http://wixtoolset.org/schemas/v4/2008/Burn";
        private static readonly XNamespace SwidTagNamespace = "http://standards.iso.org/iso/19770/-2/2015/schema.xsd";

        [Fact]
        public void CanBuildPackageWithTag()
        {
            var folder = TestData.Get(@"TestData\ProductTag");
            var build = new Builder(folder, new Type[] { }, new[] { folder });

            var results = build.BuildAndQuery(Build, "File", "SoftwareIdentificationTag");

            var replacePackageCodeStart = results[2].IndexOf("\tmsi:package/") + "\tmsi:package/".Length;
            var replacePackageCodeEnd = results[2].IndexOf("\t", replacePackageCodeStart);
            results[2] = results[2].Substring(0, replacePackageCodeStart) + "???" + results[2].Substring(replacePackageCodeEnd);
            WixAssert.CompareLineByLine(new[]
            {
                "File:filF5_pLhBuF5b4N9XEo52g_hUM5Lo\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\texample.txt\t20\t\t\t512\t1",
                "File:tagEYRYWwOt95punO7qPPAQ9p1GBpY\ttagEYRYWwOt95punO7qPPAQ9p1GBpY\tqcfv-gdx.swi|WixprojPackageVcxprojWindowsApp.swidtag\t465\t\t\t1\t2",
                "SoftwareIdentificationTag:tagEYRYWwOt95punO7qPPAQ9p1GBpY\twixtoolset.org\tmsi:package/???\tmsi:upgrade/047730A5-30FE-4A62-A520-DA9381B8226A\t"
            }, results.ToArray());
        }

        [Fact]
        public void CanBuildBundleWithTag()
        {
            var testDataFolder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(testDataFolder, "ProductTag", "PackageWithTag.wxs"),
                    Path.Combine(testDataFolder, "ProductTag", "PackageComponents.wxs"),
                    "-loc", Path.Combine(testDataFolder, "ProductTag", "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(testDataFolder, "ProductTag"),
                    "-intermediateFolder", Path.Combine(intermediateFolder, "package"),
                    "-o", Path.Combine(baseFolder, "package", @"test.msi")
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(testDataFolder, "BundleTag", "BundleWithTag.wxs"),
                    "-bindpath", Path.Combine(testDataFolder, "BundleTag"),
                    "-bindpath", Path.Combine(baseFolder, "package"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));

                using (var ouput = WixOutput.Read(Path.Combine(baseFolder, @"bin\test.wixpdb")))
                {
                    var badata = ouput.GetDataStream("wix-burndata.xml");
                    var doc = XDocument.Load(badata);

                    var swidTag = doc.Root.Element(BurnManifestNamespace + "Registration").Element(BurnManifestNamespace + "SoftwareTag").Value;

                    var swidTagPath = Path.Combine(baseFolder, "test.swidtag");
                    File.WriteAllText(swidTagPath, swidTag);

                    var docTag = XDocument.Load(swidTagPath);
                    var title = docTag.Root.Attribute("name").Value;
                    var version = docTag.Root.Attribute("version").Value;
                    Assert.Equal("~TagTestBundle", title);
                    Assert.Equal("4.3.2.1", version);

                    var msiLink = docTag.Root.Elements(SwidTagNamespace + "Link").Single();
                    Assert.Equal("component", msiLink.Attribute("rel").Value);
                    Assert.StartsWith("swid:msi:package/", msiLink.Attribute("href").Value);
                }
            }
        }

        [Fact]
        public void CanBuildBundleWithTagWhereMsiDoesNotHaveTag()
        {
            var testDataFolder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(testDataFolder, "BundleTag", "BundleWithTag.wxs"),
                    "-bindpath", Path.Combine(testDataFolder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));

                using (var ouput = WixOutput.Read(Path.Combine(baseFolder, @"bin\test.wixpdb")))
                {
                    var badata = ouput.GetDataStream("wix-burndata.xml");
                    var doc = XDocument.Load(badata);

                    var swidTag = doc.Root.Element(BurnManifestNamespace + "Registration").Element(BurnManifestNamespace + "SoftwareTag").Value;

                    var swidTagPath = Path.Combine(baseFolder, "test.swidtag");
                    File.WriteAllText(swidTagPath, swidTag);

                    var docTag = XDocument.Load(swidTagPath);
                    var title = docTag.Root.Attribute("name").Value;
                    var version = docTag.Root.Attribute("version").Value;
                    Assert.Equal("~TagTestBundle", title);
                    Assert.Equal("4.3.2.1", version);

                    Assert.Empty(docTag.Root.Elements(SwidTagNamespace + "Link"));
                }
            }
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }
    }
}
