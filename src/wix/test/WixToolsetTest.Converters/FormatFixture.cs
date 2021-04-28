// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class FormatFixture : BaseConverterFixture
    {
        [Fact]
        public void CanFixWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'",
                "              Value='Val'>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "    <Fragment>",
                "        <Property Id=\"Prop\" Value=\"Val\" />",
                "    </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 4, null, null);

            var errors = converter.FormatDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(5, errors);
        }

        [Fact]
        public void CanPreserveNewLines()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "",
                "    <Property Id='Prop' Value='Val' />",
                "",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "    <Fragment>",
                "",
                "        <Property Id=\"Prop\" Value=\"Val\" />",
                "",
                "    </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 4, null, null);

            var conversions = converter.FormatDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(4, conversions);
        }

        [Fact]
        public void CanFormatWithNewLineAtEndOfFile()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "",
                "    <Property Id='Prop' Value='Val' />",
                "",
                "  </Fragment>",
                "</Wix>",
                "");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "    <Fragment>",
                "",
                "        <Property Id=\"Prop\" Value=\"Val\" />",
                "",
                "    </Fragment>",
                "</Wix>",
                "");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 4, null, null);

            var conversions = converter.FormatDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(3, conversions);
        }
    }
}
