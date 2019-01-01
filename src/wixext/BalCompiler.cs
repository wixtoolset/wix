// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Bal Extension.
    /// </summary>
    public sealed class BalCompiler : CompilerExtension
    {
        private SourceLineNumber addedConditionLineNumber;
        private Dictionary<string, Row> prereqInfoRows;

        /// <summary>
        /// Instantiate a new BalCompiler.
        /// </summary>
        public BalCompiler()
        {
            this.addedConditionLineNumber = null;
            prereqInfoRows = new Dictionary<string, Row>();
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/bal";
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                case "Fragment":
                    switch (element.Name.LocalName)
                    {
                        case "Condition":
                            this.ParseConditionElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "BootstrapperApplicationRef":
                    switch (element.Name.LocalName)
                    {
                        case "WixStandardBootstrapperApplication":
                            this.ParseWixStandardBootstrapperApplicationElement(element);
                            break;
                        case "WixManagedBootstrapperApplicationHost":
                            this.ParseWixManagedBootstrapperApplicationHostElement(element);
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
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="attribute">Attribute to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public override void ParseAttribute(XElement parentElement, XAttribute attribute, IDictionary<string, string> context)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(parentElement);
            Row row;

            switch (parentElement.Name.LocalName)
            {
                case "ExePackage":
                case "MsiPackage":
                case "MspPackage":
                case "MsuPackage":
                    string packageId;
                    if (!context.TryGetValue("PackageId", out packageId) || String.IsNullOrEmpty(packageId))
                    {
                        this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, parentElement.Name.LocalName, "Id", attribute.Name.LocalName));
                    }
                    else
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "PrereqLicenseFile":

                                if (!prereqInfoRows.TryGetValue(packageId, out row))
                                {
                                    // at the time the extension attribute is parsed, the compiler might not yet have
                                    // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                    XAttribute prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");

                                    if (null != prereqPackage && YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, prereqPackage))
                                    {
                                        row = this.Core.CreateRow(sourceLineNumbers, "WixMbaPrereqInformation");
                                        row[0] = packageId;

                                        prereqInfoRows.Add(packageId, row);
                                    }
                                    else
                                    {
                                        this.Core.OnMessage(BalErrors.AttributeRequiresPrereqPackage(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseFile"));
                                        break;
                                    }
                                }

                                if (null != row[2])
                                {
                                    this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseFile", "PrereqLicenseUrl"));
                                }
                                else
                                {
                                    row[1] = this.Core.GetAttributeValue(sourceLineNumbers, attribute);
                                }
                                break;
                            case "PrereqLicenseUrl":

                                if (!prereqInfoRows.TryGetValue(packageId, out row))
                                {
                                    // at the time the extension attribute is parsed, the compiler might not yet have
                                    // parsed the PrereqPackage attribute, so we need to get it directly from the parent element.
                                    XAttribute prereqPackage = parentElement.Attribute(this.Namespace + "PrereqPackage");

                                    if (null != prereqPackage && YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, prereqPackage))
                                    {
                                        row = this.Core.CreateRow(sourceLineNumbers, "WixMbaPrereqInformation");
                                        row[0] = packageId;

                                        prereqInfoRows.Add(packageId, row);
                                    }
                                    else
                                    {
                                        this.Core.OnMessage(BalErrors.AttributeRequiresPrereqPackage(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseUrl"));
                                        break;
                                    }
                                }

                                if (null != row[1])
                                {
                                    this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, parentElement.Name.LocalName, "PrereqLicenseUrl", "PrereqLicenseFile"));
                                }
                                else
                                {
                                    row[2] = this.Core.GetAttributeValue(sourceLineNumbers, attribute);
                                }
                                break;
                            case "PrereqPackage":
                                if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    if (!prereqInfoRows.TryGetValue(packageId, out row))
                                    {
                                        row = this.Core.CreateRow(sourceLineNumbers, "WixMbaPrereqInformation");
                                        row[0] = packageId;

                                        prereqInfoRows.Add(packageId, row);
                                    }
                                }
                                break;
                            default:
                                this.Core.UnexpectedAttribute(parentElement, attribute);
                                break;
                        }
                    }
                    break;
                case "Payload":
                    string payloadId;
                    if (!context.TryGetValue("Id", out payloadId) || String.IsNullOrEmpty(payloadId))
                    {
                        this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, parentElement.Name.LocalName, "Id", attribute.Name.LocalName));
                    }
                    else
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "BAFunctions":
                                if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    row = this.Core.CreateRow(sourceLineNumbers, "WixBalBAFunctions");
                                    row[0] = payloadId;
                                }
                                break;
                            default:
                                this.Core.UnexpectedAttribute(parentElement, attribute);
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
                        this.Core.OnMessage(WixErrors.ExpectedParentWithAttribute(sourceLineNumbers, "Variable", "Overridable", "Name"));
                    }
                    else
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "Overridable":
                                if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    row = this.Core.CreateRow(sourceLineNumbers, "WixStdbaOverridableVariable");
                                    row[0] = variableName;
                                }
                                break;
                            default:
                                this.Core.UnexpectedAttribute(parentElement, attribute);
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
        private void ParseConditionElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string condition = this.Core.GetConditionInnerText(node); // condition is the inner text of the element.
            string message = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Message":
                            message = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            // Error check the values.
            if (String.IsNullOrEmpty(condition))
            {
                this.Core.OnMessage(WixErrors.ConditionExpected(sourceLineNumbers, node.Name.LocalName));
            }

            if (null == message)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Message"));
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixBalCondition");
                row[0] = condition;
                row[1] = message;

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
        private void ParseWixStandardBootstrapperApplicationElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
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
                            launchTarget = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchTargetElevatedId":
                            launchTargetElevatedId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchArguments":
                            launchArguments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchHidden":
                            launchHidden = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchWorkingFolder":
                            launchWorkingDir = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LicenseFile":
                            licenseFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LicenseUrl":
                            licenseUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        case "LogoFile":
                            logoFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LogoSideFile":
                            logoSideFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThemeFile":
                            themeFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressOptionsUI":
                            suppressOptionsUI = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressDowngradeFailure":
                            suppressDowngradeFailure = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressRepair":
                            suppressRepair = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ShowVersion":
                            showVersion = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SupportCacheOnly":
                            supportCacheOnly = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(licenseFile) && null == licenseUrl)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "LicenseFile", "LicenseUrl", true));
            }

            if (!this.Core.EncounteredError)
            {
                if (!String.IsNullOrEmpty(launchTarget))
                {
                    WixBundleVariableRow row = (WixBundleVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                    row.Id = "LaunchTarget";
                    row.Value = launchTarget;
                    row.Type = "string";
                }

                if (!String.IsNullOrEmpty(launchTargetElevatedId))
                {
                    WixBundleVariableRow row = (WixBundleVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                    row.Id = "LaunchTargetElevatedId";
                    row.Value = launchTargetElevatedId;
                    row.Type = "string";
                }

                if (!String.IsNullOrEmpty(launchArguments))
                {
                    WixBundleVariableRow row = (WixBundleVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                    row.Id = "LaunchArguments";
                    row.Value = launchArguments;
                    row.Type = "string";
                }

                if (YesNoType.Yes == launchHidden)
                {
                    WixBundleVariableRow row = (WixBundleVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixBundleVariable");
                    row.Id = "LaunchHidden";
                    row.Value = "yes";
                    row.Type = "string";
                }


                if (!String.IsNullOrEmpty(launchWorkingDir))
                {
                    WixBundleVariableRow row = (WixBundleVariableRow)this.Core.CreateRow(sourceLineNumbers, "Variable");
                    row.Id = "LaunchWorkingFolder";
                    row.Value = launchWorkingDir;
                    row.Type = "string";
                }

                if (!String.IsNullOrEmpty(licenseFile))
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "WixStdbaLicenseRtf";
                    wixVariableRow.Value = licenseFile;
                }

                if (null != licenseUrl)
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "WixStdbaLicenseUrl";
                    wixVariableRow.Value = licenseUrl;
                }

                if (!String.IsNullOrEmpty(logoFile))
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "WixStdbaLogo";
                    wixVariableRow.Value = logoFile;
                }

                if (!String.IsNullOrEmpty(logoSideFile))
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "WixStdbaLogoSide";
                    wixVariableRow.Value = logoSideFile;
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "WixStdbaThemeXml";
                    wixVariableRow.Value = themeFile;
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "WixStdbaThemeWxl";
                    wixVariableRow.Value = localizationFile;
                }

                if (YesNoType.Yes == suppressOptionsUI || YesNoType.Yes == suppressDowngradeFailure || YesNoType.Yes == suppressRepair || YesNoType.Yes == showVersion || YesNoType.Yes == supportCacheOnly)
                {
                    Row row = this.Core.CreateRow(sourceLineNumbers, "WixStdbaOptions");
                    if (YesNoType.Yes == suppressOptionsUI)
                    {
                        row[0] = 1;
                    }

                    if (YesNoType.Yes == suppressDowngradeFailure)
                    {
                        row[1] = 1;
                    }

                    if (YesNoType.Yes == suppressRepair)
                    {
                        row[2] = 1;
                    }

                    if (YesNoType.Yes == showVersion)
                    {
                        row[3] = 1;
                    }

                    if (YesNoType.Yes == supportCacheOnly)
                    {
                        row[4] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// Parses a WixManagedBootstrapperApplicationHost element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixManagedBootstrapperApplicationHostElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
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
                            logoFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThemeFile":
                            themeFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (!this.Core.EncounteredError)
            {
                if (!String.IsNullOrEmpty(logoFile))
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "PreqbaLogo";
                    wixVariableRow.Value = logoFile;
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "PreqbaThemeXml";
                    wixVariableRow.Value = themeFile;
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    WixVariableRow wixVariableRow = (WixVariableRow)this.Core.CreateRow(sourceLineNumbers, "WixVariable");
                    wixVariableRow.Id = "PreqbaThemeWxl";
                    wixVariableRow.Value = localizationFile;
                }
            }
        }
    }
}
