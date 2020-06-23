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

        [Fact]
        public void FixCustomRowTextValue()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <CustomTable Id='Custom1'>",
                "      <Row Id='Column1'>",
                "           Some value",
                "      </Row>",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <CustomTable Id=\"Custom1\">",
                "      <Row Id=\"Column1\" Value=\"Some value\" />",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixCustomRowCdataValue()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <CustomTable Id='Custom1'>",
                "      <Row Id='Column1'>",
                "       <![CDATA[",
                "         Some value",
                "       ]]>",
                "      </Row>",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <CustomTable Id=\"Custom1\">",
                "      <Row Id=\"Column1\" Value=\"Some value\" />",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(2, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixCustomRowWithoutValue()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <CustomTable Id='Custom1'>",
                "      <Row Id='Column1'></Row>",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <CustomTable Id=\"Custom1\">",
                "      <Row Id=\"Column1\"></Row>",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(2, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void CanConvertCustomTableBootstrapperApplicationData()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomTable Id='FgAppx' BootstrapperApplicationData='yes' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomTable Id=\"FgAppx\" Unreal=\"yes\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }
    }
}
