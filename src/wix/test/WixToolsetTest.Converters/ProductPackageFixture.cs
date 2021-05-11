// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolset.Core.TestPackage;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class ProductPackageFixture : BaseConverterFixture
    {
        [Fact]
        public void FixesCompressedWhenYes()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package Compressed='yes' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package>",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 4, expected);
        }

        [Fact]
        public void FixesCompressedWhenNo()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package Compressed='no' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package Compressed=\"no\">",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 4, expected);
        }

        [Fact]
        public void FixesCompressedWhenOmitted()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package Compressed=\"no\">",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 4, expected);
        }

        private static void AssertSuccess(string input, int expectedErrorCount, string[] expected)
        {
            var document = XDocument.Parse(input, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(expectedErrorCount, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixesInstallerVersion()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package InstallerVersion='666' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package Compressed=\"no\" InstallerVersion=\"666\">",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 3, expected);
        }

        [Fact]
        public void FixesDefaultInstallerVersion()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package InstallerVersion='500' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package Compressed=\"no\">",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 3, expected);
        }

        [Fact]
        public void FixesImplicitInstallerVersion()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package Compressed=\"no\">",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 4, expected);
        }

        [Fact]
        public void FixesNonDefaultInstallerVersion()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package InstallerVersion='200' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package Compressed=\"no\" InstallerVersion=\"200\">",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 3, expected);
        }

        [Fact]
        public void FixesLimitedInstallerPrivileges()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package InstallPrivileges='limited' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package Compressed=\"no\" Scope=\"perUser\">",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 4, expected);
        }

        [Fact]
        public void FixesElevatedInstallerPrivileges()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Product>",
                "    <Package InstallPrivileges='elevated' />",
                "    <Property Id='ALLUSERS' Value='1' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package Compressed=\"no\">",
                "    ",
                "    ",
                "  </Package>",
                "</Wix>"
            };

            AssertSuccess(parse, 4, expected);
        }

        [Fact]
        public void CanDecompileAndRecompile()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var decompiledWxsPath = Path.Combine(baseFolder, "TypicalV3.wxs");

                var folder = TestData.Get(@"TestData\PackageSummaryInformation");
                var v3msiPath = Path.Combine(folder, "TypicalV3.msi");
                var result = WixRunner.Execute(new[]
                {
                    "decompile", v3msiPath,
                    "-intermediateFolder", intermediateFolder,
                    "-o", decompiledWxsPath
                });

                result.AssertSuccess();

                var v4msiPath = Path.Combine(intermediateFolder, "TypicalV4.msi");
                result = WixRunner.Execute(new[]
                {
                    "build", decompiledWxsPath,
                    "-arch", "x64",
                    "-intermediateFolder", intermediateFolder,
                    "-o", v4msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(v4msiPath));

                var v3results = Query.QueryDatabase(v3msiPath, new[] { "_SummaryInformation", "Property" });
                var v4results = Query.QueryDatabase(v4msiPath, new[] { "_SummaryInformation", "Property" });
                WixAssert.CompareLineByLine(v3results, v4results);
            }
        }
    }
}
