// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class BitnessFixture : BaseConverterFixture
    {
        [Fact]
        public void FixComponentBitness()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Component>",
                "        <File Source='default.exe' />",
                "    </Component>",
                "    <Component Win64='no'>",
                "        <File Source='32bit.exe' />",
                "    </Component>",
                "    <Component Win64='yes'>",
                "        <File Source='64bit.exe' />",
                "    </Component>",
                "    <Component Win64='$(var.Win64)'>",
                "        <File Source='unconvert.exe' />",
                "    </Component>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Component>",
                "        <File Id=\"default.exe\" Source=\"default.exe\" />",
                "    </Component>",
                "    <Component Bitness=\"always32\">",
                "        <File Id=\"_32bit.exe\" Source=\"32bit.exe\" />",
                "    </Component>",
                "    <Component Bitness=\"always64\">",
                "        <File Id=\"_64bit.exe\" Source=\"64bit.exe\" />",
                "    </Component>",
                "    <Component Bitness=\"$(var.Win64)\">",
                "        <File Id=\"unconvert.exe\" Source=\"unconvert.exe\" />",
                "    </Component>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(10, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixRegistrySearchBitness()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Property Id='BITNESSDEFAULT'>",
                "        <RegistrySearch Id='SampleRegSearch' Root='HKLM' Key='SampleReg' Type='raw'></RegistrySearch>",
                "    </Property>",
                "    <Property Id='BITNESS32'>",
                "        <RegistrySearch Id='SampleRegSearch' Root='HKLM' Key='SampleReg' Type='raw' Win64='no'></RegistrySearch>",
                "    </Property>",
                "    <Property Id='BITNESS64'>",
                "        <RegistrySearch Id='SampleRegSearch' Root='HKLM' Key='SampleReg' Type='raw' Win64='yes'></RegistrySearch>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"BITNESSDEFAULT\">",
                "        <RegistrySearch Id=\"SampleRegSearch\" Root=\"HKLM\" Key=\"SampleReg\" Type=\"raw\"></RegistrySearch>",
                "    </Property>",
                "    <Property Id=\"BITNESS32\">",
                "        <RegistrySearch Id=\"SampleRegSearch\" Root=\"HKLM\" Key=\"SampleReg\" Type=\"raw\" Bitness=\"always32\"></RegistrySearch>",
                "    </Property>",
                "    <Property Id=\"BITNESS64\">",
                "        <RegistrySearch Id=\"SampleRegSearch\" Root=\"HKLM\" Key=\"SampleReg\" Type=\"raw\" Bitness=\"always64\"></RegistrySearch>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(4, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixUtilRegistrySearchBitness()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment>",
                "    <util:RegistrySearch Id='RegValue' Root='HKLM' Key='Converter' Variable='Test' />",
                "    <util:RegistrySearch Id='RegValue2' Root='HKLM' Key='Converter' Variable='Test' Result='value' Win64='no' />",
                "    <util:RegistrySearch Id='RegValue3' Root='HKLM' Key='Converter' Variable='Test' Result='exists' Win64='yes' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:util=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <Fragment>",
                "    <util:RegistrySearch Id=\"RegValue\" Root=\"HKLM\" Key=\"Converter\" Variable=\"Test\" />",
                "    <util:RegistrySearch Id=\"RegValue2\" Root=\"HKLM\" Key=\"Converter\" Variable=\"Test\" Result=\"value\" Bitness=\"always32\" />",
                "    <util:RegistrySearch Id=\"RegValue3\" Root=\"HKLM\" Key=\"Converter\" Variable=\"Test\" Result=\"exists\" Bitness=\"always64\" />",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(6, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixApprovedExeBitness()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Bundle>",
                "    <ApprovedExeForElevation Id='Default' Key='WixToolset\\BurnTesting' Value='Test' />",
                "    <ApprovedExeForElevation Id='Bitness32' Key='WixToolset\\BurnTesting' Value='Test' Win64='no' />",
                "    <ApprovedExeForElevation Id='Bitness64' Key='WixToolset\\BurnTesting' Value='Test' Win64='yes' />",
                "  </Bundle>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Bundle>",
                "    <ApprovedExeForElevation Id=\"Default\" Key=\"WixToolset\\BurnTesting\" Value=\"Test\" />",
                "    <ApprovedExeForElevation Id=\"Bitness32\" Key=\"WixToolset\\BurnTesting\" Value=\"Test\" Bitness=\"always32\" />",
                "    <ApprovedExeForElevation Id=\"Bitness64\" Key=\"WixToolset\\BurnTesting\" Value=\"Test\" Bitness=\"always64\" />",
                "  </Bundle>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(4, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }
    }
}
