// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Visual Studio Extension.
    /// </summary>
    public sealed class VSCompiler : CompilerExtension
    {
        internal const int MsidbCustomActionTypeExe = 0x00000002;  // Target = command line args
        internal const int MsidbCustomActionTypeProperty = 0x00000030;  // Source = full path to executable
        internal const int MsidbCustomActionTypeContinue = 0x00000040;  // ignore action return status; continue running
        internal const int MsidbCustomActionTypeRollback = 0x00000100;  // in conjunction with InScript: queue in Rollback script
        internal const int MsidbCustomActionTypeInScript = 0x00000400;  // queue for execution within script
        internal const int MsidbCustomActionTypeNoImpersonate = 0x00000800;  // queue for not impersonating

        /// <summary>
        /// Instantiate a new HelpCompiler.
        /// </summary>
        public VSCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/vs";
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
                    switch (element.Name.LocalName)
                    {
                        case "VsixPackage":
                            this.ParseVsixPackageElement(element, context["ComponentId"], null);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "File":
                    switch (element.Name.LocalName)
                    {
                        case "HelpCollection":
                            this.ParseHelpCollectionElement(element, context["FileId"]);
                            break;
                        case "HelpFile":
                            this.ParseHelpFileElement(element, context["FileId"]);
                            break;
                        case "VsixPackage":
                            this.ParseVsixPackageElement(element, context["ComponentId"], context["FileId"]);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "HelpCollectionRef":
                            this.ParseHelpCollectionRefElement(element);
                            break;
                        case "HelpFilter":
                            this.ParseHelpFilterElement(element);
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
        /// Parses a HelpCollectionRef element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        private void ParseHelpCollectionRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "HelpNamespace", id);
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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "HelpFileRef":
                            this.ParseHelpFileRefElement(child, id);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }
        }

        /// <summary>
        /// Parses a HelpCollection element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="fileId">Identifier of the parent File element.</param>
        private void ParseHelpCollectionElement(XElement node, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string description = null;
            string name = null;
            YesNoType suppressCAs = YesNoType.No;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == description)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "HelpFileRef":
                            this.ParseHelpFileRefElement(child, id);
                            break;
                        case "HelpFilterRef":
                            this.ParseHelpFilterRefElement(child, id);
                            break;
                        case "PlugCollectionInto":
                            this.ParsePlugCollectionIntoElement(child, id);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpNamespace");
                row[0] = id;
                row[1] = name;
                row[2] = fileId;
                row[3] = description;

                if (YesNoType.No == suppressCAs)
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        /// <summary>
        /// Parses a HelpFile element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="fileId">Identifier of the parent file element.</param>
        private void ParseHelpFileElement(XElement node, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string name = null;
            int language = CompilerConstants.IntegerNotSet;
            string hxi = null;
            string hxq = null;
            string hxr = null;
            string samples = null;
            YesNoType suppressCAs = YesNoType.No;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "AttributeIndex":
                            hxr = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", hxr);
                            break;
                        case "Index":
                            hxi = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", hxi);
                            break;
                        case "Language":
                            language = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SampleLocation":
                            samples = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", samples);
                            break;
                        case "Search":
                            hxq = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "File", hxq);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            //uninstall will always fail silently, leaving file registered, if Language is not set
            if (CompilerConstants.IntegerNotSet == language)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFile");
                row[0] = id;
                row[1] = name;
                row[2] = language;
                row[3] = fileId;
                row[4] = hxi;
                row[5] = hxq;
                row[6] = hxr;
                row[7] = samples;

                if (YesNoType.No == suppressCAs)
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        /// <summary>
        /// Parses a HelpFileRef element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="collectionId">Identifier of the parent help collection.</param>
        private void ParseHelpFileRefElement(XElement node, string collectionId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "HelpFile", id);
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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFileToNamespace");
                row[0] = id;
                row[1] = collectionId;
            }
        }

        /// <summary>
        /// Parses a HelpFilter element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        private void ParseHelpFilterElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string filterDefinition = null;
            string name = null;
            YesNoType suppressCAs = YesNoType.No;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "FilterDefinition":
                            filterDefinition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFilter");
                row[0] = id;
                row[1] = name;
                row[2] = filterDefinition;

                if (YesNoType.No == suppressCAs)
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        /// <summary>
        /// Parses a HelpFilterRef element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="collectionId">Identifier of the parent help collection.</param>
        private void ParseHelpFilterRefElement(XElement node, string collectionId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "HelpFilter", id);
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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFilterToNamespace");
                row[0] = id;
                row[1] = collectionId;
            }
        }

        /// <summary>
        /// Parses a PlugCollectionInto element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="parentId">Identifier of the parent help collection.</param>
        private void ParsePlugCollectionIntoElement(XElement node, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string hxa = null;
            string hxt = null;
            string hxtParent = null;
            string namespaceParent = null;
            string feature = null;
            YesNoType suppressExternalNamespaces = YesNoType.No;
            bool pluginVS05 = false;
            bool pluginVS08 = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Attributes":
                            hxa = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TableOfContents":
                            hxt = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetCollection":
                            namespaceParent = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetTableOfContents":
                            hxtParent = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetFeature":
                            feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressExternalNamespaces":
                            suppressExternalNamespaces = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            pluginVS05 = namespaceParent.Equals("MS_VSIPCC_v80", StringComparison.Ordinal);
            pluginVS08 = namespaceParent.Equals("MS.VSIPCC.v90", StringComparison.Ordinal);

            if (null == namespaceParent)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "TargetCollection"));
            }

            if (null == feature && (pluginVS05 || pluginVS08) && YesNoType.No == suppressExternalNamespaces)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "TargetFeature"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpPlugin");
                row[0] = parentId;
                row[1] = namespaceParent;
                row[2] = hxt;
                row[3] = hxa;
                row[4] = hxtParent;

                if (pluginVS05)
                {
                    if (YesNoType.No == suppressExternalNamespaces)
                    {
                        // Bring in the help 2 base namespace components for VS 2005
                        this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, String.Empty,
                            ComplexReferenceChildType.ComponentGroup, "Help2_VS2005_Namespace_Components", false);
                        // Reference CustomAction since nothing will happen without it
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction",
                            "CA_HxMerge_VSIPCC_VSCC");
                    }
                }
                else if (pluginVS08)
                {
                    if (YesNoType.No == suppressExternalNamespaces)
                    {
                        // Bring in the help 2 base namespace components for VS 2008
                        this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, String.Empty,
                            ComplexReferenceChildType.ComponentGroup, "Help2_VS2008_Namespace_Components", false);
                        // Reference CustomAction since nothing will happen without it
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction",
                            "CA_ScheduleExtHelpPlugin_VSCC_VSIPCC");
                    }
                }
                else
                {
                    // Reference the parent namespace to enforce the foreign key relationship
                    this.Core.CreateSimpleReference(sourceLineNumbers, "HelpNamespace",
                        namespaceParent);
                }
            }
        }

        /// <summary>
        /// Parses a VsixPackage element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="componentId">Identifier of the parent Component element.</param>
        /// <param name="fileId">Identifier of the parent File element.</param>
        private void ParseVsixPackageElement(XElement node, string componentId, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string propertyId = "VS_VSIX_INSTALLER_PATH";
            string packageId = null;
            YesNoType permanent = YesNoType.NotSet;
            string target = null;
            string targetVersion = null;
            YesNoType vital = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "File":
                            if (String.IsNullOrEmpty(fileId))
                            {
                                fileId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            else
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "File", "File"));
                            }
                            break;
                        case "PackageId":
                            packageId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Permanent":
                            permanent = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Target":
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (target.ToLowerInvariant())
                            {
                                case "integrated":
                                case "integratedshell":
                                    target = "IntegratedShell";
                                    break;
                                case "professional":
                                    target = "Pro";
                                    break;
                                case "premium":
                                    target = "Premium";
                                    break;
                                case "ultimate":
                                    target = "Ultimate";
                                    break;
                                case "vbexpress":
                                    target = "VBExpress";
                                    break;
                                case "vcexpress":
                                    target = "VCExpress";
                                    break;
                                case "vcsexpress":
                                    target = "VCSExpress";
                                    break;
                                case "vwdexpress":
                                    target = "VWDExpress";
                                    break;
                            }
                            break;
                        case "TargetVersion":
                            targetVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Vital":
                            vital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "VsixInstallerPathProperty":
                            propertyId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(fileId))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            if (String.IsNullOrEmpty(packageId))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PackageId"));
            }

            if (!String.IsNullOrEmpty(target) && String.IsNullOrEmpty(targetVersion))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "TargetVersion", "Target"));
            }
            else if (String.IsNullOrEmpty(target) && !String.IsNullOrEmpty(targetVersion))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Target", "TargetVersion"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                // Ensure there is a reference to the AppSearch Property that will find the VsixInstaller.exe.
                this.Core.CreateSimpleReference(sourceLineNumbers, "Property", propertyId);

                // Ensure there is a reference to the package file (even if we are a child under it).
                this.Core.CreateSimpleReference(sourceLineNumbers, "File", fileId);

                string cmdlinePrefix = "/q ";

                if (!String.IsNullOrEmpty(target))
                {
                    cmdlinePrefix = String.Format("{0} /skuName:{1} /skuVersion:{2}", cmdlinePrefix, target, targetVersion);
                }

                string installAfter = "WriteRegistryValues"; // by default, come after the registry key registration.
                int installExtraBits = VSCompiler.MsidbCustomActionTypeInScript;

                // If the package is not vital, mark the install action as continue.
                if (vital == YesNoType.No)
                {
                    installExtraBits |= VSCompiler.MsidbCustomActionTypeContinue;
                }
                else // the package is vital so ensure there is a rollback action scheduled.
                {
                    Identifier rollbackNamePerUser = this.Core.CreateIdentifier("vru", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                    Identifier rollbackNamePerMachine = this.Core.CreateIdentifier("vrm", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                    string rollbackCmdLinePerUser = String.Concat(cmdlinePrefix, " /u:\"", packageId, "\"");
                    string rollbackCmdLinePerMachine = String.Concat(rollbackCmdLinePerUser, " /admin");
                    int rollbackExtraBitsPerUser = VSCompiler.MsidbCustomActionTypeContinue | VSCompiler.MsidbCustomActionTypeRollback | VSCompiler.MsidbCustomActionTypeInScript;
                    int rollbackExtraBitsPerMachine = rollbackExtraBitsPerUser | VSCompiler.MsidbCustomActionTypeNoImpersonate;
                    string rollbackConditionPerUser = String.Format("NOT ALLUSERS AND NOT Installed AND ${0}=2 AND ?{0}>2", componentId); // NOT Installed && Component being installed but not installed already.
                    string rollbackConditionPerMachine = String.Format("ALLUSERS AND NOT Installed AND ${0}=2 AND ?{0}>2", componentId); // NOT Installed && Component being installed but not installed already.

                    this.SchedulePropertyExeAction(sourceLineNumbers, rollbackNamePerUser, propertyId, rollbackCmdLinePerUser, rollbackExtraBitsPerUser, rollbackConditionPerUser, null, installAfter);
                    this.SchedulePropertyExeAction(sourceLineNumbers, rollbackNamePerMachine, propertyId, rollbackCmdLinePerMachine, rollbackExtraBitsPerMachine, rollbackConditionPerMachine, null, rollbackNamePerUser.Id);

                    installAfter = rollbackNamePerMachine.Id;
                }

                Identifier installNamePerUser = this.Core.CreateIdentifier("viu", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                Identifier installNamePerMachine = this.Core.CreateIdentifier("vim", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                string installCmdLinePerUser = String.Format("{0} \"[#{1}]\"", cmdlinePrefix, fileId);
                string installCmdLinePerMachine = String.Concat(installCmdLinePerUser, " /admin");
                string installConditionPerUser = String.Format("NOT ALLUSERS AND ${0}=3", componentId); // only execute if the Component being installed.
                string installConditionPerMachine = String.Format("ALLUSERS AND ${0}=3", componentId); // only execute if the Component being installed.

                this.SchedulePropertyExeAction(sourceLineNumbers, installNamePerUser, propertyId, installCmdLinePerUser, installExtraBits, installConditionPerUser, null, installAfter);
                this.SchedulePropertyExeAction(sourceLineNumbers, installNamePerMachine, propertyId, installCmdLinePerMachine, installExtraBits | VSCompiler.MsidbCustomActionTypeNoImpersonate, installConditionPerMachine, null, installNamePerUser.Id);

                // If not permanent, schedule the uninstall custom action.
                if (permanent != YesNoType.Yes)
                {
                    Identifier uninstallNamePerUser = this.Core.CreateIdentifier("vuu", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                    Identifier uninstallNamePerMachine = this.Core.CreateIdentifier("vum", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                    string uninstallCmdLinePerUser = String.Concat(cmdlinePrefix, " /u:\"", packageId, "\"");
                    string uninstallCmdLinePerMachine = String.Concat(uninstallCmdLinePerUser, " /admin");
                    int uninstallExtraBitsPerUser = VSCompiler.MsidbCustomActionTypeContinue | VSCompiler.MsidbCustomActionTypeInScript;
                    int uninstallExtraBitsPerMachine = uninstallExtraBitsPerUser | VSCompiler.MsidbCustomActionTypeNoImpersonate;
                    string uninstallConditionPerUser = String.Format("NOT ALLUSERS AND ${0}=2 AND ?{0}>2", componentId); // Only execute if component is being uninstalled.
                    string uninstallConditionPerMachine = String.Format("ALLUSERS AND ${0}=2 AND ?{0}>2", componentId); // Only execute if component is being uninstalled.

                    this.SchedulePropertyExeAction(sourceLineNumbers, uninstallNamePerUser, propertyId, uninstallCmdLinePerUser, uninstallExtraBitsPerUser, uninstallConditionPerUser, "InstallFinalize", null);
                    this.SchedulePropertyExeAction(sourceLineNumbers, uninstallNamePerMachine, propertyId, uninstallCmdLinePerMachine, uninstallExtraBitsPerMachine, uninstallConditionPerMachine, "InstallFinalize", null);
                }
            }
        }

        private void SchedulePropertyExeAction(SourceLineNumber sourceLineNumbers, Identifier name, string source, string cmdline, int extraBits, string condition, string beforeAction, string afterAction)
        {
            const string sequence = "InstallExecuteSequence";

            Row actionRow = this.Core.CreateRow(sourceLineNumbers, "CustomAction", name);
            actionRow[1] = VSCompiler.MsidbCustomActionTypeProperty | VSCompiler.MsidbCustomActionTypeExe | extraBits;
            actionRow[2] = source;
            actionRow[3] = cmdline;

            Row sequenceRow = this.Core.CreateRow(sourceLineNumbers, "WixAction");
            sequenceRow[0] = sequence;
            sequenceRow[1] = name.Id;
            sequenceRow[2] = condition;
            // no explicit sequence
            sequenceRow[4] = beforeAction;
            sequenceRow[5] = afterAction;
            sequenceRow[6] = 0; // not overridable

            if (null != beforeAction)
            {
                if (WindowsInstallerStandard.IsStandardAction(beforeAction))
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "WixAction", sequence, beforeAction);
                }
                else
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", beforeAction);
                }
            }

            if (null != afterAction)
            {
                if (WindowsInstallerStandard.IsStandardAction(afterAction))
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "WixAction", sequence, afterAction);
                }
                else
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", afterAction);
                }
            }
        }
    }
}
