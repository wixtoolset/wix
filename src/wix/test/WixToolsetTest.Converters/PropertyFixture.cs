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

    public class PropertyFixture : BaseConverterFixture
    {
        [Fact]
        public void CanFixCdataWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'>",
                "       <![CDATA[1<2]]>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\" Value=\"1&lt;2\" />",
                "  </Fragment>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(1, errors);
        }

        [Fact]
        public void CanFixCdataWithWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'>",
                "       <![CDATA[",
                "           1<2",
                "       ]]>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\" Value=\"1&lt;2\" />",
                "  </Fragment>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(1, errors);
        }

        [Fact]
        public void CanKeepCdataWithOnlyWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'><![CDATA[ ]]></Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\" Value=\" \" />",
                "  </Fragment>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);
            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(1, errors);
        }

        [Fact]
        public void CanConvertWithValueAndEmptyText()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop' Value='123'>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\" Value=\"123\">",
                "    </Property>",
                "  </Fragment>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);
            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(0, errors);
        }

        [Fact]
        public void CanConvertWithValueAndComment()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop' Value='123'>",
                "      <!-- this is a comment -->",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\" Value=\"123\">",
                "      <!-- this is a comment -->",
                "    </Property>",
                "  </Fragment>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);
            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(0, errors);
        }

        [Fact]
        public void CanErrorWithValueAndText()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop' Value='123'>Not Allowed Value</Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\" Value=\"123\">Not Allowed Value</Property>",
                "  </Fragment>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);
            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            WixAssert.CompareLineByLine(new[]
            {
                "Using Property element text is deprecated. Remove the element's text and use only the 'Value' attribute. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages (InnerTextDeprecated)"
            }, messaging.Messages.Select(m => m.ToString()).ToArray());
            Assert.Equal(1, errors);
        }
    }
}
