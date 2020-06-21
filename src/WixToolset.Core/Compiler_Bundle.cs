// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        private static readonly Identifier BurnUXContainerId = new Identifier(AccessModifier.Private, BurnConstants.BurnUXContainerName);
        private static readonly Identifier BurnDefaultAttachedContainerId = new Identifier(AccessModifier.Private, BurnConstants.BurnDefaultAttachedContainerName);
        private static readonly Identifier BundleLayoutOnlyPayloads = new Identifier(AccessModifier.Private, BurnConstants.BundleLayoutOnlyPayloadsName);

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
            var win64 = YesNoType.NotSet;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Key":
                        key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Value":
                        valueName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == key)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            var attributes = WixApprovedExeForElevationAttributes.None;

            if (win64 == YesNoType.Yes)
            {
                attributes |= WixApprovedExeForElevationAttributes.Win64;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddTuple(new WixApprovedExeForElevationTuple(sourceLineNumbers, id)
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
            string copyright = null;
            string aboutUrl = null;
            var compressed = YesNoDefaultType.Default;
            WixBundleAttributes attributes = 0;
            string helpTelephone = null;
            string helpUrl = null;
            string manufacturer = null;
            string name = null;
            string tag = null;
            string updateUrl = null;
            string upgradeCode = null;
            string version = null;
            string condition = null;
            string parentName = null;

            string fileSystemSafeBundleName = null;
            string logVariablePrefixAndExtension = null;
            string iconSourceFile = null;
            string splashScreenSourceFile = null;

            // Process only standard attributes until the active section is initialized.
            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
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
                            attributes |= WixBundleAttributes.SingleChangeUninstallButton;
                            break;
                        case "yes":
                            attributes |= WixBundleAttributes.DisableModify;
                            break;
                        case "no":
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
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ParentName":
                        parentName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
            else if (!CompilerCore.IsValidModuleOrBundleVersion(version))
            {
                this.Core.Write(WarningMessages.InvalidModuleOrBundleVersion(sourceLineNumbers, "Bundle", version));
            }

            if (String.IsNullOrEmpty(upgradeCode))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "UpgradeCode"));
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

            if (String.IsNullOrEmpty(name))
            {
                logVariablePrefixAndExtension = String.Concat("WixBundleLog:Setup:log");
            }
            else
            {
                // Ensure only allowable path characters are in "name" (and change spaces to underscores).
                fileSystemSafeBundleName = CompilerCore.MakeValidLongFileName(name.Replace(' ', '_'), "_");
                logVariablePrefixAndExtension = String.Concat("WixBundleLog:", fileSystemSafeBundleName, ":log");
            }

            this.activeName = String.IsNullOrEmpty(name) ? Common.GenerateGuid() : name;
            this.Core.CreateActiveSection(this.activeName, SectionType.Bundle, 0, this.Context.CompilationId);

            // Now that the active section is initialized, process only extension attributes.
            foreach (var attrib in node.Attributes())
            {
                if (!String.IsNullOrEmpty(attrib.Name.NamespaceName) && CompilerCore.WixNamespace != attrib.Name.Namespace)
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            var baSeen = false;
            var chainSeen = false;
            var logSeen = false;

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
                        if (baSeen)
                        {
                            var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                            this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, "BootstrapperApplication"));
                        }
                        this.ParseBootstrapperApplicationElement(child);
                        baSeen = true;
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
                    case "BundleExtension":
                        this.ParseBundleExtensionElement(child);
                        break;
                    case "BundleExtensionRef":
                        this.ParseSimpleRefElement(child, TupleDefinitions.WixBundleExtension);
                        break;
                    case "OptionalUpdateRegistration":
                        this.ParseOptionalUpdateRegistrationElement(child, manufacturer, parentName, name);
                        break;
                    case "Catalog":
                        this.ParseCatalogElement(child);
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
                        this.ParseSimpleRefElement(child, TupleDefinitions.WixBundleContainer);
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
                        this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Layout, Compiler.BundleLayoutOnlyPayloads, ComplexReferenceChildType.Unknown, null);
                        break;
                    case "RelatedBundle":
                        this.ParseRelatedBundleElement(child);
                        break;
                    case "SetVariable":
                        this.ParseSetVariableElement(child);
                        break;
                    case "SetVariableRef":
                        this.ParseSimpleRefElement(child, TupleDefinitions.WixSetVariable);
                        break;
                    case "Update":
                        this.ParseUpdateElement(child);
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
                var tuple = this.Core.AddTuple(new WixBundleTuple(sourceLineNumbers)
                {
                    UpgradeCode = upgradeCode,
                    Version = version,
                    Copyright = copyright,
                    Name = name,
                    Manufacturer = manufacturer,
                    Attributes = attributes,
                    AboutUrl = aboutUrl,
                    HelpUrl = helpUrl,
                    HelpTelephone = helpTelephone,
                    UpdateUrl = updateUrl,
                    Compressed = YesNoDefaultType.Yes == compressed ? true : YesNoDefaultType.No == compressed ? (bool?)false : null,
                    IconSourceFile = iconSourceFile,
                    SplashScreenSourceFile = splashScreenSourceFile,
                    Condition = condition,
                    Tag = tag,
                    Platform = this.CurrentPlatform,
                    ParentName = parentName,
                });

                if (!String.IsNullOrEmpty(logVariablePrefixAndExtension))
                {
                    var split = logVariablePrefixAndExtension.Split(':');
                    tuple.LogPathVariable = split[0];
                    tuple.LogPrefix = split[1];
                    tuple.LogExtension = split[2];
                }

                if (null != upgradeCode)
                {
                    this.Core.AddTuple(new WixRelatedBundleTuple(sourceLineNumbers)
                    {
                        BundleId = upgradeCode,
                        Action = RelatedBundleActionType.Upgrade,
                    });
                }

                this.Core.AddTuple(new WixBundleContainerTuple(sourceLineNumbers, Compiler.BurnDefaultAttachedContainerId)
                {
                    Name = "bundle-attached.cab",
                    Type = ContainerType.Attached,
                });

                // Ensure that the bundle stores the well-known persisted values.
                this.Core.AddTuple(new WixBundleVariableTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, BurnConstants.BURN_BUNDLE_NAME))
                {
                    Hidden = false,
                    Persisted = true,
                });

                this.Core.AddTuple(new WixBundleVariableTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, BurnConstants.BURN_BUNDLE_ORIGINAL_SOURCE))
                {
                    Hidden = false,
                    Persisted = true,
                });

                this.Core.AddTuple(new WixBundleVariableTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, BurnConstants.BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER))
                {
                    Hidden = false,
                    Persisted = true,
                });

                this.Core.AddTuple(new WixBundleVariableTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, BurnConstants.BURN_BUNDLE_LAST_USED_SOURCE))
                {
                    Hidden = false,
                    Persisted = true,
                });
            }
        }

        /// <summary>
        /// Parse a Container element.
        /// </summary>
        /// <param name="node">Element to parse</param>
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
        /// Parse a Catalog element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseCatalogElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string sourceFile = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "SourceFile":
                        sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            this.Core.ParseForExtensionElements(node);

            // Create catalog row
            if (!this.Core.EncounteredError)
            {
                this.CreatePayloadRow(sourceLineNumbers, id, Path.GetFileName(sourceFile), sourceFile, null, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, ComplexReferenceChildType.Unknown, null, YesNoDefaultType.Yes, YesNoType.Yes, null, null, null);

                this.Core.AddTuple(new WixBundleCatalogTuple(sourceLineNumbers, id)
                {
                    PayloadRef = id.Id,
                });
            }
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
                        break;
                    case "DownloadUrl":
                        downloadUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Type":
                        var typeString = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        if (!Enum.TryParse<ContainerType>(typeString, out type))
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Type", typeString, "attached, detached"));
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
                this.Core.AddTuple(new WixBundleContainerTuple(sourceLineNumbers, id)
                {
                    Name = name,
                    Type = type,
                    DownloadUrl = downloadUrl
                });
            }
        }

        /// <summary>
        /// Parse the BoostrapperApplication element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBootstrapperApplicationElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier previousId = null;
            var previousType = ComplexReferenceChildType.Unknown;

            // The BootstrapperApplication element acts like a Payload element so delegate to the "Payload" attribute parsing code to parse and create a Payload entry.
            var id = this.ParsePayloadElementContent(node, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId, false);
            if (null != id)
            {
                previousId = id;
                previousType = ComplexReferenceChildType.Payload;
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Payload":
                        previousId = this.ParsePayloadElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                        previousType = ComplexReferenceChildType.Payload;
                        break;
                    case "PayloadGroupRef":
                        previousId = this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                        previousType = ComplexReferenceChildType.PayloadGroup;
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
                // We need *either* <Payload> or <PayloadGroupRef> or even just @SourceFile on the BA...
                // but we just say there's a missing <Payload>.
                // TODO: Is there a better message for this?
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "Payload"));
            }

            // Add the application as an attached container and if an Id was provided add that too.
            if (!this.Core.EncounteredError)
            {
                this.Core.AddTuple(new WixBundleContainerTuple(sourceLineNumbers, Compiler.BurnUXContainerId)
                {
                    Name = "bundle-ux.cab",
                    Type = ContainerType.Attached
                });

                if (null != id)
                {
                    this.Core.AddTuple(new WixBootstrapperApplicationTuple(sourceLineNumbers, id));
                }
            }
        }

        /// <summary>
        /// Parse the BoostrapperApplicationRef element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBootstrapperApplicationRefElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            Identifier previousId = null;
            var previousType = ComplexReferenceChildType.Unknown;

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
                        previousId = this.ParsePayloadElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                        previousType = ComplexReferenceChildType.Payload;
                        break;
                    case "PayloadGroupRef":
                        previousId = this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                        previousType = ComplexReferenceChildType.PayloadGroup;
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
                this.Core.CreateSimpleReference(sourceLineNumbers, TupleDefinitions.WixBootstrapperApplication, id);
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
            var attributeDefinitions = new List<WixBundleCustomDataAttributeTuple>();
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
                                case "bundleExtension":
                                    customDataType = WixBundleCustomDataType.BundleExtension;
                                    break;
                                default:
                                    this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Type", typeValue, "bootstrapperApplication", "bundleExtension"));
                                    customDataType = WixBundleCustomDataType.Unknown; // set a value to prevent expected attribute error below.
                                    break;
                            }
                            break;
                        case "ExtensionId":
                            extensionId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, TupleDefinitions.WixBundleExtension, extensionId);
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
                customDataType = hasExtensionId ? WixBundleCustomDataType.BundleExtension : WixBundleCustomDataType.BootstrapperApplication;
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
            else if (customDataType.Value == WixBundleCustomDataType.BundleExtension)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ExtensionId", "Type", "bundleExtension"));
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
                    var attributeNames = String.Join(new string(WixBundleCustomDataTuple.AttributeNamesSeparator, 1), attributeDefinitions.Select(c => c.Name));

                    this.Core.AddTuple(new WixBundleCustomDataTuple(sourceLineNumbers, new Identifier(AccessModifier.Public, customDataId))
                    {
                        AttributeNames = attributeNames,
                        Type = customDataType.Value,
                        BundleExtensionRef = extensionId,
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
            var foundChild = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            customDataId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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
                foundChild = true;
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

            if (!foundChild)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName));
            }
        }

        /// <summary>
        /// Parses a BundleAttributeDefinition element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="sourceLineNumbers">Element's SourceLineNumbers.</param>
        /// <param name="customDataId">BundleCustomData Id.</param>
        private WixBundleCustomDataAttributeTuple ParseBundleAttributeDefinitionElement(XElement node, SourceLineNumber sourceLineNumbers, string customDataId)
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

            var customDataAttribute = this.Core.AddTuple(new WixBundleCustomDataAttributeTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, customDataId, attributeName))
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

                        if (String.IsNullOrEmpty(value))
                        {
                            value = Common.GetInnerText(child);
                        }

                        if (!this.Core.EncounteredError)
                        {
                            this.Core.AddTuple(new WixBundleCustomDataCellTuple(childSourceLineNumbers, new Identifier(AccessModifier.Private, customDataId, elementId, attributeName))
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
                this.Core.CreateSimpleReference(sourceLineNumbers, TupleDefinitions.WixBundleCustomData, customDataId);
            }
        }

        /// <summary>
        /// Parse the BundleExtension element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        private void ParseBundleExtensionElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier previousId = null;
            var previousType = ComplexReferenceChildType.Unknown;

            // The BundleExtension element acts like a Payload element so delegate to the "Payload" attribute parsing code to parse and create a Payload entry.
            var id = this.ParsePayloadElementContent(node, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId, false);
            if (null != id)
            {
                previousId = id;
                previousType = ComplexReferenceChildType.Payload;
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Payload":
                            previousId = this.ParsePayloadElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                            previousType = ComplexReferenceChildType.Payload;
                            break;
                        case "PayloadGroupRef":
                            previousId = this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Container, Compiler.BurnUXContainerId, previousType, previousId);
                            previousType = ComplexReferenceChildType.PayloadGroup;
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
                // We need *either* <Payload> or <PayloadGroupRef> or even just @SourceFile on the BundleExtension...
                // but we just say there's a missing <Payload>.
                // TODO: Is there a better message for this?
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "Payload"));
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            // Add the BundleExtension.
            if (!this.Core.EncounteredError)
            {
                this.Core.AddTuple(new WixBundleExtensionTuple(sourceLineNumbers, id)
                {
                    PayloadRef = id.Id,
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
                this.Core.AddTuple(new WixUpdateRegistrationTuple(sourceLineNumbers)
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
        private Identifier ParsePayloadElement(XElement node, ComplexReferenceParentType parentType, Identifier parentId, ComplexReferenceChildType previousType, Identifier previousId)
        {
            Debug.Assert(ComplexReferenceParentType.PayloadGroup == parentType || ComplexReferenceParentType.Package == parentType || ComplexReferenceParentType.Container == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PayloadGroup == previousType || ComplexReferenceChildType.Payload == previousType);

            var id = this.ParsePayloadElementContent(node, parentType, parentId, previousType, previousId, true);
            var context = new Dictionary<string, string>
            {
                ["Id"] = id?.Id
            };

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

            return id;
        }

        /// <summary>
        /// Parse the attributes of the Payload element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element.</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private Identifier ParsePayloadElementContent(XElement node, ComplexReferenceParentType parentType, Identifier parentId, ComplexReferenceChildType previousType, Identifier previousId, bool required)
        {
            Debug.Assert(ComplexReferenceParentType.PayloadGroup == parentType || ComplexReferenceParentType.Package == parentType || ComplexReferenceParentType.Container == parentType);

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var compressed = YesNoDefaultType.Default;
            var enableSignatureVerification = YesNoType.No;
            Identifier id = null;
            string name = null;
            string sourceFile = null;
            string downloadUrl = null;
            RemotePayload remotePayload = null;

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
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Compressed":
                        compressed = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                        break;
                    case "Name":
                        name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                        break;
                    case "SourceFile":
                        sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DownloadUrl":
                        downloadUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "EnableSignatureVerification":
                        enableSignatureVerification = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (!required && null == sourceFile)
            {
                // Nothing left to do!
                return null;
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("pay", sourceFile?.ToUpperInvariant() ?? String.Empty);
            }

            // Now that the PayloadId is known, we can parse the extension attributes.
            var context = new Dictionary<string, string>
            {
                ["Id"] = id?.Id
            };

            foreach (var extensionAttribute in extensionAttributes)
            {
                this.Core.ParseExtensionAttribute(node, extensionAttribute, context);
            }

            // We only handle the elements we care about.  Let caller handle other children.
            foreach (var child in node.Elements(CompilerCore.WixNamespace + "RemotePayload"))
            {
                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                if (CompilerCore.WixNamespace == node.Name.Namespace && node.Name.LocalName != "ExePackage")
                {
                    this.Core.Write(ErrorMessages.RemotePayloadUnsupported(childSourceLineNumbers));
                    continue;
                }

                if (null != remotePayload)
                {
                    this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                }

                remotePayload = this.ParseRemotePayloadElement(child);
            }

            if (null != sourceFile && null != remotePayload)
            {
                this.Core.Write(ErrorMessages.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, "RemotePayload", "SourceFile"));
            }
            else if (null == sourceFile && null == remotePayload)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributeOrElement(sourceLineNumbers, node.Name.LocalName, "SourceFile", "RemotePayload"));
            }
            else if (null == sourceFile)
            {
                sourceFile = String.Empty;
            }

            if (null == downloadUrl && null != remotePayload)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributeWithElement(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "RemotePayload"));
            }

            if (Compiler.BurnUXContainerId == parentId)
            {
                if (compressed == YesNoDefaultType.No)
                {
                    this.Core.Write(WarningMessages.UxPayloadsOnlySupportEmbedding(sourceLineNumbers, sourceFile));
                }

                compressed = YesNoDefaultType.Yes;
            }

            this.CreatePayloadRow(sourceLineNumbers, id, name, sourceFile, downloadUrl, parentType, parentId, previousType, previousId, compressed, enableSignatureVerification, null, null, remotePayload);

            return id;
        }

        private RemotePayload ParseRemotePayloadElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var remotePayload = new RemotePayload();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "CertificatePublicKey":
                        remotePayload.CertificatePublicKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "CertificateThumbprint":
                        remotePayload.CertificateThumbprint = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Description":
                        remotePayload.Description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Hash":
                        remotePayload.Hash = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ProductName":
                        remotePayload.ProductName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Size":
                        remotePayload.Size = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                        break;
                    case "Version":
                        remotePayload.Version = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(remotePayload.ProductName))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProductName"));
            }

            if (String.IsNullOrEmpty(remotePayload.Description))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }

            if (String.IsNullOrEmpty(remotePayload.Hash))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Hash"));
            }

            if (0 == remotePayload.Size)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Size"));
            }

            if (String.IsNullOrEmpty(remotePayload.Version))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            return remotePayload;
        }

        /// <summary>
        /// Creates the row for a Payload.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private WixBundlePayloadTuple CreatePayloadRow(SourceLineNumber sourceLineNumbers, Identifier id, string name, string sourceFile, string downloadUrl, ComplexReferenceParentType parentType,
            Identifier parentId, ComplexReferenceChildType previousType, Identifier previousId, YesNoDefaultType compressed, YesNoType enableSignatureVerification, string displayName, string description,
            RemotePayload remotePayload)
        {
            WixBundlePayloadTuple tuple = null;

            if (!this.Core.EncounteredError)
            {
                tuple = this.Core.AddTuple(new WixBundlePayloadTuple(sourceLineNumbers, id)
                {
                    Name = String.IsNullOrEmpty(name) ? Path.GetFileName(sourceFile) : name,
                    SourceFile = new IntermediateFieldPathValue { Path = sourceFile },
                    DownloadUrl = downloadUrl,
                    Compressed = (compressed == YesNoDefaultType.Yes) ? true : (compressed == YesNoDefaultType.No) ? (bool?)false : null,
                    UnresolvedSourceFile = sourceFile, // duplicate of sourceFile but in a string column so it won't get resolved to a full path during binding.
                    DisplayName = displayName,
                    Description = description,
                    EnableSignatureValidation = (YesNoType.Yes == enableSignatureVerification)
                });

                if (null != remotePayload)
                {
                    tuple.Description = remotePayload.Description;
                    tuple.DisplayName = remotePayload.ProductName;
                    tuple.Hash = remotePayload.Hash;
                    tuple.PublicKey = remotePayload.CertificatePublicKey;
                    tuple.Thumbprint = remotePayload.CertificateThumbprint;
                    tuple.FileSize = remotePayload.Size;
                    tuple.Version = remotePayload.Version;
                }

                this.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId.Id, ComplexReferenceChildType.Payload, id.Id, previousType, previousId?.Id);
            }

            return tuple;
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

            var previousType = ComplexReferenceChildType.Unknown;
            Identifier previousId = null;
            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Payload":
                        previousId = this.ParsePayloadElement(child, ComplexReferenceParentType.PayloadGroup, id, previousType, previousId);
                        previousType = ComplexReferenceChildType.Payload;
                        break;
                    case "PayloadGroupRef":
                        previousId = this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.PayloadGroup, id, previousType, previousId);
                        previousType = ComplexReferenceChildType.PayloadGroup;
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
                this.Core.AddTuple(new WixBundlePayloadGroupTuple(sourceLineNumbers, id));

                this.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId?.Id, ComplexReferenceChildType.PayloadGroup, id.Id, ComplexReferenceChildType.Unknown, null);
            }
        }

        /// <summary>
        /// Parses a payload group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element (BA or PayloadGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private Identifier ParsePayloadGroupRefElement(XElement node, ComplexReferenceParentType parentType, Identifier parentId, ComplexReferenceChildType previousType, Identifier previousId)
        {
            Debug.Assert(ComplexReferenceParentType.Layout == parentType || ComplexReferenceParentType.PayloadGroup == parentType || ComplexReferenceParentType.Package == parentType || ComplexReferenceParentType.Container == parentType);
            Debug.Assert(ComplexReferenceChildType.Unknown == previousType || ComplexReferenceChildType.PayloadGroup == previousType || ComplexReferenceChildType.Payload == previousType);

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
                        this.Core.CreateSimpleReference(sourceLineNumbers, TupleDefinitions.WixBundlePayloadGroup, id.Id);
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

            this.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId?.Id, ComplexReferenceChildType.PayloadGroup, id?.Id, previousType, previousId?.Id);

            return id;
        }

        /// <summary>
        /// Creates group and ordering information.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="parentType">Type of parent group, if known.</param>
        /// <param name="parentId">Identifier of parent group, if known.</param>
        /// <param name="type">Type of this item.</param>
        /// <param name="id">Identifier for this item.</param>
        /// <param name="previousType">Type of previous item, if known.</param>
        /// <param name="previousId">Identifier of previous item, if known</param>
        private void CreateGroupAndOrderingRows(SourceLineNumber sourceLineNumbers,
            ComplexReferenceParentType parentType, string parentId,
            ComplexReferenceChildType type, string id,
            ComplexReferenceChildType previousType, string previousId)
        {
            if (this.Core.EncounteredError)
            {
                return;
            }

            if (ComplexReferenceParentType.Unknown != parentType && null != parentId)
            {
                this.Core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, type, id);
            }

            if (ComplexReferenceChildType.Unknown != previousType && null != previousId)
            {
                // TODO: Should we define our own enum for this, just to ensure there's no "cross-contamination"?
                // TODO: Also, we could potentially include an 'Attributes' field to track things like
                // 'before' vs. 'after', and explicit vs. inferred dependencies.
                this.Core.AddTuple(new WixOrderingTuple(sourceLineNumbers)
                {
                    ItemType = type,
                    ItemIdRef = id,
                    DependsOnType = previousType,
                    DependsOnIdRef = previousId
                });
            }
        }

        /// <summary>
        /// Parse ExitCode element.
        /// </summary>
        /// <param name="node">Element to parse</param>
        /// <param name="packageId">Id of parent element</param>
        private void ParseExitCodeElement(XElement node, string packageId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var value = CompilerConstants.IntegerNotSet;
            var behavior = ExitCodeBehaviorType.NotSet;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Value":
                        value = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, Int32.MinValue + 2, Int32.MaxValue);
                        break;
                    case "Behavior":
                        var behaviorString = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        if (!Enum.TryParse(behaviorString, true, out behavior))
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Behavior", behaviorString, "success, error, scheduleReboot, forceReboot"));
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
                this.Core.AddTuple(new WixBundlePackageExitCodeTuple(sourceLineNumbers)
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

            // Ensure there is always a rollback boundary at the beginning of the chain.
            this.CreateRollbackBoundary(sourceLineNumbers, new Identifier(AccessModifier.Public, "WixDefaultBoundary"), YesNoType.Yes, YesNoType.No, ComplexReferenceParentType.PackageGroup, "WixChain", ComplexReferenceChildType.Unknown, null);

            var previousId = "WixDefaultBoundary";
            var previousType = ComplexReferenceChildType.Package;

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "MsiPackage":
                        previousId = this.ParseMsiPackageElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                        previousType = ComplexReferenceChildType.Package;
                        break;
                    case "MspPackage":
                        previousId = this.ParseMspPackageElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                        previousType = ComplexReferenceChildType.Package;
                        break;
                    case "MsuPackage":
                        previousId = this.ParseMsuPackageElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                        previousType = ComplexReferenceChildType.Package;
                        break;
                    case "ExePackage":
                        previousId = this.ParseExePackageElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                        previousType = ComplexReferenceChildType.Package;
                        break;
                    case "RollbackBoundary":
                        previousId = this.ParseRollbackBoundaryElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
                        previousType = ComplexReferenceChildType.Package;
                        break;
                    case "PackageGroupRef":
                        previousId = this.ParsePackageGroupRefElement(child, ComplexReferenceParentType.PackageGroup, "WixChain", previousType, previousId);
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
                this.Core.AddTuple(new WixChainTuple(sourceLineNumbers)
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
                        break;
                    case "Vital":
                        vital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Transaction":
                        transaction = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (!this.Core.EncounteredError)
            {
                this.CreateRollbackBoundary(sourceLineNumbers, id, vital, transaction, parentType, parentId, previousType, previousId);
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
            Identifier id = null;
            string name = null;
            string sourceFile = null;
            string downloadUrl = null;
            string after = null;
            string installCondition = null;
            var cache = YesNoAlwaysType.Yes; // the default is to cache everything in tradeoff for stability over disk space.
            string cacheId = null;
            string description = null;
            string displayName = null;
            var logPathVariable = (packageType == WixBundlePackageType.Msu) ? String.Empty : null;
            var rollbackPathVariable = (packageType == WixBundlePackageType.Msu) ? String.Empty : null;
            var permanent = YesNoType.NotSet;
            var visible = YesNoType.NotSet;
            var vital = YesNoType.Yes;
            string installCommand = null;
            string repairCommand = null;
            string uninstallCommand = null;
            var perMachine = YesNoDefaultType.NotSet;
            string detectCondition = null;
            string protocol = null;
            var installSize = CompilerConstants.IntegerNotSet;
            string msuKB = null;
            var enableSignatureVerification = YesNoType.No;
            var compressed = YesNoDefaultType.Default;
            var enableFeatureSelection = YesNoType.NotSet;
            var forcePerMachine = YesNoType.NotSet;
            RemotePayload remotePayload = null;
            var slipstream = YesNoType.NotSet;

            var expectedNetFx4Args = new string[] { "/q", "/norestart", "/chainingpackage" };

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
                        break;
                    case "Name":
                        name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                        if (!this.Core.IsValidLongFilename(name, false, true))
                        {
                            this.Core.Write(ErrorMessages.IllegalLongFilename(sourceLineNumbers, node.Name.LocalName, "Name", name));
                        }
                        break;
                    case "SourceFile":
                        sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DownloadUrl":
                        downloadUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "After":
                        after = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "InstallCondition":
                        installCondition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Cache":
                        var value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (value)
                        {
                        case "always":
                            cache = YesNoAlwaysType.Always;
                            break;
                        case "yes":
                            cache = YesNoAlwaysType.Yes;
                            break;
                        case "no":
                            cache = YesNoAlwaysType.No;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, value, "button", "yes", "no"));
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
                        break;
                    case "Visible":
                        visible = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        allowed = (packageType == WixBundlePackageType.Msi);
                        break;
                    case "Vital":
                        vital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "InstallCommand":
                        installCommand = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        allowed = (packageType == WixBundlePackageType.Exe);
                        break;
                    case "RepairCommand":
                        repairCommand = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                        allowed = (packageType == WixBundlePackageType.Exe);
                        break;
                    case "UninstallCommand":
                        uninstallCommand = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        allowed = (packageType == WixBundlePackageType.Exe);
                        break;
                    case "PerMachine":
                        perMachine = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                        allowed = (packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msp);
                        break;
                    case "DetectCondition":
                        detectCondition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        allowed = (packageType == WixBundlePackageType.Exe || packageType == WixBundlePackageType.Msu);
                        break;
                    case "Protocol":
                        protocol = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        allowed = (packageType == WixBundlePackageType.Exe);
                        break;
                    case "InstallSize":
                        installSize = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                        break;
                    case "KB":
                        msuKB = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        allowed = (packageType == WixBundlePackageType.Msu);
                        break;
                    case "Compressed":
                        compressed = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                        break;
                    case "EnableSignatureVerification":
                        enableSignatureVerification = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            // We need to handle RemotePayload up front because it effects value of sourceFile which is used in Id generation.  Id is needed by other child elements.
            foreach (var child in node.Elements(CompilerCore.WixNamespace + "RemotePayload"))
            {
                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                if (CompilerCore.WixNamespace == node.Name.Namespace && node.Name.LocalName != "ExePackage" && node.Name.LocalName != "MsuPackage")
                {
                    this.Core.Write(ErrorMessages.RemotePayloadUnsupported(childSourceLineNumbers));
                    continue;
                }

                if (null != remotePayload)
                {
                    this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                }

                remotePayload = this.ParseRemotePayloadElement(child);
            }

            if (String.IsNullOrEmpty(sourceFile))
            {
                if (String.IsNullOrEmpty(name))
                {
                    this.Core.Write(ErrorMessages.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Name", "SourceFile"));
                }
                else if (null == remotePayload)
                {
                    sourceFile = Path.Combine("SourceDir", name);
                }
            }
            else if (null != remotePayload)
            {
                this.Core.Write(ErrorMessages.UnexpectedElementWithAttribute(sourceLineNumbers, node.Name.LocalName, "RemotePayload", "SourceFile"));
            }
            else if (sourceFile.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                if (String.IsNullOrEmpty(name))
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name", "SourceFile", sourceFile));
                }
                else
                {
                    sourceFile = Path.Combine(sourceFile, Path.GetFileName(name));
                }
            }

            if (null == downloadUrl && null != remotePayload)
            {
                this.Core.Write(ErrorMessages.ExpectedAttributeWithElement(sourceLineNumbers, node.Name.LocalName, "DownloadUrl", "RemotePayload"));
            }

            if (YesNoDefaultType.No != compressed && null != remotePayload)
            {
                compressed = YesNoDefaultType.No;
                this.Core.Write(WarningMessages.RemotePayloadsMustNotAlsoBeCompressed(sourceLineNumbers, node.Name.LocalName));
            }

            if (null == id)
            {
                if (!String.IsNullOrEmpty(name))
                {
                    id = this.Core.CreateIdentifierFromFilename(Path.GetFileName(name));
                }
                else if (!String.IsNullOrEmpty(sourceFile))
                {
                    id = this.Core.CreateIdentifierFromFilename(Path.GetFileName(sourceFile));
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

            if (null == logPathVariable)
            {
                logPathVariable = String.Concat("WixBundleLog_", id.Id);
            }

            if (null == rollbackPathVariable)
            {
                rollbackPathVariable = String.Concat("WixBundleRollbackLog_", id.Id);
            }

            if (!String.IsNullOrEmpty(protocol) && !protocol.Equals("burn", StringComparison.Ordinal) && !protocol.Equals("netfx4", StringComparison.Ordinal) && !protocol.Equals("none", StringComparison.Ordinal))
            {
                this.Core.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, node.Name.LocalName, "Protocol", protocol, "none, burn, netfx4"));
            }

            if (!String.IsNullOrEmpty(protocol) && protocol.Equals("netfx4", StringComparison.Ordinal))
            {
                foreach (var expectedArgument in expectedNetFx4Args)
                {
                    if (null == installCommand || -1 == installCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "InstallCommand", installCommand, expectedArgument, "Protocol", "netfx4"));
                    }

                    if (!String.IsNullOrEmpty(repairCommand) && -1 == repairCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "RepairCommand", repairCommand, expectedArgument, "Protocol", "netfx4"));
                    }

                    if (!String.IsNullOrEmpty(uninstallCommand) && -1 == uninstallCommand.IndexOf(expectedArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Core.Write(WarningMessages.AttributeShouldContain(sourceLineNumbers, node.Name.LocalName, "UninstallCommand", uninstallCommand, expectedArgument, "Protocol", "netfx4"));
                    }
                }
            }

            // Only set default scope for EXEs and MSPs if not already set.
            if ((WixBundlePackageType.Exe == packageType || WixBundlePackageType.Msp == packageType) && YesNoDefaultType.NotSet == perMachine)
            {
                perMachine = YesNoDefaultType.Default;
            }

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
                        this.ParsePayloadElement(child, ComplexReferenceParentType.Package, id, ComplexReferenceChildType.Unknown, null);
                        break;
                    case "PayloadGroupRef":
                        this.ParsePayloadGroupRefElement(child, ComplexReferenceParentType.Package, id, ComplexReferenceChildType.Unknown, null);
                        break;
                    case "ExitCode":
                        allowed = (packageType == WixBundlePackageType.Exe);
                        if (allowed)
                        {
                            this.ParseExitCodeElement(child, id.Id);
                        }
                        break;
                    case "CommandLine":
                        allowed = (packageType == WixBundlePackageType.Exe);
                        if (allowed)
                        {
                            this.ParseCommandLineElement(child, id.Id);
                        }
                        break;
                    case "RemotePayload":
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
                    var context = new Dictionary<string, string>() { { "Id", id?.Id } };
                    this.Core.ParseExtensionElement(node, child, context);
                }
            }

            if (!this.Core.EncounteredError)
            {
                // We create the package contents as a payload with this package as the parent
                this.CreatePayloadRow(sourceLineNumbers, id, name, sourceFile, downloadUrl, ComplexReferenceParentType.Package, id,
                    ComplexReferenceChildType.Unknown, null, compressed, enableSignatureVerification, displayName, description, remotePayload);

                this.Core.AddTuple(new WixChainItemTuple(sourceLineNumbers, id));

                WixBundlePackageAttributes attributes = 0;
                attributes |= (YesNoType.Yes == permanent) ? WixBundlePackageAttributes.Permanent : 0;
                attributes |= (YesNoType.Yes == visible) ? WixBundlePackageAttributes.Visible : 0;

                var chainPackageTuple = this.Core.AddTuple(new WixBundlePackageTuple(sourceLineNumbers, id)
                {
                    Type = packageType,
                    PayloadRef = id.Id,
                    Attributes = attributes,
                    InstallCondition = installCondition,
                    CacheId = cacheId,
                    LogPathVariable = logPathVariable,
                    RollbackLogPathVariable = rollbackPathVariable,
                });

                if (YesNoAlwaysType.NotSet != cache)
                {
                    chainPackageTuple.Cache = cache;
                }

                if (YesNoType.NotSet != vital)
                {
                    chainPackageTuple.Vital = (vital == YesNoType.Yes);
                }

                if (YesNoDefaultType.NotSet != perMachine)
                {
                    chainPackageTuple.PerMachine = perMachine;
                }

                if (CompilerConstants.IntegerNotSet != installSize)
                {
                    chainPackageTuple.InstallSize = installSize;
                }

                switch (packageType)
                {
                case WixBundlePackageType.Exe:
                    this.Core.AddTuple(new WixBundleExePackageTuple(sourceLineNumbers, id)
                    {
                        Attributes = WixBundleExePackageAttributes.None,
                        DetectCondition = detectCondition,
                        InstallCommand = installCommand,
                        RepairCommand = repairCommand,
                        UninstallCommand = uninstallCommand,
                        ExeProtocol = protocol
                    });
                    break;

                case WixBundlePackageType.Msi:
                    WixBundleMsiPackageAttributes msiAttributes = 0;
                    msiAttributes |= (YesNoType.Yes == enableFeatureSelection) ? WixBundleMsiPackageAttributes.EnableFeatureSelection : 0;
                    msiAttributes |= (YesNoType.Yes == forcePerMachine) ? WixBundleMsiPackageAttributes.ForcePerMachine : 0;

                    this.Core.AddTuple(new WixBundleMsiPackageTuple(sourceLineNumbers, id)
                    {
                        Attributes = msiAttributes
                    });
                    break;

                case WixBundlePackageType.Msp:
                    WixBundleMspPackageAttributes mspAttributes = 0;
                    mspAttributes |= (YesNoType.Yes == slipstream) ? WixBundleMspPackageAttributes.Slipstream : 0;

                    this.Core.AddTuple(new WixBundleMspPackageTuple(sourceLineNumbers, id)
                    {
                        Attributes = mspAttributes
                    });
                    break;

                case WixBundlePackageType.Msu:
                    this.Core.AddTuple(new WixBundleMsuPackageTuple(sourceLineNumbers, id)
                    {
                        DetectCondition = detectCondition,
                        MsuKB = msuKB
                    });
                    break;
                }

                this.CreateChainPackageMetaRows(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.Package, id.Id, previousType, previousId, after);
            }

            return id.Id;
        }

        /// <summary>
        /// Parse CommandLine element.
        /// </summary>
        /// <param name="node">Element to parse</param>
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
                this.Core.AddTuple(new WixBundlePackageCommandLineTuple(sourceLineNumbers)
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
                this.Core.AddTuple(new WixBundlePackageGroupTuple(sourceLineNumbers, id));
            }
        }

        /// <summary>
        /// Parses a package group reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">ComplexReferenceParentType of parent element (Unknown or PackageGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        /// <returns>Identifier for package group element.</rereturns>
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
        /// <param name="parentType">ComplexReferenceParentType of previous element (Unknown, Package, or PackageGroup).</param>
        /// <param name="parentId">Identifier of parent element.</param>
        /// <returns>Identifier for package group element.</rereturns>
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, TupleDefinitions.WixBundlePackageGroup, id);
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
        /// <param name="parentType">Type of parent group.</param>
        /// <param name="parentId">Identifier of parent group.</param>
        /// <param name="previousType">Type of previous item, if any.</param>
        /// <param name="previousId">Identifier of previous item, if any.</param>
        private void CreateRollbackBoundary(SourceLineNumber sourceLineNumbers, Identifier id, YesNoType vital, YesNoType transaction, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType previousType, string previousId)
        {
            this.Core.AddTuple(new WixChainItemTuple(sourceLineNumbers, id));

            var rollbackBoundary = this.Core.AddTuple(new WixBundleRollbackBoundaryTuple(sourceLineNumbers, id));

            if (YesNoType.NotSet != vital)
            {
                rollbackBoundary.Vital = (vital == YesNoType.Yes);
            }

            if (YesNoType.NotSet != transaction)
            {
                rollbackBoundary.Transaction = (transaction == YesNoType.Yes);
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

            this.CreateGroupAndOrderingRows(sourceLineNumbers, parentType, parentId, type, id, previousType, previousId);
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
                        value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                var tuple = this.Core.AddTuple(new WixBundleMsiPropertyTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, packageId, name))
                {
                    PackageRef = packageId,
                    Name = name,
                    Value = value
                });

                if (!String.IsNullOrEmpty(condition))
                {
                    tuple.Condition = condition;
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
                        this.Core.CreateSimpleReference(sourceLineNumbers, TupleDefinitions.WixBundlePackage, id);
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
                this.Core.AddTuple(new WixBundleSlipstreamMspTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, packageId, id))
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
            string id = null;
            var actionType = RelatedBundleActionType.Detect;

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

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddTuple(new WixRelatedBundleTuple(sourceLineNumbers)
                {
                    BundleId = id,
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
                this.Core.AddTuple(new WixBundleUpdateTuple(sourceLineNumbers)
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
            string type = null;

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
                            variable = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                            type = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            type = this.ValidateVariableTypeWithValue(sourceLineNumbers, type, value);

            this.Core.ParseForExtensionElements(node);

            if (id == null)
            {
                id = this.Core.CreateIdentifier("sbv", variable, condition, after, value, type);
            }

            this.Core.CreateWixSearchTuple(sourceLineNumbers, node.Name.LocalName, id, variable, condition, after);

            if (!this.Messaging.EncounteredError)
            {
                this.Core.AddTuple(new WixSetVariableTuple(sourceLineNumbers, id)
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
            string name = null;
            var persisted = false;
            string value = null;
            string type = null;

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
                        name = this.Core.GetAttributeBundleVariableValue(sourceLineNumbers, attrib);
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
                        type = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
            else if (name.StartsWith("Wix", StringComparison.OrdinalIgnoreCase))
            {
                this.Core.Write(ErrorMessages.ReservedNamespaceViolation(sourceLineNumbers, node.Name.LocalName, "Name", "Wix"));
            }

            type = this.ValidateVariableTypeWithValue(sourceLineNumbers, type, value);

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddTuple(new WixBundleVariableTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, name))
                {
                    Value = value,
                    Type = type,
                    Hidden = hidden,
                    Persisted = persisted
                });
            }
        }

        private string ValidateVariableTypeWithValue(SourceLineNumber sourceLineNumbers, string type, string value)
        {
            var newType = type;
            if (newType == null && value != null)
            {
                // Infer the type from the current value... 
                if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    // Version constructor does not support simple "v#" syntax so check to see if the value is
                    // non-negative real quick.
                    if (Int32.TryParse(value.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out var _))
                    {
                        newType = "version";
                    }
                    else if (Version.TryParse(value.Substring(1), out var _))
                    {
                        newType = "version";
                    }
                }

                // Not a version, check for numeric.
                if (newType == null)
                {
                    if (Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var _))
                    {
                        newType = "numeric";
                    }
                    else
                    {
                        newType = "string";
                    }
                }
            }

            if (value == null && newType != null)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, "Variable", "Value", "Type"));
            }

            return newType;
        }

        private class RemotePayload
        {
            public string CertificatePublicKey { get; set; }

            public string CertificateThumbprint { get; set; }

            public string Description { get; set; }

            public string Hash { get; set; }

            public string ProductName { get; set; }

            public int Size { get; set; }

            public string Version { get; set; }
        }
    }
}
