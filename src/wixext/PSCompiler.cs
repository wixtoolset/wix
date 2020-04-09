// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.PowerShell
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset PowerShell Extension.
    /// </summary>
    public sealed class PSCompiler : BaseCompilerExtension
    {
        private const string KeyFormat = @"SOFTWARE\Microsoft\PowerShell\{0}\PowerShellSnapIns\{1}";
        private const string VarPrefix = "PSVersionMajor";

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/powershell";

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
                case "File":
                    string fileId = context["FileId"];
                    string componentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "FormatsFile":
                            this.ParseExtensionsFile(intermediate, section, element, "Formats", fileId, componentId);
                            break;

                        case "SnapIn":
                            this.ParseSnapInElement(intermediate, section, element, fileId, componentId);
                            break;

                        case "TypesFile":
                            this.ParseExtensionsFile(intermediate, section, element, "Types", fileId, componentId);
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
        /// Parses a SnapIn element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="fileId">Identifier for parent file.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseSnapInElement(Intermediate intermediate, IntermediateSection section, XElement node, string fileId, string componentId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string id = null;
            string assemblyName = null;
            string customSnapInType = null;
            string description = null;
            string descriptionIndirect = null;
            Version requiredPowerShellVersion = CompilerConstants.IllegalVersion;
            string vendor = null;
            string vendorIndirect = null;
            string version = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;

                        case "CustomSnapInType":
                            customSnapInType = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        case "DescriptionIndirect":
                            descriptionIndirect = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        case "RequiredPowerShellVersion":
                            string ver = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            requiredPowerShellVersion = new Version(ver);
                            break;

                        case "Vendor":
                            vendor = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        case "VendorIndirect":
                            vendorIndirect = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        case "Version":
                            version = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
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

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            // Default to require PowerShell 1.0.
            if (CompilerConstants.IllegalVersion == requiredPowerShellVersion)
            {
                requiredPowerShellVersion = new Version(1, 0);
            }

            // If the snap-in version isn't explicitly specified, get it
            // from the assembly version at bind time.
            if (null == version)
            {
                version = String.Format("!(bind.assemblyVersion.{0})", fileId);
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "FormatsFile":
                            this.ParseExtensionsFile(intermediate, section, child, "Formats", id, componentId);
                            break;
                        case "TypesFile":
                            this.ParseExtensionsFile(intermediate, section, child, "Types", id, componentId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, node, child);
                }
            }

            // Get the major part of the required PowerShell version which is
            // needed for the registry key, and put that into a WiX variable
            // for use in Formats and Types files. PowerShell v2 still uses 1.
            int major = (2 == requiredPowerShellVersion.Major) ? 1 : requiredPowerShellVersion.Major;

            var variableId = new Identifier(AccessModifier.Public, String.Format(CultureInfo.InvariantCulture, "{0}_{1}", VarPrefix, id));
            var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable", variableId);
            wixVariableRow.Value = major.ToString(CultureInfo.InvariantCulture);
            wixVariableRow.Overridable = false;

            RegistryRootType registryRoot = RegistryRootType.LocalMachine; // HKLM
            string registryKey = String.Format(CultureInfo.InvariantCulture, KeyFormat, major, id);

            this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "ApplicationBase", String.Format(CultureInfo.InvariantCulture, "[${0}]", componentId), componentId, false);

            // set the assembly name automatically when binding.
            // processorArchitecture is not handled correctly by PowerShell v1.0
            // so format the assembly name explicitly.
            assemblyName = String.Format(CultureInfo.InvariantCulture, "!(bind.assemblyName.{0}), Version=!(bind.assemblyVersion.{0}), Culture=!(bind.assemblyCulture.{0}), PublicKeyToken=!(bind.assemblyPublicKeyToken.{0})", fileId);
            this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "AssemblyName", assemblyName, componentId, false);

            if (null != customSnapInType)
            {
                this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "CustomPSSnapInType", customSnapInType, componentId, false);
            }

            if (null != description)
            {
                this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "Description", description, componentId, false);
            }

            if (null != descriptionIndirect)
            {
                this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "DescriptionIndirect", descriptionIndirect, componentId, false);
            }

            this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "ModuleName", String.Format(CultureInfo.InvariantCulture, "[#{0}]", fileId), componentId, false);

            this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "PowerShellVersion", requiredPowerShellVersion.ToString(2), componentId, false);

            if (null != vendor)
            {
                this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "Vendor", vendor, componentId, false);
            }

            if (null != vendorIndirect)
            {
                this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "VendorIndirect", vendorIndirect, componentId, false);
            }

            if (null != version)
            {
                this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, "Version", version, componentId, false);
            }
        }

        /// <summary>
        /// Parses a FormatsFile and TypesFile element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="valueName">Registry value name.</param>
        /// <param name="id">Idendifier for parent file or snap-in.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseExtensionsFile(Intermediate intermediate, IntermediateSection section, XElement node, string valueName, string id, string componentId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string fileId = null;
            string snapIn = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "FileId":
                            fileId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            snapIn = id;
                            break;

                        case "SnapIn":
                            fileId = id;
                            snapIn = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == fileId && null == snapIn)
            {
                this.Messaging.Write(PSErrors.NeitherIdSpecified(sourceLineNumbers, valueName));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            RegistryRootType registryRoot = RegistryRootType.LocalMachine; // HKLM
            string registryKey = String.Format(CultureInfo.InvariantCulture, KeyFormat, String.Format(CultureInfo.InvariantCulture, "!(wix.{0}_{1})", VarPrefix, snapIn), snapIn);

            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "File", fileId);
            this.ParseHelper.CreateRegistryRow(section, sourceLineNumbers, registryRoot, registryKey, valueName, String.Format(CultureInfo.InvariantCulture, "[~][#{0}]", fileId), componentId, false);
        }
    }
}
