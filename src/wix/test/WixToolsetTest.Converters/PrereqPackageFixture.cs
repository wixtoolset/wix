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

    public class PrereqPackageFixture : BaseConverterFixture
    {
        [Fact]
        public void CanConvertWixMbaPrereqPackageIdToPrereqPackage()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs' xmlns:bal='http://wixtoolset.org/schemas/v4/wxs/bal'>",
                "  <Fragment>",
                "    <WixVariable Id='WixMbaPrereqPackageId' Value='NetFx452Web' />",
                "    <WixVariable Id='WixMbaPrereqLicenseUrl' Value='$(var.NetFx452EulaLink)' Overridable='yes' />",
                "    <PackageGroup Id='NetFx452Web'>",
                "      <ExePackage Id='NetFx452Web' />",
                "    </PackageGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:bal=\"http://wixtoolset.org/schemas/v4/wxs/bal\">",
                "  <Fragment>",
                "    <PackageGroup Id=\"NetFx452Web\">",
                "      <ExePackage Id=\"NetFx452Web\" bal:PrereqPackage=\"yes\" bal:PrereqLicenseUrl=\"$(var.NetFx452EulaLink)\" />",
                "    </PackageGroup>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(2, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(new[]
            {
                "[Converted] The magic WixVariable 'WixMbaPrereqPackageId' has been removed. Add bal:PrereqPackage=\"yes\" to the target package instead. (WixMbaPrereqPackageIdDeprecated)",
                "[Converted] The magic WixVariable 'WixMbaPrereqLicenseUrl' has been removed. Add bal:PrereqLicenseUrl=\"<url>\" to a prereq package instead. (WixMbaPrereqLicenseUrlDeprecated)",
            }, messaging.Messages.Select(m => m.ToString()).ToArray());
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void CanWarnAboutOrphanWixMbaPrereqPackageId()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <WixVariable Id='WixMbaPrereqPackageId' Value='NetFx452Web' />",
                "    <WixVariable Id='WixMbaPrereqLicenseUrl' Value='$(var.NetFx452EulaLink)' Overridable='yes' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <WixVariable Id=\"WixMbaPrereqPackageId\" Value=\"NetFx452Web\" />",
                "    <WixVariable Id=\"WixMbaPrereqLicenseUrl\" Value=\"$(var.NetFx452EulaLink)\" Overridable=\"yes\" />",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(2, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(new[]
            {
                "The magic WixVariable 'WixMbaPrereqPackageId' has been removed. Add bal:PrereqPackage=\"yes\" to the target package instead. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles (WixMbaPrereqPackageIdDeprecated)",
                "The magic WixVariable 'WixMbaPrereqLicenseUrl' has been removed. Add bal:PrereqLicenseUrl=\"<url>\" to a prereq package instead. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-bundles (WixMbaPrereqLicenseUrlDeprecated)",
            }, messaging.Messages.Select(m => m.ToString()).ToArray());
            WixAssert.CompareLineByLine(expected, actualLines);
        }
    }
}
