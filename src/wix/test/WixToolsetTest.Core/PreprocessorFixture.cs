// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Core
{
    using System;
    using System.IO;
    using System.Xml;
    using WixBuildTools.TestSupport;
    using WixToolset.Core;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class PreprocessorFixture
    {
        [Fact]
        public void CanPreprocessWithSingleEquals()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?define A=0?>",
                "<?if $(A)=\"0\" ?>",
                "  <Package />",
                "<?endif?>",
                "</Wix>"
            );
            var expected = new[]
            {
                "<Wix>",
                "  <Package />",
                "</Wix>"
            };

            var result = PreprocessFromString(input);

            var actual = result.Document.ToString().Split("\r\n");
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanPreprocessWithDoubleEquals()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?define A=0?>",
                "<?if $(A)==\"0\" ?>",
                "  <Fragment />",
                "<?endif?>",
                "</Wix>"
            );
            var expected = new[]
            {
                "<Wix>",
                "  <Fragment />",
                "</Wix>"
            };

            var result = PreprocessFromString(input);

            var actual = result.Document.ToString().Split("\r\n");
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanPreprocessFalsyCondition()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?define A=0?>",
                "<?if $(A) ?>",
                "  <Fragment />",
                "<?endif?>",
                "</Wix>"
            );
            var expected = new[]
            {
                "<Wix />"
            };

            var result = PreprocessFromString(input);

            var actual = result.Document.ToString().Split("\r\n");
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanPreprocessTruthyCondition()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?define A=1?>",
                "<?if $(A) ?>",
                "  <Fragment />",
                "<?endif?>",
                "</Wix>"
            );
            var expected = new[]
            {
                "<Wix>",
                "  <Fragment />",
                "</Wix>"
            };

            var result = PreprocessFromString(input);

            var actual = result.Document.ToString().Split("\r\n");
            WixAssert.CompareLineByLine(expected, actual);
        }

        private static IPreprocessResult PreprocessFromString(string xml)
        {
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader);

            var sp = WixToolsetServiceProviderFactory.CreateServiceProvider();

            var context = sp.GetService<IPreprocessContext>();
            context.SourcePath = @"path\provided\for\testing\purposes\only.wxs";

            var preprocessor = context.ServiceProvider.GetService<IPreprocessor>();
            var result = preprocessor.Preprocess(context, xmlReader);
            return result;
        }
    }
}
