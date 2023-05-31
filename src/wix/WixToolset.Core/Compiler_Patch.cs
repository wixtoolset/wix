// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        /// <summary>
        /// Parses an patch element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePatchElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string patchId = null;
            string codepage = null;
            ////bool versionMismatches = false;
            ////bool productMismatches = false;
            var allowRemoval = false;
            string classification = null;
            string clientPatchId = null;
            string description = null;
            string displayName = null;
            string comments = null;
            string manufacturer = null;
            var minorUpdateTargetRTM = YesNoType.NotSet;
            string moreInfoUrl = null;
            var optimizeCA = CompilerConstants.IntegerNotSet;
            var optimizedInstallMode = YesNoType.NotSet;
            string targetProductName = null;
            // string replaceGuids = String.Empty;
            var apiPatchingSymbolFlags = 0;
            var optimizePatchSizeForLargeFiles = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        patchId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
                        break;
                    case "Codepage":
                        codepage = this.Core.GetAttributeLocalizableCodePageValue(sourceLineNumbers, attrib);
                        break;
                    case "AllowMajorVersionMismatches":
                        ////versionMismatches = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                        break;
                    case "AllowProductCodeMismatches":
                        ////productMismatches = (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                        break;
                    case "AllowRemoval":
                        allowRemoval = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
                        break;
                    case "Classification":
                        classification = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ClientPatchId":
                        clientPatchId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Description":
                        description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DisplayName":
                        displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Comments":
                        comments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Manufacturer":
                        manufacturer = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MinorUpdateTargetRTM":
                        minorUpdateTargetRTM = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "MoreInfoURL":
                        moreInfoUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "OptimizedInstallMode":
                        optimizedInstallMode = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "TargetProductName":
                        targetProductName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ApiPatchingSymbolNoImagehlpFlag":
                        apiPatchingSymbolFlags |= (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlags.PatchSymbolNoImagehlp : 0;
                        break;
                    case "ApiPatchingSymbolNoFailuresFlag":
                        apiPatchingSymbolFlags |= (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlags.PatchSymbolNoFailures : 0;
                        break;
                    case "ApiPatchingSymbolUndecoratedTooFlag":
                        apiPatchingSymbolFlags |= (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib)) ? (int)PatchSymbolFlags.PatchSymbolUndecoratedToo : 0;
                        break;
                    case "OptimizePatchSizeForLargeFiles":
                        optimizePatchSizeForLargeFiles = (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib));
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

            if (patchId == null || patchId == "*")
            {
                // auto-generate at compile time, since this value gets dispersed to several locations
                patchId = Common.GenerateGuid();
            }
            this.activeName = patchId;

            if (null == this.activeName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            if (null == classification)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Classification"));
            }
            if (null == clientPatchId)
            {
                clientPatchId = String.Concat("_", new Guid(patchId).ToString("N", CultureInfo.InvariantCulture).ToUpper(CultureInfo.InvariantCulture));
            }
            if (null == description)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }
            if (null == displayName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayName"));
            }
            if (null == manufacturer)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Manufacturer"));
            }

            this.Core.CreateActiveSection(this.activeName, SectionType.Patch, this.Context.CompilationId);

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "PatchInformation":
                        this.ParsePatchInformationElement(child);
                        break;
                    case "Media":
                        this.ParseMediaElement(child, patchId);
                        break;
                    case "OptimizeCustomActions":
                        optimizeCA = this.ParseOptimizeCustomActionsElement(child);
                        break;
                    case "PatchFamily":
                        this.ParsePatchFamilyElement(child, ComplexReferenceParentType.Patch, patchId);
                        break;
                    case "PatchFamilyRef":
                        this.ParsePatchFamilyRefElement(child, ComplexReferenceParentType.Patch, patchId);
                        break;
                    case "PatchFamilyGroup":
                        this.ParsePatchFamilyGroupElement(child, ComplexReferenceParentType.Patch, patchId);
                        break;
                    case "PatchFamilyGroupRef":
                        this.ParsePatchFamilyGroupRefElement(child, ComplexReferenceParentType.Patch, patchId);
                        break;
                    case "PatchProperty":
                        this.ParsePatchPropertyElement(child, true);
                        break;
                    case "TargetProductCodes":
                        this.ParseTargetProductCodesElement(child);
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
                this.Core.AddSymbol(new WixPatchSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, patchId))
                {
                    Codepage = codepage,
                    ClientPatchId = clientPatchId,
                    OptimizePatchSizeForLargeFiles = optimizePatchSizeForLargeFiles,
                    ApiPatchingSymbolFlags = apiPatchingSymbolFlags,
                });

                if (allowRemoval)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "AllowRemoval", allowRemoval ? "1" : "0");
                }

                if (null != classification)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "Classification", classification);
                }

                // always generate the CreationTimeUTC
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "CreationTimeUTC", DateTime.UtcNow.ToString("MM-dd-yy HH:mm", CultureInfo.InvariantCulture));
                }

                if (null != description)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "Description", description);
                }

                if (null != displayName)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "DisplayName", displayName);
                }

                if (null != manufacturer)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "ManufacturerName", manufacturer);
                }

                if (YesNoType.NotSet != minorUpdateTargetRTM)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "MinorUpdateTargetRTM", YesNoType.Yes == minorUpdateTargetRTM ? "1" : "0");
                }

                if (null != moreInfoUrl)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "MoreInfoURL", moreInfoUrl);
                }

                if (CompilerConstants.IntegerNotSet != optimizeCA)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "OptimizeCA", optimizeCA.ToString(CultureInfo.InvariantCulture));
                }

                if (YesNoType.NotSet != optimizedInstallMode)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "OptimizedInstallMode", YesNoType.Yes == optimizedInstallMode ? "1" : "0");
                }

                if (null != targetProductName)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "TargetProductName", targetProductName);
                }

                if (null != comments)
                {
                    this.AddMsiPatchMetadata(sourceLineNumbers, null, "Comments", comments);
                }
            }
            // TODO: do something with versionMismatches and productMismatches
        }

        /// <summary>
        /// Parses the OptimizeCustomActions element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>The combined integer value for callers to store as appropriate.</returns>
        private int ParseOptimizeCustomActionsElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var optimizeCA = OptimizeCAFlags.None;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "SkipAssignment":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            optimizeCA |= OptimizeCAFlags.SkipAssignment;
                        }
                        break;
                    case "SkipImmediate":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            optimizeCA |= OptimizeCAFlags.SkipImmediate;
                        }
                        break;
                    case "SkipDeferred":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            optimizeCA |= OptimizeCAFlags.SkipDeferred;
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

            return (int)optimizeCA;
        }

        /// <summary>
        /// Parses a PatchFamily element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="parentType"></param>
        /// <param name="parentId"></param>
        private void ParsePatchFamilyElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string productCode = null;
            string version = null;
            var attributes = 0;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "ProductCode":
                        productCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                        break;
                    case "Version":
                        version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                        break;
                    case "Supersede":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x1;
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

            if (String.IsNullOrEmpty(version))
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }
            else if (!CompilerCore.IsValidProductVersion(version))
            {
                this.Core.Write(WarningMessages.InvalidMsiProductVersion(sourceLineNumbers, version));
            }

            // find unexpected child elements
            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "All":
                        this.ParseAllElement(child);
                        break;
                    case "BinaryRef":
                        this.ParsePatchChildRefElement(child, "Binary");
                        break;
                    case "ComponentRef":
                        this.ParsePatchChildRefElement(child, "Component");
                        break;
                    case "CustomActionRef":
                        this.ParsePatchChildRefElement(child, "CustomAction");
                        break;
                    case "DirectoryRef":
                        this.ParsePatchChildRefElement(child, "Directory");
                        break;
                    case "DigitalCertificateRef":
                        this.ParsePatchChildRefElement(child, "MsiDigitalCertificate");
                        break;
                    case "FeatureRef":
                        this.ParsePatchChildRefElement(child, "Feature");
                        break;
                    case "IconRef":
                        this.ParsePatchChildRefElement(child, "Icon");
                        break;
                    case "PropertyRef":
                        this.ParsePatchChildRefElement(child, "Property");
                        break;
                    case "SoftwareTagRef":
                        this.ParseSoftwareTagRefElement(child);
                        break;
                    case "UIRef":
                        this.ParsePatchChildRefElement(child, "WixUI");
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
                this.Core.AddSymbol(new MsiPatchFamilySymbol(sourceLineNumbers, new Identifier(id.Access, id.Id, productCode))
                {
                    PatchFamily = id.Id,
                    ProductCode = productCode,
                    Sequence = version,
                    Attributes = attributes
                });

                if (ComplexReferenceParentType.Unknown != parentType)
                {
                    this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamily, id.Id, ComplexReferenceParentType.Patch == parentType);
                }
            }
        }

        /// <summary>
        /// Parses a PatchFamilyGroup element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType"></param>
        /// <param name="parentId"></param>
        private void ParsePatchFamilyGroupElement(XElement node, ComplexReferenceParentType parentType, string parentId)
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

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "PatchFamily":
                        this.ParsePatchFamilyElement(child, ComplexReferenceParentType.PatchFamilyGroup, id.Id);
                        break;
                    case "PatchFamilyRef":
                        this.ParsePatchFamilyRefElement(child, ComplexReferenceParentType.PatchFamilyGroup, id.Id);
                        break;
                    case "PatchFamilyGroupRef":
                        this.ParsePatchFamilyGroupRefElement(child, ComplexReferenceParentType.PatchFamilyGroup, id.Id);
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
                this.Core.AddSymbol(new WixPatchFamilyGroupSymbol(sourceLineNumbers, id));

                //Add this PatchFamilyGroup and its parent in WixGroup.
                this.Core.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, ComplexReferenceChildType.PatchFamilyGroup, id.Id);
            }
        }

        /// <summary>
        /// Parses a PatchFamilyGroup reference element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">The type of parent.</param>
        /// <param name="parentId">Identifier of parent element.</param>
        private void ParsePatchFamilyGroupRefElement(XElement node, ComplexReferenceParentType parentType, string parentId)
        {
            Debug.Assert(ComplexReferenceParentType.PatchFamilyGroup == parentType || ComplexReferenceParentType.Patch == parentType);

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
                        this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixPatchFamilyGroup, id);
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
                this.Core.CreateComplexReference(sourceLineNumbers, parentType, parentId, null, ComplexReferenceChildType.PatchFamilyGroup, id, true);
            }
        }

        /// <summary>
        /// Parses a TargetProductCodes element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseTargetProductCodesElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var replace = false;
            var targetProductCodes = new List<string>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Replace":
                        replace = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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
                    case "TargetProductCode":
                        var id = this.ParseTargetProductCodeElement(child);
                        if (0 == String.CompareOrdinal("*", id))
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeValueWhenNested(sourceLineNumbers, child.Name.LocalName, "Id", id, node.Name.LocalName));
                        }
                        else
                        {
                            targetProductCodes.Add(id);
                        }
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
                // By default, target ProductCodes should be added.
                if (!replace)
                {
                    this.Core.AddSymbol(new WixPatchTargetSymbol(sourceLineNumbers)
                    {
                        ProductCode = "*"
                    });
                }

                foreach (var targetProductCode in targetProductCodes)
                {
                    this.Core.AddSymbol(new WixPatchTargetSymbol(sourceLineNumbers)
                    {
                        ProductCode = targetProductCode
                    });
                }
            }
        }

        private void AddMsiPatchMetadata(SourceLineNumber sourceLineNumbers, string company, string property, string value)
        {
            this.Core.AddSymbol(new MsiPatchMetadataSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, company, property))
            {
                Company = company,
                Property = property,
                Value = value
            });
        }
    }
}
