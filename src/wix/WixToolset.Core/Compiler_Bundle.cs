// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        private static readonly Identifier BurnUXContainerId = new Identifier(AccessModifier.Section, BurnConstants.BurnUXContainerName);
        private static readonly Identifier BurnDefaultAttachedContainerId = new Identifier(AccessModifier.Section, BurnConstants.BurnDefaultAttachedContainerName);
        private static readonly Identifier BundleLayoutOnlyPayloads = new Identifier(AccessModifier.Section, BurnConstants.BundleLayoutOnlyPayloadsName);

        /// <summary>
        /// Parses an ApprovedExeForElevation element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseApprovedExeForElevation(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string key = null;
            string valueName = null;
            var win64 = this.Context.IsCurrentPlatform64Bit;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Bitness":
                            var bitnessValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (bitnessValue)
                            {
                                case "always32":
                                    win64 = false;
                                    break;
                                case "always64":
                                    win64 = true;
                                    break;
                                case "default":
                                case "":
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, bitnessValue, "default", "always32", "always64"));
                                    break;
                            }
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            valueName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            var attributes = WixApprovedExeForElevationAttributes.None;

            if (win64)
            {
                attributes |= WixApprovedExeForElevationAttributes.Win64;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixApprovedExeForElevationSymbol(sourceLineNumbers, id)
                {
                    Key = key,
                    ValueName = valueName,
                    Attributes = attributes,
                });
            }
        }

        /// <summary>
        /// Parses a Bundle element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBundleElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string copyright = null;
            string aboutUrl = null;
            var modifyType = WixBundleModifyType.Allowed;
            var compressed = YesNoDefaultType.Default;
            WixBundleAttributes attributes = 0;
            string helpTelephone = null;
            string helpUrl = null;
            string inProgressName = null;
            string manufacturer = null;
            string name = null;
            string tag = null;
            string updateUrl = null;
            string upgradeCode = null;
            string version = null;
            string condition = null;
            string parentName = null;

            string fileSystemSafeBundleName = null;
            string logVariablePrefixAndExtension;
            string iconSourceFile = null;
            string splashScreenSourceFile = null;

            // Process only standard attributes until the active section is initialized.
            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AboutUrl":
                            aboutUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Compressed":
                            compressed = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Copyright":
                            copyright = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisableModify":
                            var value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (value)
                            {
                                case "button":
                                    modifyType = WixBundleModifyType.SingleChangeUninstallButton;
                                    break;
                                case "yes":
                                    modifyType = WixBundleModifyType.Disabled;
                                    break;
                                case "no":
                                    modifyType = WixBundleModifyType.Allowed;
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, value, "button", "yes", "no"));
                                    break;
                            }
                            break;
                        case "DisableRemove":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixBundleAttributes.DisableRemove;
                            }
                            break;
                        case "HelpTelephone":
                            helpTelephone = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HelpUrl":
                            helpUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Manufacturer":
                            manufacturer = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IconSourceFile":
                            iconSourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InProgressName":
                            inProgressName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParentName":
                            parentName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProviderKey":
                            // This can't be processed until we create the section.
                            break;
                        case "SplashScreenSourceFile":
                            splashScreenSourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Tag":
                            tag = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpdateUrl":
                            updateUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
            }

            if (String.IsNullOrEmpty(version))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            if (String.IsNullOrEmpty(upgradeCode))
            {
                if (id is null)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Id", "UpgradeCode"));
                }
                else
                {
                    upgradeCode = this.Core.CreateGuid(Compiler.UpgradeCodeGuidNamespace, id.Id);
                }
            }

            if (String.IsNullOrEmpty(copyright))
            {
                if (String.IsNullOrEmpty(manufacturer))
                {
                    copyright = "Copyright (c). All rights reserved.";
                }
                else
                {
                    copyright = String.Format("Copyright (c) {0}. All rights reserved.", manufacturer);
                }
            }

            if (String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(inProgressName))
            {
                name = inProgressName;
                inProgressName = null;
            }

            if (String.IsNullOrEmpty(name))
            {
                logVariablePrefixAndExtension = String.Concat("WixBundleLog:Setup:log");
            }
            else
            {
                // Ensure only allowable path characters are in "name" (and change spaces to underscores).
                fileSystemSafeBundleName = CompilerCore.MakeValidLongFileName(name.Replace(' ', '_'), '_');
                logVariablePrefixAndExtension = String.Concat("WixBundleLog:", fileSystemSafeBundleName, ":log");
            }

            this.activeName = String.IsNullOrEmpty(name) ? String.IsNullOrEmpty(id?.Id) ? Common.GenerateGuid() : id.Id : name;
            this.Core.CreateActiveSection(this.activeName, SectionType.Bundle, this.Context.CompilationId);

            // Now that the active section is initialized, process only extension attributes and the special ProviderKey attribute.
            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "ProviderKey":
                            this.ParseBundleProviderKeyAttribute(sourceLineNumbers, node, attrib);
                            break;
                            // Unknown attributes were reported earlier.
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            var chainSeen = false;
            var logSeen = false;
            var updateSeen = false;

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ApprovedExeForElevation":
                            this.ParseApprovedExeForElevation(child);
                            break;
                        case "BootstrapperApplication":
                            this.ParseBootstrapperApplicationElement(child);
                            break;
                        case "BootstrapperApplicationRef":
                            this.ParseBootstrapperApplicationRefElement(child);
                            break;
                        case "BundleCustomData":
                            this.ParseBundleCustomDataElement(child);
                            break;
                        case "BundleCustomDataRef":
                            this.ParseBundleCustomDataRefElement(child);
                            break;
                        case "BundleExtension":     // kept for backward compatibility
                        case "BootstrapperExtension":
                            this.ParseBootstrapperExtensionElement(child);
                            break;
                        case "BundleExtensionRef":  // kept for backward compatibility
                        case "BootstrapperExtensionRef":
                            this.ParseSimpleRefElement(child, SymbolDefinitions.WixBootstrapperExtension);
                            break;
                        case "OptionalUpdateRegistration":
                            this.ParseOptionalUpdateRegistrationElement(child, manufacturer, parentName, name);
                            break;
                        case "Chain":
                            if (chainSeen)
                            {
                                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "Chain"));
                            }
                            this.ParseChainElement(child);
                            chainSeen = true;
                            break;
                        case "Container":
                            this.ParseContainerElement(child);
                            break;
                        case "ContainerRef":
                            this.ParseSimpleRefElement(child, SymbolDefinitions.WixBundleContainer);
                            break;
                        case "Log":
                            if (logSeen)
                            {
                                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "Log"));
                            }
                            logVariablePrefixAndExtension = this.ParseLogElement(child, fileSystemSafeBundleName);
                            logSeen = true;
                            break;
                        case "PayloadGroup":
                            this.ParsePayloadGroupElement(child, ComplexReferenceParentType.Layout, Compiler.BundleLayoutOnlyPayloads);
                            break;
                        case "PayloadGroupRef":
                            this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Layout, Compiler.BundleLayoutOnlyPayloads);
                            break;
                        case "RelatedBundle":
                            this.ParseRelatedBundleElement(child);
                            break;
                        case "Requires":
                            this.ParseRequiresElement(child, null);
                            break;
                        case "SetVariable":
                            this.ParseSetVariableElement(child);
                            break;
                        case "SetVariableRef":
                            this.ParseSimpleRefElement(child, SymbolDefinitions.WixSetVariable);
                            break;
                        case "SoftwareTag":
                            this.ParseBundleTagElement(child);
                            break;
                        case "Update":
                            if (updateSeen)
                            {
                                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "Update"));
                            }
                            this.ParseUpdateElement(child);
                            updateSeen = true;
                            break;
                        case "Variable":
                            this.ParseVariableElement(child);
                            break;
                        case "WixVariable":
                            this.ParseWixVariableElement(child);
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

            if (!chainSeen)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "Chain"));
            }

            if (!this.Core.EncounteredError)
            {
                var symbol = this.Core.AddSymbol(new WixBundleSymbol(sourceLineNumbers, id)
                {
                    UpgradeCode = upgradeCode,
                    Version = version,
                    Copyright = copyright,
                    DisableModify = modifyType,
                    InProgressName = inProgressName,
                    Name = name,
                    Manufacturer = manufacturer,
                    Attributes = attributes,
                    AboutUrl = aboutUrl,
                    HelpUrl = helpUrl,
                    HelpTelephone = helpTelephone,
                    UpdateUrl = updateUrl,
                    Compressed = YesNoDefaultType.Yes == compressed ? true : YesNoDefaultType.No == compressed ? (bool?)false : null,
                    IconSourceFile = new IntermediateFieldPathValue { Path = iconSourceFile },
                    SplashScreenSourceFile = new IntermediateFieldPathValue { Path = splashScreenSourceFile },
                    Condition = condition,
                    Tag = tag,
                    Platform = this.CurrentPlatform,
                    ParentName = parentName,
                });

                if (!String.IsNullOrEmpty(logVariablePrefixAndExtension))
                {
                    var split = logVariablePrefixAndExtension.Split(':');
                    symbol.LogPathVariable = split[0];
                    symbol.LogPrefix = split[1];
                    symbol.LogExtension = split[2];
                }

                if (null != upgradeCode)
                {
                    this.Core.AddSymbol(new WixRelatedBundleSymbol(sourceLineNumbers)
                    {
                        BundleCode = upgradeCode,
                        Action = RelatedBundleActionType.Upgrade,
                    });
                }

                this.Core.AddSymbol(new WixBundleContainerSymbol(sourceLineNumbers, Compiler.BurnUXContainerId)
                {
                    Name = "bundle-ux.cab",
                    Type = ContainerType.Attached
                });

                this.Core.AddSymbol(new WixBundleContainerSymbol(sourceLineNumbers, Compiler.BurnDefaultAttachedContainerId)
                {
                    Name = "bundle-attached.cab",
                    Type = ContainerType.Attached,
                });
            }
        }

        /// <summary>
        /// Parse a Container element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="fileSystemSafeBundleName"></param>
        private string ParseLogElement(XElement node, string fileSystemSafeBundleName)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var disableLog = YesNoType.NotSet;
            var variable = "WixBundleLog";
            var logPrefix = fileSystemSafeBundleName ?? "Setup";
            var logExtension = ".log";

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Disable":
                            disableLog = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "PathVariable":
                            variable = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Prefix":
                            logPrefix = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Extension":
                            logExtension = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (!logExtension.StartsWith(".", StringComparison.Ordinal))
            {
                logExtension = String.Concat(".", logExtension);
            }

            this.Core.ParseForExtensionElements(node);

            return YesNoType.Yes == disableLog ? null : String.Join(":", variable, logPrefix, logExtension);
        }

        /// <summary>
        /// Parse a Container element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseContainerElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string downloadUrl = null;
            string name = null;
            var type = ContainerType.Detached;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            if (id?.Id == BurnConstants.BurnUXContainerName || id?.Id == BurnConstants.BurnDefaultAttachedContainerName)
                            {
                                this.Messaging.Write(CompilerErrors.ReservedValue(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                            }
                            break;
                        case "DownloadUrl":
                            downloadUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            var typeString = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (typeString)
                            {
                                case "attached":
                                    type = ContainerType.Attached;
                                    break;
                                case "detached":
                                    type = ContainerType.Detached;
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Type", typeString, "attached, detached"));
                                    break;
                            }
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
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.Core.CreateIdentifierFromFilename(name);
                }

                if (null == id)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.Core.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }
            else if (null == name)
            {
                name = id.Id;
            }

            if (!String.IsNullOrEmpty(downloadUrl) && ContainerType.Detached != type)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "Type", "attached"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "PackageGroupRef":
                            this.ParsePackageGroupRefElement(child, ComplexReferenceParentType.Container, id.Id);
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
                this.Core.AddSymbol(new WixBundleContainerSymbol(sourceLineNumbers, id)
                {
                    Name = name,
                    Type = type,
                    DownloadUrl = downloadUrl
                });
            }
        }

        /// <summary>
        /// Parse the BootstrapperApplication element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBootstrapperApplicationElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var compilerPayload = new CompilerPayload(this.Core, sourceLineNumbers, node);
            XElement exePayloadRefNode = null;
            Identifier exePayloadRefId = null;
            bool? secondary = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            compilerPayload.ParseId(attrib);
                            break;

                        case "Name":
                            compilerPayload.ParseName(attrib);
                            break;

                        case "Secondary":
                            secondary = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.Yes;
                            break;

                        case "SourceFile":
                            compilerPayload.ParseSourceFile(attrib);
                            break;

                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "BootstrapperApplicationDll":
                            if (exePayloadRefId == null)
                            {
                                exePayloadRefNode = node;
                                exePayloadRefId = this.ParseBootstrapperApplicationDllElement(child, compilerPayload.Id);
                            }
                            else
                            {
                                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                var exePayloadSourceLineNumbers = Preprocessor.GetSourceLineNumbers(exePayloadRefNode);
                                this.Messaging.Write(CompilerErrors.AlreadyDefinedBootstrapperApplicationSource(childSourceLineNumbers, exePayloadSourceLineNumbers, exePayloadRefNode.Name.LocalName));
                            }
                            break;

                        case "Payload":
                            this.ParsePayloadElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, isRemoteAllowed: false);
                            break;

                        case "PayloadGroupRef":
                            this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId);
                            break;

                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    var context = new Dictionary<string, string>() { { "Id", compilerPayload.Id?.Id }, { "Name", compilerPayload.Name }, { "Secondary", secondary?.ToString() }, { "Source", compilerPayload.SourceFile }, };
                    var possibleKeyPath = this.Core.ParsePossibleKeyPathExtensionElement(node, child, context);
                    if (possibleKeyPath?.Type == PossibleKeyPathType.File)
                    {
                        if (exePayloadRefNode == null)
                        {
                            exePayloadRefNode = node;
                            exePayloadRefId = possibleKeyPath.Id;
                            secondary = possibleKeyPath.Explicit;
                        }
                        else
                        {
                            var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                            var exePayloadSourceLineNumbers = Preprocessor.GetSourceLineNumbers(exePayloadRefNode);
                            this.Messaging.Write(CompilerErrors.AlreadyDefinedBootstrapperApplicationSource(childSourceLineNumbers, exePayloadSourceLineNumbers, exePayloadRefNode.Name.LocalName));
                        }
                    }
                }
            }

            if (compilerPayload.Id == null)
            {
                compilerPayload.Id = exePayloadRefId ?? this.Core.CreateIdentifier("ba", compilerPayload.Name, compilerPayload.SourceFile);
            }

            if (String.IsNullOrEmpty(compilerPayload.SourceFile) && String.IsNullOrEmpty(compilerPayload.Name))
            {
                if (exePayloadRefId == null)
                {
                    compilerPayload.FinishCompilingPayload(Compiler.BurnUXContainerId.Id);
                }
            }
            else if (exePayloadRefId != null)
            {
                var exePayloadSourceLineNumbers = Preprocessor.GetSourceLineNumbers(exePayloadRefNode);

                this.Messaging.Write(CompilerErrors.AlreadyDefinedBootstrapperApplicationSource(exePayloadSourceLineNumbers, sourceLineNumbers, node.Name.LocalName));
            }
            else
            {
                compilerPayload.FinishCompilingPayload(Compiler.BurnUXContainerId.Id);

                var exePayload = compilerPayload.CreatePayloadSymbol(ComplexReferenceParentType.Container, Compiler.BurnUXContainerId.Id);

                exePayloadRefId = exePayload?.Id;
            }

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBootstrapperApplicationSymbol(sourceLineNumbers, compilerPayload.Id)
                {
                    ExePayloadRef = exePayloadRefId?.Id,
                    Secondary = secondary
                });
            }
        }

        /// <summary>
        /// Parse the deprecated BootstrapperApplicationDll element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="defaultId">Default bootstrapper application identifieir</param>
        private Identifier ParseBootstrapperApplicationDllElement(XElement node, Identifier defaultId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var compilerPayload = new CompilerPayload(this.Core, sourceLineNumbers, node)
            {
                Id = defaultId
            };

            this.Core.Write(WarningMessages.DeprecatedElement(sourceLineNumbers, node.Name.LocalName));

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            var extensionAttributes = new List<XAttribute>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            compilerPayload.ParseId(attrib);
                            break;
                        case "Name":
                            compilerPayload.ParseName(attrib);
                            break;
                        case "SourceFile":
                            compilerPayload.ParseSourceFile(attrib);
                            break;
                        case "DpiAwareness":
                            // Ignore for backwards compatibility.
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    extensionAttributes.Add(attrib);
                }
            }

            compilerPayload.FinishCompilingPayload(Compiler.BurnUXContainerId.Id);

            // Now that the Id is known, we can parse the extension attributes.
            var context = new Dictionary<string, string>
            {
                ["Id"] = compilerPayload.Id.Id,
            };

            foreach (var extensionAttribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, extensionAttribute, context);
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                compilerPayload.CreatePayloadSymbol(ComplexReferenceParentType.Container, Compiler.BurnUXContainerId.Id);
            }

            return compilerPayload.Id;
        }

        /// <summary>
        /// Parse the BootstrapperApplicationRef element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBootstrapperApplicationRefElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Payload":
                            this.ParsePayloadElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, isRemoteAllowed: false);
                            break;
                        case "PayloadGroupRef":
                            this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId);
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

            if (String.IsNullOrEmpty(id))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixBootstrapperApplication, id);
            }
        }

        /// <summary>
        /// Parses a BundleCustomData element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseBundleCustomDataElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string customDataId = null;
            WixBundleCustomDataType? customDataType = null;
            string extensionId = null;
            var attributeDefinitions = new List<WixBundleCustomDataAttributeSymbol>();
            var foundAttributeDefinitions = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            customDataId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            var typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (typeValue)
                            {
                                case "bootstrapperApplication":
                                    customDataType = WixBundleCustomDataType.BootstrapperApplication;
                                    break;
                                case "bundleExtension":         // kept for backward compatibility
                                case "bootstrapperExtension":
                                    customDataType = WixBundleCustomDataType.BootstrapperExtension;
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "bootstrapperApplication", "bootstrapperExtension"));
                                    customDataType = WixBundleCustomDataType.Unknown; // set a value to prevent expected attribute error below.
                                    break;
                            }
                            break;
                        case "ExtensionId":
                            extensionId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixBootstrapperExtension, extensionId);
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

            if (null == customDataId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            var hasExtensionId = null != extensionId;
            if (!customDataType.HasValue)
            {
                customDataType = hasExtensionId ? WixBundleCustomDataType.BootstrapperExtension : WixBundleCustomDataType.BootstrapperApplication;
            }

            if (!customDataType.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
            }
            else if (hasExtensionId)
            {
                if (customDataType.Value == WixBundleCustomDataType.BootstrapperApplication)
                {
                    this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ExtensonId", "Type", "bootstrapperApplication"));
                }
            }
            else if (customDataType.Value == WixBundleCustomDataType.BootstrapperExtension)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ExtensionId", "Type", "bootstrapperExtension"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "BundleAttributeDefinition":
                            foundAttributeDefinitions = true;

                            var attributeDefinition = this.ParseBundleAttributeDefinitionElement(child, childSourceLineNumbers, customDataId);
                            if (attributeDefinition != null)
                            {
                                attributeDefinitions.Add(attributeDefinition);
                            }
                            break;
                        case "BundleElement":
                            this.ParseBundleElementElement(child, childSourceLineNumbers, customDataId);
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

            if (attributeDefinitions.Count > 0)
            {
                if (!this.Core.EncounteredError)
                {
                    var attributeNames = String.Join(new string(WixBundleCustomDataSymbol.AttributeNamesSeparator, 1), attributeDefinitions.Select(c => c.Name));

                    this.Core.AddSymbol(new WixBundleCustomDataSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, customDataId))
                    {
                        AttributeNames = attributeNames,
                        Type = customDataType.Value,
                        BootstrapperExtensionRef = extensionId,
                    });
                }
            }
            else if (!foundAttributeDefinitions)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "BundleAttributeDefinition"));
            }
        }

        /// <summary>
        /// Parses a BundleCustomDataRef element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseBundleCustomDataRefElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string customDataId = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            customDataId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixBundleCustomData, customDataId);
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

            if (null == customDataId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "BundleElement":
                            this.ParseBundleElementElement(child, childSourceLineNumbers, customDataId);
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
        /// Parses a BundleAttributeDefinition element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="sourceLineNumbers">Element's SourceLineNumbers.</param>
        /// <param name="customDataId">BundleCustomData Id.</param>
        private WixBundleCustomDataAttributeSymbol ParseBundleAttributeDefinitionElement(XElement node, SourceLineNumber sourceLineNumbers, string customDataId)
        {
            string attributeName = null;

            foreach (var attrib in node.Attributes())
            {
                switch (attrib.Name.LocalName)
                {
                    case "Id":
                        attributeName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                }
            }

            if (null == attributeName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (this.Core.EncounteredError)
            {
                return null;
            }

            var customDataAttribute = this.Core.AddSymbol(new WixBundleCustomDataAttributeSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, customDataId, attributeName))
            {
                CustomDataRef = customDataId,
                Name = attributeName,
            });
            return customDataAttribute;
        }

        /// <summary>
        /// Parses a BundleElement element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="sourceLineNumbers">Element's SourceLineNumbers.</param>
        /// <param name="customDataId">BundleCustomData Id.</param>
        private void ParseBundleElementElement(XElement node, SourceLineNumber sourceLineNumbers, string customDataId)
        {
            var elementId = Guid.NewGuid().ToString("N").ToUpperInvariant();

            foreach (var attrib in node.Attributes())
            {
                this.Core.ParseExtensionAttribute(node, attrib);
            }

            foreach (var child in node.Elements())
            {
                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                switch (child.Name.LocalName)
                {
                    case "BundleAttribute":
                        string attributeName = null;
                        string value = null;
                        foreach (var attrib in child.Attributes())
                        {
                            switch (attrib.Name.LocalName)
                            {
                                case "Id":
                                    attributeName = this.Core.GetAttributeValue(childSourceLineNumbers, attrib);
                                    break;
                                case "Value":
                                    value = this.Core.GetAttributeValue(childSourceLineNumbers, attrib);
                                    break;
                                default:
                                    this.Core.ParseExtensionAttribute(child, attrib);
                                    break;
                            }
                        }

                        if (null == attributeName)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Id"));
                        }

                        if (!this.Core.EncounteredError)
                        {
                            this.Core.AddSymbol(new WixBundleCustomDataCellSymbol(childSourceLineNumbers, new Identifier(AccessModifier.Section, customDataId, elementId, attributeName))
                            {
                                ElementId = elementId,
                                AttributeRef = attributeName,
                                CustomDataRef = customDataId,
                                Value = value,
                            });
                        }
                        break;
                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                }
            }

            if (!this.Core.EncounteredError)
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixBundleCustomData, customDataId);
            }
        }

        /// <summary>
        /// Parse the BootstrapperExtension element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBootstrapperExtensionElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var compilerPayload = new CompilerPayload(this.Core, sourceLineNumbers, node);

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            var extensionAttributes = new List<XAttribute>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            compilerPayload.ParseId(attrib);
                            break;
                        case "Name":
                            compilerPayload.ParseName(attrib);
                            break;
                        case "SourceFile":
                            compilerPayload.ParseSourceFile(attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    extensionAttributes.Add(attrib);
                }
            }

            compilerPayload.FinishCompilingPayload(Compiler.BurnUXContainerId.Id);

            // Now that the Id is known, we can parse the extension attributes.
            var context = new Dictionary<string, string>
            {
                ["Id"] = compilerPayload.Id.Id,
            };

            foreach (var extensionAttribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, extensionAttribute, context);
            }

            compilerPayload.CreatePayloadSymbol(ComplexReferenceParentType.Container, Compiler.BurnUXContainerId.Id);

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Payload":
                            this.ParsePayloadElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, isRemoteAllowed: false);
                            break;
                        case "PayloadGroupRef":
                            this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId);
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

            // Add the BootstrapperExtension.
            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBootstrapperExtensionSymbol(sourceLineNumbers, compilerPayload.Id)
                {
                    PayloadRef = compilerPayload.Id.Id,
                });
            }
        }

        /// <summary>
        /// Parse the OptionalUpdateRegistration element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="defaultManufacturer">The manufacturer.</param>
        /// <param name="defaultProductFamily">The product family.</param>
        /// <param name="defaultName">The bundle name.</param>
        private void ParseOptionalUpdateRegistrationElement(XElement node, string defaultManufacturer, string defaultProductFamily, string defaultName)
        {
            const string defaultClassification = "Update";

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string manufacturer = null;
            string department = null;
            string productFamily = null;
            string name = null;
            var classification = defaultClassification;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Manufacturer":
                            manufacturer = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Department":
                            department = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductFamily":
                            productFamily = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Classification":
                            classification = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(manufacturer))
            {
                if (!String.IsNullOrEmpty(defaultManufacturer))
                {
                    manufacturer = defaultManufacturer;
                }
                else
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributeInElementOrParent(sourceLineNumbers, node.Name.LocalName, "Manufacturer", node.Parent.Name.LocalName));
                }
            }

            if (String.IsNullOrEmpty(productFamily))
            {
                if (!String.IsNullOrEmpty(defaultProductFamily))
                {
                    productFamily = defaultProductFamily;
                }
            }

            if (String.IsNullOrEmpty(name))
            {
                if (!String.IsNullOrEmpty(defaultName))
                {
                    name = defaultName;
                }
                else
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributeInElementOrParent(sourceLineNumbers, node.Name.LocalName, "Name", node.Parent.Name.LocalName));
                }
            }

            if (String.IsNullOrEmpty(classification))
            {
                this.Core.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, node.Name.LocalName, "Classification", defaultClassification));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixUpdateRegistrationSymbol(sourceLineNumbers)
                {
                    Manufacturer = manufacturer,
                    Department = department,
                    ProductFamily = productFamily,
                    Name = name,
                    Classification = classification
                });
            }
        }

        /// <summary>
        /// Parse Payload element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element. (BA or PayloadGroup)</param>
        /// <param name="parentId">Identifier of parent element.</param>
        /// <param name="isRemoteAllowed">Indicates if the Payload element can be remote or not.</param>
        private Identifier ParsePayloadElement(XElement node, ComplexReferenceParentType parentType, Identifier parentId, bool isRemoteAllowed)
        {
            Debug.Assert(ComplexReferenceParentType.PayloadGroup == parentType || ComplexReferenceParentType.Package == parentType || ComplexReferenceParentType.Container == parentType);

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var compilerPayload = new CompilerPayload(this.Core, sourceLineNumbers, node)
            {
                IsRemoteAllowed = isRemoteAllowed
            };

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            var extensionAttributes = new List<XAttribute>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    var allowed = true;
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            compilerPayload.ParseId(attrib);
                            break;
                        case "CertificatePublicKey":
                            compilerPayload.ParseCertificatePublicKey(attrib);
                            break;
                        case "CertificateThumbprint":
                            compilerPayload.ParseCertificateThumbprint(attrib);
                            break;
                        case "Compressed":
                            compilerPayload.ParseCompressed(attrib);
                            break;
                        case "Hash":
                            compilerPayload.ParseHash(attrib);
                            break;
                        case "Name":
                            compilerPayload.ParseName(attrib);
                            break;
                        case "Size":
                            compilerPayload.ParseSize(attrib);
                            break;
                        case "SourceFile":
                            compilerPayload.ParseSourceFile(attrib);
                            break;
                        case "DownloadUrl":
                            compilerPayload.ParseDownloadUrl(attrib);
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.Core.UnexpectedAttribute(node, attrib);
                    }
                }
                else
                {
                    extensionAttributes.Add(attrib);
                }
            }

            compilerPayload.FinishCompilingPayload(parentId?.Id);

            // Now that the PayloadId is known, we can parse the extension attributes.
            var context = new Dictionary<string, string>
            {
                ["Id"] = compilerPayload.Id.Id,
            };

            foreach (var extensionAttribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, extensionAttribute, context);
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }

            compilerPayload.CreatePayloadSymbol(parentType, parentId?.Id);

            return compilerPayload.Id;
        }

        /// <summary>
        /// Parse PayloadGroup element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Optional ComplexReferenceParentType of parent element. (typically another PayloadGroup)</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private void ParsePayloadGroupElement(XElement node, ComplexReferenceParentType parentType, Identifier parentId)
        {
            Debug.Assert(ComplexReferenceParentType.Unknown == parentType || ComplexReferenceParentType.Layout == parentType || ComplexReferenceParentType.PayloadGroup == parentType);

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    WixBundlePackageType? packageType = null;
                    switch (child.Name.LocalName)
                    {
                        case "BundlePackagePayload":
                            packageType = WixBundlePackageType.Bundle;
                            break;
                        case "ExePackagePayload":
                            packageType = WixBundlePackageType.Exe;
                            break;
                        case "MsiPackagePayload":
                            packageType = WixBundlePackageType.Msi;
                            break;
                        case "MspPackagePayload":
                            packageType = WixBundlePackageType.Msp;
                            break;
                        case "MsuPackagePayload":
                            packageType = WixBundlePackageType.Msu;
                            break;
                        case "Payload":
                            this.ParsePayloadElement(child, ComplexReferenceParentType.PayloadGroup, id, isRemoteAllowed: true);
                            break;
                        case "PayloadGroupRef":
                            this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.PayloadGroup, id);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }

                    if (packageType.HasValue)
                    {
                        var compilerPackagePayload = this.ParsePackagePayloadElement(null, child, packageType.Value, null);
                        compilerPackagePayload.CreatePackagePayloadSymbol(ComplexReferenceParentType.PayloadGroup, id?.Id);
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBundlePayloadGroupSymbol(sourceLineNumbers, id));

                this.Core.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId?.Id, ComplexReferenceChildType.PayloadGroup, id.Id, ComplexReferenceChildType.Unknown, null);
            }
        }

        /// <summary>
        /// Parses a payload group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element (BA or PayloadGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private Identifier ParsePayloadGroupRefElement(XElement node, ComplexReferenceParentType parentType, Identifier parentId)
        {
            Debug.Assert(ComplexReferenceParentType.Layout == parentType || ComplexReferenceParentType.PayloadGroup == parentType || ComplexReferenceParentType.Package == parentType || ComplexReferenceParentType.Container == parentType);

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixBundlePayloadGroup, id.Id);
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            this.Core.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId?.Id, ComplexReferenceChildType.PayloadGroup, id?.Id, ComplexReferenceChildType.Unknown, null);

            return id;
        }

        /// <summary>
        /// Parse ExitCode element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageId">Id of parent element</param>
        private void ParseExitCodeElement(XElement node, string packageId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int? value = null;
            var behavior = ExitCodeBehaviorType.NotSet;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Value":
                            value = this.Core.GetAttributeRawIntegerValue(sourceLineNumbers, attrib);
                            break;
                        case "Behavior":
                            var behaviorString = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (behaviorString)
                            {
                                case "error":
                                    behavior = ExitCodeBehaviorType.Error;
                                    break;
                                case "errorForceReboot":
                                    behavior = ExitCodeBehaviorType.ErrorForceReboot;
                                    break;
                                case "errorScheduleReboot":
                                    behavior = ExitCodeBehaviorType.ErrorScheduleReboot;
                                    break;
                                case "forceReboot":
                                    behavior = ExitCodeBehaviorType.ForceReboot;
                                    break;
                                case "scheduleReboot":
                                    behavior = ExitCodeBehaviorType.ScheduleReboot;
                                    break;
                                case "success":
                                    behavior = ExitCodeBehaviorType.Success;
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Behavior", behaviorString, "success, error, scheduleReboot, forceReboot, errorScheduleReboot, errorForceReboot"));
                                    behavior = ExitCodeBehaviorType.Success; // set value to avoid ExpectedAttribute below.
                                    break;
                            }
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

            if (ExitCodeBehaviorType.NotSet == behavior)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Behavior"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBundlePackageExitCodeSymbol(sourceLineNumbers)
                {
                    ChainPackageId = packageId,
                    Code = value,
                    Behavior = behavior
                });
            }
        }

        /// <summary>
        /// Parse Chain element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseChainElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var attributes = WixChainAttributes.None;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "DisableRollback":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.DisableRollback;
                            }
                            break;
                        case "DisableSystemRestore":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.DisableSystemRestore;
                            }
                            break;
                        case "ParallelCache":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixChainAttributes.ParallelCache;
                            }
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

            string previousId = null;
            var previousType = ComplexReferenceChildType.Unknown;

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "MsiPackage":
                            previousId = this.ParseMsiPackageElement(child, ComplexReferenceParentType.PackageGroup, BurnConstants.BundleChainPackageGroupId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "MspPackage":
                            previousId = this.ParseMspPackageElement(child, ComplexReferenceParentType.PackageGroup, BurnConstants.BundleChainPackageGroupId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "MsuPackage":
                            previousId = this.ParseMsuPackageElement(child, ComplexReferenceParentType.PackageGroup, BurnConstants.BundleChainPackageGroupId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "ExePackage":
                            previousId = this.ParseExePackageElement(child, ComplexReferenceParentType.PackageGroup, BurnConstants.BundleChainPackageGroupId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "BundlePackage":
                            previousId = this.ParseBundlePackageElement(child, ComplexReferenceParentType.PackageGroup, BurnConstants.BundleChainPackageGroupId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "RollbackBoundary":
                            previousId = this.ParseRollbackBoundaryElement(child, ComplexReferenceParentType.PackageGroup, BurnConstants.BundleChainPackageGroupId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "PackageGroupRef":
                            previousId = this.ParsePackageGroupRefElement(child, ComplexReferenceParentType.PackageGroup, BurnConstants.BundleChainPackageGroupId, previousType, previousId);
                            previousType = ComplexReferenceChildType.PackageGroup;
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


            if (null == previousId)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "MsiPackage", "ExePackage", "PackageGroupRef"));
            }

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixChainSymbol(sourceLineNumbers)
                {
                    Attributes = attributes
                });
            }
        }

        /// <summary>
        /// Parse MsiPackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseMsiPackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return this.ParseChainPackage(node, WixBundlePackageType.Msi, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse MspPackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseMspPackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return this.ParseChainPackage(node, WixBundlePackageType.Msp, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse MsuPackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseMsuPackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return this.ParseChainPackage(node, WixBundlePackageType.Msu, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse ExePackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseExePackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return this.ParseChainPackage(node, WixBundlePackageType.Exe, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse BundlePackage element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseBundlePackageElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            return this.ParseChainPackage(node, WixBundlePackageType.Bundle, parentType, parentId, previousType, previousId);
        }

        /// <summary>
        /// Parse RollbackBoundary element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        private string ParseRollbackBoundaryElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            Debug.Assert(ComplexReferenceParentType.PackageGroup == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PackageGroup == previousType || ComplexReferenceChildType.Package == previousType);

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var vital = YesNoType.Yes;
            var transaction = YesNoType.No;
            string logPathVariable = null;

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            var extensionAttributes = new List<XAttribute>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    var allowed = true;
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            if (id?.Id == BurnConstants.BundleDefaultBoundaryId)
                            {
                                this.Messaging.Write(CompilerErrors.ReservedValue(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                            }
                            break;
                        case "Vital":
                            vital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Transaction":
                            transaction = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "LogPathVariable":
                            logPathVariable = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.Core.UnexpectedAttribute(node, attrib);
                    }
                }
                else
                {
                    // Save the extension attributes for later...
                    extensionAttributes.Add(attrib);
                }
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(previousId))
                {
                    id = this.Core.CreateIdentifier("rba", previousId);
                }

                if (null == id)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else if (!Common.IsIdentifier(id.Id))
                {
                    this.Core.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                }
            }

            // Now that the rollback identifier is known, we can parse the extension attributes...
            var contextValues = new Dictionary<string, string>
            {
                ["RollbackBoundaryId"] = id.Id
            };
            foreach (var attribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, attribute, contextValues);
            }

            this.Core.ParseForExtensionElements(node);

            if (transaction == YesNoType.Yes)
            {
                if (logPathVariable == null)
                {
                    logPathVariable = String.Concat("WixBundleLog_", id.Id);
                }
            }
            else if (logPathVariable != null)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "LogPathVariable", logPathVariable, "Transaction"));
            }

            if (!this.Core.EncounteredError)
            {
                this.CreateRollbackBoundary(sourceLineNumbers, id, vital, transaction, logPathVariable, parentType, parentId, previousType, previousId);
            }

            return id.Id;
        }

        /// <summary>
        /// Parses one of the ChainPackage elements
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageType">Type of package to parse</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <returns>Identifier for package element.</returns>
        /// <remarks>This method contains the shared logic for parsing all of the ChainPackage
        /// types, as there is more in common between them than different.</remarks>
        private string ParseChainPackage(XElement node, WixBundlePackageType packageType, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            Debug.Assert(ComplexReferenceParentType.PackageGroup == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PackageGroup == previousType || ComplexReferenceChildType.Package == previousType);

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var compilerPayload = new CompilerPayload(this.Core, sourceLineNumbers, node)
            {
                IsRequired = false,
            };
            string after = null;
            string installCondition = null;
            string repairCondition = null;
            var cache = BundleCacheType.Keep; // the default is to cache everything in tradeoff for stability over disk space.
            string cacheId = null;
            string description = null;
            string displayName = null;
            var logPathVariable = (packageType == WixBundlePackageType.Msu) ? String.Empty : null;
            var rollbackPathVariable = (packageType == WixBundlePackageType.Msu) ? String.Empty : null;
            var permanent = YesNoType.NotSet;
            var visible = YesNoType.NotSet;
            var vital = YesNoType.Yes;
            string installArguments = null;
            string repairArguments = null;
            string uninstallArguments = null;
            var perMachine = YesNoDefaultType.NotSet;
            string detectCondition = null;
            string protocol = null;
            long? installSize = null;
            var enableFeatureSelection = YesNoType.NotSet;
            var forcePerMachine = YesNoType.NotSet;
            CompilerPackagePayload childCompilerPackagePayload = null;
            var bundle = YesNoType.NotSet;
            var slipstream = YesNoType.NotSet;
            var hasPayloadInfo = false;
            var exeDetectionType = WixBundleExePackageDetectionType.None;
            string arpId = null;
            string arpDisplayVersion = null;
            var arpWin64 = YesNoType.NotSet;
            var arpUseUninstallString = YesNoType.NotSet;

            var expectedNetFx4Args = new string[] { "/q", "/norestart" };

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            var extensionAttributes = new List<XAttribute>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    var allowed = true;
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            compilerPayload.ParseId(attrib);
                            break;
                        case "Name":
                            compilerPayload.ParseName(attrib);
                            hasPayloadInfo = true;
                            break;
                        case "SourceFile":
                            compilerPayload.ParseSourceFile(attrib);
                            hasPayloadInfo = true;
                            break;
                        case "DownloadUrl":
                            compilerPayload.ParseDownloadUrl(attrib);
                            hasPayloadInfo = true;
                            break;
                        case "After":
                            after = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallCondition":
                            installCondition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RepairCondition":
                            repairCondition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType != WixBundlePackageType.Msu);
                            break;
                        case "Cache":
                            var value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (value)
                            {
                                case "force":
                                    cache = BundleCacheType.Force;
                                    break;
                                case "keep":
                                    cache = BundleCacheType.Keep;
                                    break;
                                case "remove":
                                    cache = BundleCacheType.Remove;
                                    break;
                                case "":
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, value, "force", "keep", "remove"));
                                    break;
                            }
                            break;
                        case "CacheId":
                            cacheId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EnableFeatureSelection":
                            enableFeatureSelection = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "ForcePerMachine":
                            forcePerMachine = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msi);
                            break;
                        case "LogPathVariable":
                            logPathVariable = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "RollbackLogPathVariable":
                            rollbackPathVariable = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Permanent":
                            permanent = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType != WixBundlePackageType.Msu);
                            break;
                        case "Visible":
                            visible = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Bundle || packageType == WixBundlePackageType.Msi);
                            break;
                        case "Vital":
                            vital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Bundle":
                            bundle = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "InstallArguments":
                            installArguments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Bundle || packageType == WixBundlePackageType.Exe);
                            break;
                        case "RepairArguments":
                            repairArguments = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            allowed = (packageType == WixBundlePackageType.Bundle || packageType == WixBundlePackageType.Exe);
                            break;
                        case "UninstallArguments":
                            uninstallArguments = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            allowed = (packageType == WixBundlePackageType.Bundle || packageType == WixBundlePackageType.Exe);
                            break;
                        case "PerMachine":
                            perMachine = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msp);
                            break;
                        case "DetectCondition":
                            detectCondition = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            allowed = (packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msu);
                            exeDetectionType = WixBundleExePackageDetectionType.Condition;
                            break;
                        case "Protocol":
                            protocol = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Exe);
                            break;
                        case "InstallSize":
                            installSize = this.Core.GetAttributeLongValue(sourceLineNumbers, attrib, 0, Int64.MaxValue);
                            break;
                        case "Compressed":
                            compilerPayload.ParseCompressed(attrib);
                            hasPayloadInfo = true;
                            break;
                        case "Slipstream":
                            slipstream = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            allowed = (packageType == WixBundlePackageType.Msp);
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.Core.UnexpectedAttribute(node, attrib);
                    }
                }
                else
                {
                    // Save the extension attributes for later...
                    extensionAttributes.Add(attrib);
                }
            }

            // We need to handle the package payload up front because it affects Id generation.  Id is needed by other child elements.
            var packagePayloadElementName = packageType + "PackagePayload";
            foreach (var child in node.Elements(CompilerCore.WixNamespace + packagePayloadElementName))
            {
                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                if (childCompilerPackagePayload != null)
                {
                    this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                }
                else if (hasPayloadInfo)
                {
                    this.Core.Write(ErrorMessages.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, child.Name.LocalName, "SourceFile", "Name", "DownloadUrl", "Compressed"));
                }

                childCompilerPackagePayload = this.ParsePackagePayloadElement(childSourceLineNumbers, child, packageType, compilerPayload.Id);
            }

            if (compilerPayload.Id == null && childCompilerPackagePayload != null)
            {
                compilerPayload.Id = childCompilerPackagePayload.CompilerPayload.Id;
            }

            compilerPayload.FinishCompilingPackage();
            var id = compilerPayload.Id;

            // Now that the package ID is known, we can parse the extension attributes...
            var contextValues = new Dictionary<string, string>() { { "PackageId", id.Id } };
            foreach (var attribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, attribute, contextValues);
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var allowed = true;
                    switch (child.Name.LocalName)
                    {
                        case "ArpEntry":
                            allowed = packageType == WixBundlePackageType.Exe;
                            if (allowed)
                            {
                                if (exeDetectionType != WixBundleExePackageDetectionType.None)
                                {
                                    this.Core.Write(ErrorMessages.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, child.Name.LocalName, "DetectCondition"));
                                }
                                if (null != uninstallArguments)
                                {
                                    this.Core.Write(ErrorMessages.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, child.Name.LocalName, "UninstallArguments"));
                                }

                                exeDetectionType = WixBundleExePackageDetectionType.Arp;
                                this.ParseExePackageArpEntryElement(child, out arpId, out arpDisplayVersion, out arpWin64, out uninstallArguments, out arpUseUninstallString);
                            }
                            break;
                        case "SlipstreamMsp":
                            allowed = (packageType == WixBundlePackageType.Msi);
                            if (allowed)
                            {
                                this.ParseSlipstreamMspElement(child, id.Id);
                            }
                            break;
                        case "MsiProperty":
                            allowed = (packageType == WixBundlePackageType.Msi || packageType == WixBundlePackageType.Msp);
                            if (allowed)
                            {
                                this.ParseMsiPropertyElement(child, id.Id);
                            }
                            break;
                        case "Payload":
                            this.ParsePayloadElement(child, ComplexReferenceParentType.Package, id, isRemoteAllowed: true);
                            break;
                        case "PayloadGroupRef":
                            this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Package, id);
                            break;
                        case "Provides":
                            this.ParseProvidesElement(child, packageType, id.Id, out _);
                            break;
                        case "ExitCode":
                            allowed = (packageType == WixBundlePackageType.Bundle || packageType == WixBundlePackageType.Exe);
                            if (allowed)
                            {
                                this.ParseExitCodeElement(child, id.Id);
                            }
                            break;
                        case "CommandLine":
                            allowed = (packageType == WixBundlePackageType.Bundle || packageType == WixBundlePackageType.Exe);
                            if (allowed)
                            {
                                this.ParseCommandLineElement(child, id.Id);
                            }
                            break;
                        case "BundlePackagePayload":
                        case "ExePackagePayload":
                        case "MsiPackagePayload":
                        case "MspPackagePayload":
                        case "MsuPackagePayload":
                            allowed = packagePayloadElementName == child.Name.LocalName;
                            // Handled previously
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }
                }
                else
                {
                    var context = new Dictionary<string, string>() { { "Id", id.Id } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }

            if (packageType == WixBundlePackageType.Exe && exeDetectionType == WixBundleExePackageDetectionType.None && uninstallArguments != null)
            {
                exeDetectionType = WixBundleExePackageDetectionType.Condition;
            }

            if (id.Id == BurnConstants.BundleDefaultBoundaryId)
            {
                this.Messaging.Write(CompilerErrors.ReservedValue(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
            }

            if (null == logPathVariable)
            {
                logPathVariable = String.Concat("WixBundleLog_", id.Id);
            }

            if (null == rollbackPathVariable)
            {
                rollbackPathVariable = String.Concat("WixBundleRollbackLog_", id.Id);
            }

            if (packageType == WixBundlePackageType.Bundle || packageType == WixBundlePackageType.Msi)
            {
                if (permanent == YesNoType.Yes && visible == YesNoType.NotSet)
                {
                    visible = YesNoType.Yes;
                }
            }
            else if (packageType == WixBundlePackageType.Exe)
            {
                // Set default scope for EXEs and MSPs if not already set.
                if (perMachine == YesNoDefaultType.NotSet)
                {
                    perMachine = YesNoDefaultType.Default;
                }

                if (exeDetectionType == WixBundleExePackageDetectionType.Arp)
                {
                    // Missing attributes are reported when parsing the element.
                }
                else if (exeDetectionType == WixBundleExePackageDetectionType.Condition)
                {
                    if (String.IsNullOrEmpty(detectCondition))
                    {
                        if (permanent == YesNoType.No)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DetectCondition", "Permanent", "no"));
                        }
                        else if (permanent == YesNoType.NotSet)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttributeWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DetectCondition", "Permanent"));
                        }
                        else if (repairArguments != null)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttributeWithValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DetectCondition", "RepairArguments"));
                        }
                        else if (uninstallArguments != null)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttributeWithValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DetectCondition", "UninstallArguments"));
                        }
                        else
                        {
                            Debug.Assert(detectCondition == String.Empty);
                            exeDetectionType = WixBundleExePackageDetectionType.None;
                        }
                    }

                    if (uninstallArguments == null)
                    {
                        if (permanent == YesNoType.No)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "UninstallArguments", "Permanent", "no"));
                        }
                        else if (permanent == YesNoType.NotSet)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttributeWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "UninstallArguments", "Permanent"));
                        }
                    }
                }
                else if (exeDetectionType == WixBundleExePackageDetectionType.None)
                {
                    if (permanent == YesNoType.No)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttributeOrElementWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DetectCondition", "ArpEntry", "Permanent", "no"));
                    }
                    else if (permanent == YesNoType.NotSet)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttributeOrElementWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DetectCondition", "ArpEntry", "Permanent"));
                    }
                    else if (repairArguments != null)
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttributeOrElementWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "DetectCondition", "ArpEntry", "RepairArguments"));
                    }
                    else
                    {
                        this.Core.Write(WarningMessages.ExePackageDetectInformationRecommended(sourceLineNumbers));
                    }
                }

                if (repairArguments == null && repairCondition != null)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributeWithValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "RepairArguments", "RepairCondition"));
                }

                // Validate the protocol if provided.
                if (!String.IsNullOrEmpty(protocol))
                {
                    if (protocol.Equals("netfx4", StringComparison.Ordinal))
                    {
                        foreach (var expectedArgument in expectedNetFx4Args)
                        {
                            if (null == installArguments || -1 == installArguments.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                            {
                                this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "InstallArguments", installArguments, expectedArgument, "Protocol", "netfx4"));
                            }

                            if (!String.IsNullOrEmpty(repairArguments) && -1 == repairArguments.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                            {
                                this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "RepairArguments", repairArguments, expectedArgument, "Protocol", "netfx4"));
                            }

                            if (!String.IsNullOrEmpty(uninstallArguments) && -1 == uninstallArguments.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                            {
                                this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "UninstallArguments", uninstallArguments, expectedArgument, "Protocol", "netfx4"));
                            }
                        }

                        if (bundle == YesNoType.Yes)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Protocol", protocol, "Bundle", "yes"));
                        }
                    }
                    else if (!protocol.Equals("burn", StringComparison.Ordinal) && !protocol.Equals("none", StringComparison.Ordinal))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Protocol", protocol, "none, burn, netfx4"));
                    }
                }
                else if (bundle == YesNoType.Yes)
                {
                    protocol = "burn";
                }
            }
            else if (packageType == WixBundlePackageType.Msp)
            {
                // Set default scope for EXEs and MSPs if not already set.
                if (perMachine == YesNoDefaultType.NotSet)
                {
                    perMachine = YesNoDefaultType.Default;
                }
            }
            else if (packageType == WixBundlePackageType.Msu)
            {
                // Detect condition is recommended for Msu packages.
                if (String.IsNullOrEmpty(detectCondition))
                {
                    this.Core.Write(WarningMessages.DetectConditionRecommended(sourceLineNumbers, node.Name.LocalName));
                }
            }

            if (!this.Core.EncounteredError)
            {
                var compilerPackagePayload = childCompilerPackagePayload ?? (hasPayloadInfo ? new CompilerPackagePayload(compilerPayload, packageType) : null);
                compilerPackagePayload?.CreatePackagePayloadSymbol(ComplexReferenceParentType.Package, id.Id);

                this.Core.AddSymbol(new WixChainItemSymbol(sourceLineNumbers, id));

                WixBundlePackageAttributes attributes = 0;
                attributes |= (YesNoType.Yes == permanent) ? WixBundlePackageAttributes.Permanent : 0;
                attributes |= (YesNoType.Yes == visible) ? WixBundlePackageAttributes.Visible : 0;

                var chainPackageSymbol = this.Core.AddSymbol(new WixBundlePackageSymbol(sourceLineNumbers, id)
                {
                    Type = packageType,
                    Attributes = attributes,
                    InstallCondition = installCondition,
                    RepairCondition = repairCondition,
                    Cache = cache,
                    CacheId = cacheId,
                    Description = description,
                    DisplayName = displayName,
                    LogPathVariable = logPathVariable,
                    RollbackLogPathVariable = rollbackPathVariable,
                    Vital = vital == YesNoType.Yes,
                });

                if (perMachine == YesNoDefaultType.Yes)
                {
                    chainPackageSymbol.PerMachine = true;
                }
                else if (perMachine == YesNoDefaultType.No)
                {
                    chainPackageSymbol.PerMachine = false;
                }

                if (installSize.HasValue)
                {
                    chainPackageSymbol.InstallSize = installSize;
                }

                switch (packageType)
                {
                    case WixBundlePackageType.Bundle:
                        WixBundleBundlePackageAttributes bundleAttributes = 0;

                        this.Core.AddSymbol(new WixBundleBundlePackageSymbol(sourceLineNumbers, id)
                        {
                            Attributes = bundleAttributes,
                            InstallCommand = installArguments,
                            RepairCommand = repairArguments,
                            UninstallCommand = uninstallArguments,
                        });
                        break;

                    case WixBundlePackageType.Exe:
                        WixBundleExePackageAttributes exeAttributes = 0;
                        exeAttributes |= (YesNoType.Yes == bundle) ? WixBundleExePackageAttributes.Bundle : 0;
                        exeAttributes |= (YesNoType.Yes == arpWin64) ? WixBundleExePackageAttributes.ArpWin64 : 0;
                        exeAttributes |= (YesNoType.Yes == arpUseUninstallString) ? WixBundleExePackageAttributes.ArpUseUninstallString : 0;

                        this.Core.AddSymbol(new WixBundleExePackageSymbol(sourceLineNumbers, id)
                        {
                            Attributes = exeAttributes,
                            DetectCondition = detectCondition,
                            InstallCommand = installArguments,
                            RepairCommand = repairArguments,
                            UninstallCommand = uninstallArguments,
                            ExeProtocol = protocol,
                            DetectionType = exeDetectionType,
                            ArpId = arpId,
                            ArpDisplayVersion = arpDisplayVersion,
                        });
                        break;

                    case WixBundlePackageType.Msi:
                        WixBundleMsiPackageAttributes msiAttributes = 0;
                        msiAttributes |= (YesNoType.Yes == enableFeatureSelection) ? WixBundleMsiPackageAttributes.EnableFeatureSelection : 0;
                        msiAttributes |= (YesNoType.Yes == forcePerMachine) ? WixBundleMsiPackageAttributes.ForcePerMachine : 0;

                        this.Core.AddSymbol(new WixBundleMsiPackageSymbol(sourceLineNumbers, id)
                        {
                            Attributes = msiAttributes
                        });
                        break;

                    case WixBundlePackageType.Msp:
                        WixBundleMspPackageAttributes mspAttributes = 0;
                        mspAttributes |= (YesNoType.Yes == slipstream) ? WixBundleMspPackageAttributes.Slipstream : 0;

                        this.Core.AddSymbol(new WixBundleMspPackageSymbol(sourceLineNumbers, id)
                        {
                            Attributes = mspAttributes
                        });
                        break;

                    case WixBundlePackageType.Msu:
                        this.Core.AddSymbol(new WixBundleMsuPackageSymbol(sourceLineNumbers, id)
                        {
                            DetectCondition = detectCondition
                        });
                        break;
                }

                this.CreateChainPackageMetaRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.Package, id.Id, previousType, previousId, after);
                this.Core.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.ContainerPackage, id.Id, ComplexReferenceChildType.Unknown, null);
            }

            return id.Id;
        }

        private CompilerPackagePayload ParsePackagePayloadElement(SourceLineNumber sourceLineNumbers, XElement node, WixBundlePackageType packageType, Identifier defaultId)
        {
            sourceLineNumbers = sourceLineNumbers ?? Preprocessor.GetSourceLineNumbers(node);
            var compilerPayload = new CompilerPayload(this.Core, sourceLineNumbers, node)
            {
                Id = defaultId,
                IsRemoteAllowed = packageType == WixBundlePackageType.Bundle || packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msu,
            };
            var compilerPackagePayload = new CompilerPackagePayload(compilerPayload, packageType);

            // This list lets us evaluate extension attributes *after* all core attributes
            // have been parsed and dealt with, regardless of authoring order.
            var extensionAttributes = new List<XAttribute>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    var allowed = true;
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            compilerPayload.ParseId(attrib);
                            break;
                        case "Compressed":
                            compilerPayload.ParseCompressed(attrib);
                            break;
                        case "Name":
                            compilerPayload.ParseName(attrib);
                            break;
                        case "SourceFile":
                            compilerPayload.ParseSourceFile(attrib);
                            break;
                        case "CertificatePublicKey":
                            allowed = compilerPayload.IsRemoteAllowed;
                            if (allowed)
                            {
                                compilerPayload.ParseCertificatePublicKey(attrib);
                            }
                            break;
                        case "CertificateThumbprint":
                            allowed = compilerPayload.IsRemoteAllowed;
                            if (allowed)
                            {
                                compilerPayload.ParseCertificateThumbprint(attrib);
                            }
                            break;
                        case "DownloadUrl":
                            compilerPayload.ParseDownloadUrl(attrib);
                            break;
                        case "Description":
                            allowed = compilerPayload.IsRemoteAllowed;
                            if (allowed)
                            {
                                compilerPayload.ParseDescription(attrib);
                            }
                            break;
                        case "Hash":
                            allowed = compilerPayload.IsRemoteAllowed;
                            if (allowed)
                            {
                                compilerPayload.ParseHash(attrib);
                            }
                            break;
                        case "PayloadGeneration":
                            allowed = compilerPackagePayload.ParsePayloadGeneration(attrib);
                            break;
                        case "ProductName":
                            allowed = compilerPayload.IsRemoteAllowed;
                            if (allowed)
                            {
                                compilerPayload.ParseProductName(attrib);
                            }
                            break;
                        case "Size":
                            allowed = compilerPayload.IsRemoteAllowed;
                            if (allowed)
                            {
                                compilerPayload.ParseSize(attrib);
                            }
                            break;
                        case "Version":
                            allowed = compilerPayload.IsRemoteAllowed;
                            if (allowed)
                            {
                                compilerPayload.ParseVersion(attrib);
                            }
                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.Core.UnexpectedAttribute(node, attrib);
                    }
                }
                else
                {
                    extensionAttributes.Add(attrib);
                }
            }

            compilerPayload.FinishCompilingPackagePayload();

            // Now that the PayloadId is known, we can parse the extension attributes.
            var context = new Dictionary<string, string>
            {
                ["Id"] = compilerPayload.Id.Id,
            };

            foreach (var extensionAttribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, extensionAttribute, context);
            }

            var remoteBundleSeen = false;

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    bool allowed;
                    switch (child.Name.LocalName)
                    {
                        case "RemoteBundle":
                            allowed = packageType == WixBundlePackageType.Bundle;

                            if (allowed)
                            {
                                if (compilerPackagePayload.PayloadGenerationType.HasValue)
                                {
                                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                    this.Core.Write(ErrorMessages.UnexpectedElementWithAttribute(childSourceLineNumbers, node.Name.LocalName, "RemoteBundle", "PayloadGeneration"));
                                }

                                if (remoteBundleSeen)
                                {
                                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                    this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "RemoteBundle"));
                                }

                                this.ParseRemoteBundleElement(child, compilerPayload.Id.Id);
                                remoteBundleSeen = true;
                            }

                            break;
                        default:
                            allowed = false;
                            break;
                    }

                    if (!allowed)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }

            var isLocal = !String.IsNullOrEmpty(compilerPayload.SourceFile);
            if (packageType == WixBundlePackageType.Bundle && !isLocal && !remoteBundleSeen)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "RemoteBundle"));
            }

            return compilerPackagePayload;
        }

        private void ParseRemoteBundleElement(XElement node, string packagePayloadId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string bundleCode = null;
            string displayName = null;
            string engineVersion = null;
            long? installSize = null;
            string manifestNamespace = null;
            var perMachine = YesNoType.NotSet;
            var protocolVersion = -1;
            string providerKey = null;
            string upgradeCode = null;
            string version = null;
            var win64 = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "BundleId":
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "BundleCode"));
                            goto case "BundleCode";
                        case "BundleCode":
                            bundleCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EngineVersion":
                            engineVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "InstallSize":
                            installSize = this.Core.GetAttributeLongValue(sourceLineNumbers, attrib, 0, Int64.MaxValue);
                            break;
                        case "ManifestNamespace":
                            manifestNamespace = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PerMachine":
                            perMachine = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ProtocolVersion":
                            protocolVersion = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                            break;
                        case "ProviderKey":
                            providerKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Win64":
                            win64 = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(bundleCode))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "BundleCode"));
            }

            if (String.IsNullOrEmpty(manifestNamespace))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ManifestNamespace"));
            }

            if (perMachine == YesNoType.NotSet)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PerMachine"));
            }

            if (protocolVersion == -1)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProtocolVersion"));
            }

            if (String.IsNullOrEmpty(providerKey))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProviderKey"));
            }

            if (String.IsNullOrEmpty(upgradeCode))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "UpgradeCode"));
            }

            if (String.IsNullOrEmpty(version))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            if (win64 == YesNoType.NotSet)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Win64"));
            }

            if (!this.Messaging.EncounteredError)
            {
                WixBundleHarvestedBundlePackageAttributes bundleAttributes = 0;
                bundleAttributes |= (YesNoType.Yes == perMachine) ? WixBundleHarvestedBundlePackageAttributes.PerMachine : 0;
                bundleAttributes |= (YesNoType.Yes == win64) ? WixBundleHarvestedBundlePackageAttributes.Win64 : 0;

                var symbol = this.Core.AddSymbol(new WixBundleHarvestedBundlePackageSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, packagePayloadId))
                {
                    Attributes = bundleAttributes,
                    BundleCode = bundleCode,
                    DisplayName = displayName,
                    EngineVersion = engineVersion,
                    ManifestNamespace = manifestNamespace,
                    ProtocolVersion = protocolVersion,
                    Version = version,
                });

                if (installSize.HasValue)
                {
                    symbol.InstallSize = installSize.Value;
                }

                this.Core.AddSymbol(new WixBundlePackageRelatedBundleSymbol(sourceLineNumbers)
                {
                    PackagePayloadRef = packagePayloadId,
                    BundleCode = upgradeCode,
                    Action = RelatedBundleActionType.Upgrade,
                });

                var depId = this.Core.CreateIdentifier("dep", packagePayloadId, providerKey);
                this.Core.AddSymbol(new WixBundleHarvestedDependencyProviderSymbol(sourceLineNumbers, depId)
                {
                    PackagePayloadRef = packagePayloadId,
                    ProviderKey = providerKey,
                    Version = version,
                });
            }

            var context = new Dictionary<string, string>
            {
                ["Id"] = packagePayloadId,
            };

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "RemoteRelatedBundle":
                            this.ParseRemoteRelatedBundleElement(child, packagePayloadId);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }
        }

        private void ParseRemoteRelatedBundleElement(XElement node, string payloadId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            RelatedBundleActionType? actionType = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Action":
                            var action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (action)
                            {
                                case "Detect":
                                case "detect":
                                    actionType = RelatedBundleActionType.Detect;
                                    break;
                                case "Upgrade":
                                case "upgrade":
                                    actionType = RelatedBundleActionType.Upgrade;
                                    break;
                                case "Addon":
                                case "addon":
                                    actionType = RelatedBundleActionType.Addon;
                                    break;
                                case "Patch":
                                case "patch":
                                    actionType = RelatedBundleActionType.Patch;
                                    break;
                                case "":
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Action", action, "Detect", "Upgrade", "Addon", "Patch"));
                                    break;
                            }
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (!actionType.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
            }

            var context = new Dictionary<string, string>
            {
                ["PayloadId"] = payloadId,
            };

            this.Core.ParseForExtensionElements(node, context);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBundlePackageRelatedBundleSymbol(sourceLineNumbers)
                {
                    PackagePayloadRef = payloadId,
                    BundleCode = id,
                    Action = actionType.Value,
                });
            }
        }

        private void ParseExePackageArpEntryElement(XElement node, out string id, out string version, out YesNoType win64, out string uninstallArguments, out YesNoType arpUseUninstallString)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            id = null;
            version = null;
            win64 = YesNoType.NotSet;
            arpUseUninstallString = YesNoType.NotSet;
            uninstallArguments = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "AdditionalUninstallArguments":
                            uninstallArguments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UseUninstallString":
                            arpUseUninstallString = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Win64":
                            win64 = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(id))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (String.IsNullOrEmpty(version))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            if (win64 == YesNoType.NotSet)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Win64"));
            }
        }

        /// <summary>
        /// Parse CommandLine element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageId">Parent packageId</param>
        private void ParseCommandLineElement(XElement node, string packageId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string installArgument = null;
            string uninstallArgument = null;
            string repairArgument = null;
            string condition = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "InstallArgument":
                            installArgument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UninstallArgument":
                            uninstallArgument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RepairArgument":
                            repairArgument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(condition))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Condition"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBundlePackageCommandLineSymbol(sourceLineNumbers)
                {
                    WixBundlePackageRef = packageId,
                    InstallArgument = installArgument,
                    UninstallArgument = uninstallArgument,
                    RepairArgument = repairArgument,
                    Condition = condition
                });
            }
        }

        /// <summary>
        /// Parse PackageGroup element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParsePackageGroupElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            if (id?.Id == BurnConstants.BundleChainPackageGroupId)
                            {
                                this.Messaging.Write(CompilerErrors.ReservedValue(sourceLineNumbers, node.Name.LocalName, "Id", id.Id));
                            }
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            var previousType = ComplexReferenceChildType.Unknown;
            string previousId = null;
            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "MsiPackage":
                            previousId = this.ParseMsiPackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "MspPackage":
                            previousId = this.ParseMspPackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "MsuPackage":
                            previousId = this.ParseMsuPackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "ExePackage":
                            previousId = this.ParseExePackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "BundlePackage":
                            previousId = this.ParseBundlePackageElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "RollbackBoundary":
                            previousId = this.ParseRollbackBoundaryElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.Package;
                            break;
                        case "PackageGroupRef":
                            previousId = this.ParsePackageGroupRefElement(child, ComplexReferenceParentType.PackageGroup, id.Id, previousType, previousId);
                            previousType = ComplexReferenceChildType.PackageGroup;
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
                this.Core.AddSymbol(new WixBundlePackageGroupSymbol(sourceLineNumbers, id));
            }
        }

        /// <summary>
        /// Parses a package group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element (Unknown or PackageGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        /// <returns>Identifier for package group element.</returns>
        private string ParsePackageGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            return this.ParsePackageGroupRefElement(node, parentType, parentId, ComplexReferenceChildType.Unknown, null);
        }

        /// <summary>
        /// Parses a package group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element (Unknown or PackageGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        /// <param name="previousType"></param>
        /// <param name="previousId"></param>
        /// <returns>Identifier for package group element.</returns>
        private string ParsePackageGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            Debug.Assert(ComplexReferenceParentType.Unknown == parentType || ComplexReferenceParentType.PackageGroup == parentType || ComplexReferenceParentType.Container == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PackageGroup == previousType || ComplexReferenceChildType.Package == previousType);

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string after = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            if (id == BurnConstants.BundleChainPackageGroupId)
                            {
                                this.Messaging.Write(CompilerErrors.ReservedValue(sourceLineNumbers, node.Name.LocalName, "Id", id));
                            }
                            else
                            {
                                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixBundlePackageGroup, id);
                            }
                            break;
                        case "After":
                            after = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null != after && ComplexReferenceParentType.Container == parentType)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "After", parentId));
            }

            this.Core.ParseForExtensionElements(node);

            if (ComplexReferenceParentType.Container == parentType)
            {
                this.Core.CreateWixGroupRow(sourceLineNumbers, ComplexReferenceParentType.Container, parentId, ComplexReferenceChildType.PackageGroup, id);
            }
            else
            {
                this.CreateChainPackageMetaRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.PackageGroup, id, previousType, previousId, after);
            }

            return id;
        }

        /// <summary>
        /// Creates rollback boundary.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="id">Identifier for the rollback boundary.</param>
        /// <param name="vital">Indicates whether the rollback boundary is vital or not.</param>
        /// <param name="transaction">Indicates whether the rollback boundary will use an MSI transaction.</param>
        /// <param name="logPathVariable">The variable for the path of the MSI transaction log.</param>
        /// <param name="parentType">Type of parent group.</param>
        /// <param name="parentId">Identifier of parent group.</param>
        /// <param name="previousType">Type of previous item, if any.</param>
        /// <param name="previousId">Identifier of previous item, if any.</param>
        private void CreateRollbackBoundary(SourceLineNumber sourceLineNumbers, Identifier id, YesNoType vital, YesNoType transaction, string logPathVariable, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            this.Core.AddSymbol(new WixChainItemSymbol(sourceLineNumbers, id));

            var rollbackBoundary = this.Core.AddSymbol(new WixBundleRollbackBoundarySymbol(sourceLineNumbers, id)
            {
                Transaction = transaction == YesNoType.Yes,
                Vital = vital == YesNoType.Yes,
            });

            if (logPathVariable != null)
            {
                rollbackBoundary.LogPathVariable = logPathVariable;
            }

            this.CreateChainPackageMetaRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.Package, id.Id, previousType, previousId, null);
        }

        /// <summary>
        /// Creates group and ordering information for packages
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="type">Type of this item.</param>
        /// <param name="id">Identifier for this item.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        /// <param name="afterId">Identifier of explicit 'After' attribute, if given.</param>
        private void CreateChainPackageMetaRows(SourceLineNumber sourceLineNumbers,
            ComplexReferenceParentType parentType, string parentId,
            ComplexReferenceChildType type, string id,
            ComplexReferenceChildType previousType, string previousId, string afterId)
        {
            // If there's an explicit 'After' attribute, it overrides the inferred previous item.
            if (null != afterId)
            {
                previousType = ComplexReferenceChildType.Package;
                previousId = afterId;
            }

            this.Core.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId, type, id, previousType, previousId);
        }

        /// <summary>
        /// Parse MsiProperty element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageId">Id of parent element</param>
        private void ParseMsiPropertyElement(XElement node, string packageId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string value = null;
            string condition = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeMsiPropertyNameValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var symbol = this.Core.AddSymbol(new WixBundleMsiPropertySymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, packageId, name))
                {
                    PackageRef = packageId,
                    Name = name,
                    Value = value
                });

                if (!String.IsNullOrEmpty(condition))
                {
                    symbol.Condition = condition;
                }
            }
        }

        /// <summary>
        /// Parse SlipstreamMsp element
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageId">Id of parent element</param>
        private void ParseSlipstreamMspElement(XElement node, string packageId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixBundlePackage, id);
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
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBundleSlipstreamMspSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, packageId, id))
                {
                    TargetPackageRef = packageId,
                    MspPackageRef = id
                });
            }
        }

        /// <summary>
        /// Parse RelatedBundle element
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseRelatedBundleElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string code = null;
            var actionType = RelatedBundleActionType.Detect;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Code"));
                            goto case "Code";
                        case "Code":
                            code = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Action":
                            var action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (action)
                            {
                                case "Detect":
                                case "detect":
                                    actionType = RelatedBundleActionType.Detect;
                                    break;
                                case "Upgrade":
                                case "upgrade":
                                    actionType = RelatedBundleActionType.Upgrade;
                                    break;
                                case "Addon":
                                case "addon":
                                    actionType = RelatedBundleActionType.Addon;
                                    break;
                                case "Patch":
                                case "patch":
                                    actionType = RelatedBundleActionType.Patch;
                                    break;
                                case "":
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Action", action, "Detect", "Upgrade", "Addon", "Patch"));
                                    break;
                            }
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

            if (null == code)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Code"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixRelatedBundleSymbol(sourceLineNumbers)
                {
                    BundleCode = code,
                    Action = actionType,
                });
            }
        }

        /// <summary>
        /// Parse Update element
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseUpdateElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string location = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Location":
                            location = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == location)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Location"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBundleUpdateSymbol(sourceLineNumbers)
                {
                    Location = location
                });
            }
        }

        /// <summary>
        /// Parse SetVariable element
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseSetVariableElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string value = null;
            string typeValue = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Variable":
                            variable = this.Core.GetAttributeBundleVariableNameValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "After":
                            after = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;

                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib, null);
                }
            }

            var type = this.ValidateVariableTypeWithValue(sourceLineNumbers, node, typeValue, value);

            this.Core.ParseForExtensionElements(node);

            if (id == null)
            {
                id = this.Core.CreateIdentifier("sbv", variable, condition, after, value, type.ToString());
            }

            if (!this.Messaging.EncounteredError)
            {
                this.Core.CreateWixSearchSymbol(sourceLineNumbers, node.Name.LocalName, id, variable, condition, after);

                this.Core.AddSymbol(new WixSetVariableSymbol(sourceLineNumbers, id)
                {
                    Value = value,
                    Type = type,
                });
            }
        }

        /// <summary>
        /// Parse Variable element
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseVariableElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var hidden = false;
            Identifier name = null;
            var persisted = false;
            string value = null;
            string typeValue = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Hidden":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                hidden = true;
                            }
                            break;
                        case "Name":
                            name = this.Core.GetAttributeBundleVariableNameIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Persisted":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                persisted = true;
                            }
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "Type":
                            typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (hidden && persisted)
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Hidden", "yes", "Persisted"));
            }

            var type = this.ValidateVariableTypeWithValue(sourceLineNumbers, node, typeValue, value);

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, name)
                {
                    Value = value,
                    Type = type,
                    Hidden = hidden,
                    Persisted = persisted
                });
            }
        }

        private WixBundleVariableType ValidateVariableTypeWithValue(SourceLineNumber sourceLineNumbers, XElement node, string typeValue, string value)
        {
            WixBundleVariableType type;
            switch (typeValue)
            {
                case "formatted":
                    type = WixBundleVariableType.Formatted;
                    break;
                case "numeric":
                    type = WixBundleVariableType.Numeric;
                    break;
                case "string":
                    type = WixBundleVariableType.String;
                    break;
                case "version":
                    type = WixBundleVariableType.Version;
                    break;
                case null:
                    type = WixBundleVariableType.Unknown;
                    break;
                default:
                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "formatted", "numeric", "string", "version"));
                    return WixBundleVariableType.Unknown;
            }

            if (type != WixBundleVariableType.Unknown)
            {
                if (value == null)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, "Variable", "Value", "Type"));
                }

                return type;
            }
            else if (value == null)
            {
                return type;
            }

            // Infer the type from the current value...
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                // Version constructor does not support simple "v#" syntax so check to see if the value is
                // non-negative real quick.
                if (Int32.TryParse(value.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out var _))
                {
                    return WixBundleVariableType.Version;
                }
                else if (Version.TryParse(value.Substring(1), out var _))
                {
                    return WixBundleVariableType.Version;
                }
            }

            // Not a version, check for numeric.
            if (Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var _))
            {
                return WixBundleVariableType.Numeric;
            }

            return WixBundleVariableType.String;
        }
    }
}
