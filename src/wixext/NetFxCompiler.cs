// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Netfx.Tuples;

    /// <summary>
    /// The compiler for the WiX Toolset .NET Framework Extension.
    /// </summary>
    public sealed class NetfxCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/netfx";

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "File":
                    string fileId = context["FileId"];

                    switch (element.Name.LocalName)
                    {
                        case "NativeImage":
                            this.ParseNativeImageElement(intermediate, section, element, fileId);
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
        /// Parses a NativeImage element.
        /// </summary>
        /// <param name="element">The element to parse.</param>
        /// <param name="fileId">The file identifier of the parent element.</param>
        private void ParseNativeImageElement(Intermediate intermediate, IntermediateSection section, XElement element, string fileId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string appBaseDirectory = null;
            string assemblyApplication = null;
            int attributes = 0x8; // 32bit is on by default
            int priority = 3;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AppBaseDirectory":
                            appBaseDirectory = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);

                            // See if a formatted value is specified.
                            if (-1 == appBaseDirectory.IndexOf("[", StringComparison.Ordinal))
                            {
                                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Directory", appBaseDirectory);
                            }
                            break;
                        case "AssemblyApplication":
                            assemblyApplication = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);

                            // See if a formatted value is specified.
                            if (-1 == assemblyApplication.IndexOf("[", StringComparison.Ordinal))
                            {
                                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "File", assemblyApplication);
                            }
                            break;
                        case "Debug":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1;
                            }
                            break;
                        case "Dependencies":
                            if (YesNoType.No == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x2;
                            }
                            break;
                        case "Platform":
                            string platformValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < platformValue.Length)
                            {
                                switch (platformValue)
                                {
                                    case "32bit":
                                        // 0x8 is already on by default
                                        break;
                                    case "64bit":
                                        attributes &= ~0x8;
                                        attributes |= 0x10;
                                        break;
                                    case "all":
                                        attributes |= 0x10;
                                        break;
                                }
                            }
                            break;
                        case "Priority":
                            priority = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 3);
                            break;
                        case "Profile":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x4;
                            }
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

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "NetFxScheduleNativeImage");

            if (!this.Messaging.EncounteredError)
            {
                section.Tuples.Add(new NetFxNativeImageTuple(sourceLineNumbers, id)
                {
                    FileRef = fileId,
                    Priority = priority,
                    Attributes = attributes,
                    ApplicationFileRef = assemblyApplication,
                    ApplicationBaseDirectoryRef = appBaseDirectory,
                });
            }
        }
    }
}
