// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
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
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(4, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixCustomRowTextValue()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <CustomTable Id='Custom1'>",
                "      <Row>",
                "        <Data Id='Column1'>",
                "           Some value",
                "        </Data>",
                "      </Row>",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <CustomTable Id=\"Custom1\">",
                "      <Row>",
                "        <Data Id=\"Column1\" Value=\"Some value\" />",
                "      </Row>",
                "    </CustomTable>",
                "  </Fragment>",
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
        public void FixCustomRowCdataValue()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <CustomTable Id='Custom1'>",
                "      <Row>",
                "        <Data Id='Column1'>",
                "         <![CDATA[",
                "           Some value",
                "         ]]>",
                "        </Data>",
                "      </Row>",
                "    </CustomTable>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <CustomTable Id=\"Custom1\">",
                "      <Row>",
                "        <Data Id=\"Column1\" Value=\"Some value\" />",
                "      </Row>",
                "    </CustomTable>",
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
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void CanConvertBundleCustomTableBootstrapperApplicationData()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomTable Id='FgAppx' BootstrapperApplicationData='yes'>",
                "    <Column Id='Column1' PrimaryKey='yes' Type='string' Width='0' Category='text' Description='The first custom column.' />",
                "    <Row>",
                "      <Data Column='Column1'>Row1</Data>",
                "    </Row>",
                "  </CustomTable>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <BundleCustomData Id=\"FgAppx\">",
                "    <BundleAttributeDefinition Id=\"Column1\" />",
                "    <BundleElement>",
                "      <BundleAttribute Id=\"Column1\" Value=\"Row1\" />",
                "    </BundleElement>",
                "  </BundleCustomData>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, customTableTarget: CustomTableTarget.Bundle);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertBundleCustomTableRef()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomTable Id='FgAppx'>",
                "    <Row>",
                "      <Data Column='Column1'>Row1</Data>",
                "    </Row>",
                "  </CustomTable>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <BundleCustomDataRef Id=\"FgAppx\">",
                "    <BundleElement>",
                "      <BundleAttribute Id=\"Column1\" Value=\"Row1\" />",
                "    </BundleElement>",
                "  </BundleCustomDataRef>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, customTableTarget: CustomTableTarget.Bundle);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertMsiCustomTableBootstrapperApplicationData()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomTable Id='FgAppx' BootstrapperApplicationData='yes'>",
                "    <Column Id='Column1' PrimaryKey='yes' Type='string' Width='0' Category='text' Description='The first custom column.' />",
                "    <Row>",
                "      <Data Column='Column1'>Row1</Data>",
                "    </Row>",
                "  </CustomTable>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomTable Id=\"FgAppx\" Unreal=\"yes\">",
                "    <Column Id=\"Column1\" PrimaryKey=\"yes\" Type=\"string\" Width=\"0\" Category=\"text\" Description=\"The first custom column.\" />",
                "    <Row>",
                "      <Data Column=\"Column1\" Value=\"Row1\" />",
                "    </Row>",
                "  </CustomTable>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, customTableTarget: CustomTableTarget.Msi);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertMsiCustomTableRef()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomTable Id='FgAppx'>",
                "    <Row>",
                "      <Data Column='Column1'>Row1</Data>",
                "    </Row>",
                "  </CustomTable>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomTableRef Id=\"FgAppx\">",
                "    <Row>",
                "      <Data Column=\"Column1\" Value=\"Row1\" />",
                "    </Row>",
                "  </CustomTableRef>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, customTableTarget: CustomTableTarget.Msi);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanDetectAmbiguousCustomTableBootstrapperApplicationData()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomTable Id='FgAppx' BootstrapperApplicationData='yes' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomTable Id=\"FgAppx\" BootstrapperApplicationData=\"yes\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanRemoveBootstrapperApplicationDataFromRealCustomTable()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomTable Id='FgAppx' BootstrapperApplicationData='no' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomTable Id=\"FgAppx\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }
    }
}
