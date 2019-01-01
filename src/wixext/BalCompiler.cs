// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Bal.Tuples;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Bal Extension.
    /// </summary>
    public sealed class BalCompiler : BaseCompilerExtension
    {
        private SourceLineNumber addedConditionLineNumber;
        private Dictionary<string, WixMbaPrereqInformationTuple> prereqInfoRows;

        /// <summary>
        /// Instantiate a new BalCompiler.
        /// </summary>
        public BalCompiler()
        {
            this.addedConditionLineNumber = null;
            this.prereqInfoRows = new Dictionary<string, WixMbaPrereqInformationTuple>();
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
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "BootstrapperApplicationRef":
                    switch (element.Name.LocalName)
                    {
                        case "WixStandardBootstrapperApplication":
                            this.ParseWixStandardBootstrapperApplicationElement(intermediate, section, element);
                            break;
                        case "WixManagedBootstrapperApplicationHost":
                            this.ParseWixManagedBootstrapperApplicationHostElement(intermediate, section, element);
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
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(parentElement);
            WixMbaPrereqInformationTuple prereqInfo;

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
                            case "PrereqLicenseFile":

                                if (!this.prereqInfoRows.TryGetValue(packageId, out prereqInfo))
                                {
                                    // at the time the extension attribute is parsed, the compiler might not yet have
                                    // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                    XAttribute prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");

                                    if (null != prereqPackage && YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, prereqPackage))
                                    {
                                        prereqInfo = (WixMbaPrereqInformationTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixMbaPrereqInformation");
                                        prereqInfo.PackageId = packageId;

                                        this.prereqInfoRows.Add(packageId, prereqInfo);
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

                                if (!this.prereqInfoRows.TryGetValue(packageId, out prereqInfo))
                                {
                                    // at the time the extension attribute is parsed, the compiler might not yet have
                                    // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                    XAttribute prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");

                                    if (null != prereqPackage && YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, prereqPackage))
                                    {
                                        prereqInfo = (WixMbaPrereqInformationTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixMbaPrereqInformation");
                                        prereqInfo.PackageId = packageId;

                                        this.prereqInfoRows.Add(packageId, prereqInfo);
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
                                    if (!this.prereqInfoRows.TryGetValue(packageId, out prereqInfo))
                                    {
                                        prereqInfo = (WixMbaPrereqInformationTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixMbaPrereqInformation");
                                        prereqInfo.PackageId = packageId;

                                        this.prereqInfoRows.Add(packageId, prereqInfo);
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
                            case "BAFunctions":
                                if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    var tuple = (WixBalBAFunctionsTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixBalBAFunctions");
                                    tuple.PayloadId = payloadId;
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
                    XAttribute variableName = parentElement.Attribute("Name");
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
                                    var tuple = (WixStdbaOverridableVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixStdbaOverridableVariable");
                                    tuple.Name = variableName.Value;
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
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string condition = this.ParseHelper.GetConditionInnerText(node); // condition is the inner text of the element.
            string message = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Message":
                            message = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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
                var tuple = (WixBalConditionTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixBalCondition");
                tuple.Condition = condition;
                tuple.Message = message;

                if (null == this.addedConditionLineNumber)
                {
                    this.addedConditionLineNumber = sourceLineNumbers;
                }
            }
        }

        /// <summary>
        /// Parses a WixStandardBootstrapperApplication element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixStandardBootstrapperApplicationElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string launchTarget = null;
            string launchTargetElevatedId = null;
            string launchArguments = null;
            YesNoType launchHidden = YesNoType.NotSet;
            string launchWorkingDir = null;
            string licenseFile = null;
            string licenseUrl = null;
            string logoFile = null;
            string logoSideFile = null;
            string themeFile = null;
            string localizationFile = null;
            YesNoType suppressOptionsUI = YesNoType.NotSet;
            YesNoType suppressDowngradeFailure = YesNoType.NotSet;
            YesNoType suppressRepair = YesNoType.NotSet;
            YesNoType showVersion = YesNoType.NotSet;
            YesNoType supportCacheOnly = YesNoType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
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

            if (String.IsNullOrEmpty(licenseFile) && null == licenseUrl)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "LicenseFile", "LicenseUrl", true));
            }

            if (!this.Messaging.EncounteredError)
            {
                if (!String.IsNullOrEmpty(launchTarget))
                {
                    var row = (WixBundleVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixBundleVariable");
                    row.Id = new Identifier("LaunchTarget", AccessModifier.Public);
                    row.Value = launchTarget;
                    row.Type = "string";
                }

                if (!String.IsNullOrEmpty(launchTargetElevatedId))
                {
                    var row = (WixBundleVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixBundleVariable");
                    row.Id = new Identifier("LaunchTargetElevatedId", AccessModifier.Public);
                    row.Value = launchTargetElevatedId;
                    row.Type = "string";
                }

                if (!String.IsNullOrEmpty(launchArguments))
                {
                    var row = (WixBundleVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixBundleVariable");
                    row.Id = new Identifier("LaunchArguments", AccessModifier.Public);
                    row.Value = launchArguments;
                    row.Type = "string";
                }

                if (YesNoType.Yes == launchHidden)
                {
                    var row = (WixBundleVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixBundleVariable");
                    row.Id = new Identifier("LaunchHidden", AccessModifier.Public);
                    row.Value = "yes";
                    row.Type = "string";
                }


                if (!String.IsNullOrEmpty(launchWorkingDir))
                {
                    var row = (WixBundleVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "Variable");
                    row.Id = new Identifier("LaunchWorkingFolder", AccessModifier.Public);
                    row.Value = launchWorkingDir;
                    row.Type = "string";
                }

                if (!String.IsNullOrEmpty(licenseFile))
                {
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("WixStdbaLicenseRtf", AccessModifier.Public);
                    wixVariableRow.Value = licenseFile;
                }

                if (null != licenseUrl)
                {
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("WixStdbaLicenseUrl", AccessModifier.Public);
                    wixVariableRow.Value = licenseUrl;
                }

                if (!String.IsNullOrEmpty(logoFile))
                {
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("WixStdbaLogo", AccessModifier.Public);
                    wixVariableRow.Value = logoFile;
                }

                if (!String.IsNullOrEmpty(logoSideFile))
                {
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("WixStdbaLogoSide", AccessModifier.Public);
                    wixVariableRow.Value = logoSideFile;
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("WixStdbaThemeXml", AccessModifier.Public);
                    wixVariableRow.Value = themeFile;
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("WixStdbaThemeWxl", AccessModifier.Public);
                    wixVariableRow.Value = localizationFile;
                }

                if (YesNoType.Yes == suppressOptionsUI || YesNoType.Yes == suppressDowngradeFailure || YesNoType.Yes == suppressRepair || YesNoType.Yes == showVersion || YesNoType.Yes == supportCacheOnly)
                {
                    var tuple = (WixStdbaOptionsTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixStdbaOptions");
                    if (YesNoType.Yes == suppressOptionsUI)
                    {
                        tuple.SuppressOptionsUI = 1;
                    }

                    if (YesNoType.Yes == suppressDowngradeFailure)
                    {
                        tuple.SuppressDowngradeFailure = 1;
                    }

                    if (YesNoType.Yes == suppressRepair)
                    {
                        tuple.SuppressRepair = 1;
                    }

                    if (YesNoType.Yes == showVersion)
                    {
                        tuple.ShowVersion = 1;
                    }

                    if (YesNoType.Yes == supportCacheOnly)
                    {
                        tuple.SupportCacheOnly = 1;
                    }
                }
            }
        }

        /// <summary>
        /// Parses a WixManagedBootstrapperApplicationHost element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixManagedBootstrapperApplicationHostElement(Intermediate intermediate, IntermediateSection section, XElement node)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string logoFile = null;
            string themeFile = null;
            string localizationFile = null;

            foreach (XAttribute attrib in node.Attributes())
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
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("PreqbaLogo", AccessModifier.Public);
                    wixVariableRow.Value = logoFile;
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("PreqbaThemeXml", AccessModifier.Public);
                    wixVariableRow.Value = themeFile;
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    var wixVariableRow = (WixVariableTuple)this.ParseHelper.CreateRow(section, sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = new Identifier("PreqbaThemeWxl", AccessModifier.Public);
                    wixVariableRow.Value = localizationFile;
                }
            }
        }
    }
}
