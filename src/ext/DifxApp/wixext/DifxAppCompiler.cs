// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.DifxApp
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.DifxApp.Symbols;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Driver Install Frameworks for Applications Extension.
    /// </summary>
    public sealed class DifxAppCompiler : BaseCompilerExtension
    {
        private HashSet<string> components;

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/difxapp";
        /// <summary>
        /// Instantiate a new DifxAppCompiler.
        /// </summary>
        public DifxAppCompiler()
        {
            this.components = new HashSet<string>();
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    var componentId = context["ComponentId"];
                    var directoryId = context["DirectoryId"];
                    var componentWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "Driver":
                            this.ParseDriverElement(intermediate, section, element, componentId, componentWin64);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.ParseHelper.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        /// <summary>
        /// Parses a Driver element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseDriverElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentId, bool win64)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            int attributes = 0;
            var sequence = CompilerConstants.IntegerNotSet;

            // check the number of times a Driver element has been nested under this Component element
            if (null != componentId)
            {
                if (this.components.Contains(componentId))
                {
                    this.Messaging.Write(ErrorMessages.TooManyElements(sourceLineNumbers, "Component", node.Name.LocalName, 1));
                }
                else
                {
                    this.components.Add(componentId);
                }
            }

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "AddRemovePrograms":
                        if (YesNoType.No == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x4;
                        }
                        break;
                    case "DeleteFiles":
                        if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x10;
                        }
                        break;
                    case "ForceInstall":
                        if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x1;
                        }
                        break;
                    case "Legacy":
                        if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x8;
                        }
                        break;
                    case "PlugAndPlayPrompt":
                        if (YesNoType.No == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x2;
                        }
                        break;
                    case "Sequence":
                        sequence = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                        break;
                    default:
                        this.ParseHelper.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            if (!this.Messaging.EncounteredError)
            {
                switch (this.Context.Platform)
                {
                    case Platform.X86:
                        this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, "MsiProcessDrivers");
                        break;
                    case Platform.X64:
                        this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, "MsiProcessDrivers_x64");
                        break;
                }

                var symbol = section.AddSymbol(new MsiDriverPackagesSymbol(sourceLineNumbers)
                {
                    ComponentRef = componentId,
                    Flags = attributes,
                });

                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    symbol.Sequence = sequence;
                }
            }
        }
    }
}
