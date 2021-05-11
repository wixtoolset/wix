// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class IncludeFixture : BaseConverterFixture
    {
        [Fact]
        public void EnsureNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<Include>",
                "  <Fragment />",
                "</Include>");

            var expected = new[]
            {
                "<Include xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Include>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(1, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<Include xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment />",
                "</Include>");

            var expected = new[]
            {
                "<Include xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Include>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(1, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }
    }
}
