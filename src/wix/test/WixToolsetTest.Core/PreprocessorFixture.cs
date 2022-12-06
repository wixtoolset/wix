// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Core
{
    using System;
    using System.IO;
    using System.Xml;
    using WixInternal.TestSupport;
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

        [Fact]
        public void CanPreprocessIfdefWithFullVariableSyntaxFalsy()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?ifdef $(A) ?>",
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
        public void CanPreprocessIfdefWithFullVariableSyntaxTruthy()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?define A?>",
                "<?ifdef $(A) ?>",
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
        public void CanPreprocessIfndefWithFullVariableSyntaxTruthy()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?ifndef $(A) ?>",
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
        public void CanPreprocessIfndefWithFullVariableSyntaxFalsy()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?define A?>",
                "<?ifndef $(A) ?>",
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
        public void CanPreprocessIfdefWithFullVariableV3SyntaxTruthy()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?define A?>",
                "<?ifdef $(var.A) ?>",
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
        public void CanPreprocessIfndefWithFullVariableV3SyntaxTruthy()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?ifndef $(var.A) ?>",
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
        public void CanPreprocessIfndefWithFullVariableV3SyntaxFalsy()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?define A?>",
                "<?ifndef $(var.A) ?>",
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
        public void CanPreprocessForeach()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?foreach value in  A ; B ; C  ?>",
                "  <Fragment Id='$(value)' />",
                "<?endforeach?>",
                "</Wix>"
            );
            var expected = new[]
            {
                "<Wix>",
                "  <Fragment Id=\"A \" />",
                "  <Fragment Id=\" B \" />",
                "  <Fragment Id=\" C\" />",
                "</Wix>"
            };

            var result = PreprocessFromString(input);

            var actual = result.Document.ToString().Split("\r\n");
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanPreprocessForeachWithQuotes()
        {
            var input = String.Join(Environment.NewLine,
                "<Wix>",
                "<?foreach value in \" A ; B ; C \" ?>",
                "  <Fragment Id='$(value)' />",
                "<?endforeach?>",
                "</Wix>"
            );
            var expected = new[]
            {
                "<Wix>",
                "  <Fragment Id=\" A \" />",
                "  <Fragment Id=\" B \" />",
                "  <Fragment Id=\" C \" />",
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
