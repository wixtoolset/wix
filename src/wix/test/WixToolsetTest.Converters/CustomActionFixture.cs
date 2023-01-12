// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class CustomActionFixture : BaseConverterFixture
    {
        [Fact]
        public void CanConvertCustomAction()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomAction Id='Foo1' BinaryKey='WixCA' DllEntry='CAQuietExec' />",
                "  <CustomAction Id='Foo2' BinaryKey='WixCA_x64' DllEntry='CAQuietExec64' />",
                "  <CustomAction Id='Foo3' BinaryKey='UtilCA' DllEntry='WixQuietExec' />",
                "  <CustomAction Id='Foo4' BinaryKey='UtilCA_x64' DllEntry='WixQuietExec64' />",
                "  <CustomAction Id='Foo5' BinaryKey='WixCA' DllEntry='CAQuietExec64' Execute='deferred' Return='check' Impersonate='no' />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomAction Id=\"Foo1\" DllEntry=\"WixQuietExec\" BinaryRef=\"Wix4UtilCA_X86\" />",
                "  <CustomAction Id=\"Foo2\" DllEntry=\"WixQuietExec64\" BinaryRef=\"Wix4UtilCA_X64\" />",
                "  <CustomAction Id=\"Foo3\" DllEntry=\"WixQuietExec\" BinaryRef=\"Wix4UtilCA_X86\" />",
                "  <CustomAction Id=\"Foo4\" DllEntry=\"WixQuietExec64\" BinaryRef=\"Wix4UtilCA_X64\" />",
                "  <CustomAction Id=\"Foo5\" DllEntry=\"WixQuietExec64\" Execute=\"deferred\" Return=\"check\" Impersonate=\"no\" BinaryRef=\"Wix4UtilCA_X86\" />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(14, errors);
        }

        [Fact]
        public void CanConvertCustomActionScript()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomAction Id='Foo' Script='jscript'>",
                "  function() {",
                "    var x = 0;",
                "    return x;",
                "  }",
                "  </CustomAction>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomAction Id=\"Foo\" Script=\"jscript\" ScriptSourceFile=\"Foo.js\" />",
                "</Wix>",
            };

            var expectedScript = String.Join("\n",
                "function() {",
                "    var x = 0;",
                "    return x;",
                "  }");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(2, errors);
            WixAssert.CompareLineByLine(expected, actual);

            var script = File.ReadAllText("Foo.js");
            WixAssert.StringEqual(expectedScript, script);
        }
    }
}
