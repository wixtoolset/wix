// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Bal.Symbols;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// The compiler for the WiX Toolset Bal Extension.
    /// </summary>
    public sealed class BalCompiler : BaseCompilerExtension
    {
        private readonly Dictionary<string, WixMbaPrereqInformationSymbol> prereqInfoSymbolsByPackageId;

        private enum WixDotNetCoreBootstrapperApplicationHostTheme
        {
            Unknown,
            None,
            Standard,
        }

        private enum WixManagedBootstrapperApplicationHostTheme
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

        /// <summary>
        /// Instantiate a new BalCompiler.
        /// </summary>
        public BalCompiler()
        {
            this.prereqInfoSymbolsByPackageId = new Dictionary<string, WixMbaPrereqInformationSymbol>();
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
            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                case "Fragment":
                    switch (element.Name.LocalName)
                    {
                        case "Condition":
                            this.ParseConditionElement(intermediate, section, element);
                            break;
                        case "ManagedBootstrapperApplicationPrereqInformation":
                            this.ParseMbaPrereqInfoElement(intermediate, section, element);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "BootstrapperApplication":
                    switch (element.Name.LocalName)
                    {
                        case "WixStandardBootstrapperApplication":
                            this.ParseWixStandardBootstrapperApplicationElement(intermediate, section, element);
                            break;
                        case "WixManagedBootstrapperApplicationHost":
                            this.ParseWixManagedBootstrapperApplicationHostElement(intermediate, section, element);
                            break;
                        case "WixDotNetCoreBootstrapperApplicationHost":
                            this.ParseWixDotNetCoreBootstrapperApplicationHostElement(intermediate, section, element);
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
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="attribute">Attribute to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public override void ParseAttribute(Intermediate intermediate, IntermediateSection section, XElement parentElement, XAttribute attribute, IDictionary<string, string> context)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(parentElement);
            WixMbaPrereqInformationSymbol prereqInfo;

            switch (parentElement.Name.LocalName)
            {
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
                                        section.AddSymbol(new WixBalPackageInfoSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, packageId))
                                        {
                                            PackageId = packageId,
                                            DisplayInternalUICondition = displayInternalUICondition,
                                        });
                                        break;
                                    default:
                                        this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                                        break;
                                }
                                break;
                            case "PrereqLicenseFile":

                                if (!this.prereqInfoSymbolsByPackageId.TryGetValue(packageId, out prereqInfo))
                                {
                                    // at the time the extension attribute is parsed, the compiler might not yet have
                                    // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                    var prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");

                                    if (null != prereqPackage && YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, prereqPackage))
                                    {
                                        prereqInfo = section.AddSymbol(new WixMbaPrereqInformationSymbol(sourceLineNumbers)
                                        {
                                            PackageId = packageId,
                                        });

                                        this.prereqInfoSymbolsByPackageId.Add(packageId, prereqInfo);
                                    }
                                    else
                                    {
                                        this.Messaging.Write(BalErrors.AttributeRequiresPrereqPackage(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseFile"));
                                        break;
                                    }
                                }

                                if (null != prereqInfo.LicenseUrl)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseFile", "PrereqLicenseUrl"));
                                }
                                else
                                {
                                    prereqInfo.LicenseFile = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                                }
                                break;
                            case "PrereqLicenseUrl":

                                if (!this.prereqInfoSymbolsByPackageId.TryGetValue(packageId, out prereqInfo))
                                {
                                    // at the time the extension attribute is parsed, the compiler might not yet have
                                    // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                    var prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");

                                    if (null != prereqPackage && YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, prereqPackage))
                                    {
                                        prereqInfo = section.AddSymbol(new WixMbaPrereqInformationSymbol(sourceLineNumbers)
                                        {
                                            PackageId = packageId,
                                        });

                                        this.prereqInfoSymbolsByPackageId.Add(packageId, prereqInfo);
                                    }
                                    else
                                    {
                                        this.Messaging.Write(BalErrors.AttributeRequiresPrereqPackage(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseUrl"));
                                        break;
                                    }
                                }

                                if (null != prereqInfo.LicenseFile)
                                {
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseUrl", "PrereqLicenseFile"));
                                }
                                else
                                {
                                    prereqInfo.LicenseUrl = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                                }
                                break;
                            case "PrereqPackage":
                                if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    if (!this.prereqInfoSymbolsByPackageId.TryGetValue(packageId, out prereqInfo))
                                    {
                                        prereqInfo = section.AddSymbol(new WixMbaPrereqInformationSymbol(sourceLineNumbers)
                                        {
                                            PackageId = packageId,
                                        });

                                        this.prereqInfoSymbolsByPackageId.Add(packageId, prereqInfo);
                                    }
                                }
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
                                if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    // There can only be one.
                                    var id = new Identifier(AccessModifier.Public, "TheBAFactoryAssembly");
                                    section.AddSymbol(new WixBalBAFactoryAssemblySymbol(sourceLineNumbers, id)
                                    {
                                        PayloadId = payloadId,
                                    });
                                }
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
        private void ParseMbaPrereqInfoElement(Intermediate intermediate, IntermediateSection section, XElement node)
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
                section.AddSymbol(new WixMbaPrereqInformationSymbol(sourceLineNumbers)
                {
                    PackageId = packageId,
                    LicenseFile = licenseFile,
                    LicenseUrl = licenseUrl,
                });
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBundlePackage, packageId);
            }
        }

        /// <summary>
        /// Parses a WixStandardBootstrapperApplication element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixStandardBootstrapperApplicationElement(Intermediate intermediate, IntermediateSection section, XElement node)
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
                this.CreateBARef(section, sourceLineNumbers, node, "WixStandardBootstrapperApplication");

                if (!String.IsNullOrEmpty(launchTarget))
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "LaunchTarget"))
                    {
                        Value = launchTarget,
                        Type = WixBundleVariableType.Formatted,
                    });
                }

                if (!String.IsNullOrEmpty(launchTargetElevatedId))
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "LaunchTargetElevatedId"))
                    {
                        Value = launchTargetElevatedId,
                        Type = WixBundleVariableType.Formatted,
                    });
                }

                if (!String.IsNullOrEmpty(launchArguments))
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "LaunchArguments"))
                    {
                        Value = launchArguments,
                        Type = WixBundleVariableType.Formatted,
                    });
                }

                if (YesNoType.Yes == launchHidden)
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "LaunchHidden"))
                    {
                        Value = "yes",
                        Type = WixBundleVariableType.Formatted,
                    });
                }


                if (!String.IsNullOrEmpty(launchWorkingDir))
                {
                    section.AddSymbol(new WixBundleVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "LaunchWorkingFolder"))
                    {
                        Value = launchWorkingDir,
                        Type = WixBundleVariableType.Formatted,
                    });
                }

                if (!String.IsNullOrEmpty(licenseFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "WixStdbaLicenseRtf"))
                    {
                        Value = licenseFile,
                    });
                }

                if (null != licenseUrl)
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "WixStdbaLicenseUrl"))
                    {
                        Value = licenseUrl,
                    });
                }

                if (!String.IsNullOrEmpty(logoFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "WixStdbaLogo"))
                    {
                        Value = logoFile,
                    });
                }

                if (!String.IsNullOrEmpty(logoSideFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "WixStdbaLogoSide"))
                    {
                        Value = logoSideFile,
                    });
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "WixStdbaThemeXml"))
                    {
                        Value = themeFile,
                    });
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "WixStdbaThemeWxl"))
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

                string themePayloadGroup = null;
                switch (theme)
                {
                    case WixStandardBootstrapperApplicationTheme.HyperlinkLargeLicense:
                        themePayloadGroup = "WixStdbaHyperlinkLargeLicensePayloads";
                        break;
                    case WixStandardBootstrapperApplicationTheme.HyperlinkLicense:
                        themePayloadGroup = "WixStdbaHyperlinkLicensePayloads";
                        break;
                    case WixStandardBootstrapperApplicationTheme.HyperlinkSidebarLicense:
                        themePayloadGroup = "WixStdbaHyperlinkSidebarLicensePayloads";
                        break;
                    case WixStandardBootstrapperApplicationTheme.RtfLargeLicense:
                        themePayloadGroup = "WixStdbaRtfLargeLicensePayloads";
                        break;
                    case WixStandardBootstrapperApplicationTheme.RtfLicense:
                        themePayloadGroup = "WixStdbaRtfLicensePayloads";
                        break;
                }

                if (themePayloadGroup != null)
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBundlePayloadGroup, themePayloadGroup);
                }
            }
        }

        /// <summary>
        /// Parses a WixManagedBootstrapperApplicationHost element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixManagedBootstrapperApplicationHostElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string logoFile = null;
            string themeFile = null;
            string localizationFile = null;
            WixManagedBootstrapperApplicationHostTheme? theme = null;

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

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            if (!this.Messaging.EncounteredError)
            {
                this.CreateBARef(section, sourceLineNumbers, node, "WixManagedBootstrapperApplicationHost");

                if (!String.IsNullOrEmpty(logoFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "PreqbaLogo"))
                    {
                        Value = logoFile,
                    });
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "PreqbaThemeXml"))
                    {
                        Value = themeFile,
                    });
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "PreqbaThemeWxl"))
                    {
                        Value = localizationFile,
                    });
                }

                string themePayloadGroup = null;
                switch (theme)
                {
                    case WixManagedBootstrapperApplicationHostTheme.Standard:
                        themePayloadGroup = "MbaPreqStandardPayloads";
                        break;
                }

                if (themePayloadGroup != null)
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBundlePayloadGroup, themePayloadGroup);
                }
            }
        }

        /// <summary>
        /// Parses a WixDotNetCoreBootstrapperApplication element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixDotNetCoreBootstrapperApplicationHostElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string logoFile = null;
            string themeFile = null;
            string localizationFile = null;
            var selfContainedDeployment = YesNoType.NotSet;
            WixDotNetCoreBootstrapperApplicationHostTheme? theme = null;

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
                this.CreateBARef(section, sourceLineNumbers, node, "WixDotNetCoreBootstrapperApplicationHost");

                if (!String.IsNullOrEmpty(logoFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "DncPreqbaLogo"))
                    {
                        Value = logoFile,
                    });
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "DncPreqbaThemeXml"))
                    {
                        Value = themeFile,
                    });
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    section.AddSymbol(new WixVariableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Public, "DncPreqbaThemeWxl"))
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

                string themePayloadGroup = null;
                switch (theme)
                {
                    case WixDotNetCoreBootstrapperApplicationHostTheme.Standard:
                        themePayloadGroup = "DncPreqStandardPayloads";
                        break;
                }

                if (themePayloadGroup != null)
                {
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBundlePayloadGroup, themePayloadGroup);
                }
            }
        }

        private void CreateBARef(IntermediateSection section, SourceLineNumber sourceLineNumbers, XElement node, string name)
        {
            var id = this.ParseHelper.CreateIdentifierValueFromPlatform(name, this.Context.Platform, BurnPlatforms.X86 | BurnPlatforms.X64 | BurnPlatforms.ARM64);
            if (id == null)
            {
                this.Messaging.Write(ErrorMessages.UnsupportedPlatformForElement(sourceLineNumbers, this.Context.Platform.ToString(), node.Name.LocalName));
            }

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBootstrapperApplication, id);
            }
        }
    }
}
