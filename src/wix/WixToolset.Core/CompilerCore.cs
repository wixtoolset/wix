// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Core class for the compiler.
    /// </summary>
    internal class CompilerCore
    {
        internal static readonly XNamespace W3SchemaPrefix = "http://www.w3.org/";
        internal static readonly XNamespace WixNamespace = "http://wixtoolset.org/schemas/v4/wxs";

        private readonly Dictionary<XNamespace, ICompilerExtension> extensions;
        private readonly IBundleValidator bundleValidator;
        private readonly IParseHelper parseHelper;
        private readonly Intermediate intermediate;
        private readonly IMessaging messaging;
        private Dictionary<string, string> activeSectionCachedInlinedDirectoryIds;
        private HashSet<string> activeSectionSimpleReferences;

        /// <summary>
        /// Constructor for all compiler core.
        /// </summary>
        /// <param name="intermediate">The Intermediate object representing compiled source document.</param>
        /// <param name="messaging"></param>
        /// <param name="bundleValidator"></param>
        /// <param name="parseHelper"></param>
        /// <param name="extensions">The WiX extensions collection.</param>
        internal CompilerCore(Intermediate intermediate, IMessaging messaging, IBundleValidator bundleValidator, IParseHelper parseHelper, Dictionary<XNamespace, ICompilerExtension> extensions)
        {
            this.extensions = extensions;
            this.bundleValidator = bundleValidator;
            this.parseHelper = parseHelper;
            this.intermediate = intermediate;
            this.messaging = messaging;
        }

        /// <summary>
        /// Gets the section the compiler is currently emitting symbols into.
        /// </summary>
        /// <value>The section the compiler is currently emitting symbols into.</value>
        public IntermediateSection ActiveSection { get; private set; }

        /// <summary>
        /// Gets whether the compiler core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError => this.messaging.EncounteredError;

        /// <summary>
        /// Gets or sets the option to show pedantic messages.
        /// </summary>
        /// <value>The option to show pedantic messages.</value>
        public bool ShowPedanticMessages { get; set; }

        /// <summary>
        /// Add a symbol to the active section.
        /// </summary>
        /// <param name="symbol">Symbol to add.</param>
        public T AddSymbol<T>(T symbol)
            where T : IntermediateSymbol
        {
            return this.ActiveSection.AddSymbol(symbol);
        }

        /// <summary>
        /// Convert a bit array into an int value.
        /// </summary>
        /// <param name="bits">The bit array to convert.</param>
        /// <returns>The converted int value.</returns>
        public int CreateIntegerFromBitArray(BitArray bits)
        {
            if (32 != bits.Length)
            {
                throw new ArgumentException(String.Format("Can only convert a bit array with 32-bits to integer. Actual number of bits in array: {0}", bits.Length), "bits");
            }

            int[] intArray = new int[1];
            bits.CopyTo(intArray, 0);

            return intArray[0];
        }

        /// <summary>
        /// Sets a bit in a bit array based on the index at which an attribute name was found in a string array.
        /// </summary>
        /// <param name="attributeNames">Array of attributes that map to bits.</param>
        /// <param name="attributeName">Name of attribute to check.</param>
        /// <param name="attributeValue">Value of attribute to check.</param>
        /// <param name="bits">The bit array in which the bit will be set if found.</param>
        /// <param name="offset">The offset into the bit array.</param>
        /// <returns>true if the bit was set; false otherwise.</returns>
        public bool TrySetBitFromName(string[] attributeNames, string attributeName, YesNoType attributeValue, BitArray bits, int offset)
        {
            for (int i = 0; i < attributeNames.Length; i++)
            {
                if (attributeName.Equals(attributeNames[i], StringComparison.Ordinal))
                {
                    bits.Set(i + offset, YesNoType.Yes == attributeValue);
                    return true;
                }
            }

            return false;
        }

        internal void InnerTextDisallowed(XElement element)
        {
            this.parseHelper.InnerTextDisallowed(element);
        }

        internal void InnerTextDisallowed(XElement element, string attributeName)
        {
            this.parseHelper.InnerTextDisallowed(element, attributeName);
        }

        /// <summary>
        /// Verifies that a filename is ambiguous.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <returns>true if the filename is ambiguous; false otherwise.</returns>
        public static bool IsAmbiguousFilename(string filename)
        {
            if (String.IsNullOrEmpty(filename))
            {
                return false;
            }

            var tilde = filename.IndexOf('~');
            return (tilde > 0 && tilde < filename.Length) && Char.IsNumber(filename[tilde + 1]);
        }

        /// <summary>
        /// Verifies that a value is a legal identifier.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <returns>true if the value is an identifier; false otherwise.</returns>
        public bool IsValidIdentifier(string value)
        {
            return this.parseHelper.IsValidIdentifier(value);
        }

        /// <summary>
        /// Verifies if an identifier is a valid loc identifier.
        /// </summary>
        /// <param name="identifier">Identifier to verify.</param>
        /// <returns>True if the identifier is a valid loc identifier.</returns>
        public bool IsValidLocIdentifier(string identifier)
        {
            return this.parseHelper.IsValidLocIdentifier(identifier);
        }

        /// <summary>
        /// Verifies if a filename is a valid long filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="allowRelative">true if relative paths are allowed in the filename.</param>
        /// <returns>True if the filename is a valid long filename</returns>
        public bool IsValidLongFilename(string filename, bool allowWildcards = false, bool allowRelative = false)
        {
            return this.parseHelper.IsValidLongFilename(filename, allowWildcards, allowRelative);
        }

        /// <summary>
        /// Verifies if a filename is a valid short filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>True if the filename is a valid short filename</returns>
        public bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            return this.parseHelper.IsValidShortFilename(filename, allowWildcards);
        }

        /// <summary>
        /// Replaces the illegal filename characters to create a legal name.
        /// </summary>
        /// <param name="filename">Filename to make valid.</param>
        /// <param name="replace">Replacement string for invalid characters in filename.</param>
        /// <returns>Valid filename.</returns>
        public static string MakeValidLongFileName(string filename, char replace)
        {
            if (String.IsNullOrEmpty(filename))
            {
                return filename;
            }

            StringBuilder sb = null;

            var found = filename.IndexOfAny(Common.IllegalLongFilenameCharacters);
            while (found != -1)
            {
                if (sb == null)
                {
                    sb = new StringBuilder(filename);
                }

                sb[found] = replace;

                found = (found + 1 < filename.Length) ? filename.IndexOfAny(Common.IllegalLongFilenameCharacters, found + 1) : -1;
            }

            return sb?.ToString() ?? filename;
        }

        /// <summary>
        /// Verifies the given string is a valid product version.
        /// </summary>
        /// <param name="version">The product version to verify.</param>
        /// <returns>True if version is a valid product version</returns>
        public static bool IsValidProductVersion(string version)
        {
            return Common.IsValidBinderVariable(version) || Common.IsValidMsiProductVersion(version);
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
        public void CreateGroupAndOrderingRows(SourceLineNumber sourceLineNumbers,
            ComplexReferenceParentType parentType, string parentId,
            ComplexReferenceChildType type, string id,
            ComplexReferenceChildType previousType, string previousId)
        {
            if (this.EncounteredError)
            {
                return;
            }

            if (parentType != ComplexReferenceParentType.Unknown && parentId != null)
            {
                this.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, type, id);
            }

            if (previousType != ComplexReferenceChildType.Unknown && previousId != null)
            {
                // TODO: Should we define our own enum for this, just to ensure there's no "cross-contamination"?
                // TODO: Also, we could potentially include an 'Attributes' field to track things like
                // 'before' vs. 'after', and explicit vs. inferred dependencies.
                this.AddSymbol(new WixOrderingSymbol(sourceLineNumbers)
                {
                    ItemType = type,
                    ItemIdRef = id,
                    DependsOnType = previousType,
                    DependsOnIdRef = previousId,
                });
            }
        }

        /// <summary>
        /// Creates a version 3 name-based UUID.
        /// </summary>
        /// <param name="namespaceGuid">The namespace UUID.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generated GUID for the given namespace and value.</returns>
        public string CreateGuid(Guid namespaceGuid, string value)
        {
            return this.parseHelper.CreateGuid(namespaceGuid, value);
        }

        /// <summary>
        /// Creates directories using the inline directory syntax.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="parentId">Optional identifier of parent directory.</param>
        /// <param name="inlineSyntax">Optional inline syntax to override attribute's value.</param>
        /// <returns>Identifier of the leaf directory created.</returns>
        public string CreateDirectoryReferenceFromInlineSyntax(SourceLineNumber sourceLineNumbers, string parentId, string inlineSyntax = null)
        {
            return this.parseHelper.CreateDirectoryReferenceFromInlineSyntax(this.ActiveSection, sourceLineNumbers, attribute: null, parentId, inlineSyntax, this.activeSectionCachedInlinedDirectoryIds);
        }

        /// <summary>
        /// Creates a Registry row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of the current row.</param>
        /// <param name="root">The registry entry root.</param>
        /// <param name="key">The registry entry key.</param>
        /// <param name="name">The registry entry name.</param>
        /// <param name="value">The registry entry value.</param>
        /// <param name="componentId">The component which will control installation/uninstallation of the registry entry.</param>
        public Identifier CreateRegistryStringSymbol(SourceLineNumber sourceLineNumbers, RegistryRootType root, string key, string name, string value, string componentId)
        {
            return this.parseHelper.CreateRegistrySymbol(this.ActiveSection, sourceLineNumbers, root, key, name, value, componentId);
        }

        /// <summary>
        /// Create a WixSimpleReferenceSymbol in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="symbolName">The symbol name of the simple reference.</param>
        /// <param name="primaryKey">The primary key of the simple reference.</param>
        public void CreateSimpleReference(SourceLineNumber sourceLineNumbers, string symbolName, string primaryKey)
        {
            if (!this.EncounteredError)
            {
                var id = String.Concat(symbolName, ":", primaryKey);

                // If this simple reference hasn't been added to the active section already, add it.
                if (this.activeSectionSimpleReferences.Add(id))
                {
                    this.parseHelper.CreateSimpleReference(this.ActiveSection, sourceLineNumbers, symbolName, primaryKey);
                }
            }
        }

        /// <summary>
        /// Create a WixSimpleReferenceSymbol in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="symbolName">The symbol name of the simple reference.</param>
        /// <param name="primaryKeys">The primary keys of the simple reference.</param>
        public void CreateSimpleReference(SourceLineNumber sourceLineNumbers, string symbolName, params string[] primaryKeys)
        {
            if (!this.EncounteredError)
            {
                var joinedKeys = String.Join("/", primaryKeys);
                var id = String.Concat(symbolName, ":", joinedKeys);

                // If this simple reference hasn't been added to the active section already, add it.
                if (this.activeSectionSimpleReferences.Add(id))
                {
                    this.parseHelper.CreateSimpleReference(this.ActiveSection, sourceLineNumbers, symbolName, primaryKeys);
                }
            }
        }

        /// <summary>
        /// Create a WixSimpleReferenceSymbol in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="symbolDefinition">The symbol definition of the simple reference.</param>
        /// <param name="primaryKey">The primary key of the simple reference.</param>
        public void CreateSimpleReference(SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition, string primaryKey)
        {
            this.CreateSimpleReference(sourceLineNumbers, symbolDefinition.Name, primaryKey);
        }

        /// <summary>
        /// Create a WixSimpleReferenceSymbol in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="symbolDefinition">The symbol definition of the simple reference.</param>
        /// <param name="primaryKeys">The primary keys of the simple reference.</param>
        public void CreateSimpleReference(SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition, params string[] primaryKeys)
        {
            this.CreateSimpleReference(sourceLineNumbers, symbolDefinition.Name, primaryKeys);
        }

        /// <summary>
        /// A row in the WixGroup table is added for this child node and its parent node.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="parentType">Type of child's complex reference parent.</param>
        /// <param name="parentId">Id of the parenet node.</param>
        /// <param name="childType">Complex reference type of child</param>
        /// <param name="childId">Id of the Child Node.</param>
        public void CreateWixGroupRow(SourceLineNumber sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType childType, string childId)
        {
            if (!this.EncounteredError)
            {
                this.parseHelper.CreateWixGroupSymbol(this.ActiveSection, sourceLineNumbers, parentType, parentId, childType, childId);
            }
        }

        /// <summary>
        /// Add the appropriate symbols to make sure that the given table shows up
        /// in the resulting output.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="tableName">Name of the table to ensure existance of.</param>
        public void EnsureTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            if (!this.EncounteredError)
            {
                this.parseHelper.EnsureTable(this.ActiveSection, sourceLineNumbers, tableName);
            }
        }

        /// <summary>
        /// Add the appropriate symbols to make sure that the given table shows up
        /// in the resulting output.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="tableDefinition">Definition of the table to ensure existance of.</param>
        public void EnsureTable(SourceLineNumber sourceLineNumbers, TableDefinition tableDefinition)
        {
            if (!this.EncounteredError)
            {
                this.parseHelper.EnsureTable(this.ActiveSection, sourceLineNumbers, tableDefinition);
            }
        }

        /// <summary>
        /// Get an attribute value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="emptyRule">A rule for the contents of the value. If the contents do not follow the rule, an error is thrown.</param>
        /// <returns>The attribute's value.</returns>
        public string GetAttributeValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, EmptyRule emptyRule = EmptyRule.CanBeWhitespaceOnly)
        {
            return this.parseHelper.GetAttributeValue(sourceLineNumbers, attribute, emptyRule);
        }

        /// <summary>
        /// Get a valid code page by web name or number from a string attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>A valid code page integer value.</returns>
        public int GetAttributeCodePageValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            try
            {
                return Common.GetValidCodePage(value);
            }
            catch (NotSupportedException)
            {
                this.Write(ErrorMessages.IllegalCodepageAttribute(sourceLineNumbers, value, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }

            return CompilerConstants.IllegalInteger;
        }

        /// <summary>
        /// Get a valid code page by web name or number from a string attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="onlyAnsi">Whether to allow Unicode (UCS) or UTF code pages.</param>
        /// <returns>A valid code page integer value or variable expression.</returns>
        public string GetAttributeLocalizableCodePageValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool onlyAnsi = false)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            // Allow for localization of code page names and values.
            if (this.IsValidLocIdentifier(value))
            {
                return value;
            }

            try
            {
                var codePage = Common.GetValidCodePage(value, false, onlyAnsi, sourceLineNumbers);
                return codePage.ToString(CultureInfo.InvariantCulture);
            }
            catch (NotSupportedException)
            {
                // Not a valid windows code page.
                this.messaging.Write(ErrorMessages.IllegalCodepageAttribute(sourceLineNumbers, value, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }
            catch (WixException e)
            {
                this.messaging.Write(e.Error);
            }

            return null;
        }

        /// <summary>
        /// Get an integer attribute value and displays an error for an illegal integer value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's integer value or a special value if an error occurred during conversion.</returns>
        public int GetAttributeIntegerValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, int minimum, int maximum)
        {
            return this.parseHelper.GetAttributeIntegerValue(sourceLineNumbers, attribute, minimum, maximum);
        }

        /// <summary>
        /// Get an integer attribute value and displays an error for an illegal integer value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's integer value or null if an error occurred during conversion.</returns>
        public int? GetAttributeRawIntegerValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return Common.GetAttributeRawIntegerValue(this.messaging, sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Get a long integral attribute value and displays an error for an illegal long value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's long value or a special value if an error occurred during conversion.</returns>
        public long GetAttributeLongValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, long minimum, long maximum)
        {
            return this.parseHelper.GetAttributeLongValue(sourceLineNumbers, attribute, minimum, maximum);
        }

        /// <summary>
        /// Get a date time attribute value and display errors for illegal values.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>Int representation of the date time.</returns>
        public int GetAttributeDateTimeValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                try
                {
                    DateTime date = DateTime.Parse(value, CultureInfo.InvariantCulture.DateTimeFormat);

                    return ((((date.Year - 1980) * 512) + (date.Month * 32 + date.Day)) * 65536) +
                        (date.Hour * 2048) + (date.Minute * 32) + (date.Second / 2);
                }
                catch (ArgumentOutOfRangeException)
                {
                    this.Write(ErrorMessages.InvalidDateTimeFormat(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                catch (FormatException)
                {
                    this.Write(ErrorMessages.InvalidDateTimeFormat(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                catch (OverflowException)
                {
                    this.Write(ErrorMessages.InvalidDateTimeFormat(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return CompilerConstants.IllegalInteger;
        }

        /// <summary>
        /// Get an integer attribute value or localize variable and displays an error for
        /// an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's integer value or localize variable as a string or a special value if an error occurred during conversion.</returns>
        public string GetAttributeLocalizableIntegerValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, int minimum, int maximum)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            Debug.Assert(minimum > CompilerConstants.IntegerNotSet && minimum > CompilerConstants.IllegalInteger, "The legal values for this attribute collide with at least one sentinel used during parsing.");

            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (this.IsValidLocIdentifier(value) || Common.IsValidBinderVariable(value))
                {
                    return value;
                }
                else
                {
                    try
                    {
                        var integer = Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat);

                        if (CompilerConstants.IntegerNotSet == integer || CompilerConstants.IllegalInteger == integer)
                        {
                            this.Write(ErrorMessages.IntegralValueSentinelCollision(sourceLineNumbers, integer));
                        }
                        else if (minimum > integer || maximum < integer)
                        {
                            this.Write(ErrorMessages.IntegralValueOutOfRange(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, integer, minimum, maximum));
                            integer = CompilerConstants.IllegalInteger;
                        }

                        return value;
                    }
                    catch (FormatException)
                    {
                        this.Write(ErrorMessages.IllegalIntegerValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
                    catch (OverflowException)
                    {
                        this.Write(ErrorMessages.IllegalIntegerValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get a guid attribute value and displays an error for an illegal guid value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="generatable">Determines whether the guid can be automatically generated.</param>
        /// <param name="canBeEmpty">If true, no error is raised on empty value. If false, an error is raised.</param>
        /// <returns>The attribute's guid value or a special value if an error occurred.</returns>
        public string GetAttributeGuidValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool generatable = false, bool canBeEmpty = false)
        {
            return this.parseHelper.GetAttributeGuidValue(sourceLineNumbers, attribute, generatable, canBeEmpty);
        }

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        public Identifier GetAttributeIdentifier(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return this.parseHelper.GetAttributeIdentifier(sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        public string GetAttributeIdentifierValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return this.parseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Gets a yes/no value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        public YesNoType GetAttributeYesNoValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return this.parseHelper.GetAttributeYesNoValue(sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Gets a yes/no/default value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoDefaultType value.</returns>
        public YesNoDefaultType GetAttributeYesNoDefaultValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return this.parseHelper.GetAttributeYesNoDefaultValue(sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Gets a short filename value and displays an error for an illegal short filename value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>The attribute's short filename value.</returns>
        public string GetAttributeShortFilename(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowWildcards = false)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (!this.parseHelper.IsValidShortFilename(value, allowWildcards)
                    && !Common.ContainsValidBinderVariable(value)
                    && !this.IsValidLocIdentifier(value))
                {
                    this.Write(ErrorMessages.IllegalShortFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                else if (CompilerCore.IsAmbiguousFilename(value))
                {
                    this.Write(WarningMessages.AmbiguousFileOrDirectoryName(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return value;
        }

        /// <summary>
        /// Gets a long filename value and displays an error for an illegal long filename value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="allowRelative">true if relative paths are allowed in the filename.</param>
        /// <returns>The attribute's long filename value.</returns>
        public string GetAttributeLongFilename(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowWildcards = false, bool allowRelative = false)
        {
            return this.parseHelper.GetAttributeLongFilename(sourceLineNumbers, attribute, allowWildcards, allowRelative);
        }

        /// <summary>
        /// Gets a version value or possibly a binder variable and displays an error for an illegal version value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's version value.</returns>
        public string GetAttributeVersionValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return this.parseHelper.GetAttributeVersionValue(sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Gets a RegistryRoot as a MsiInterop.MsidbRegistryRoot value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowHkmu">Whether HKMU is returned as -1 (true), or treated as an error (false).</param>
        /// <returns>The attribute's RegisitryRootType value.</returns>
        public RegistryRootType? GetAttributeRegistryRootValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowHkmu)
        {
            return this.parseHelper.GetAttributeRegistryRootValue(sourceLineNumbers, attribute, allowHkmu);
        }

        /// <summary>
        /// Gets a bundle variable value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's value.</returns>
        public Identifier GetAttributeBundleVariableNameIdentifier(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return this.parseHelper.GetAttributeBundleVariableNameIdentifier(sourceLineNumbers, attribute);
        }

        public string GetAttributeBundleVariableNameValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return this.parseHelper.GetAttributeBundleVariableNameValue(sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Gets an MsiProperty name value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's value.</returns>
        public string GetAttributeMsiPropertyNameValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                this.bundleValidator.ValidateBundleMsiPropertyName(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value);
            }

            return value;
        }

        /// <summary>
        /// Checks if the string contains a property (i.e. "foo[Property]bar")
        /// </summary>
        /// <param name="possibleProperty">String to evaluate for properties.</param>
        /// <returns>True if a property is found in the string.</returns>
        public bool ContainsProperty(string possibleProperty)
        {
            return this.parseHelper.ContainsProperty(possibleProperty);
        }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        public Identifier CreateIdentifier(string prefix, params string[] args)
        {
            return this.parseHelper.CreateIdentifier(prefix, args);
        }

        /// <summary>
        /// Create an identifier based on passed file name
        /// </summary>
        /// <param name="filename">File name to generate identifer from</param>
        /// <returns></returns>
        public Identifier CreateIdentifierFromFilename(string filename)
        {
            return this.parseHelper.CreateIdentifierFromFilename(filename);
        }

        /// <summary>
        /// Attempts to use an extension to parse the attribute.
        /// </summary>
        /// <param name="element">Element containing attribute to be parsed.</param>
        /// <param name="attribute">Attribute to be parsed.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public void ParseExtensionAttribute(XElement element, XAttribute attribute, IDictionary<string, string> context = null)
        {
            this.parseHelper.ParseExtensionAttribute(this.extensions.Values, this.intermediate, this.ActiveSection, element, attribute, context);
        }

        /// <summary>
        /// Attempts to use an extension to parse the element.
        /// </summary>
        /// <param name="parentElement">Element containing element to be parsed.</param>
        /// <param name="element">Element to be parsed.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public void ParseExtensionElement(XElement parentElement, XElement element, IDictionary<string, string> context = null)
        {
            this.parseHelper.ParseExtensionElement(this.extensions.Values, this.intermediate, this.ActiveSection, parentElement, element, context);
        }

        /// <summary>
        /// Process all children of the element looking for extensions and erroring on the unexpected.
        /// </summary>
        /// <param name="element">Element to parse children.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public void ParseForExtensionElements(XElement element, IDictionary<string, string> context = null)
        {
            this.parseHelper.ParseForExtensionElements(this.extensions.Values, this.intermediate, this.ActiveSection, element, context);
        }

        /// <summary>
        /// Attempts to use an extension to parse the element, with support for setting component keypath.
        /// </summary>
        /// <param name="parentElement">Element containing element to be parsed.</param>
        /// <param name="element">Element to be parsed.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public IComponentKeyPath ParsePossibleKeyPathExtensionElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            return this.parseHelper.ParsePossibleKeyPathExtensionElement(this.extensions.Values, this.intermediate, this.ActiveSection, parentElement, element, context);
        }

        /// <summary>
        /// Displays an unexpected attribute error if the attribute is not the namespace attribute.
        /// </summary>
        /// <param name="element">Element containing unexpected attribute.</param>
        /// <param name="attribute">The unexpected attribute.</param>
        public void UnexpectedAttribute(XElement element, XAttribute attribute)
        {
            this.parseHelper.UnexpectedAttribute(element, attribute);
        }

        /// <summary>
        /// Display an unexepected element error.
        /// </summary>
        /// <param name="parentElement">The parent element.</param>
        /// <param name="childElement">The unexpected child element.</param>
        public void UnexpectedElement(XElement parentElement, XElement childElement)
        {
            this.parseHelper.UnexpectedElement(parentElement, childElement);
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public void Write(Message message)
        {
            this.messaging.Write(message);
        }

        /// <summary>
        /// Verifies that the calling assembly version is equal to or newer than the given <paramref name="requiredVersion"/>.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="requiredVersion">The version required of the calling assembly.</param>
        internal void VerifyRequiredVersion(SourceLineNumber sourceLineNumbers, string requiredVersion)
        {
            // an null or empty string means any version will work
            if (!String.IsNullOrEmpty(requiredVersion))
            {
                Assembly caller = Assembly.GetCallingAssembly();
                AssemblyName name = caller.GetName();
                FileVersionInfo fv = FileVersionInfo.GetVersionInfo(caller.Location);

                Version versionRequired = new Version(requiredVersion);
                Version versionCurrent = new Version(fv.FileVersion);

                if (versionRequired > versionCurrent)
                {
                    if (this.GetType().Assembly.Equals(caller))
                    {
                        this.Write(ErrorMessages.InsufficientVersion(sourceLineNumbers, versionCurrent, versionRequired));
                    }
                    else
                    {
                        this.Write(ErrorMessages.InsufficientVersion(sourceLineNumbers, versionCurrent, versionRequired, name.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new section and makes it the active section in the core.
        /// </summary>
        /// <param name="id">Unique identifier for the section.</param>
        /// <param name="type">Type of section to create.</param>
        /// <param name="compilationId">Unique identifier for the compilation.</param>
        /// <returns>New section.</returns>
        internal IntermediateSection CreateActiveSection(string id, SectionType type, string compilationId)
        {
            this.ActiveSection = this.CreateSection(id, type, compilationId);

            this.activeSectionCachedInlinedDirectoryIds = new Dictionary<string, string>();
            this.activeSectionSimpleReferences = new HashSet<string>();

            return this.ActiveSection;
        }

        /// <summary>
        /// Creates a new section.
        /// </summary>
        /// <param name="id">Unique identifier for the section.</param>
        /// <param name="type">Type of section to create.</param>
        /// <param name="compilationId">Unique identifier for the compilation.</param>
        /// <returns>New section.</returns>
        internal IntermediateSection CreateSection(string id, SectionType type, string compilationId)
        {
            var section = new IntermediateSection(id, type, compilationId);

            this.intermediate.AddSection(section);

            return section;
        }

        /// <summary>
        /// Creates WixComplexReference and WixGroup rows in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="parentType">The parent type.</param>
        /// <param name="parentId">The parent id.</param>
        /// <param name="parentLanguage">The parent language.</param>
        /// <param name="childType">The child type.</param>
        /// <param name="childId">The child id.</param>
        /// <param name="isPrimary">Whether the child is primary.</param>
        public void CreateComplexReference(SourceLineNumber sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, string parentLanguage, ComplexReferenceChildType childType, string childId, bool isPrimary)
        {
            this.parseHelper.CreateComplexReference(this.ActiveSection, sourceLineNumbers, parentType, parentId, parentLanguage, childType, childId, isPrimary);
        }

        /// <summary>
        /// Creates a directory row from a name.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="id">Optional identifier for the new row.</param>
        /// <param name="parentId">Optional identifier for the parent row.</param>
        /// <param name="name">Long name of the directory.</param>
        /// <param name="shortName">Optional short name of the directory.</param>
        /// <param name="sourceName">Optional source name for the directory.</param>
        /// <param name="shortSourceName">Optional short source name for the directory.</param>
        /// <returns>Identifier for the newly created row.</returns>
        internal Identifier CreateDirectorySymbol(SourceLineNumber sourceLineNumbers, Identifier id, string parentId, string name, string shortName = null, string sourceName = null, string shortSourceName = null)
        {
            return this.parseHelper.CreateDirectorySymbol(this.ActiveSection, sourceLineNumbers, id, parentId, name, shortName, sourceName, shortSourceName);
        }

        public void CreateWixSearchSymbol(SourceLineNumber sourceLineNumbers, string elementName, Identifier id, string variable, string condition, string after)
        {
            this.parseHelper.CreateWixSearchSymbol(this.ActiveSection, sourceLineNumbers, elementName, id, variable, condition, after, null);
        }

        internal WixActionSymbol ScheduleActionSymbol(SourceLineNumber sourceLineNumbers, AccessModifier access, SequenceTable sequence, string actionName, string condition = null, string beforeAction = null, string afterAction = null, bool overridable = false)
        {
            return this.parseHelper.ScheduleActionSymbol(this.ActiveSection, sourceLineNumbers, access, sequence, actionName, condition, beforeAction, afterAction, overridable);
        }
    }
}
