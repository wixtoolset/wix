// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Driver Install Frameworks for Applications Extension.
    /// </summary>
    public sealed class DifxAppCompiler : CompilerExtension
    {
        private HashSet<string> components;

        /// <summary>
        /// Instantiate a new DifxAppCompiler.
        /// </summary>
        public DifxAppCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/difxapp";
            this.components = new HashSet<string>();
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    string componentId = context["ComponentId"];
                    string directoryId = context["DirectoryId"];

                    switch (element.Name.LocalName)
                    {
                        case "Driver":
                            this.ParseDriverElement(element, componentId);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        /// <summary>
        /// Parses a Driver element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseDriverElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int attributes = 0;
            int sequence = CompilerConstants.IntegerNotSet;

            // check the number of times a Driver element has been nested under this Component element
            if (null != componentId)
            {
                if (this.components.Contains(componentId))
                {
                    this.Core.OnMessage(WixErrors.TooManyElements(sourceLineNumbers, "Component", node.Name.LocalName, 1));
                }
                else
                {
                    this.components.Add(componentId);
                }
            }

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "AddRemovePrograms":
                        if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x4;
                        }
                        break;
                    case "DeleteFiles":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x10;
                        }
                        break;
                    case "ForceInstall":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x1;
                        }
                        break;
                    case "Legacy":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x8;
                        }
                        break;
                    case "PlugAndPlayPrompt":
                        if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x2;
                        }
                        break;
                    case "Sequence":
                        sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "MsiDriverPackages");
                row[0] = componentId;
                row[1] = attributes;
                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    row[2] = sequence;
                }

                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "MsiProcessDrivers");
            }
        }
    }
}
