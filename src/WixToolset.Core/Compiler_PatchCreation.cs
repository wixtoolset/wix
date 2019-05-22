// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        /// <summary>
        /// Parses a patch creation element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePatchCreationElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var clean = true; // Default is to clean
            var codepage = 0;
            string outputPath = null;
            var productMismatches = false;
            var replaceGuids = String.Empty;
            string sourceList = null;
            string symbolFlags = null;
            var targetProducts = String.Empty;
            var versionMismatches = false;
            var wholeFiles = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        this.activeName = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                        break;
                    case "AllowMajorVersionMismatches":
                        versionMismatches = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "AllowProductCodeMismatches":
                        productMismatches = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "CleanWorkingFolder":
                        clean = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Codepage":
                        codepage = this.Core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                        break;
                    case "OutputPath":
                        outputPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SourceList":
                        sourceList = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SymbolFlags":
                        symbolFlags = String.Format(CultureInfo.InvariantCulture, "0x{0:x8}", this.Core.GetAttributeLongValue(sourceLineNumbers, attrib, 0, UInt32.MaxValue));
                        break;
                    case "WholeFilesOnly":
                        wholeFiles = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == this.activeName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.CreateActiveSection(this.activeName, SectionType.PatchCreation, codepage, this.Context.CompilationId);

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Family":
                        this.ParseFamilyElement(child);
                        break;
                    case "PatchInformation":
                        this.ParsePatchInformationElement(child);
                        break;
                    case "PatchMetadata":
                        this.ParsePatchMetadataElement(child);
                        break;
                    case "PatchProperty":
                        this.ParsePatchPropertyElement(child, false);
                        break;
                    case "PatchSequence":
                        this.ParsePatchSequenceElement(child);
                        break;
                    case "ReplacePatch":
                        replaceGuids = String.Concat(replaceGuids, this.ParseReplacePatchElement(child));
                        break;
                    case "TargetProductCode":
                        var targetProduct = this.ParseTargetProductCodeElement(child);
                        if (0 < targetProducts.Length)
                        {
                            targetProducts = String.Concat(targetProducts, ";");
                        }
                        targetProducts = String.Concat(targetProducts, targetProduct);
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

            this.AddPrivateProperty(sourceLineNumbers, "PatchGUID", this.activeName);
            this.AddPrivateProperty(sourceLineNumbers, "AllowProductCodeMismatches", productMismatches ? "1" : "0");
            this.AddPrivateProperty(sourceLineNumbers, "AllowProductVersionMajorMismatches", versionMismatches ? "1" : "0");
            this.AddPrivateProperty(sourceLineNumbers, "DontRemoveTempFolderWhenFinished", clean ? "0" : "1");
            this.AddPrivateProperty(sourceLineNumbers, "IncludeWholeFilesOnly", wholeFiles ? "1" : "0");

            if (null != symbolFlags)
            {
                this.AddPrivateProperty(sourceLineNumbers, "ApiPatchingSymbolFlags", symbolFlags);
            }

            if (0 < replaceGuids.Length)
            {
                this.AddPrivateProperty(sourceLineNumbers, "ListOfPatchGUIDsToReplace", replaceGuids);
            }

            if (0 < targetProducts.Length)
            {
                this.AddPrivateProperty(sourceLineNumbers, "ListOfTargetProductCodes", targetProducts);
            }

            if (null != outputPath)
            {
                this.AddPrivateProperty(sourceLineNumbers, "PatchOutputPath", outputPath);
            }

            if (null != sourceList)
            {
                this.AddPrivateProperty(sourceLineNumbers, "PatchSourceList", sourceList);
            }
        }

        /// <summary>
        /// Parses a family element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseFamilyElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var diskId = CompilerConstants.IntegerNotSet;
            string diskPrompt = null;
            string mediaSrcProp = null;
            string name = null;
            var sequenceStart = CompilerConstants.IntegerNotSet;
            string volumeLabel = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "DiskId":
                        diskId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int16.MaxValue);
                        break;
                    case "DiskPrompt":
                        diskPrompt = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MediaSrcProp":
                        mediaSrcProp = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SequenceStart":
                        sequenceStart = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, Int32.MaxValue);
                        break;
                    case "VolumeLabel":
                        volumeLabel = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
            else if (0 < name.Length)
            {
                if (8 < name.Length) // check the length
                {
                    this.Core.Write(ErrorMessages.FamilyNameTooLong(sourceLineNumbers, node.Name.LocalName, "Name", name, name.Length));
                }
                else // check for illegal characters
                {
                    foreach (var character in name)
                    {
                        if (!Char.IsLetterOrDigit(character) && '_' != character)
                        {
                            this.Core.Write(ErrorMessages.IllegalFamilyName(sourceLineNumbers, node.Name.LocalName, "Name", name));
                        }
                    }
                }
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "UpgradeImage":
                        this.ParseUpgradeImageElement(child, name);
                        break;
                    case "ExternalFile":
                        this.ParseExternalFileElement(child, name);
                        break;
                    case "ProtectFile":
                        this.ParseProtectFileElement(child, name);
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
                var tuple = new ImageFamiliesTuple(sourceLineNumbers)
                {
                    Family = name,
                    MediaSrcPropName = mediaSrcProp,
                    DiskPrompt = diskPrompt,
                    VolumeLabel = volumeLabel
                };

                if (CompilerConstants.IntegerNotSet != diskId)
                {
                    tuple.MediaDiskId = diskId;
                }

                if (CompilerConstants.IntegerNotSet != sequenceStart)
                {
                    tuple.FileSequenceStart = sequenceStart;
                }

                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses an upgrade image element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="family">The family for this element.</param>
        private void ParseUpgradeImageElement(XElement node, string family)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string sourceFile = null;
            string sourcePatch = null;
            var symbols = new List<string>();
            string upgrade = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        upgrade = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        if (13 < upgrade.Length)
                        {
                            this.Core.Write(ErrorMessages.IdentifierTooLongError(sourceLineNumbers, node.Name.LocalName, "Id", upgrade, 13));
                        }
                        break;
                    case "SourceFile":
                    case "src":
                        if (null != sourceFile)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "SourceFile"));
                        }

                        if ("src" == attrib.Name.LocalName)
                        {
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourceFile"));
                        }
                        sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SourcePatch":
                    case "srcPatch":
                        if (null != sourcePatch)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "srcPatch", "SourcePatch"));
                        }

                        if ("srcPatch" == attrib.Name.LocalName)
                        {
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourcePatch"));
                        }
                        sourcePatch = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == upgrade)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "SymbolPath":
                        symbols.Add(this.ParseSymbolPathElement(child));
                        break;
                    case "TargetImage":
                        this.ParseTargetImageElement(child, upgrade, family);
                        break;
                    case "UpgradeFile":
                        this.ParseUpgradeFileElement(child, upgrade);
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
                this.Core.AddTuple(new UpgradedImagesTuple(sourceLineNumbers)
                {
                    Upgraded = upgrade,
                    MsiPath = sourceFile,
                    PatchMsiPath = sourcePatch,
                    SymbolPaths = String.Join(";", symbols),
                    Family = family
                });
            }
        }

        /// <summary>
        /// Parses an upgrade file element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="upgrade">The upgrade key for this element.</param>
        private void ParseUpgradeFileElement(XElement node, string upgrade)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var allowIgnoreOnError = false;
            string file = null;
            var ignore = false;
            var symbols = new List<string>();
            var wholeFile = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "AllowIgnoreOnError":
                        allowIgnoreOnError = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "File":
                        file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Ignore":
                        ignore = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "WholeFile":
                        wholeFile = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == file)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "SymbolPath":
                        symbols.Add(this.ParseSymbolPathElement(child));
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
                if (ignore)
                {
                    this.Core.AddTuple(new UpgradedFilesToIgnoreTuple(sourceLineNumbers)
                    {
                        Upgraded = upgrade,
                        FTK = file
                    });
                }
                else
                {
                    this.Core.AddTuple(new UpgradedFiles_OptionalDataTuple(sourceLineNumbers)
                    {
                        Upgraded = upgrade,
                        FTK = file,
                        SymbolPaths = String.Join(";", symbols),
                        AllowIgnoreOnPatchError = allowIgnoreOnError,
                        IncludeWholeFile = wholeFile
                    });
                }
            }
        }

        /// <summary>
        /// Parses a target image element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="upgrade">The upgrade key for this element.</param>
        /// <param name="family">The family key for this element.</param>
        private void ParseTargetImageElement(XElement node, string upgrade, string family)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var ignore = false;
            var order = CompilerConstants.IntegerNotSet;
            string sourceFile = null;
            string symbols = null;
            string target = null;
            string validation = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        if (target.Length > 13)
                        {
                            this.Core.Write(ErrorMessages.IdentifierTooLongError(sourceLineNumbers, node.Name.LocalName, "Id", target, 13));
                        }
                        break;
                    case "IgnoreMissingFiles":
                        ignore = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Order":
                        order = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, Int32.MinValue + 2, Int32.MaxValue);
                        break;
                    case "SourceFile":
                    case "src":
                        if (null != sourceFile)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "SourceFile"));
                        }

                        if ("src" == attrib.Name.LocalName)
                        {
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "SourceFile"));
                        }
                        sourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Validation":
                        validation = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == target)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == sourceFile)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SourceFile"));
            }

            if (CompilerConstants.IntegerNotSet == order)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Order"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "SymbolPath":
                        if (null != symbols)
                        {
                            symbols = String.Concat(symbols, ";", this.ParseSymbolPathElement(child));
                        }
                        else
                        {
                            symbols = this.ParseSymbolPathElement(child);
                        }
                        break;
                    case "TargetFile":
                        this.ParseTargetFileElement(child, target, family);
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
                this.Core.AddTuple(new TargetImagesTuple(sourceLineNumbers)
                {
                    Target = target,
                    MsiPath = sourceFile,
                    SymbolPaths = symbols,
                    Upgraded = upgrade,
                    Order = order,
                    ProductValidateFlags = validation,
                    IgnoreMissingSrcFiles = ignore
                });
            }
        }

        /// <summary>
        /// Parses an upgrade file element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="target">The upgrade key for this element.</param>
        /// <param name="family">The family key for this element.</param>
        private void ParseTargetFileElement(XElement node, string target, string family)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string ignoreLengths = null;
            string ignoreOffsets = null;
            string protectLengths = null;
            string protectOffsets = null;
            string symbols = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == file)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "IgnoreRange":
                        this.ParseRangeElement(child, ref ignoreOffsets, ref ignoreLengths);
                        break;
                    case "ProtectRange":
                        this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
                        break;
                    case "SymbolPath":
                        symbols = this.ParseSymbolPathElement(child);
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
                var tuple = new TargetFiles_OptionalDataTuple(sourceLineNumbers)
                {
                    Target = target,
                    FTK = file,
                    SymbolPaths = symbols,
                    IgnoreOffsets = ignoreOffsets,
                    IgnoreLengths = ignoreLengths
                };

                this.Core.AddTuple(tuple);

                if (null != protectOffsets)
                {
                    tuple.RetainOffsets = protectOffsets;

                    this.Core.AddTuple(new FamilyFileRangesTuple(sourceLineNumbers)
                    {
                        Family = family,
                        FTK = file,
                        RetainOffsets = protectOffsets,
                        RetainLengths = protectLengths
                    });
                }
            }
        }

        /// <summary>
        /// Parses an external file element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="family">The family for this element.</param>
        private void ParseExternalFileElement(XElement node, string family)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string ignoreLengths = null;
            string ignoreOffsets = null;
            var order = CompilerConstants.IntegerNotSet;
            string protectLengths = null;
            string protectOffsets = null;
            string source = null;
            string symbols = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "File":
                        file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Order":
                        order = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, Int32.MinValue + 2, Int32.MaxValue);
                        break;
                    case "Source":
                    case "src":
                        if (null != source)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "src", "Source"));
                        }

                        if ("src" == attrib.Name.LocalName)
                        {
                            this.Core.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Source"));
                        }
                        source = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == file)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            if (null == source)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Source"));
            }

            if (CompilerConstants.IntegerNotSet == order)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Order"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "IgnoreRange":
                        this.ParseRangeElement(child, ref ignoreOffsets, ref ignoreLengths);
                        break;
                    case "ProtectRange":
                        this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
                        break;
                    case "SymbolPath":
                        symbols = this.ParseSymbolPathElement(child);
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
                var tuple = new ExternalFilesTuple(sourceLineNumbers)
                {
                    Family = family,
                    FTK = file,
                    FilePath = source,
                    SymbolPaths = symbols,
                    IgnoreOffsets = ignoreOffsets,
                    IgnoreLengths = ignoreLengths
                };

                if (null != protectOffsets)
                {
                    tuple.RetainOffsets = protectOffsets;
                }

                if (CompilerConstants.IntegerNotSet != order)
                {
                    tuple.Order = order;
                }

                this.Core.AddTuple(tuple);

                if (null != protectOffsets)
                {
                    this.Core.AddTuple(new FamilyFileRangesTuple(sourceLineNumbers)
                    {
                        Family = family,
                        FTK = file,
                        RetainOffsets = protectOffsets,
                        RetainLengths = protectLengths
                    });
                }
            }
        }

        /// <summary>
        /// Parses a protect file element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="family">The family for this element.</param>
        private void ParseProtectFileElement(XElement node, string family)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string file = null;
            string protectLengths = null;
            string protectOffsets = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "File":
                        file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == file)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "ProtectRange":
                        this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
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

            if (null == protectOffsets || null == protectLengths)
            {
                this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "ProtectRange"));
            }

            if (!this.Core.EncounteredError)
            {
                this.Core.AddTuple(new FamilyFileRangesTuple(sourceLineNumbers)
                {
                    Family = family,
                    FTK = file,
                    RetainOffsets = protectOffsets,
                    RetainLengths = protectLengths
                });
            }
        }

        /// <summary>
        /// Parses a range element (ProtectRange, IgnoreRange, etc).
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="offsets">Reference to the offsets string.</param>
        /// <param name="lengths">Reference to the lengths string.</param>
        private void ParseRangeElement(XElement node, ref string offsets, ref string lengths)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string length = null;
            string offset = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Length":
                        length = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Offset":
                        offset = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == length)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Length"));
            }

            if (null == offset)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Offset"));
            }

            this.Core.ParseForExtensionElements(node);

            if (null != lengths)
            {
                lengths = String.Concat(lengths, ",", length);
            }
            else
            {
                lengths = length;
            }

            if (null != offsets)
            {
                offsets = String.Concat(offsets, ",", offset);
            }
            else
            {
                offsets = offset;
            }
        }

        /// <summary>
        /// Parses a patch metadata element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParsePatchMetadataElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var allowRemoval = YesNoType.NotSet;
            string classification = null;
            string creationTimeUtc = null;
            string description = null;
            string displayName = null;
            string manufacturerName = null;
            string minorUpdateTargetRTM = null;
            string moreInfoUrl = null;
            var optimizeCA = CompilerConstants.IntegerNotSet;
            var optimizedInstallMode = YesNoType.NotSet;
            string targetProductName = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "AllowRemoval":
                        allowRemoval = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Classification":
                        classification = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "CreationTimeUTC":
                        creationTimeUtc = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Description":
                        description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DisplayName":
                        displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ManufacturerName":
                        manufacturerName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MinorUpdateTargetRTM":
                        minorUpdateTargetRTM = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (YesNoType.NotSet == allowRemoval)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "AllowRemoval"));
            }

            if (null == classification)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Classification"));
            }

            if (null == description)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Description"));
            }

            if (null == displayName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "DisplayName"));
            }

            if (null == manufacturerName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ManufacturerName"));
            }

            if (null == moreInfoUrl)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "MoreInfoURL"));
            }

            if (null == targetProductName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "TargetProductName"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "CustomProperty":
                        this.ParseCustomPropertyElement(child);
                        break;
                    case "OptimizeCustomActions":
                        optimizeCA = this.ParseOptimizeCustomActionsElement(child);
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
                if (YesNoType.NotSet != allowRemoval)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "AllowRemoval", YesNoType.Yes == allowRemoval ? "1" : "0");
                }

                if (null != classification)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "Classification", classification);
                }

                if (null != creationTimeUtc)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "CreationTimeUTC", creationTimeUtc);
                }

                if (null != description)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "Description", description);
                }

                if (null != displayName)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "DisplayName", displayName);
                }

                if (null != manufacturerName)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "ManufacturerName", manufacturerName);
                }

                if (null != minorUpdateTargetRTM)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "MinorUpdateTargetRTM", minorUpdateTargetRTM);
                }

                if (null != moreInfoUrl)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "MoreInfoURL", moreInfoUrl);
                }

                if (CompilerConstants.IntegerNotSet != optimizeCA)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "OptimizeCA", optimizeCA.ToString(CultureInfo.InvariantCulture));
                }

                if (YesNoType.NotSet != optimizedInstallMode)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "OptimizedInstallMode", YesNoType.Yes == optimizedInstallMode ? "1" : "0");
                }

                if (null != targetProductName)
                {
                    this.AddPatchMetadata(sourceLineNumbers, null, "TargetProductName", targetProductName);
                }
            }
        }

        /// <summary>
        /// Parses a custom property element for the PatchMetadata table.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseCustomPropertyElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string company = null;
            string property = null;
            string value = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Company":
                        company = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Property":
                        property = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Value":
                        value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == company)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Company"));
            }

            if (null == property)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.AddPatchMetadata(sourceLineNumbers, company, property, value);
            }
        }

        /// <summary>
        /// Parses a patch sequence element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParsePatchSequenceElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string family = null;
            string target = null;
            string sequence = null;
            var attributes = 0;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "PatchFamily":
                        family = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "ProductCode":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Target", "TargetImage"));
                        }
                        target = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                        break;
                    case "Target":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "TargetImage", "ProductCode"));
                        }
                        this.Core.Write(WarningMessages.DeprecatedPatchSequenceTargetAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "TargetImage":
                        if (null != target)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Target", "ProductCode"));
                        }
                        target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "TargetImages", target);
                        break;
                    case "Sequence":
                        sequence = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
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

            if (null == family)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PatchFamily"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new PatchSequenceTuple(sourceLineNumbers)
                {
                    PatchFamily = family,
                    Target = target,
                    Sequence = sequence,
                    Supersede = attributes
                };

                this.Core.AddTuple(tuple);
            }
        }

        private void AddPatchMetadata(SourceLineNumber sourceLineNumbers, string company, string property, string value)
        {
            this.Core.AddTuple(new PatchMetadataTuple(sourceLineNumbers, new Identifier(AccessModifier.Private, company, property))
            {
                Company = company,
                Property = property,
                Value = value
            });
        }
    }
}
