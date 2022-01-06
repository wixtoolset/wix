// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace ForTestingUseOnly
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using ForTestingUseOnly.Symbols;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// Extension for doing completely unsupported things in the name of testing.
    /// </summary>
    public sealed class ForTestingUseOnlyCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/fortestinguseonly";

        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (element.Name.LocalName)
            {
                case "ForTestingUseOnlyBundle":
                    this.ParseForTestingUseOnlyBundleElement(intermediate, section, element);
                    break;
                default:
                    this.ParseHelper.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        private void ParseForTestingUseOnlyBundleElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string bundleId = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            bundleId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == bundleId)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }
            else
            {
                bundleId = Guid.Parse(bundleId).ToString("B").ToUpperInvariant();
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new ForTestingUseOnlyBundleSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "ForTestingUseOnlyBundle"))
                {
                    BundleId = bundleId,
                });
            }
        }
    }
}
