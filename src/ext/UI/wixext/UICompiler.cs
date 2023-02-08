// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.UI
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;

    /// <summary>
    /// The decompiler for the WiX Toolset UI Extension.
    /// </summary>
    public sealed class UICompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/ui";

        public override List<string> ActionNames { get; } = new List<string> { "WixUIValidatePath", "WixUIPrintEula" };

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
                case "Fragment":
                case "Module":
                case "PatchFamily":
                case "Package":
                case "UI":
                    switch (element.Name.LocalName)
                    {
                        case "WixUI":
                            this.ParseWixUIElement(intermediate, section, element);
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
        /// Parses a WixUI element.
        /// </summary>
        private void ParseWixUIElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string id = null;
            string installDirectory = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallDirectory":
                            installDirectory = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }
            else
            {
                var platform = this.Context.Platform == Platform.ARM64 ? "A64" : this.Context.Platform.ToString();
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixUI, id);
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixUI, $"{id}_{platform}");

                if (installDirectory != null)
                {
                    section.AddSymbol(new PropertySymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WIXUI_INSTALLDIR"))
                    {
                        Value = installDirectory
                    });
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);
        }
    }
}
