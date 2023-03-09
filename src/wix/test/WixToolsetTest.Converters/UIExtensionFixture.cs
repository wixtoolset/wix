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

    public class UIExtensionFixture : BaseConverterFixture
    {
        [Fact]
        public void FixUIRefs()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <UIRef Id=\"WixUI_Advanced\" />",
                "    <UIRef Id=\"WixUI_FeatureTree\" />",
                "    <UIRef Id=\"WixUI_BobsSpecialUI\" />",
                "    <UI>",
                "      <UIRef Id=\"WixUI_Advanced\" />",
                "      <UIRef Id=\"WixUI_FeatureTree\" />",
                "      <UIRef Id=\"WixUI_BobsSpecialUI\" />",
                "    </UI>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:ui=\"http://wixtoolset.org/schemas/v4/wxs/ui\">",
                "  <Fragment>",
                "    <ui:WixUI Id=\"WixUI_Advanced\" />",
                "    <ui:WixUI Id=\"WixUI_FeatureTree\" />",
                "    <UIRef Id=\"WixUI_BobsSpecialUI\" />",
                "    <UI>",
                "      <ui:WixUI Id=\"WixUI_Advanced\" />",
                "      <ui:WixUI Id=\"WixUI_FeatureTree\" />",
                "      <UIRef Id=\"WixUI_BobsSpecialUI\" />",
                "    </UI>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(5, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixPrintCustomAction()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <UI>",
                "      <Dialog Id='CustomResumeDlg' Width='370' Height='270' Title='!(loc.ResumeDlg_Title)'>",
                "        <Control Id='Print' Type='PushButton' X='112' Y='243' Width='56' Height='17' Text='!(loc.WixUIPrint)'>",
                "          <Publish Event='DoAction' Value='WixUIPrintEula'>1</Publish>",
                "        </Control>",
                "      </Dialog>",
                "    </UI>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <UI>",
                "      <Dialog Id=\"CustomResumeDlg\" Width=\"370\" Height=\"270\" Title=\"!(loc.ResumeDlg_Title)\">",
                "        <Control Id=\"Print\" Type=\"PushButton\" X=\"112\" Y=\"243\" Width=\"56\" Height=\"17\" Text=\"!(loc.WixUIPrint)\">",
                "          <Publish Event=\"DoAction\" Value=\"WixUIPrintEula_$(sys.BUILDARCHSHORT)\" />",
                "        </Control>",
                "      </Dialog>",
                "    </UI>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(4, errors);
        }

        [Fact]
        public void FixValidatePathCustomAction()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <UI Id='WixUI_Test'>",
                "      <Publish Dialog='BrowseDlg' Control='OK' Event='DoAction' Value='WixUIValidatePath' Order='3' />",
                "    </UI>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <UI Id=\"WixUI_Test\">",
                "      <Publish Dialog=\"BrowseDlg\" Control=\"OK\" Event=\"DoAction\" Value=\"WixUIValidatePath_$(sys.BUILDARCHSHORT)\" Order=\"3\" />",
                "    </UI>",
                "  </Fragment>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Single(messaging.Messages.Where(m => m.Id == 65));
            Assert.Equal(2, errors);
        }
    }
}
