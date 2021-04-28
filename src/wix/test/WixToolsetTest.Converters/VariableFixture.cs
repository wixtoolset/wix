// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class VariableFixture : BaseConverterFixture
    {
        [Fact]
        public void FixFormattedType()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Variable Name='ExplicitString' Type='string' Value='explicit' />",
                "    <Variable Name='ImplicitNumber' Value='42' />",
                "    <Variable Name='ImplicitString' Value='implicit' />",
                "    <Variable Name='ImplicitVersion' Value='v2' />",
                "    <Variable Name='NoTypeOrValue' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Variable Name=\"ExplicitString\" Type=\"formatted\" Value=\"explicit\" />",
                "    <Variable Name=\"ImplicitNumber\" Value=\"42\" />",
                "    <Variable Name=\"ImplicitString\" Value=\"implicit\" Type=\"formatted\" />",
                "    <Variable Name=\"ImplicitVersion\" Value=\"v2\" />",
                "    <Variable Name=\"NoTypeOrValue\" />",
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
        public void DoesntFixFormattedTypeFromV4()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Variable Name='ImplicitString' Value='implicit' />",
                "    <Variable Name='ExplicitString' Type='string' Value='explicit' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Variable Name=\"ImplicitString\" Value=\"implicit\" />",
                "    <Variable Name=\"ExplicitString\" Type=\"string\" Value=\"explicit\" />",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(0, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }
    }
}
