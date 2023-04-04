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

    public class BalConditionFixture : BaseConverterFixture
    {
        [Fact]
        public void CanConvertActionToLowercase()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs' xmlns:bal='http://schemas.microsoft.com/wix/BalExtension'>",
                "  <Fragment>",
                "    <bal:Condition Message='Example message.'>WixBundleInstalled OR NOT (VersionNT = v6.1) OR (VersionNT = v6.1 AND ServicePackLevel = 1)</bal:Condition>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:bal=\"http://wixtoolset.org/schemas/v4/wxs/bal\">",
                "  <Fragment>",
                "    <bal:Condition Message=\"Example message.\" Condition=\"WixBundleInstalled OR NOT (VersionNT = v6.1) OR (VersionNT = v6.1 AND ServicePackLevel = 1)\" />",
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
                "[Converted] The namespace 'http://schemas.microsoft.com/wix/BalExtension' is out of date. It must be 'http://wixtoolset.org/schemas/v4/wxs/bal'. (XmlnsValueWrong)",
                "[Converted] Using Condition element text is deprecated. Use the 'Condition' attribute instead. (InnerTextDeprecated)",
            }, messaging.Messages.Select(m => m.ToString()).ToArray());

            Assert.Equal(2, errors);
        }
    }
}
