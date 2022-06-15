// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class MsuPackageFixture : BaseConverterFixture
    {
        [Fact]
        public void CanRemoveMsuPackageDeprecatedAttributes()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <PackageGroup Id='msu'>",
                "      <MsuPackage Id='PermanentMsuPackage' KB='1234' Permanent='yes' DetectCondition='none'>",
                "        <MsuPackagePayload DownloadUrl='example.com' SourceFile='ignored.msu' />",
                "      </MsuPackage>",
                "    </PackageGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <PackageGroup Id=\"msu\">",
                "      <MsuPackage Id=\"PermanentMsuPackage\" DetectCondition=\"none\">",
                "        <MsuPackagePayload DownloadUrl=\"example.com\" SourceFile=\"ignored.msu\" />",
                "      </MsuPackage>",
                "    </PackageGroup>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(new[]
            {
                "[Converted] The MsuPackage element contains obsolete 'KB' attribute. Windows no longer supports silently removing MSUs so the attribute is unnecessary. The attribute will be removed. (MsuPackageKBObsolete)",
                "[Converted] The MsuPackage element contains obsolete 'Permanent' attribute. MSU packages are now always permanent because Windows no longer supports silently removing MSUs. The attribute will be removed. (MsuPackagePermanentObsolete)",
            }, messaging.Messages.Select(m => m.ToString()).ToArray());
            WixAssert.CompareLineByLine(expected, actualLines);

            Assert.Equal(2, errors);
        }
    }
}
