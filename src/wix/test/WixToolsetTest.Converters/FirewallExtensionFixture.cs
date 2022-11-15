// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class FirewallExtensionFixture : BaseConverterFixture
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
        public void FixNamespacePlacement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <RemoteAddress xmlns='http://schemas.microsoft.com/wix/FirewallExtension'>",
                "      127.0.0.1",
                "    </RemoteAddress>",
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
            WixAssert.CompareLineByLine(new[]
            {
                "[Converted] The namespace 'http://schemas.microsoft.com/wix/2006/wi' is out of date.  It must be 'http://wixtoolset.org/schemas/v4/wxs'. (XmlnsValueWrong)",
                "[Converted] The namespace 'http://schemas.microsoft.com/wix/FirewallExtension' is out of date.  It must be 'http://wixtoolset.org/schemas/v4/wxs/firewall'. (XmlnsValueWrong)",
                "[Converted] Using RemoteAddress element text is deprecated. Use the 'Value' attribute instead. (InnerTextDeprecated)",
                "[Converted] Namespace should be defined on the root. The 'http://wixtoolset.org/schemas/v4/wxs/firewall' namespace was move to the root element. (MoveNamespacesToRoot)"
            }, messaging.Messages.Select(m => m.ToString()).ToArray());
            Assert.Equal(4, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixNamespacePlacementWhenItExists()
        {
             //xmlns:abc='http://schemas.microsoft.com/wix/FirewallExtension'
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <RemoteAddress xmlns='http://schemas.microsoft.com/wix/FirewallExtension'>",
                "      127.0.0.1",
                "    </RemoteAddress>",
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
            WixAssert.CompareLineByLine(new[]
            {
                "[Converted] The namespace 'http://schemas.microsoft.com/wix/2006/wi' is out of date.  It must be 'http://wixtoolset.org/schemas/v4/wxs'. (XmlnsValueWrong)",
                "[Converted] The namespace 'http://schemas.microsoft.com/wix/FirewallExtension' is out of date.  It must be 'http://wixtoolset.org/schemas/v4/wxs/firewall'. (XmlnsValueWrong)",
                "[Converted] Using RemoteAddress element text is deprecated. Use the 'Value' attribute instead. (InnerTextDeprecated)",
                "[Converted] Namespace should be defined on the root. The 'http://wixtoolset.org/schemas/v4/wxs/firewall' namespace was move to the root element. (MoveNamespacesToRoot)"
            }, messaging.Messages.Select(m => m.ToString()).ToArray());
            Assert.Equal(4, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }
    }
}
