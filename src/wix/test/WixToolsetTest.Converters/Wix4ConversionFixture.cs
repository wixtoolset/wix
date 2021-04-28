// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class Wix4ConversionFixture : BaseConverterFixture
    {
        [Fact]
        public void DoesNotAddFileId()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <ComponentGroup Id='ProductComponents' Directory='INSTALLFOLDER'>",
                "      <Component>",
                "        <File Source='example.txt' />",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <ComponentGroup Id=\"ProductComponents\" Directory=\"INSTALLFOLDER\">",
                "      <Component>",
                "        <File Source=\"example.txt\" />",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>"
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
