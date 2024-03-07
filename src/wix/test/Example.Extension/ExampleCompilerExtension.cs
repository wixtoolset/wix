// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    internal class ExampleCompilerExtension : BaseCompilerExtension
    {
        private const string BootstrapperExtensionId = "ExampleBootstrapperExtension";

        public override XNamespace Namespace => ExampleConstants.Namespace;

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
                    var componentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "Example":
                            this.ParseExampleElement(intermediate, section, element, componentId);
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

        public override IComponentKeyPath ParsePossibleKeyPathElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    var componentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "ExampleSetKeyPath":
                            return this.ParseExampleSetKeyPathElement(intermediate, section, element, componentId);
                    }
                    break;
            }

            return base.ParsePossibleKeyPathElement(intermediate, section, parentElement, element, context);
        }

        private void ParseExampleElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
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
                symbol.Set(0, componentId);
                symbol.Set(1, value);
            }
        }

        private IComponentKeyPath ParseExampleSetKeyPathElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier file = null;
            Identifier reg = null;
            var explicitly = false;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "File":
                            file = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;

                        case "Registry":
                            reg = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;

                        case "Explicitly":
                            explicitly = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.Yes;
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

            if (file == null && reg == null)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "File", "Registry", true));
            }

            if (!this.Messaging.EncounteredError)
            {
                var componentKeyPath = this.CreateComponentKeyPath();
                componentKeyPath.Id = file ?? reg;
                componentKeyPath.Explicit = explicitly;
                componentKeyPath.Type = file is null ? PossibleKeyPathType.Registry : PossibleKeyPathType.File;
                return componentKeyPath;
            }

            return null;
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
                this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, BootstrapperExtensionId);
            }

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new ExampleSearchSymbol(sourceLineNumbers, id)
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
