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

    public class MsiPackageFixture : BaseConverterFixture
    {
        [Fact]
        public void CanRemoveSuppressSignatureVerificationAttributes()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <PackageGroup Id='msi'>",
                "      <MsiPackage Id='MsiPackage1' SuppressSignatureVerification='yes'>",
                "      </MsiPackage>",
                "    </PackageGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <PackageGroup Id=\"msi\">",
                "      <MsiPackage Id=\"MsiPackage1\">",
                "      </MsiPackage>",
                "    </PackageGroup>",
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
                "[Converted] The chain package element contains obsolete 'SuppressSignatureVerification' attribute. The attribute will be removed. (SuppressSignatureVerificationObsolete)",
            }, messaging.Messages.Select(m => m.ToString()).ToArray());

            Assert.Equal(1, errors);
        }
    }
}
