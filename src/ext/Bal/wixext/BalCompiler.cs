// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplications
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.BootstrapperApplications.Symbols;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// The compiler for the WiX Toolset Bal Extension.
    /// </summary>
    public sealed class BalCompiler : BaseCompilerExtension
    {
        private readonly Dictionary<string, WixBalPackageInfoSymbol> packageInfoSymbolsByPackageId = new Dictionary<string, WixBalPackageInfoSymbol>();
        private readonly Dictionary<string, WixPrereqInformationSymbol> prereqInfoSymbolsByPackageId = new Dictionary<string, WixPrereqInformationSymbol>();

        private enum WixPrerequisiteBootstrapperApplicationTheme
        {
            Unknown,
            None,
            Standard,
        }

        private enum WixInternalUIBootstrapperApplicationTheme
        {
            Unknown,
            None,
            Standard,
        }

        private enum WixStandardBootstrapperApplicationTheme
        {
            Unknown,
            HyperlinkLargeLicense,
            HyperlinkLicense,
            HyperlinkSidebarLicense,
            None,
            RtfLargeLicense,
            RtfLicense,
        }

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/bal";

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="section"></param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            this.ParsePossibleKeyPathElement(intermediate, section, parentElement, element, context);
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override IComponentKeyPath ParsePossibleKeyPathElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            IComponentKeyPath exePayloadRef = null;

            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                case "Fragment":
                    switch (element.Name.LocalName)
                    {
                        case "BootstrapperApplicationPrerequisiteInformation":
                            this.ParseBootstrapperApplicationPrerequisiteInformationElement(intermediate, section, element);
                            break;
                        case "Condition":
                            this.ParseConditionElement(intermediate, section, element);
                            break;
                        case "ManagedBootstrapperApplicationPrereqInformation":
                            this.Messaging.Write(WarningMessages.DeprecatedElement(this.ParseHelper.GetSourceLineNumbers(element), element.Name.LocalName, "BootstrapperApplicationPrerequisiteInformation"));
                            this.ParseBootstrapperApplicationPrerequisiteInformationElement(intermediate, section, element);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "BootstrapperApplication":
                    switch (element.Name.LocalName)
                    {
                        case "WixInternalUIBootstrapperApplication":
                            exePayloadRef = this.ParseWixInternalUIBootstrapperApplicationElement(intermediate, section, element);
                            break;
                        case "WixPrerequisiteBootstrapperApplication":
                            this.ParseWixPrerequisiteBootstrapperApplicationElement(intermediate, section, element, context);
                            break;
                        case "WixStandardBootstrapperApplication":
                            exePayloadRef = this.ParseWixStandardBootstrapperApplicationElement(intermediate, section, element);
                            break;
                        case "WixManagedBootstrapperApplicationHost":
                            this.Messaging.Write(WarningMessages.DeprecatedElement(this.ParseHelper.GetSourceLineNumbers(element), element.Name.LocalName));
                            break;
                        case "WixDotNetCoreBootstrapperApplicationHost":
                            this.Messaging.Write(WarningMessages.DeprecatedElement(this.ParseHelper.GetSourceLineNumbers(element), element.Name.LocalName));
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

            return exePayloadRef;
        }

        /// <summary>
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="attribute">Attribute to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public override void ParseAttribute(Intermediate intermediate, IntermediateSection section, XElement parentElement, XAttribute attribute, IDictionary<string, string> context)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(parentElement);

            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                    switch (attribute.Name.LocalName)
                    {
                        case "CommandLineVariables":
                            WixStdbaCommandLineVariableType? variableType = null;

                            var commandLineVariablesValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                            switch (commandLineVariablesValue)
                            {
                                case "caseInsensitive":
                                    variableType = WixStdbaCommandLineVariableType.CaseInsensitive;
                                    break;
                                case "caseSensitive":
                                    variableType = WixStdbaCommandLineVariableType.CaseSensitive;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, parentElement.Name.LocalName, attribute.Name.LocalName, commandLineVariablesValue, "caseInsensitive", "caseSensitive"));
                                    break;
                            }

                            if (variableType.HasValue)
                            {
                                // There can only be one.
                                var id = new Identifier(AccessModifier.Global, "WixStdbaCommandLineVariableType");
                                section.AddSymbol(new WixStdbaCommandLineSymbol(sourceLineNumbers, id)
                                {
                                    VariableType = variableType.Value,
                                });
                            }

                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                            break;
                    }
                    break;
                case "BundlePackage":
                case "ExePackage":
                case "MsiPackage":
                case "MspPackage":
                case "MsuPackage":
                    string packageId;
                    if (!context.TryGetValue("PackageId", out packageId) || String.IsNullOrEmpty(packageId))
                    {
                        this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, parentElement.Name.LocalName, "Id", attribute.Name.LocalName));
                    }
                    else
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "DisplayInternalUICondition":
                                switch (parentElement.Name.LocalName)
                                {
                                    case "MsiPackage":
                                    case "MspPackage":
                                        var displayInternalUICondition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                                        var packageInfo = this.GetBalPackageInfoSymbol(section, sourceLineNumbers, packageId);
                                        packageInfo.DisplayInternalUICondition = displayInternalUICondition;
                                        break;
                                    default:
                                        this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                                        break;
                                }
                                break;
                            case "DisplayFilesInUseDialogCondition":
                                switch (parentElement.Name.LocalName)
                                {
                                    case "MsiPackage":
                                    case "MspPackage":
                                        var displayFilesInUseDialogCondition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                                        var packageInfo = this.GetBalPackageInfoSymbol(section, sourceLineNumbers, packageId);
                                        packageInfo.DisplayFilesInUseDialogCondition = displayFilesInUseDialogCondition;
                                        break;
                                    default:
                                        this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                                        break;
                                }
                                break;
                            case "PrimaryPackageType":
                            {
                                var primaryPackageType = BalPrimaryPackageType.None;
                                var primaryPackageTypeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                                switch (primaryPackageTypeValue)
                                {
                                    case "default":
                                        primaryPackageType = BalPrimaryPackageType.Default;
                                        break;
                                    case "x86":
                                        primaryPackageType = BalPrimaryPackageType.X86;
                                        break;
                                    case "x64":
                                        primaryPackageType = BalPrimaryPackageType.X64;
                                        break;
                                    case "arm64":
                                        primaryPackageType = BalPrimaryPackageType.ARM64;
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, parentElement.Name.LocalName, "PrimaryPackageType", primaryPackageTypeValue, "default", "x86", "x64", "arm64"));
                                        break;
                                }

                                // at the time the extension attribute is parsed, the compiler might not yet have
                                // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                var prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");
                                var prereqInfo = this.GetMbaPrereqInformationSymbol(section, sourceLineNumbers, prereqPackage, packageId);
                                if (prereqInfo != null)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, parentElement.Name.LocalName, "PrereqPackage", "yes", "PrimaryPackageType"));
                                }
                                else
                                {
                                    var packageInfo = this.GetBalPackageInfoSymbol(section, sourceLineNumbers, packageId);
                                    packageInfo.PrimaryPackageType = primaryPackageType;
                                }
                                break;
                            }
                            case "PrereqLicenseFile":
                            {
                                // at the time the extension attribute is parsed, the compiler might not yet have
                                // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                var prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");
                                var prereqInfo = this.GetMbaPrereqInformationSymbol(section, sourceLineNumbers, prereqPackage, packageId);
                                if (prereqInfo == null)
                                {
                                    this.Messaging.Write(BalErrors.AttributeRequiresPrereqPackage(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseFile"));
                                }
                                else if (null != prereqInfo.LicenseUrl)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseFile", "PrereqLicenseUrl"));
                                }
                                else
                                {
                                    prereqInfo.LicenseFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                                }
                                break;
                            }
                            case "PrereqLicenseUrl":
                            {
                                // at the time the extension attribute is parsed, the compiler might not yet have
                                // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                var prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");
                                var prereqInfo = this.GetMbaPrereqInformationSymbol(section, sourceLineNumbers, prereqPackage, packageId);

                                if (prereqInfo == null)
                                {
                                    this.Messaging.Write(BalErrors.AttributeRequiresPrereqPackage(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseUrl"));
                                }
                                else if (null != prereqInfo.LicenseFile)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseUrl", "PrereqLicenseFile"));
                                }
                                else
                                {
                                    prereqInfo.LicenseUrl = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                                }
                                break;
                            }
                            case "PrereqPackage":
                                this.GetMbaPrereqInformationSymbol(section, sourceLineNumbers, attribute, packageId);
                                break;
                            default:
                                this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                                break;
                        }
                    }
                    break;
                case "Payload":
                    string payloadId;
                    if (!context.TryGetValue("Id", out payloadId) || String.IsNullOrEmpty(payloadId))
                    {
                        this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, parentElement.Name.LocalName, "Id", attribute.Name.LocalName));
                    }
                    else
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "BAFactoryAssembly":
                                this.Messaging.Write(BalWarnings.DeprecatedBAFactoryAssemblyAttribute(this.ParseHelper.GetSourceLineNumbers(parentElement), parentElement.Name.LocalName, attribute.Name.LocalName));
                                break;
                            case "BAFunctions":
                                if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    section.AddSymbol(new WixBalBAFunctionsSymbol(sourceLineNumbers)
                                    {
                                        PayloadId = payloadId,
                                    });
                                }
                                break;
                            default:
                                this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                                break;
                        }
                    }
                    break;
                case "Variable":
                    // at the time the extension attribute is parsed, the compiler might not yet have
                    // parsed the Name attribute, so we need to get it directly from the parent element.
                    var variableName = parentElement.Attribute("Name");
                    if (null == variableName)
                    {
                        this.Messaging.Write(ErrorMessages.ExpectedParentWithAttribute(sourceLineNumbers, "Variable", "Overridable", "Name"));
                    }
                    else
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "Overridable":
                                if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    section.AddSymbol(new WixStdbaOverridableVariableSymbol(sourceLineNumbers)
                                    {
                                        Name = variableName.Value,
                                    });
                                }
                                break;
                            default:
                                this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                                break;
                        }
                    }
                    break;
            }
        }

        private WixBalPackageInfoSymbol GetBalPackageInfoSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, string packageId)
        {
            if (!this.packageInfoSymbolsByPackageId.TryGetValue(packageId, out var packageInfo))
            {
                packageInfo = section.AddSymbol(new WixBalPackageInfoSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, packageId))
                {
                    PackageId = packageId,
                });

                this.packageInfoSymbolsByPackageId.Add(packageId, packageInfo);
            }

            return packageInfo;
        }

        private WixPrereqInformationSymbol GetMbaPrereqInformationSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, XAttribute prereqAttribute, string packageId)
        {
            WixPrereqInformationSymbol prereqInfo = null;

            if (prereqAttribute != null && YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, prereqAttribute))
            {
                if (!this.prereqInfoSymbolsByPackageId.TryGetValue(packageId, out prereqInfo))
                {
                    prereqInfo = section.AddSymbol(new WixPrereqInformationSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, packageId))
                    {
                        PackageId = packageId,
                    });

                    this.prereqInfoSymbolsByPackageId.Add(packageId, prereqInfo);
                }
            }

            return prereqInfo;
        }

        /// <summary>
        /// Parses a Condition element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseConditionElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string condition = null;
            string message = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Message":
                            message = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            // Error check the values.
            if (String.IsNullOrEmpty(condition))
            {
                this.Messaging.Write(ErrorMessages.ConditionExpected(sourceLineNumbers, node.Name.LocalName));
            }

            if (null == message)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Message"));
            }

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixBalConditionSymbol(sourceLineNumbers)
                {
                    Condition = condition,
                    Message = message,
                });
            }
        }

        /// <summary>
        /// Parses a Condition element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseBootstrapperApplicationPrerequisiteInformationElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string packageId = null;
            string licenseFile = null;
            string licenseUrl = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "LicenseFile":
                            licenseFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LicenseUrl":
                            licenseUrl = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PackageId":
                            packageId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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

            if (null == packageId)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PackageId"));
            }

            if (null == licenseFile && null == licenseUrl ||
                null != licenseFile && null != licenseUrl)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "LicenseFile", "LicenseUrl", true));
            }

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixPrereqInformationSymbol(sourceLineNumbers)
                {
                    PackageId = packageId,
                    LicenseFile = licenseFile,
                    LicenseUrl = licenseUrl,
                });
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBundlePackage, packageId);
            }
        }

        private IComponentKeyPath ParseWixInternalUIBootstrapperApplicationElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            var theme = WixInternalUIBootstrapperApplicationTheme.Standard;
            string themeFile = null;
            string logoFile = null;
            string localizationFile = null;

            IComponentKeyPath exePayloadRef = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "LogoFile":
                            logoFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThemeFile":
                            themeFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Theme":
                            var themeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (themeValue)
                            {
                                case "none":
                                    theme = WixInternalUIBootstrapperApplicationTheme.None;
                                    break;
                                case "standard":
                                    theme = WixInternalUIBootstrapperApplicationTheme.Standard;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Theme", themeValue, "none", "standard"));
                                    theme = WixInternalUIBootstrapperApplicationTheme.Unknown;
                                    break;
                            }
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
                if (!String.IsNullOrEmpty(logoFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixIuibaLogo"))
                    {
                        Value = logoFile,
                    });
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixIuibaThemeXml"))
                    {
                        Value = themeFile,
                    });
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixIuibaThemeWxl"))
                    {
                        Value = localizationFile,
                    });
                }

                switch (theme)
                {
                    case WixInternalUIBootstrapperApplicationTheme.Standard:
                        this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixIuibaStandardPayloads", platformSpecific: false);
                        break;
                }

                section.AddSymbol(new WixBalBootstrapperApplicationSymbol(sourceLineNumbers)
                {
                    Type = WixBalBootstrapperApplicationType.InternalUi,
                });

                section.AddSymbol(new WixPrereqOptionsSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixPrereqOptions"))
                {
                    Primary = 1,
                    HandleHelp = 1,
                    HandleLayout = 1,
                });

                var exePayloadId = this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixInternalUIBootstrapperApplication", platformSpecific: true);

                exePayloadRef = this.CreateComponentKeyPath();
                exePayloadRef.Id = new Identifier(AccessModifier.Section, exePayloadId);
                exePayloadRef.Explicit = true; // Internal UI BA is always secondary because the PrereqBA is always primary to handle the help and layout options.
                exePayloadRef.Type = PossibleKeyPathType.File;
            }

            return exePayloadRef;
        }

        /// <summary>
        /// Parses a WixStandardBootstrapperApplication element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private IComponentKeyPath ParseWixStandardBootstrapperApplicationElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string launchTarget = null;
            string launchTargetElevatedId = null;
            string launchArguments = null;
            var launchHidden = YesNoType.NotSet;
            string launchWorkingDir = null;
            string licenseFile = null;
            string licenseUrl = null;
            string logoFile = null;
            string logoSideFile = null;
            WixStandardBootstrapperApplicationTheme? theme = null;
            string themeFile = null;
            string localizationFile = null;
            var suppressOptionsUI = YesNoType.NotSet;
            var suppressDowngradeFailure = YesNoType.NotSet;
            var suppressRepair = YesNoType.NotSet;
            var showVersion = YesNoType.NotSet;
            var supportCacheOnly = YesNoType.NotSet;

            IComponentKeyPath exePayloadRef = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "LaunchTarget":
                            launchTarget = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchTargetElevatedId":
                            launchTargetElevatedId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchArguments":
                            launchArguments = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchHidden":
                            launchHidden = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchWorkingFolder":
                            launchWorkingDir = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LicenseFile":
                            licenseFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LicenseUrl":
                            licenseUrl = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "LogoFile":
                            logoFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LogoSideFile":
                            logoSideFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThemeFile":
                            themeFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressOptionsUI":
                            suppressOptionsUI = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressDowngradeFailure":
                            suppressDowngradeFailure = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressRepair":
                            suppressRepair = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ShowVersion":
                            showVersion = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SupportCacheOnly":
                            supportCacheOnly = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Theme":
                            var themeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (themeValue)
                            {
                                case "hyperlinkLargeLicense":
                                    theme = WixStandardBootstrapperApplicationTheme.HyperlinkLargeLicense;
                                    break;
                                case "hyperlinkLicense":
                                    theme = WixStandardBootstrapperApplicationTheme.HyperlinkLicense;
                                    break;
                                case "hyperlinkSidebarLicense":
                                    theme = WixStandardBootstrapperApplicationTheme.HyperlinkSidebarLicense;
                                    break;
                                case "none":
                                    theme = WixStandardBootstrapperApplicationTheme.None;
                                    break;
                                case "rtfLargeLicense":
                                    theme = WixStandardBootstrapperApplicationTheme.RtfLargeLicense;
                                    break;
                                case "rtfLicense":
                                    theme = WixStandardBootstrapperApplicationTheme.RtfLicense;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Theme", themeValue, "hyperlinkLargeLicense", "hyperlinkLicense", "hyperlinkSidebarLicense", "none", "rtfLargeLicense", "rtfLicense"));
                                    theme = WixStandardBootstrapperApplicationTheme.Unknown; // set a value to prevent expected attribute error below.
                                    break;
                            }
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

            if (!theme.HasValue)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Theme"));
            }

            if (theme != WixStandardBootstrapperApplicationTheme.None && String.IsNullOrEmpty(licenseFile) && null == licenseUrl)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "LicenseFile", "LicenseUrl", true));
            }

            if (!this.Messaging.EncounteredError)
            {
                if (!String.IsNullOrEmpty(launchTarget))
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "LaunchTarget"))
                    {
                        Value = launchTarget,
                        Type = WixBundleVariableType.Formatted,
                    });
                }

                if (!String.IsNullOrEmpty(launchTargetElevatedId))
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "LaunchTargetElevatedId"))
                    {
                        Value = launchTargetElevatedId,
                        Type = WixBundleVariableType.Formatted,
                    });
                }

                if (!String.IsNullOrEmpty(launchArguments))
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "LaunchArguments"))
                    {
                        Value = launchArguments,
                        Type = WixBundleVariableType.Formatted,
                    });
                }

                if (YesNoType.Yes == launchHidden)
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "LaunchHidden"))
                    {
                        Value = "yes",
                        Type = WixBundleVariableType.Formatted,
                    });
                }


                if (!String.IsNullOrEmpty(launchWorkingDir))
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "LaunchWorkingFolder"))
                    {
                        Value = launchWorkingDir,
                        Type = WixBundleVariableType.Formatted,
                    });
                }

                if (!String.IsNullOrEmpty(licenseFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixStdbaLicenseRtf"))
                    {
                        Value = licenseFile,
                    });
                }

                if (null != licenseUrl)
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixStdbaLicenseUrl"))
                    {
                        Value = licenseUrl,
                    });
                }

                if (!String.IsNullOrEmpty(logoFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixStdbaLogo"))
                    {
                        Value = logoFile,
                    });
                }

                if (!String.IsNullOrEmpty(logoSideFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixStdbaLogoSide"))
                    {
                        Value = logoSideFile,
                    });
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixStdbaThemeXml"))
                    {
                        Value = themeFile,
                    });
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixStdbaThemeWxl"))
                    {
                        Value = localizationFile,
                    });
                }

                if (YesNoType.Yes == suppressOptionsUI || YesNoType.Yes == suppressDowngradeFailure || YesNoType.Yes == suppressRepair || YesNoType.Yes == showVersion || YesNoType.Yes == supportCacheOnly)
                {
                    var symbol = section.AddSymbol(new WixStdbaOptionsSymbol(sourceLineNumbers));
                    if (YesNoType.Yes == suppressOptionsUI)
                    {
                        symbol.SuppressOptionsUI = 1;
                    }

                    if (YesNoType.Yes == suppressDowngradeFailure)
                    {
                        symbol.SuppressDowngradeFailure = 1;
                    }

                    if (YesNoType.Yes == suppressRepair)
                    {
                        symbol.SuppressRepair = 1;
                    }

                    if (YesNoType.Yes == showVersion)
                    {
                        symbol.ShowVersion = 1;
                    }

                    if (YesNoType.Yes == supportCacheOnly)
                    {
                        symbol.SupportCacheOnly = 1;
                    }
                }

                switch (theme)
                {
                    case WixStandardBootstrapperApplicationTheme.HyperlinkLargeLicense:
                        this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixStdbaHyperlinkLargeLicensePayloads", platformSpecific: false);
                        break;
                    case WixStandardBootstrapperApplicationTheme.HyperlinkLicense:
                        this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixStdbaHyperlinkLicensePayloads", platformSpecific: false);
                        break;
                    case WixStandardBootstrapperApplicationTheme.HyperlinkSidebarLicense:
                        this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixStdbaHyperlinkSidebarLicensePayloads", platformSpecific: false);
                        break;
                    case WixStandardBootstrapperApplicationTheme.RtfLargeLicense:
                        this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixStdbaRtfLargeLicensePayloads", platformSpecific: false);
                        break;
                    case WixStandardBootstrapperApplicationTheme.RtfLicense:
                        this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixStdbaRtfLicensePayloads", platformSpecific: false);
                        break;
                }

                section.AddSymbol(new WixBalBootstrapperApplicationSymbol(sourceLineNumbers)
                {
                    Type = WixBalBootstrapperApplicationType.Standard,
                });

                var exePayloadId = this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixStandardBootstrapperApplication", platformSpecific: true);

                exePayloadRef = this.CreateComponentKeyPath();
                exePayloadRef.Id = new Identifier(AccessModifier.Section, exePayloadId);
                exePayloadRef.Type = PossibleKeyPathType.File;
            }

            return exePayloadRef;
        }

        /// <summary>
        /// Parses a WixManagedBootstrapperApplicationHost element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixPrerequisiteBootstrapperApplicationElement(Intermediate intermediate, IntermediateSection section, XElement node, IDictionary<string, string> context)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string logoFile = null;
            string themeFile = null;
            string localizationFile = null;
            var theme = WixPrerequisiteBootstrapperApplicationTheme.Standard;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "LogoFile":
                            logoFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThemeFile":
                            themeFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Theme":
                            var themeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (themeValue)
                            {
                                case "none":
                                    theme = WixPrerequisiteBootstrapperApplicationTheme.None;
                                    break;
                                case "standard":
                                    theme = WixPrerequisiteBootstrapperApplicationTheme.Standard;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Theme", themeValue, "none", "standard"));
                                    theme = WixPrerequisiteBootstrapperApplicationTheme.Unknown;
                                    break;
                            }
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
                if (!String.IsNullOrEmpty(logoFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixPreqbaLogo"))
                    {
                        Value = logoFile,
                    });
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixPreqbaThemeXml"))
                    {
                        Value = themeFile,
                    });
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixPreqbaThemeWxl"))
                    {
                        Value = localizationFile,
                    });
                }

                switch (theme)
                {
                    case WixPrerequisiteBootstrapperApplicationTheme.Standard:
                        this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixPreqbaStandardPayloads", platformSpecific: false);
                        break;
                }

                section.AddSymbol(new WixBalBootstrapperApplicationSymbol(sourceLineNumbers)
                {
                    Type = WixBalBootstrapperApplicationType.Prerequisite,
                });

                var primary = context.TryGetValue("Secondary", out var parentSecondaryValue) && "True".Equals(parentSecondaryValue, StringComparison.OrdinalIgnoreCase) ? true : false;

                var baId = this.CreateIdentifierFromPlatform(sourceLineNumbers, node, primary ? "WixPrereqBootstrapperApplication.Primary" : "WixPrereqBootstrapperApplication.Secondary");

                if (!String.IsNullOrEmpty(baId))
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBootstrapperApplication, baId);
                }

                if (primary)
                {
                    section.AddSymbol(new WixPrereqOptionsSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixPrereqOptions"))
                    {
                        Primary = 1
                    });
                }
            }
        }

#if DELETE
        /// <summary>
        /// Parses a WixManagedBootstrapperApplicationHost element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private IComponentKeyPath ParseWixManagedBootstrapperApplicationHostElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            bool alwaysInstallPrereqs = false;
            string logoFile = null;
            string themeFile = null;
            string localizationFile = null;
            WixManagedBootstrapperApplicationHostTheme? theme = null;

            IComponentKeyPath exePayloadRef = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "AlwaysInstallPrereqs":
                            alwaysInstallPrereqs = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.Yes;
                            break;
                        case "LogoFile":
                            logoFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThemeFile":
                            themeFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Theme":
                            var themeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (themeValue)
                            {
                                case "none":
                                    theme = WixManagedBootstrapperApplicationHostTheme.None;
                                    break;
                                case "standard":
                                    theme = WixManagedBootstrapperApplicationHostTheme.Standard;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Theme", themeValue, "none", "standard"));
                                    theme = WixManagedBootstrapperApplicationHostTheme.Unknown;
                                    break;
                            }
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

            if (!theme.HasValue)
            {
                theme = WixManagedBootstrapperApplicationHostTheme.Standard;
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            if (!this.Messaging.EncounteredError)
            {
                if (!String.IsNullOrEmpty(logoFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "PreqbaLogo"))
                    {
                        Value = logoFile,
                    });
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "PreqbaThemeXml"))
                    {
                        Value = themeFile,
                    });
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "PreqbaThemeWxl"))
                    {
                        Value = localizationFile,
                    });
                }

                var baId = "WixManagedBootstrapperApplicationHost";
                switch (theme)
                {
                    case WixManagedBootstrapperApplicationHostTheme.Standard:
                        baId = "WixManagedBootstrapperApplicationHost.Standard";
                        break;
                }

                exePayloadRef = this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixManagedBootstrapperApplicationHost", baId, WixBalBootstrapperApplicationType.ManagedHost);

                if (alwaysInstallPrereqs)
                {
                    section.AddSymbol(new WixPrereqOptionsSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixPrereqOptions"))
                    {
                        AlwaysInstallPrereqs = 1,
                    });
                }
            }

            return exePayloadRef;
        }

        /// <summary>
        /// Parses a WixDotNetCoreBootstrapperApplication element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private IComponentKeyPath ParseWixDotNetCoreBootstrapperApplicationHostElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            bool alwaysInstallPrereqs = false;
            string logoFile = null;
            string themeFile = null;
            string localizationFile = null;
            var selfContainedDeployment = YesNoType.NotSet;
            WixDotNetCoreBootstrapperApplicationHostTheme? theme = null;

            IComponentKeyPath exePayloadRef = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "AlwaysInstallPrereqs":
                            alwaysInstallPrereqs = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.Yes;
                            break;
                        case "LogoFile":
                            logoFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThemeFile":
                            themeFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SelfContainedDeployment":
                            selfContainedDeployment = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Theme":
                            var themeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (themeValue)
                            {
                                case "none":
                                    theme = WixDotNetCoreBootstrapperApplicationHostTheme.None;
                                    break;
                                case "standard":
                                    theme = WixDotNetCoreBootstrapperApplicationHostTheme.Standard;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Theme", themeValue, "none", "standard"));
                                    theme = WixDotNetCoreBootstrapperApplicationHostTheme.Unknown;
                                    break;
                            }
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

            if (!theme.HasValue)
            {
                theme = WixDotNetCoreBootstrapperApplicationHostTheme.Standard;
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            if (!this.Messaging.EncounteredError)
            {
                if (!String.IsNullOrEmpty(logoFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "DncPreqbaLogo"))
                    {
                        Value = logoFile,
                    });
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "DncPreqbaThemeXml"))
                    {
                        Value = themeFile,
                    });
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "DncPreqbaThemeWxl"))
                    {
                        Value = localizationFile,
                    });
                }

                if (YesNoType.Yes == selfContainedDeployment)
                {
                    section.AddSymbol(new WixDncOptionsSymbol(sourceLineNumbers)
                    {
                        SelfContainedDeployment = 1,
                    });
                }

                var baId = "WixDotNetCoreBootstrapperApplicationHost";
                switch (theme)
                {
                    case WixDotNetCoreBootstrapperApplicationHostTheme.Standard:
                        baId = "WixDotNetCoreBootstrapperApplicationHost.Standard";
                        break;
                }

                exePayloadRef = this.CreatePayloadGroupRef(section, sourceLineNumbers, node, "WixDotNetCoreBootstrapperApplicationHost", baId, WixBalBootstrapperApplicationType.DotNetCoreHost);

                if (alwaysInstallPrereqs)
                {
                    section.AddSymbol(new WixPrereqOptionsSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, "WixPrereqOptions"))
                    {
                        AlwaysInstallPrereqs = 1,
                    });
                }
            }

            return exePayloadRef;
        }
#endif

        private string CreatePayloadGroupRef(IntermediateSection section, SourceLineNumber sourceLineNumbers, XElement node, string basePayloadGroupId, bool platformSpecific)
        {
            var id = platformSpecific ? this.CreateIdentifierFromPlatform(sourceLineNumbers, node, basePayloadGroupId) : basePayloadGroupId;

            if (!String.IsNullOrEmpty(id))
            {
                this.ParseHelper.CreateWixGroupSymbol(section, sourceLineNumbers, ComplexReferenceParentType.Container, BurnConstants.BurnUXContainerName, ComplexReferenceChildType.PayloadGroup, id);

                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBundlePayloadGroup, id);
            }

            return id;
        }

        private string CreateIdentifierFromPlatform(SourceLineNumber sourceLineNumbers, XElement node, string name)
        {
            var id = this.ParseHelper.CreateIdentifierValueFromPlatform(name, this.Context.Platform, BurnPlatforms.X86 | BurnPlatforms.X64 | BurnPlatforms.ARM64);
            if (id == null)
            {
                this.Messaging.Write(ErrorMessages.UnsupportedPlatformForElement(sourceLineNumbers, this.Context.Platform.ToString(), node.Name.LocalName));
            }

            return id;
        }
    }
}
