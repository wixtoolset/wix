// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class UtilExtensionFixture : BaseConverterFixture
    {
        [Fact]
        public void FixCloseAppsCondition()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment>",
                "    <util:CloseApplication Id='EndApp' Target='example.exe'>",
                "      a&lt;>b",
                "    </util:CloseApplication>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:util=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <Fragment>",
                "    <util:CloseApplication Id=\"EndApp\" Target=\"example.exe\" Condition=\"a&lt;&gt;b\" />",
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

        [Fact]
        public void WarnsOnAllRegistryValueSearches()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment>",
                "    <util:RegistrySearch Id='RegValue' Root='HKLM' Key='Converter' Variable='Test' />",
                "    <util:RegistrySearch Id='RegValue2' Root='HKLM' Key='Converter' Variable='Test' Result='value' />",
                "    <util:RegistrySearch Id='RegValue3' Root='HKLM' Key='Converter' Variable='Test' Result='exists' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:util=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <Fragment>",
                "    <util:RegistrySearch Id=\"RegValue\" Root=\"HKLM\" Key=\"Converter\" Variable=\"Test\" />",
                "    <util:RegistrySearch Id=\"RegValue2\" Root=\"HKLM\" Key=\"Converter\" Variable=\"Test\" Result=\"value\" />",
                "    <util:RegistrySearch Id=\"RegValue3\" Root=\"HKLM\" Key=\"Converter\" Variable=\"Test\" Result=\"exists\" />",
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
        public void FixXmlConfigValueCData()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment>",
                "    <util:XmlConfig Id='Change' ElementPath='book'>",
                "      <![CDATA[a<>b]]>",
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

        [Fact]
        public void FixQueryOsPropertyRefs()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment>",
                "    <PropertyRef Id=\"WIX_SUITE_ENTERPRISE\" />",
                "    <PropertyRef Id=\"WIX_DIR_COMMON_DOCUMENTS\" />",
                "    <CustomActionRef Id=\"WixFailWhenDeferred\" />",
                "    <UI>",
                "      <PropertyRef Id=\"WIX_ACCOUNT_LOCALSERVICE\" />",
                "    </UI>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:util=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <Fragment>",
                "    <util:QueryWindowsSuiteInfo />",
                "    <util:QueryWindowsDirectories />",
                "    <util:FailWhenDeferred />",
                "    <UI>",
                "      <util:QueryWindowsWellKnownSIDs />",
                "    </UI>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(6, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }
    }
}
