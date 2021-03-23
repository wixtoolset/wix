// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class TagFixture : BaseConverterFixture
    {
        [Fact]
        public void FixTagExtension()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:tag='http://schemas.microsoft.com/wix/TagExtension'>",
                "  <Product>",
                "    <tag:Tag Regid='wixtoolset.org' InstallDirectory='InstallFolder' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package>",
                "    <SoftwareTag Regid=\"wixtoolset.org\" InstallDirectory=\"InstallFolder\" />",
                "  </Package>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(4, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixTagExtensionDeprecations()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:tag='http://schemas.microsoft.com/wix/TagExtension'>",
                "  <Product>",
                "    <tag:Tag Regid='wixtoolset.org' InstallDirectory='InstallFolder' Licensed='true' Type='component' Win64='yes' />",
                "  </Product>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Package>",
                "    <SoftwareTag Regid=\"wixtoolset.org\" InstallDirectory=\"InstallFolder\" Bitness=\"always64\" />",
                "  </Package>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(7, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixTagExtensionTagRef()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:tag='http://schemas.microsoft.com/wix/TagExtension'>",
                "  <Fragment>",
                "    <PatchFamily>",
                "      <tag:TagRef Regid='wixtoolset.org' />",
                "    </PatchFamily>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <PatchFamily>",
                "      <SoftwareTagRef Regid=\"wixtoolset.org\" />",
                "    </PatchFamily>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }
    }
}
