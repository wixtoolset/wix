// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class DirectXExtensionFixture : BaseConverterFixture
    {
        [Fact]
        public void FixUIRefs()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <PropertyRef Id=\"WIX_DIRECTX_PIXELSHADERVERSION\" />",
                "    <UI>",
                "      <PropertyRef Id=\"WIX_DIRECTX_VERTEXSHADERVERSION\" />",
                "    </UI>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:directx=\"http://wixtoolset.org/schemas/v4/wxs/directx\">",
                "  <Fragment>",
                "    <directx:GetCapabilities />",
                "    <UI>",
                "      <directx:GetCapabilities />",
                "    </UI>",
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
    }
}
