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

    public class RelatedBundleFixture : BaseConverterFixture
    {
        [Fact]
        public void CanConvertActionToLowercase()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <RelatedBundle Id='D' Action='Detect' />",
                "    <RelatedBundle Id='U' Action='Upgrade' />",
                "    <RelatedBundle Id='A' Action='Addon' />",
                "    <RelatedBundle Id='P' Action='Patch' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <RelatedBundle Id=\"D\" Action=\"detect\" />",
                "    <RelatedBundle Id=\"U\" Action=\"upgrade\" />",
                "    <RelatedBundle Id=\"A\" Action=\"addon\" />",
                "    <RelatedBundle Id=\"P\" Action=\"patch\" />",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);

            WixAssert.CompareLineByLine(new[]
            {
                "[Converted] The RelatedBundle element's Action attribute value must now be all lowercase. The Action='Detect' will be converted to 'detect' (RelatedBundleActionLowercase)",
                "[Converted] The RelatedBundle element's Action attribute value must now be all lowercase. The Action='Upgrade' will be converted to 'upgrade' (RelatedBundleActionLowercase)",
                "[Converted] The RelatedBundle element's Action attribute value must now be all lowercase. The Action='Addon' will be converted to 'addon' (RelatedBundleActionLowercase)",
                "[Converted] The RelatedBundle element's Action attribute value must now be all lowercase. The Action='Patch' will be converted to 'patch' (RelatedBundleActionLowercase)",
            }, messaging.Messages.Select(m => m.ToString()).ToArray());

            Assert.Equal(4, errors);
        }
    }
}
