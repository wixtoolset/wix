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

        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            var processed = false;

            switch (parentElement.Name.LocalName)
            {
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
                var tuple = this.ParseHelper.CreateTuple(section, sourceLineNumbers, "Example", id);
                tuple.Set(1, value);
            }
        }
    }
}
