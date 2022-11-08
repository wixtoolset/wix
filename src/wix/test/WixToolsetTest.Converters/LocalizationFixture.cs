// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class LocalizationFixture : BaseConverterFixture
    {
        [Fact]
        public void EnsureNoXmlDeclaration()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' ?>",
                "<WixLocalization Culture='en-us'>",
                "  <String Id='SomeId'>Value</String>",
                "</WixLocalization>");

            var expected = new[]
            {
                "<WixLocalization Culture=\"en-us\" xmlns=\"http://wixtoolset.org/schemas/v4/wxl\">",
                "  <String Id=\"SomeId\" Value=\"Value\" />",
                "</WixLocalization>"
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
        public void EnsureNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<WixLocalization Culture='en-us'>",
                "  <String Id='SomeId'>Value</String>",
                "</WixLocalization>");

            var expected = new[]
            {
                "<WixLocalization Culture=\"en-us\" xmlns=\"http://wixtoolset.org/schemas/v4/wxl\">",
                "  <String Id=\"SomeId\" Value=\"Value\" />",
                "</WixLocalization>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(2, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<WixLocalization Culture='en-us' xmlns='http://schemas.microsoft.com/wix/2006/localization'>",
                "  <String Id='SomeId'>Value</String>",
                "</WixLocalization>");

            var expected = new[]
            {
                "<WixLocalization Culture=\"en-us\" xmlns=\"http://wixtoolset.org/schemas/v4/wxl\">",
                "  <String Id=\"SomeId\" Value=\"Value\" />",
                "</WixLocalization>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(2, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixStringTextWithComment()
        {
            var parse = String.Join(Environment.NewLine,
                "<WixLocalization Culture='en-us' xmlns='http://wixtoolset.org/schemas/v4/wxl'>",
                "  <String Id='SomeId'><!-- Comment -->Value</String>",
                "</WixLocalization>");

            var expected = new[]
            {
                "<WixLocalization Culture=\"en-us\" xmlns=\"http://wixtoolset.org/schemas/v4/wxl\">",
                "  <!-- Comment -->",
                "  <String Id=\"SomeId\" Value=\"Value\" />",
                "</WixLocalization>"
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
        public void FixUILocalization()
        {
            var parse = String.Join(Environment.NewLine,
                "<WixLocalization Culture='en-us' xmlns='http://wixtoolset.org/schemas/v4/wxl'>",
                "  <UI Dialog='DialogId' Control='ControlId' X='1' Y='2'>Some text</UI>",
                "</WixLocalization>");

            var expected = new[]
            {
                "<WixLocalization Culture=\"en-us\" xmlns=\"http://wixtoolset.org/schemas/v4/wxl\">",
                "  <UI Dialog=\"DialogId\" Control=\"ControlId\" X=\"1\" Y=\"2\" Text=\"Some text\" />",
                "</WixLocalization>"
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
