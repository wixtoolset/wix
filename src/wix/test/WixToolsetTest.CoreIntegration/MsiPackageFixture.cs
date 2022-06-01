// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class MsiPackageFixture
    {
        [Fact]
        public void CanDefaultPermanentMsiPackageToVisible()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var exePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MsiPackage", "VisibleWhenPermanent.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                var extractResult = BundleExtractor.ExtractBAContainer(null, exePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var msiProperties = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:MsiPackage/burn:MsiProperty")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml())
                                            .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<MsiProperty Id='MSIFASTINSTALL' Value='7' />"
                }, msiProperties);
            }
        }
    }
}
