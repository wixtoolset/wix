// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class CustomTableFixture : BaseConverterFixture
    {
        [Fact]
        public void FixCustomTableCategoryAndModularization()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <CustomTable Id='Custom1'>",
                "      <Column Id='Column1' Type='string' Category='Text' Modularize='Column' />",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <CustomTable Id=\"Custom1\">",
                "      <Column Id=\"Column1\" Type=\"string\" Category=\"text\" Modularize=\"column\" />",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(4, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }
    }
}
