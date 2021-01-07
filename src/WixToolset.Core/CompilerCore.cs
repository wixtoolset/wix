// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
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

    internal enum ValueListKind
    {
        /// <summary>
        /// A list of values with nothing before the final value.
        /// </summary>
        None,

        /// <summary>
        /// A list of values with 'and' before the final value.
        /// </summary>
        And,

        /// <summary>
        /// A list of values with 'or' before the final value.
        /// </summary>
        Or
    }

    /// <summary>
    /// Core class for the compiler.
    /// </summary>
    internal class CompilerCore
    {
        internal static readonly XNamespace W3SchemaPrefix = "http://www.w3.org/";
        internal static readonly XNamespace WixNamespace = "http://wixtoolset.org/schemas/v4/wxs";

        // Built-in variables (from burn\engine\variable.cpp, "vrgBuiltInVariables", around line 113)
        private static readonly List<string> BuiltinBundleVariables = new List<string>(
            new string[] {
                "AdminToolsFolder",
                "AppDataFolder",
                "CommonAppDataFolder",
                "CommonFiles64Folder",
                "CommonFilesFolder",
                "CompatibilityMode",
                "Date",
                "DesktopFolder",
                "FavoritesFolder",
                "FontsFolder",
                "InstallerName",
                "InstallerVersion",
                "LocalAppDataFolder",
                "LogonUser",
                "MyPicturesFolder",
                "NTProductType",
                "NTSuiteBackOffice",
                "NTSuiteDataCenter",
                "NTSuiteEnterprise",
                "NTSuitePersonal",
                "NTSuiteSmallBusiness",
                "NTSuiteSmallBusinessRestricted",
                "NTSuiteWebServer",
                "PersonalFolder",
                "Privileged",
                "ProgramFiles64Folder",
                "ProgramFiles6432Folder",
                "ProgramFilesFolder",
                "ProgramMenuFolder",
                "RebootPending",
                "SendToFolder",
                "ServicePackLevel",
                "StartMenuFolder",
                "StartupFolder",
                "System64Folder",
                "SystemFolder",
                "TempFolder",
                "TemplateFolder",
                "TerminalServer",
                "UserLanguageID",
                "UserUILanguageID",
                "VersionMsi",
                "VersionNT",
                "VersionNT64",
                "WindowsFolder",
                "WindowsVolume",
                "WixBundleAction",
                "WixBundleForcedRestartPackage",
                "WixBundleElevated",
                "WixBundleInstalled",
                "WixBundleProviderKey",
                "WixBundleTag",
                "WixBundleVersion",
            });

        private static readonly List<string> DisallowedMsiProperties = new List<string>(
            new string[] {
                "ACTION",
                "ADDLOCAL",
                "ADDSOURCE",
                "ADDDEFAULT",
                "ADVERTISE",
                "ALLUSERS",
                "REBOOT",
                "REINSTALL",
                "REINSTALLMODE",
                "REMOVE"
            });

        private readonly Dictionary<XNamespace, ICompilerExtension> extensions;
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
        /// <param name="parseHelper"></param>
        /// <param name="extensions">The WiX extensions collection.</param>
        internal CompilerCore(Intermediate intermediate, IMessaging messaging, IParseHelper parseHelper, Dictionary<XNamespace, ICompilerExtension> extensions)
        {
            this.extensions = extensions;
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
        /// Creates a short file/directory name using an identifier and long file/directory name as input.
        /// </summary>
        /// <param name="longName">The long file/directory name.</param>
        /// <param name="keepExtension">The option to keep the extension on generated short names.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="args">Any additional information to include in the hash for the generated short name.</param>
        /// <returns>The generated 8.3-compliant short file/directory name.</returns>
        [Obsolete]
        public string CreateShortName(string longName, bool keepExtension, bool allowWildcards, params string[] args)
        {
            return this.parseHelper.CreateShortName(longName, keepExtension, allowWildcards, args);
        }

        /// <summary>
        /// Verifies the given string is a valid product version.
        /// </summary>
        /// <param name="version">The product version to verify.</param>
        /// <returns>True if version is a valid product version</returns>
        public static bool IsValidProductVersion(string version)
        {
            if (!Common.IsValidBinderVariable(version))
            {
                Version ver = new Version(version);

                if (255 < ver.Major || 255 < ver.Minor || 65535 < ver.Build)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifies the given string is a valid module or bundle version.
        /// </summary>
        /// <param name="version">The version to verify.</param>
        /// <returns>True if version is a valid module or bundle version.</returns>
        public static bool IsValidModuleOrBundleVersion(string version)
        {
            return Common.IsValidModuleOrBundleVersion(version);
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
        /// <param name="attribute">Attribute containing the inline syntax.</param>
        /// <param name="parentId">Optional identifier of parent directory.</param>
        /// <param name="inlineSyntax">Optional inline syntax to override attribute's value.</param>
        /// <returns>Identifier of the leaf directory created.</returns>
        public string CreateDirectoryReferenceFromInlineSyntax(SourceLineNumber sourceLineNumbers, XAttribute attribute, string parentId, string inlineSyntax = null)
        {
            return this.parseHelper.CreateDirectoryReferenceFromInlineSyntax(this.ActiveSection, sourceLineNumbers, attribute, parentId, inlineSyntax, this.activeSectionCachedInlinedDirectoryIds);
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
        public Identifier CreateRegistryRow(SourceLineNumber sourceLineNumbers, RegistryRootType root, string key, string name, string value, string componentId)
        {
            return this.parseHelper.CreateRegistrySymbol(this.ActiveSection, sourceLineNumbers, root, key, name, value, componentId, true);
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
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            try
            {
                int codePage = Common.GetValidCodePage(value);
                return codePage;
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
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            // allow for localization of code page names and values
            if (this.IsValidLocIdentifier(value))
            {
                return value;
            }

            try
            {
                int codePage = Common.GetValidCodePage(value, false, onlyAnsi, sourceLineNumbers);
                return codePage.ToString(CultureInfo.InvariantCulture);
            }
            catch (NotSupportedException)
            {
                // not a valid windows code page
                this.Write(ErrorMessages.IllegalCodepageAttribute(sourceLineNumbers, value, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
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

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

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
                        int integer = Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat);

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
        public string GetAttributeShortFilename(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowWildcards)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (!this.IsValidShortFilename(value, allowWildcards) && !Common.ContainsValidBinderVariable(value))
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
        /// Gets a Bundle variable value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's value.</returns>
        public string GetAttributeBundleVariableValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(value))
            {
                if (CompilerCore.BuiltinBundleVariables.Contains(value))
                {
                    string illegalValues = CompilerCore.CreateValueList(ValueListKind.Or, CompilerCore.BuiltinBundleVariables);
                    this.Write(ErrorMessages.IllegalAttributeValueWithIllegalList(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, illegalValues));
                }
            }

            return value;
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
                if (CompilerCore.DisallowedMsiProperties.Contains(value))
                {
                    string illegalValues = CompilerCore.CreateValueList(ValueListKind.Or, CompilerCore.DisallowedMsiProperties);
                    this.Write(ErrorMessages.DisallowedMsiProperty(sourceLineNumbers, value, illegalValues));
                }
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
        public void ParseForExtensionElements(XElement element)
        {
            this.parseHelper.ParseForExtensionElements(this.extensions.Values, this.intermediate, this.ActiveSection, element);
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
        /// Adds inline directory syntax generated identifier.
        /// </summary>
        /// <param name="inlineSyntax">Inline directory syntax the identifier was generated.</param>
        /// <param name="id">Generated identifier for inline syntax.</param>
        internal void AddInlineDirectoryId(string inlineSyntax, string id)
        {
            this.activeSectionCachedInlinedDirectoryIds.Add(inlineSyntax, id);
        }

        /// <summary>
        /// Creates a new section and makes it the active section in the core.
        /// </summary>
        /// <param name="id">Unique identifier for the section.</param>
        /// <param name="type">Type of section to create.</param>
        /// <param name="codepage">Codepage for the resulting database for this ection.</param>
        /// <param name="compilationId"></param>
        /// <returns>New section.</returns>
        internal IntermediateSection CreateActiveSection(string id, SectionType type, int codepage, string compilationId)
        {
            this.ActiveSection = this.CreateSection(id, type, codepage, compilationId);

            this.activeSectionCachedInlinedDirectoryIds = new Dictionary<string, string>();
            this.activeSectionSimpleReferences = new HashSet<string>();

            return this.ActiveSection;
        }

        /// <summary>
        /// Creates a new section.
        /// </summary>
        /// <param name="id">Unique identifier for the section.</param>
        /// <param name="type">Type of section to create.</param>
        /// <param name="codepage">Codepage for the resulting database for this ection.</param>
        /// <param name="compilationId"></param>
        /// <returns>New section.</returns>
        internal IntermediateSection CreateSection(string id, SectionType type, int codepage, string compilationId)
        {
            var section = new IntermediateSection(id, type, codepage);
            section.CompilationId = compilationId;

            this.intermediate.Sections.Add(section);

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

        internal void VerifyNoInnerText(SourceLineNumber sourceLineNumbers, XElement element)
        {
            var innerText = Common.GetInnerText(element);
            if (!String.IsNullOrWhiteSpace(innerText))
            {
                this.messaging.Write(ErrorMessages.IllegalInnerText(sourceLineNumbers, element.Name.LocalName, innerText));
            }
        }

        private static string CreateValueList(ValueListKind kind, IEnumerable<string> values)
        {
            // Ideally, we could denote the list kind (and the list itself) directly in the
            // message XML, and detect and expand in the MessageHandler.GenerateMessageString()
            // method.  Doing so would make vararg-style messages much easier, but impacts
            // every single message we format.  For now, callers just have to know when a
            // message takes a list of values in a single string argument, the caller will
            // have to do the expansion themselves.  (And, unfortunately, hard-code the knowledge
            // that the list is an 'and' or 'or' list.)

            // For a localizable solution, we need to be able to get the list format string
            // from resources. We aren't currently localized right now, so the values are
            // just hard-coded.
            const string valueFormat = "'{0}'";
            const string valueSeparator = ", ";
            string terminalTerm = String.Empty;

            switch (kind)
            {
                case ValueListKind.None:
                    terminalTerm = "";
                    break;
                case ValueListKind.And:
                    terminalTerm = "and ";
                    break;
                case ValueListKind.Or:
                    terminalTerm = "or ";
                    break;
            }

            StringBuilder list = new StringBuilder();

            // This weird construction helps us determine when we're adding the last value
            // to the list.  Instead of adding them as we encounter them, we cache the current
            // value and append the *previous* one.
            string previousValue = null;
            bool haveValues = false;
            foreach (string value in values)
            {
                if (null != previousValue)
                {
                    if (haveValues)
                    {
                        list.Append(valueSeparator);
                    }
                    list.AppendFormat(valueFormat, previousValue);
                    haveValues = true;
                }

                previousValue = value;
            }

            // If we have no previous value, that means that the list contained no values, and
            // something has gone very wrong.
            Debug.Assert(null != previousValue);
            if (null != previousValue)
            {
                if (haveValues)
                {
                    list.Append(valueSeparator);
                    list.Append(terminalTerm);
                }
                list.AppendFormat(valueFormat, previousValue);
                haveValues = true;
            }

            return list.ToString();
        }
    }
}
