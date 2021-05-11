// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    internal class ExampleCompilerExtension : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://www.example.com/scheams/v1/wxs";
        public string BundleExtensionId => "ExampleBundleExtension";

        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            var processed = false;

            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                case "Fragment":
                    switch (element.Name.LocalName)
                    {
                        case "ExampleEnsureTable":
                            this.ParseExampleEnsureTableElement(intermediate, section, element);
                            processed = true;
                            break;
                        case "ExampleSearch":
                            this.ParseExampleSearchElement(intermediate, section, element);
                            processed = true;
                            break;
                        case "ExampleSearchRef":
                            this.ParseExampleSearchRefElement(intermediate, section, element);
                            processed = true;
                            break;
                    }
                    break;
                case "Component":
                    switch (element.Name.LocalName)
                    {
                        case "Example":
                            this.ParseExampleElement(intermediate, section, element);
                            processed = true;
                            break;
                    }
                    break;
            }

            if (!processed)
            {
                base.ParseElement(intermediate, section, parentElement, element, context);
            }
        }

        private void ParseExampleElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string value = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;

                        case "Value":
                            value = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseAttribute(intermediate, section, element, attrib, null);
                }
            }

            if (null == id)
            {
                //this.Messaging(WixErrors.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (!this.Messaging.EncounteredError)
            {
                var symbol = this.ParseHelper.CreateSymbol(section, sourceLineNumbers, "Example", id);
                symbol.Set(0, value);
            }
        }

        private void ParseExampleEnsureTableElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            this.ParseHelper.EnsureTable(section, sourceLineNumbers, ExampleTableDefinitions.NotInAll);
        }

        private void ParseExampleSearchElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string searchFor = null;
            string variable = null;
            string condition = null;
            string after = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Variable":
                            variable = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "After":
                            after = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SearchFor":
                            searchFor = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseAttribute(intermediate, section, element, attrib, null);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, this.BundleExtensionId);
            }

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new ExampleSearchSymbol(sourceLineNumbers, id)
                {
                    SearchFor = searchFor,
                });
            }
        }

        private void ParseExampleSearchRefElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            var refId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ExampleSymbolDefinitions.ExampleSearch, refId);
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

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);
        }
    }
}
