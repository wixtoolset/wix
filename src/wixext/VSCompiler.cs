// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Visual Studio Extension.
    /// </summary>
    public sealed class VSCompiler : BaseCompilerExtension
    {
        internal const int MsidbCustomActionTypeExe = 0x00000002;  // Target = command line args
        internal const int MsidbCustomActionTypeProperty = 0x00000030;  // Source = full path to executable
        internal const int MsidbCustomActionTypeContinue = 0x00000040;  // ignore action return status; continue running
        internal const int MsidbCustomActionTypeRollback = 0x00000100;  // in conjunction with InScript: queue in Rollback script
        internal const int MsidbCustomActionTypeInScript = 0x00000400;  // queue for execution within script
        internal const int MsidbCustomActionTypeNoImpersonate = 0x00000800;  // queue for not impersonating

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/vs";

        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    switch (element.Name.LocalName)
                    {
                        case "VsixPackage":
                            this.ParseVsixPackageElement(intermediate, section, element, context["ComponentId"], null);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "File":
                    switch (element.Name.LocalName)
                    {
                        case "HelpCollection":
                            this.ParseHelpCollectionElement(intermediate, section, element, context["FileId"]);
                            break;
                        case "HelpFile":
                            this.ParseHelpFileElement(intermediate, section, element, context["FileId"]);
                            break;
                        case "VsixPackage":
                            this.ParseVsixPackageElement(intermediate, section, element, context["ComponentId"], context["FileId"]);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "HelpCollectionRef":
                            this.ParseHelpCollectionRefElement(intermediate, section, element);
                            break;
                        case "HelpFilter":
                            this.ParseHelpFilterElement(intermediate, section, element);
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

        private void ParseHelpCollectionRefElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "HelpNamespace", id.Id);
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

            foreach (XElement child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "HelpFileRef":
                            this.ParseHelpFileRefElement(intermediate, section, child, id);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }
        }

        private void ParseHelpCollectionElement(Intermediate intermediate, IntermediateSection section, XElement element, string fileId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string description = null;
            string name = null;
            YesNoType suppressCAs = YesNoType.No;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == description)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Description"));
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            foreach (XElement child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "HelpFileRef":
                            this.ParseHelpFileRefElement(intermediate, section, child, id);
                            break;
                        case "HelpFilterRef":
                            this.ParseHelpFilterRefElement(intermediate, section, child, id);
                            break;
                        case "PlugCollectionInto":
                            this.ParsePlugCollectionIntoElement(intermediate, section, child, id);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "HelpNamespace", id);
                row.Set(1, name);
                row.Set(2, fileId);
                row.Set(3, description);

                if (YesNoType.No == suppressCAs)
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        private void ParseHelpFileElement(Intermediate intermediate, IntermediateSection section, XElement element, string fileId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string name = null;
            int language = CompilerConstants.IntegerNotSet;
            string hxi = null;
            string hxq = null;
            string hxr = null;
            string samples = null;
            YesNoType suppressCAs = YesNoType.No;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AttributeIndex":
                            hxr = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "File", hxr);
                            break;
                        case "Index":
                            hxi = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "File", hxi);
                            break;
                        case "Language":
                            language = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SampleLocation":
                            samples = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "File", samples);
                            break;
                        case "Search":
                            hxq = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "File", hxq);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            // Uninstall will always fail silently, leaving file registered, if Language is not set
            if (CompilerConstants.IntegerNotSet == language)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Language"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "HelpFile", id);
                row.Set(1, name);
                row.Set(2, language);
                row.Set(3, fileId);
                row.Set(4, hxi);
                row.Set(5, hxq);
                row.Set(6, hxr);
                row.Set(7, samples);

                if (YesNoType.No == suppressCAs)
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        private void ParseHelpFileRefElement(Intermediate intermediate, IntermediateSection section, XElement element, Identifier collectionId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "HelpFile", id.Id);
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

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "HelpFileToNamespace", id);
                row.Set(1, collectionId.Id);
            }
        }

        private void ParseHelpFilterElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string filterDefinition = null;
            string name = null;
            YesNoType suppressCAs = YesNoType.No;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "FilterDefinition":
                            filterDefinition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "HelpFilter", id);
                row.Set(1, name);
                row.Set(2, filterDefinition);

                if (YesNoType.No == suppressCAs)
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        private void ParseHelpFilterRefElement(Intermediate intermediate, IntermediateSection section, XElement element, Identifier collectionId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "HelpFilter", id.Id);
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

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "HelpFilterToNamespace", id);
                row.Set(1, collectionId.Id);
            }
        }

        private void ParsePlugCollectionIntoElement(Intermediate intermediate, IntermediateSection section, XElement element, Identifier parentId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string hxa = null;
            string hxt = null;
            string hxtParent = null;
            string namespaceParent = null;
            string feature = null;
            YesNoType suppressExternalNamespaces = YesNoType.No;
            bool pluginVS05 = false;
            bool pluginVS08 = false;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Attributes":
                            hxa = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TableOfContents":
                            hxt = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetCollection":
                            namespaceParent = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetTableOfContents":
                            hxtParent = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetFeature":
                            feature = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressExternalNamespaces":
                            suppressExternalNamespaces = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            pluginVS05 = namespaceParent.Equals("MS_VSIPCC_v80", StringComparison.Ordinal);
            pluginVS08 = namespaceParent.Equals("MS.VSIPCC.v90", StringComparison.Ordinal);

            if (null == namespaceParent)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "TargetCollection"));
            }

            if (null == feature && (pluginVS05 || pluginVS08) && YesNoType.No == suppressExternalNamespaces)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "TargetFeature"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "HelpPlugin", parentId);
                row.Set(1, namespaceParent);
                row.Set(2, hxt);
                row.Set(3, hxa);
                row.Set(4, hxtParent);

                if (pluginVS05)
                {
                    if (YesNoType.No == suppressExternalNamespaces)
                    {
                        // Bring in the help 2 base namespace components for VS 2005
                        this.ParseHelper.CreateComplexReference(section, sourceLineNumbers, ComplexReferenceParentType.Feature, feature, String.Empty,
                            ComplexReferenceChildType.ComponentGroup, "Help2_VS2005_Namespace_Components", false);
                        // Reference CustomAction since nothing will happen without it
                        this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "CA_HxMerge_VSIPCC_VSCC");
                    }
                }
                else if (pluginVS08)
                {
                    if (YesNoType.No == suppressExternalNamespaces)
                    {
                        // Bring in the help 2 base namespace components for VS 2008
                        this.ParseHelper.CreateComplexReference(section, sourceLineNumbers, ComplexReferenceParentType.Feature, feature, String.Empty,
                            ComplexReferenceChildType.ComponentGroup, "Help2_VS2008_Namespace_Components", false);
                        // Reference CustomAction since nothing will happen without it
                        this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "CA_ScheduleExtHelpPlugin_VSCC_VSIPCC");
                    }
                }
                else
                {
                    // Reference the parent namespace to enforce the foreign key relationship
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "HelpNamespace", namespaceParent);
                }
            }
        }

        private void ParseVsixPackageElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string fileId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string propertyId = "VS_VSIX_INSTALLER_PATH";
            string packageId = null;
            YesNoType permanent = YesNoType.NotSet;
            string target = null;
            string targetVersion = null;
            YesNoType vital = YesNoType.NotSet;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "File":
                            if (String.IsNullOrEmpty(fileId))
                            {
                                fileId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            else
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, "File", "File"));
                            }
                            break;
                        case "PackageId":
                            packageId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Permanent":
                            permanent = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Target":
                            target = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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
                            targetVersion = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Vital":
                            vital = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "VsixInstallerPathProperty":
                            propertyId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(fileId))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "File"));
            }

            if (String.IsNullOrEmpty(packageId))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "PackageId"));
            }

            if (!String.IsNullOrEmpty(target) && String.IsNullOrEmpty(targetVersion))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "TargetVersion", "Target"));
            }
            else if (String.IsNullOrEmpty(target) && !String.IsNullOrEmpty(targetVersion))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Target", "TargetVersion"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                // Ensure there is a reference to the AppSearch Property that will find the VsixInstaller.exe.
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Property", propertyId);

                // Ensure there is a reference to the package file (even if we are a child under it).
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "File", fileId);

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
                    Identifier rollbackNamePerUser = this.ParseHelper.CreateIdentifier("vru", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                    Identifier rollbackNamePerMachine = this.ParseHelper.CreateIdentifier("vrm", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                    string rollbackCmdLinePerUser = String.Concat(cmdlinePrefix, " /u:\"", packageId, "\"");
                    string rollbackCmdLinePerMachine = String.Concat(rollbackCmdLinePerUser, " /admin");
                    int rollbackExtraBitsPerUser = VSCompiler.MsidbCustomActionTypeContinue | VSCompiler.MsidbCustomActionTypeRollback | VSCompiler.MsidbCustomActionTypeInScript;
                    int rollbackExtraBitsPerMachine = rollbackExtraBitsPerUser | VSCompiler.MsidbCustomActionTypeNoImpersonate;
                    string rollbackConditionPerUser = String.Format("NOT ALLUSERS AND NOT Installed AND ${0}=2 AND ?{0}>2", componentId); // NOT Installed && Component being installed but not installed already.
                    string rollbackConditionPerMachine = String.Format("ALLUSERS AND NOT Installed AND ${0}=2 AND ?{0}>2", componentId); // NOT Installed && Component being installed but not installed already.

                    this.SchedulePropertyExeAction(section, sourceLineNumbers, rollbackNamePerUser, propertyId, rollbackCmdLinePerUser, rollbackExtraBitsPerUser, rollbackConditionPerUser, null, installAfter);
                    this.SchedulePropertyExeAction(section, sourceLineNumbers, rollbackNamePerMachine, propertyId, rollbackCmdLinePerMachine, rollbackExtraBitsPerMachine, rollbackConditionPerMachine, null, rollbackNamePerUser.Id);

                    installAfter = rollbackNamePerMachine.Id;
                }

                Identifier installNamePerUser = this.ParseHelper.CreateIdentifier("viu", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                Identifier installNamePerMachine = this.ParseHelper.CreateIdentifier("vim", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                string installCmdLinePerUser = String.Format("{0} \"[#{1}]\"", cmdlinePrefix, fileId);
                string installCmdLinePerMachine = String.Concat(installCmdLinePerUser, " /admin");
                string installConditionPerUser = String.Format("NOT ALLUSERS AND ${0}=3", componentId); // only execute if the Component being installed.
                string installConditionPerMachine = String.Format("ALLUSERS AND ${0}=3", componentId); // only execute if the Component being installed.

                this.SchedulePropertyExeAction(section, sourceLineNumbers, installNamePerUser, propertyId, installCmdLinePerUser, installExtraBits, installConditionPerUser, null, installAfter);
                this.SchedulePropertyExeAction(section, sourceLineNumbers, installNamePerMachine, propertyId, installCmdLinePerMachine, installExtraBits | VSCompiler.MsidbCustomActionTypeNoImpersonate, installConditionPerMachine, null, installNamePerUser.Id);

                // If not permanent, schedule the uninstall custom action.
                if (permanent != YesNoType.Yes)
                {
                    Identifier uninstallNamePerUser = this.ParseHelper.CreateIdentifier("vuu", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                    Identifier uninstallNamePerMachine = this.ParseHelper.CreateIdentifier("vum", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                    string uninstallCmdLinePerUser = String.Concat(cmdlinePrefix, " /u:\"", packageId, "\"");
                    string uninstallCmdLinePerMachine = String.Concat(uninstallCmdLinePerUser, " /admin");
                    int uninstallExtraBitsPerUser = VSCompiler.MsidbCustomActionTypeContinue | VSCompiler.MsidbCustomActionTypeInScript;
                    int uninstallExtraBitsPerMachine = uninstallExtraBitsPerUser | VSCompiler.MsidbCustomActionTypeNoImpersonate;
                    string uninstallConditionPerUser = String.Format("NOT ALLUSERS AND ${0}=2 AND ?{0}>2", componentId); // Only execute if component is being uninstalled.
                    string uninstallConditionPerMachine = String.Format("ALLUSERS AND ${0}=2 AND ?{0}>2", componentId); // Only execute if component is being uninstalled.

                    this.SchedulePropertyExeAction(section, sourceLineNumbers, uninstallNamePerUser, propertyId, uninstallCmdLinePerUser, uninstallExtraBitsPerUser, uninstallConditionPerUser, "InstallFinalize", null);
                    this.SchedulePropertyExeAction(section, sourceLineNumbers, uninstallNamePerMachine, propertyId, uninstallCmdLinePerMachine, uninstallExtraBitsPerMachine, uninstallConditionPerMachine, "InstallFinalize", null);
                }
            }
        }

        private void SchedulePropertyExeAction(IntermediateSection section, SourceLineNumber sourceLineNumbers, Identifier name, string source, string cmdline, int extraBits, string condition, string beforeAction, string afterAction)
        {
            const string sequence = "InstallExecuteSequence";

            var actionRow = this.ParseHelper.CreateRow(section, sourceLineNumbers, "CustomAction", name);
            actionRow.Set(1, VSCompiler.MsidbCustomActionTypeProperty | VSCompiler.MsidbCustomActionTypeExe | extraBits);
            actionRow.Set(2, source);
            actionRow.Set(3, cmdline);

            var sequenceRow = this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixAction", new Identifier(name.Access, sequence, name.Id));
            sequenceRow.Set(0, sequence);
            sequenceRow.Set(1, name.Id);
            sequenceRow.Set(2, condition);
            // no explicit sequence
            sequenceRow.Set(4, beforeAction);
            sequenceRow.Set(5, afterAction);
            sequenceRow.Set(6, 0); // not overridable

            if (null != beforeAction)
            {
                if (WindowsInstallerStandard.IsStandardAction(beforeAction))
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "WixAction", sequence, beforeAction);
                }
                else
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", beforeAction);
                }
            }

            if (null != afterAction)
            {
                if (WindowsInstallerStandard.IsStandardAction(afterAction))
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "WixAction", sequence, afterAction);
                }
                else
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", afterAction);
                }
            }
        }
    }
}
