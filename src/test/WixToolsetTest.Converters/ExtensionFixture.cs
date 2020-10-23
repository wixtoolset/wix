// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class ExtensionFixture : BaseConverterFixture
    {
        [Fact]
        public void FixRemoteAddressValue()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:fw='http://schemas.microsoft.com/wix/FirewallExtension'>",
                "  <Fragment>",
                "    <fw:RemoteAddress>",
                "      127.0.0.1",
                "    </fw:RemoteAddress>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:fw=\"http://wixtoolset.org/schemas/v4/wxs/firewall\">",
                "  <Fragment>",
                "    <fw:RemoteAddress Value=\"127.0.0.1\" />",
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

        [Fact]
        public void FixXmlConfigValue()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment>",
                "    <util:XmlConfig Id='Change' ElementPath='book'>",
                "      a&lt;>b",
                "    </util:XmlConfig>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:util=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <Fragment>",
                "    <util:XmlConfig Id=\"Change\" ElementPath=\"book\" Value=\"a&lt;&gt;b\" />",
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
