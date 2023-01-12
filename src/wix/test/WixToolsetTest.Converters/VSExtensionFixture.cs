// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class VSExtensionFixture : BaseConverterFixture
    {
        [Fact]
        public void FixVsLocatorPropertyRefs()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <PropertyRef Id=\"VS2019_ROOT_FOLDER\" />",
                "    <PropertyRef Id=\"VS2022_BOOTSTRAPPER_PACKAGE_FOLDER\" />",
                "    <CustomActionRef Id=\"VS2017Setup\" />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:vs=\"http://wixtoolset.org/schemas/v4/wxs/vs\">",
                "  <Fragment>",
                "    <vs:FindVisualStudio />",
                "    <vs:FindVisualStudio /><PropertyRef Id=\"VS2022_BOOTSTRAPPER_PACKAGE_FOLDER\" />",
                "    <CustomActionRef Id=\"VS2017Setup\" />",
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
