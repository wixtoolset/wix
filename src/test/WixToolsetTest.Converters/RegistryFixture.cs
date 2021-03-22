// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class RegistryFixture : BaseConverterFixture
    {
        [Fact]
        public void FixRegistryKeyAction()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Component>",
                "        <RegistryKey Id='ExampleRegistryKey1' Action='create' Root='HKLM' Key='TestRegistryKey1' />",
                "        <RegistryKey Id='ExampleRegistryKey2' Action='createAndRemoveOnUninstall' Root='HKLM' Key='TestRegistryKey2' />",
                "        <RegistryKey Id='ExampleRegistryKey3' Action='none' Root='HKLM' Key='TestRegistryKey3' />",
                "    </Component>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Component>",
                "        <RegistryKey Id=\"ExampleRegistryKey1\" Root=\"HKLM\" Key=\"TestRegistryKey1\" ForceCreateOnInstall=\"yes\" />",
                "        <RegistryKey Id=\"ExampleRegistryKey2\" Root=\"HKLM\" Key=\"TestRegistryKey2\" ForceCreateOnInstall=\"yes\" ForceDeleteOnUninstall=\"yes\" />",
                "        <RegistryKey Id=\"ExampleRegistryKey3\" Root=\"HKLM\" Key=\"TestRegistryKey3\" />",
                "    </Component>",
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
    }
}
