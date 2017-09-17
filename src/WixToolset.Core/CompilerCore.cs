// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;
    using Wix = WixToolset.Data.Serialize;

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
    internal sealed class CompilerCore : ICompilerCore
    {
        internal static readonly XNamespace W3SchemaPrefix = "http://www.w3.org/";
        internal static readonly XNamespace WixNamespace = "http://wixtoolset.org/schemas/v4/wxs";

        public const int DefaultMaximumUncompressedMediaSize = 200; // Default value is 200 MB
        public const int MinValueOfMaxCabSizeForLargeFileSplitting = 20; // 20 MB
        public const int MaxValueOfMaxCabSizeForLargeFileSplitting = 2 * 1024; // 2048 MB (i.e. 2 GB)

        private static readonly Regex AmbiguousFilename = new Regex(@"^.{6}\~\d", RegexOptions.Compiled);

        private const string IllegalLongFilenameCharacters = @"[\\\?|><:/\*""]"; // illegal: \ ? | > < : / * "
        private static readonly Regex IllegalLongFilename = new Regex(IllegalLongFilenameCharacters, RegexOptions.Compiled);

        private const string LegalLongFilenameCharacters = @"[^\\\?|><:/\*""]";  // opposite of illegal above.
        private static readonly Regex LegalLongFilename = new Regex(String.Concat("^", LegalLongFilenameCharacters, @"{1,259}$"), RegexOptions.Compiled);

        private const string LegalRelativeLongFilenameCharacters = @"[^\?|><:/\*""]"; // (like legal long, but we allow '\') illegal: ? | > < : / * "
        private static readonly Regex LegalRelativeLongFilename = new Regex(String.Concat("^", LegalRelativeLongFilenameCharacters, @"{1,259}$"), RegexOptions.Compiled);

        private const string LegalWildcardLongFilenameCharacters = @"[^\\|><:/""]"; // illegal: \ | > < : / "
        private static readonly Regex LegalWildcardLongFilename = new Regex(String.Concat("^", LegalWildcardLongFilenameCharacters, @"{1,259}$"));

        private static readonly Regex PutGuidHere = new Regex(@"PUT\-GUID\-(?:\d+\-)?HERE", RegexOptions.Singleline);

        private static readonly Regex LegalIdentifierWithAccess = new Regex(@"^((?<access>public|internal|protected|private)\s+)?(?<id>[_A-Za-z][0-9A-Za-z_\.]*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        // Built-in variables (from burn\engine\variable.cpp, "vrgBuiltInVariables", around line 113)
        private static readonly List<String> BuiltinBundleVariables = new List<string>(
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

        private TableDefinitionCollection tableDefinitions;
        private Dictionary<XNamespace, ICompilerExtension> extensions;
        private Intermediate intermediate;
        private bool showPedanticMessages;

        private HashSet<string> activeSectionInlinedDirectoryIds;
        private HashSet<string> activeSectionSimpleReferences;

        /// <summary>
        /// Constructor for all compiler core.
        /// </summary>
        /// <param name="intermediate">The Intermediate object representing compiled source document.</param>
        /// <param name="tableDefinitions">The loaded table definition collection.</param>
        /// <param name="extensions">The WiX extensions collection.</param>
        internal CompilerCore(Intermediate intermediate, TableDefinitionCollection tableDefinitions, Dictionary<XNamespace, ICompilerExtension> extensions)
        {
            this.tableDefinitions = tableDefinitions;
            this.extensions = extensions;
            this.intermediate = intermediate;
        }

        /// <summary>
        /// Gets the section the compiler is currently emitting symbols into.
        /// </summary>
        /// <value>The section the compiler is currently emitting symbols into.</value>
        public Section ActiveSection { get; private set; }

        /// <summary>
        /// Gets or sets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        public Platform CurrentPlatform { get; set; }

        /// <summary>
        /// Gets whether the compiler core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
        }

        /// <summary>
        /// Gets or sets the option to show pedantic messages.
        /// </summary>
        /// <value>The option to show pedantic messages.</value>
        public bool ShowPedanticMessages
        {
            get { return this.showPedanticMessages; }
            set { this.showPedanticMessages = value; }
        }

        /// <summary>
        /// Gets the table definitions used by the compiler core.
        /// </summary>
        /// <value>Table definition collection.</value>
        public TableDefinitionCollection TableDefinitions
        {
            get { return this.tableDefinitions; }
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
            if (null == filename || 0 == filename.Length)
            {
                return false;
            }

            return CompilerCore.AmbiguousFilename.IsMatch(filename);
        }

        /// <summary>
        /// Verifies that a value is a legal identifier.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <returns>true if the value is an identifier; false otherwise.</returns>
        public bool IsValidIdentifier(string value)
        {
            return Common.IsIdentifier(value);
        }

        /// <summary>
        /// Verifies if an identifier is a valid loc identifier.
        /// </summary>
        /// <param name="identifier">Identifier to verify.</param>
        /// <returns>True if the identifier is a valid loc identifier.</returns>
        public bool IsValidLocIdentifier(string identifier)
        {
            if (String.IsNullOrEmpty(identifier))
            {
                return false;
            }

            Match match = Common.WixVariableRegex.Match(identifier);

            return (match.Success && "loc" == match.Groups["namespace"].Value && 0 == match.Index && identifier.Length == match.Length);
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
            if (String.IsNullOrEmpty(filename))
            {
                return false;
            }

            // check for a non-period character (all periods is not legal)
            bool nonPeriodFound = false;
            foreach (char character in filename)
            {
                if ('.' != character)
                {
                    nonPeriodFound = true;
                    break;
                }
            }

            if (allowWildcards)
            {
                return (nonPeriodFound && CompilerCore.LegalWildcardLongFilename.IsMatch(filename));
            }
            else if (allowRelative)
            {
                return (nonPeriodFound && CompilerCore.LegalRelativeLongFilename.IsMatch(filename));
            }
            else
            {
                return (nonPeriodFound && CompilerCore.LegalLongFilename.IsMatch(filename));
            }
        }

        /// <summary>
        /// Verifies if a filename is a valid short filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>True if the filename is a valid short filename</returns>
        public bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            return Common.IsValidShortFilename(filename, allowWildcards);
        }

        /// <summary>
        /// Replaces the illegal filename characters to create a legal name.
        /// </summary>
        /// <param name="filename">Filename to make valid.</param>
        /// <param name="replace">Replacement string for invalid characters in filename.</param>
        /// <returns>Valid filename.</returns>
        public static string MakeValidLongFileName(string filename, string replace)
        {
            return CompilerCore.IllegalLongFilename.Replace(filename, replace);
        }

        /// <summary>
        /// Creates a short file/directory name using an identifier and long file/directory name as input.
        /// </summary>
        /// <param name="longName">The long file/directory name.</param>
        /// <param name="keepExtension">The option to keep the extension on generated short names.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="args">Any additional information to include in the hash for the generated short name.</param>
        /// <returns>The generated 8.3-compliant short file/directory name.</returns>
        public string CreateShortName(string longName, bool keepExtension, bool allowWildcards, params string[] args)
        {
            // canonicalize the long name if its not a localization identifier (they are case-sensitive)
            if (!this.IsValidLocIdentifier(longName))
            {
                longName = longName.ToLowerInvariant();
            }

            // collect all the data
            List<string> strings = new List<string>(1 + args.Length);
            strings.Add(longName);
            strings.AddRange(args);

            // prepare for hashing
            string stringData = String.Join("|", strings);
            byte[] data = Encoding.UTF8.GetBytes(stringData);

            // hash the data
            byte[] hash;
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                hash = sha1.ComputeHash(data);
            }

            // generate the short file/directory name without an extension
            StringBuilder shortName = new StringBuilder(Convert.ToBase64String(hash));
            shortName.Remove(8, shortName.Length - 8).Replace('+', '-').Replace('/', '_');

            if (keepExtension)
            {
                string extension = Path.GetExtension(longName);

                if (4 < extension.Length)
                {
                    extension = extension.Substring(0, 4);
                }

                shortName.Append(extension);

                // check the generated short name to ensure its still legal (the extension may not be legal)
                if (!this.IsValidShortFilename(shortName.ToString(), allowWildcards))
                {
                    // remove the extension (by truncating the generated file name back to the generated characters)
                    shortName.Length -= extension.Length;
                }
            }

            return shortName.ToString().ToLowerInvariant();
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
        /// Get an element's inner text and trims any extra whitespace.
        /// </summary>
        /// <param name="element">The element with inner text to be trimmed.</param>
        /// <returns>The node's inner text trimmed.</returns>
        public string GetTrimmedInnerText(XElement element)
        {
            string value = Common.GetInnerText(element);
            return (null == value) ? null : value.Trim();
        }

        /// <summary>
        /// Gets element's inner text and ensure's it is safe for use in a condition by trimming any extra whitespace.
        /// </summary>
        /// <param name="element">The element to ensure inner text is a condition.</param>
        /// <returns>The value converted into a safe condition.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetConditionInnerText(XElement element)
        {
            string value = element.Value;
            if (0 < value.Length)
            {
                value = value.Trim();
                value = value.Replace('\t', ' ');
                value = value.Replace('\r', ' ');
                value = value.Replace('\n', ' ');
            }
            else // return null for a non-existant condition
            {
                value = null;
            }

            return value;
        }

        /// <summary>
        /// Creates a version 3 name-based UUID.
        /// </summary>
        /// <param name="namespaceGuid">The namespace UUID.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generated GUID for the given namespace and value.</returns>
        public string CreateGuid(Guid namespaceGuid, string value)
        {
            return Uuid.NewUuid(namespaceGuid, value).ToString("B").ToUpperInvariant();
        }

        /// <summary>
        /// Creates a row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        /// <param name="tableName">Name of table to create row in.</param>
        /// <returns>New row.</returns>
        public Row CreateRow(SourceLineNumber sourceLineNumbers, string tableName, Identifier identifier = null)
        {
            return this.CreateRow(sourceLineNumbers, tableName, this.ActiveSection, identifier);
        }

        /// <summary>
        /// Creates a row in the active given <paramref name="section"/>.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        /// <param name="tableName">Name of table to create row in.</param>
        /// <param name="section">The section to which the row is added. If null, the row is added to the active section.</param>
        /// <returns>New row.</returns>
        internal Row CreateRow(SourceLineNumber sourceLineNumbers, string tableName, Section section, Identifier identifier = null)
        {
            TableDefinition tableDefinition = this.tableDefinitions[tableName];
            Table table = section.EnsureTable(tableDefinition);
            Row row = table.CreateRow(sourceLineNumbers);

            if (null != identifier)
            {
                row.Access = identifier.Access;
                row[0] = identifier.Id;
            }

            return row;
        }

        /// <summary>
        /// Creates directories using the inline directory syntax.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="attribute">The attribute to parse.</param>
        /// <param name="parentId">Optional identifier of parent directory.</param>
        /// <returns>Identifier of the leaf directory created.</returns>
        public string CreateDirectoryReferenceFromInlineSyntax(SourceLineNumber sourceLineNumbers, XAttribute attribute, string parentId)
        {
            string id = null;
            string[] inlineSyntax = this.GetAttributeInlineDirectorySyntax(sourceLineNumbers, attribute, true);

            if (null != inlineSyntax)
            {
                // Special case the single entry in the inline syntax since it is the most common case
                // and needs no extra processing. It's just a reference to an existing directory.
                if (1 == inlineSyntax.Length)
                {
                    id = inlineSyntax[0];
                    this.CreateSimpleReference(sourceLineNumbers, "Directory", id);
                }
                else // start creating rows for the entries in the inline syntax
                {
                    id = parentId;

                    int pathStartsAt = 0;
                    if (inlineSyntax[0].EndsWith(":"))
                    {
                        // TODO: should overriding the parent identifier with a specific id be an error or a warning or just let it slide?
                        //if (null != parentId)
                        //{
                        //    this.core.OnMessage(WixErrors.Xxx(sourceLineNumbers));
                        //}

                        id = inlineSyntax[0].TrimEnd(':');
                        this.CreateSimpleReference(sourceLineNumbers, "Directory", id);

                        pathStartsAt = 1;
                    }

                    for (int i = pathStartsAt; i < inlineSyntax.Length; ++i)
                    {
                        Identifier inlineId = this.CreateDirectoryRow(sourceLineNumbers, null, id, inlineSyntax[i]);
                        id = inlineId.Id;
                    }
                }
            }

            return id;
        }

        /// <summary>
        /// Creates a patch resource reference to the list of resoures to be filtered when producing a patch. This method should only be used when processing children of a patch family.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        /// <param name="tableName">Name of table to create row in.</param>
        /// <param name="primaryKeys">Array of keys that make up the primary key of the table.</param>
        /// <returns>New row.</returns>
        public void CreatePatchFamilyChildReference(SourceLineNumber sourceLineNumbers, string tableName, params string[] primaryKeys)
        {
            Row patchReferenceRow = this.CreateRow(sourceLineNumbers, "WixPatchRef");
            patchReferenceRow[0] = tableName;
            patchReferenceRow[1] = String.Join("/", primaryKeys);
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
        /// <param name="escapeLeadingHash">If true, "escape" leading '#' characters so the value is written as a REG_SZ.</param>
        public Identifier CreateRegistryRow(SourceLineNumber sourceLineNumbers, int root, string key, string name, string value, string componentId, bool escapeLeadingHash)
        {
            Identifier id = null;

            if (!this.EncounteredError)
            {
                if (-1 > root || 3 < root)
                {
                    throw new ArgumentOutOfRangeException("root");
                }

                if (null == key)
                {
                    throw new ArgumentNullException("key");
                }

                if (null == componentId)
                {
                    throw new ArgumentNullException("componentId");
                }

                // escape the leading '#' character for string registry values
                if (escapeLeadingHash && null != value && value.StartsWith("#", StringComparison.Ordinal))
                {
                    value = String.Concat("#", value);
                }

                id = this.CreateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), key.ToLowerInvariant(), (null != name ? name.ToLowerInvariant() : name));
                Row row = this.CreateRow(sourceLineNumbers, "Registry", id);
                row[1] = root;
                row[2] = key;
                row[3] = name;
                row[4] = value;
                row[5] = componentId;
            }

            return id;
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
        public Identifier CreateRegistryRow(SourceLineNumber sourceLineNumbers, int root, string key, string name, string value, string componentId)
        {
            return this.CreateRegistryRow(sourceLineNumbers, root, key, name, value, componentId, true);
        }

        /// <summary>
        /// Create a WixSimpleReference row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="tableName">The table name of the simple reference.</param>
        /// <param name="primaryKeys">The primary keys of the simple reference.</param>
        public void CreateSimpleReference(SourceLineNumber sourceLineNumbers, string tableName, params string[] primaryKeys)
        {
            if (!this.EncounteredError)
            {
                string joinedKeys = String.Join("/", primaryKeys);
                string id = String.Concat(tableName, ":", joinedKeys);

                // If this simple reference hasn't been added to the active section already, add it.
                if (this.activeSectionSimpleReferences.Add(id))
                {
                    WixSimpleReferenceRow wixSimpleReferenceRow = (WixSimpleReferenceRow)this.CreateRow(sourceLineNumbers, "WixSimpleReference");
                    wixSimpleReferenceRow.TableName = tableName;
                    wixSimpleReferenceRow.PrimaryKeys = joinedKeys;
                }
            }
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
                if (null == parentId || ComplexReferenceParentType.Unknown == parentType)
                {
                    return;
                }

                if (null == childId)
                {
                    throw new ArgumentNullException("childId");
                }

                WixGroupRow WixGroupRow = (WixGroupRow)this.CreateRow(sourceLineNumbers, "WixGroup");
                WixGroupRow.ParentId = parentId;
                WixGroupRow.ParentType = parentType;
                WixGroupRow.ChildId = childId;
                WixGroupRow.ChildType = childType;
            }
        }

        /// <summary>
        /// Add the appropriate rows to make sure that the given table shows up
        /// in the resulting output.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="tableName">Name of the table to ensure existance of.</param>
        public void EnsureTable(SourceLineNumber sourceLineNumbers, string tableName)
        {
            if (!this.EncounteredError)
            {
                Row row = this.CreateRow(sourceLineNumbers, "WixEnsureTable");
                row[0] = tableName;

                // We don't add custom table definitions to the tableDefinitions collection,
                // so if it's not in there, it better be a custom table. If the Id is just wrong,
                // instead of a custom table, we get an unresolved reference at link time.
                if (!this.tableDefinitions.Contains(tableName))
                {
                    this.CreateSimpleReference(sourceLineNumbers, "WixCustomTable", tableName);
                }
            }
        }

        /// <summary>
        /// Get an attribute value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="emptyRule">A rule for the contents of the value. If the contents do not follow the rule, an error is thrown.</param>
        /// <returns>The attribute's value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, EmptyRule emptyRule = EmptyRule.CanBeWhitespaceOnly)
        {
            return Common.GetAttributeValue(sourceLineNumbers, attribute, emptyRule);
        }

        /// <summary>
        /// Get a valid code page by web name or number from a string attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>A valid code page integer value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
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
                this.OnMessage(WixErrors.IllegalCodepageAttribute(sourceLineNumbers, value, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }

            return CompilerConstants.IllegalInteger;
        }

        /// <summary>
        /// Get a valid code page by web name or number from a string attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="onlyAscii">Whether to allow Unicode (UCS) or UTF code pages.</param>
        /// <returns>A valid code page integer value or variable expression.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeLocalizableCodePageValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool onlyAnsi = false)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            // allow for localization of code page names and values
            if (IsValidLocIdentifier(value))
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
                this.OnMessage(WixErrors.IllegalCodepageAttribute(sourceLineNumbers, value, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }
            catch (WixException e)
            {
                this.OnMessage(e.Error);
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
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public int GetAttributeIntegerValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, int minimum, int maximum)
        {
            return Common.GetAttributeIntegerValue(sourceLineNumbers, attribute, minimum, maximum);
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
            Debug.Assert(minimum > CompilerConstants.LongNotSet && minimum > CompilerConstants.IllegalLong, "The legal values for this attribute collide with at least one sentinel used during parsing.");

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                try
                {
                    long longValue = Convert.ToInt64(value, CultureInfo.InvariantCulture.NumberFormat);

                    if (CompilerConstants.LongNotSet == longValue || CompilerConstants.IllegalLong == longValue)
                    {
                        this.OnMessage(WixErrors.IntegralValueSentinelCollision(sourceLineNumbers, longValue));
                    }
                    else if (minimum > longValue || maximum < longValue)
                    {
                        this.OnMessage(WixErrors.IntegralValueOutOfRange(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, longValue, minimum, maximum));
                        longValue = CompilerConstants.IllegalLong;
                    }

                    return longValue;
                }
                catch (FormatException)
                {
                    this.OnMessage(WixErrors.IllegalLongValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                catch (OverflowException)
                {
                    this.OnMessage(WixErrors.IllegalLongValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return CompilerConstants.IllegalLong;
        }

        /// <summary>
        /// Get a date time attribute value and display errors for illegal values.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>Int representation of the date time.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
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
                    this.OnMessage(WixErrors.InvalidDateTimeFormat(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                catch (FormatException)
                {
                    this.OnMessage(WixErrors.InvalidDateTimeFormat(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                catch (OverflowException)
                {
                    this.OnMessage(WixErrors.InvalidDateTimeFormat(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
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
                if (IsValidLocIdentifier(value) || Common.IsValidBinderVariable(value))
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
                            this.OnMessage(WixErrors.IntegralValueSentinelCollision(sourceLineNumbers, integer));
                        }
                        else if (minimum > integer || maximum < integer)
                        {
                            this.OnMessage(WixErrors.IntegralValueOutOfRange(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, integer, minimum, maximum));
                            integer = CompilerConstants.IllegalInteger;
                        }

                        return value;
                    }
                    catch (FormatException)
                    {
                        this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
                    catch (OverflowException)
                    {
                        this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
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
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        [SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation")]
        public string GetAttributeGuidValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool generatable = false, bool canBeEmpty = false)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            EmptyRule emptyRule = canBeEmpty ? EmptyRule.CanBeEmpty : EmptyRule.CanBeWhitespaceOnly;
            string value = this.GetAttributeValue(sourceLineNumbers, attribute, emptyRule);

            if (String.IsNullOrEmpty(value) && canBeEmpty)
            {
                return String.Empty;
            }
            else if (!String.IsNullOrEmpty(value))
            {
                // If the value starts and ends with braces or parenthesis, accept that and strip them off.
                if ((value.StartsWith("{", StringComparison.Ordinal) && value.EndsWith("}", StringComparison.Ordinal))
                    || (value.StartsWith("(", StringComparison.Ordinal) && value.EndsWith(")", StringComparison.Ordinal)))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                try
                {
                    Guid guid;

                    if (generatable && "*".Equals(value, StringComparison.Ordinal))
                    {
                        return value;
                    }

                    if (CompilerCore.PutGuidHere.IsMatch(value))
                    {
                        this.OnMessage(WixErrors.ExampleGuid(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                        return CompilerConstants.IllegalGuid;
                    }
                    else if (value.StartsWith("!(loc", StringComparison.Ordinal) || value.StartsWith("$(loc", StringComparison.Ordinal) || value.StartsWith("!(wix", StringComparison.Ordinal))
                    {
                        return value;
                    }
                    else
                    {
                        guid = new Guid(value);
                    }

                    string uppercaseGuid = guid.ToString().ToUpper(CultureInfo.InvariantCulture);

                    if (this.showPedanticMessages)
                    {
                        if (uppercaseGuid != value)
                        {
                            this.OnMessage(WixErrors.GuidContainsLowercaseLetters(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                        }
                    }

                    return String.Concat("{", uppercaseGuid, "}");
                }
                catch (FormatException)
                {
                    this.OnMessage(WixErrors.IllegalGuidValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return CompilerConstants.IllegalGuid;
        }

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        public Identifier GetAttributeIdentifier(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = Common.GetAttributeValue(sourceLineNumbers, attribute, EmptyRule.CanBeEmpty);
            AccessModifier access = AccessModifier.Public;

            Match match = CompilerCore.LegalIdentifierWithAccess.Match(value);
            if (!match.Success)
            {
                return null;
            }
            else if (match.Groups["access"].Success)
            {
                access = (AccessModifier)Enum.Parse(typeof(AccessModifier), match.Groups["access"].Value, true);
            }

            value = match.Groups["id"].Value;

            if (Common.IsIdentifier(value) && 72 < value.Length)
            {
                this.OnMessage(WixWarnings.IdentifierTooLong(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
            }

            return new Identifier(value, access);
        }

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        public string GetAttributeIdentifierValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return Common.GetAttributeIdentifierValue(sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Gets a yes/no value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        public YesNoType GetAttributeYesNoValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            YesNoType result = YesNoType.IllegalValue;
            if (value.Equals("yes") || value.Equals("true"))
            {
                result = YesNoType.Yes;
            }
            else if (value.Equals("no") || value.Equals("false"))
            {
                result = YesNoType.No;
            }
            else
            {
                this.OnMessage(WixErrors.IllegalYesNoValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
            }

            return result;
        }

        /// <summary>
        /// Gets a yes/no/default value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoDefaultType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public YesNoDefaultType GetAttributeYesNoDefaultValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                switch (Wix.Enums.ParseYesNoDefaultType(value))
                {
                    case Wix.YesNoDefaultType.@default:
                        return YesNoDefaultType.Default;
                    case Wix.YesNoDefaultType.no:
                        return YesNoDefaultType.No;
                    case Wix.YesNoDefaultType.yes:
                        return YesNoDefaultType.Yes;
                    case Wix.YesNoDefaultType.NotSet:
                        // Previous code never returned 'NotSet'!
                        break;
                    default:
                        this.OnMessage(WixErrors.IllegalYesNoDefaultValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                        break;
                }
            }

            return YesNoDefaultType.IllegalValue;
        }

        /// <summary>
        /// Gets a yes/no/always value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoAlwaysType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public YesNoAlwaysType GetAttributeYesNoAlwaysValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                switch (Wix.Enums.ParseYesNoAlwaysType(value))
                {
                    case Wix.YesNoAlwaysType.@always:
                        return YesNoAlwaysType.Always;
                    case Wix.YesNoAlwaysType.no:
                        return YesNoAlwaysType.No;
                    case Wix.YesNoAlwaysType.yes:
                        return YesNoAlwaysType.Yes;
                    case Wix.YesNoAlwaysType.NotSet:
                        // Previous code never returned 'NotSet'!
                        break;
                    default:
                        this.OnMessage(WixErrors.IllegalYesNoAlwaysValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                        break;
                }
            }

            return YesNoAlwaysType.IllegalValue;
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

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (!this.IsValidShortFilename(value, allowWildcards) && !this.IsValidLocIdentifier(value))
                {
                    this.OnMessage(WixErrors.IllegalShortFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                else if (CompilerCore.IsAmbiguousFilename(value))
                {
                    this.OnMessage(WixWarnings.AmbiguousFileOrDirectoryName(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
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
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeLongFilename(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowWildcards = false, bool allowRelative = false)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (!this.IsValidLongFilename(value, allowWildcards, allowRelative) && !this.IsValidLocIdentifier(value))
                {
                    if (allowRelative)
                    {
                        this.OnMessage(WixErrors.IllegalRelativeLongFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
                    else
                    {
                        this.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
                }
                else if (allowRelative)
                {
                    string normalizedPath = value.Replace('\\', '/');
                    if (normalizedPath.StartsWith("../", StringComparison.Ordinal) || normalizedPath.Contains("/../"))
                    {
                        this.OnMessage(WixErrors.PayloadMustBeRelativeToCache(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
                }
                else if (CompilerCore.IsAmbiguousFilename(value))
                {
                    this.OnMessage(WixWarnings.AmbiguousFileOrDirectoryName(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return value;
        }

        /// <summary>
        /// Gets a version value or possibly a binder variable and displays an error for an illegal version value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's version value.</returns>
        public string GetAttributeVersionValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(value))
            {
                try
                {
                    return new Version(value).ToString();
                }
                catch (FormatException) // illegal integer in version
                {
                    // Allow versions to contain binder variables.
                    if (Common.ContainsValidBinderVariable(value))
                    {
                        return value;
                    }

                    this.OnMessage(WixErrors.IllegalVersionValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                catch (ArgumentException)
                {
                    this.OnMessage(WixErrors.IllegalVersionValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a RegistryRoot value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowHkmu">Whether HKMU is considered a valid value.</param>
        /// <returns>The attribute's RegisitryRootType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public Wix.RegistryRootType GetAttributeRegistryRootValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowHkmu)
        {
            Wix.RegistryRootType registryRoot = Wix.RegistryRootType.NotSet;
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                registryRoot = Wix.Enums.ParseRegistryRootType(value);

                if (Wix.RegistryRootType.IllegalValue == registryRoot || (!allowHkmu && Wix.RegistryRootType.HKMU == registryRoot))
                {
                    // TODO: Find a way to expose the valid values programatically!
                    if (allowHkmu)
                    {
                        this.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value,
                            "HKMU", "HKCR", "HKCU", "HKLM", "HKU"));
                    }
                    else
                    {
                        this.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value,
                            "HKCR", "HKCU", "HKLM", "HKU"));
                    }
                }
            }

            return registryRoot;
        }

        /// <summary>
        /// Gets a RegistryRoot as a MsiInterop.MsidbRegistryRoot value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowHkmu">Whether HKMU is returned as -1 (true), or treated as an error (false).</param>
        /// <returns>The attribute's RegisitryRootType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public int GetAttributeMsidbRegistryRootValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowHkmu)
        {
            Wix.RegistryRootType registryRoot = this.GetAttributeRegistryRootValue(sourceLineNumbers, attribute, allowHkmu);

            switch (registryRoot)
            {
                case Wix.RegistryRootType.NotSet:
                    return CompilerConstants.IntegerNotSet;
                case Wix.RegistryRootType.HKCR:
                    return Core.Native.MsiInterop.MsidbRegistryRootClassesRoot;
                case Wix.RegistryRootType.HKCU:
                    return Core.Native.MsiInterop.MsidbRegistryRootCurrentUser;
                case Wix.RegistryRootType.HKLM:
                    return Core.Native.MsiInterop.MsidbRegistryRootLocalMachine;
                case Wix.RegistryRootType.HKU:
                    return Core.Native.MsiInterop.MsidbRegistryRootUsers;
                case Wix.RegistryRootType.HKMU:
                    // This is gross, but there was *one* registry root parsing instance
                    // (in Compiler.ParseRegistrySearchElement()) that did not explicitly
                    // handle HKMU and it fell through to the default error case. The
                    // others treated it as -1, which is what we do here.
                    if (allowHkmu)
                    {
                        return -1;
                    }
                    break;
            }

            return CompilerConstants.IntegerNotSet;
        }

        /// <summary>
        /// Gets an InstallUninstallType value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's InstallUninstallType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public Wix.InstallUninstallType GetAttributeInstallUninstallValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            Wix.InstallUninstallType installUninstall = Wix.InstallUninstallType.NotSet;
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                installUninstall = Wix.Enums.ParseInstallUninstallType(value);

                if (Wix.InstallUninstallType.IllegalValue == installUninstall)
                {
                    // TODO: Find a way to expose the valid values programatically!
                    this.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value,
                         "install", "uninstall", "both"));
                }
            }

            return installUninstall;
        }

        /// <summary>
        /// Gets an ExitType value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's ExitType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public Wix.ExitType GetAttributeExitValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            Wix.ExitType result = Wix.ExitType.NotSet;
            if (!Enum.TryParse<Wix.ExitType>(value, out result))
            {
                result = Wix.ExitType.IllegalValue;

                // TODO: Find a way to expose the valid values programatically!
                this.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value,
                     "success", "cancel", "error", "suspend"));
            }

            return result;
        }

        /// <summary>
        /// Gets a Bundle variable value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeBundleVariableValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(value))
            {
                if (CompilerCore.BuiltinBundleVariables.Contains(value))
                {
                    string illegalValues = CompilerCore.CreateValueList(ValueListKind.Or, CompilerCore.BuiltinBundleVariables);
                    this.OnMessage(WixErrors.IllegalAttributeValueWithIllegalList(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, illegalValues));
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
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeMsiPropertyNameValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (CompilerCore.DisallowedMsiProperties.Contains(value))
                {
                    string illegalValues = CompilerCore.CreateValueList(ValueListKind.Or, CompilerCore.DisallowedMsiProperties);
                    this.OnMessage(WixErrors.DisallowedMsiProperty(sourceLineNumbers, value, illegalValues));
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
            return Common.ContainsProperty(possibleProperty);
        }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public Identifier CreateIdentifier(string prefix, params string[] args)
        {
            string id = Common.GenerateIdentifier(prefix, args);
            return new Identifier(id, AccessModifier.Private);
        }

        /// <summary>
        /// Create an identifier based on passed file name
        /// </summary>
        /// <param name="name">File name to generate identifer from</param>
        /// <returns></returns>
        public Identifier CreateIdentifierFromFilename(string filename)
        {
            string id = Common.GetIdentifierFromName(filename);
            return new Identifier(id, AccessModifier.Private);
        }

        /// <summary>
        /// Attempts to use an extension to parse the attribute.
        /// </summary>
        /// <param name="element">Element containing attribute to be parsed.</param>
        /// <param name="attribute">Attribute to be parsed.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public void ParseExtensionAttribute(XElement element, XAttribute attribute, IDictionary<string, string> context = null)
        {
            // Ignore attributes defined by the W3C because we'll assume they are always right.
            if ((String.IsNullOrEmpty(attribute.Name.NamespaceName) && attribute.Name.LocalName.Equals("xmlns", StringComparison.Ordinal)) ||
                attribute.Name.NamespaceName.StartsWith(CompilerCore.W3SchemaPrefix.NamespaceName, StringComparison.Ordinal))
            {
                return;
            }

            ICompilerExtension extension;
            if (this.TryFindExtension(attribute.Name.NamespaceName, out extension))
            {
                extension.ParseAttribute(element, attribute, context);
            }
            else
            {
                SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);
                this.OnMessage(WixErrors.UnhandledExtensionAttribute(sourceLineNumbers, element.Name.LocalName, attribute.Name.LocalName, attribute.Name.NamespaceName));
            }
        }

        /// <summary>
        /// Attempts to use an extension to parse the element.
        /// </summary>
        /// <param name="parentElement">Element containing element to be parsed.</param>
        /// <param name="element">Element to be parsed.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public void ParseExtensionElement(XElement parentElement, XElement element, IDictionary<string, string> context = null)
        {
            ICompilerExtension extension;
            if (this.TryFindExtension(element.Name.Namespace, out extension))
            {
                SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(parentElement);
                extension.ParseElement(parentElement, element, context);
            }
            else
            {
                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);
                this.OnMessage(WixErrors.UnhandledExtensionElement(childSourceLineNumbers, parentElement.Name.LocalName, element.Name.LocalName, element.Name.NamespaceName));
            }
        }

        /// <summary>
        /// Process all children of the element looking for extensions and erroring on the unexpected.
        /// </summary>
        /// <param name="element">Element to parse children.</param>
        public void ParseForExtensionElements(XElement element)
        {
            foreach (XElement child in element.Elements())
            {
                if (element.Name.Namespace == child.Name.Namespace)
                {
                    this.UnexpectedElement(element, child);
                }
                else
                {
                    this.ParseExtensionElement(element, child);
                }
            }
        }

        /// <summary>
        /// Attempts to use an extension to parse the element, with support for setting component keypath.
        /// </summary>
        /// <param name="parentElement">Element containing element to be parsed.</param>
        /// <param name="element">Element to be parsed.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public ComponentKeyPath ParsePossibleKeyPathExtensionElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            ComponentKeyPath keyPath = null;

            ICompilerExtension extension;
            if (this.TryFindExtension(element.Name.Namespace, out extension))
            {
                keyPath = extension.ParsePossibleKeyPathElement(parentElement, element, context);
            }
            else
            {
                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);
                this.OnMessage(WixErrors.UnhandledExtensionElement(childSourceLineNumbers, parentElement.Name.LocalName, element.Name.LocalName, element.Name.NamespaceName));
            }

            return keyPath;
        }

        /// <summary>
        /// Displays an unexpected attribute error if the attribute is not the namespace attribute.
        /// </summary>
        /// <param name="element">Element containing unexpected attribute.</param>
        /// <param name="attribute">The unexpected attribute.</param>
        public void UnexpectedAttribute(XElement element, XAttribute attribute)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);
            Common.UnexpectedAttribute(sourceLineNumbers, attribute);
        }

        /// <summary>
        /// Display an unexepected element error.
        /// </summary>
        /// <param name="parentElement">The parent element.</param>
        /// <param name="childElement">The unexpected child element.</param>
        public void UnexpectedElement(XElement parentElement, XElement childElement)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(childElement);

            this.OnMessage(WixErrors.UnexpectedElement(sourceLineNumbers, parentElement.Name.LocalName, childElement.Name.LocalName));
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            Messaging.Instance.OnMessage(e);
        }

        /// <summary>
        /// Verifies that the calling assembly version is equal to or newer than the given <paramref name="requiredVersion"/>.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="requiredVersion">The version required of the calling assembly.</param>
        internal void VerifyRequiredVersion(SourceLineNumber sourceLineNumbers, string requiredVersion)
        {
            // an null or empty string means any version will work
            if (!string.IsNullOrEmpty(requiredVersion))
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
                        this.OnMessage(WixErrors.InsufficientVersion(sourceLineNumbers, versionCurrent, versionRequired));
                    }
                    else
                    {
                        this.OnMessage(WixErrors.InsufficientVersion(sourceLineNumbers, versionCurrent, versionRequired, name.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new section and makes it the active section in the core.
        /// </summary>
        /// <param name="id">Unique identifier for the section.</param>
        /// <param name="type">Type of section to create.</param>
        /// <param name="codepage">Codepage for the resulting database for this ection.</param>
        /// <returns>New section.</returns>
        internal Section CreateActiveSection(string id, SectionType type, int codepage)
        {
            this.ActiveSection = this.CreateSection(id, type, codepage);

            this.activeSectionInlinedDirectoryIds = new HashSet<string>();
            this.activeSectionSimpleReferences = new HashSet<string>();

            return this.ActiveSection;
        }

        /// <summary>
        /// Creates a new section.
        /// </summary>
        /// <param name="id">Unique identifier for the section.</param>
        /// <param name="type">Type of section to create.</param>
        /// <param name="codepage">Codepage for the resulting database for this ection.</param>
        /// <returns>New section.</returns>
        internal Section CreateSection(string id, SectionType type, int codepage)
        {
            Section newSection = new Section(id, type, codepage);
            this.intermediate.AddSection(newSection);

            return newSection;
        }

        /// <summary>
        /// Creates a WiX complex reference in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="parentType">The parent type.</param>
        /// <param name="parentId">The parent id.</param>
        /// <param name="parentLanguage">The parent language.</param>
        /// <param name="childType">The child type.</param>
        /// <param name="childId">The child id.</param>
        /// <param name="isPrimary">Whether the child is primary.</param>
        internal void CreateWixComplexReferenceRow(SourceLineNumber sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, string parentLanguage, ComplexReferenceChildType childType, string childId, bool isPrimary)
        {
            if (!this.EncounteredError)
            {
                WixComplexReferenceRow wixComplexReferenceRow = (WixComplexReferenceRow)this.CreateRow(sourceLineNumbers, "WixComplexReference");
                wixComplexReferenceRow.ParentId = parentId;
                wixComplexReferenceRow.ParentType = parentType;
                wixComplexReferenceRow.ParentLanguage = parentLanguage;
                wixComplexReferenceRow.ChildId = childId;
                wixComplexReferenceRow.ChildType = childType;
                wixComplexReferenceRow.IsPrimary = isPrimary;
            }
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
            this.CreateWixComplexReferenceRow(sourceLineNumbers, parentType, parentId, parentLanguage, childType, childId, isPrimary);
            this.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, childType, childId);
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
        internal Identifier CreateDirectoryRow(SourceLineNumber sourceLineNumbers, Identifier id, string parentId, string name, string shortName = null, string sourceName = null, string shortSourceName = null)
        {
            string defaultDir = null;

            if (name.Equals("SourceDir") || this.IsValidShortFilename(name, false))
            {
                defaultDir = name;
            }
            else
            {
                if (String.IsNullOrEmpty(shortName))
                {
                    shortName = this.CreateShortName(name, false, false, "Directory", parentId);
                }

                defaultDir = String.Concat(shortName, "|", name);
            }

            if (!String.IsNullOrEmpty(sourceName))
            {
                if (this.IsValidShortFilename(sourceName, false))
                {
                    defaultDir = String.Concat(defaultDir, ":", sourceName);
                }
                else
                {
                    if (String.IsNullOrEmpty(shortSourceName))
                    {
                        shortSourceName = this.CreateShortName(sourceName, false, false, "Directory", parentId);
                    }

                    defaultDir = String.Concat(defaultDir, ":", shortSourceName, "|", sourceName);
                }
            }

            // For anonymous directories, create the identifier. If this identifier already exists in the
            // active section, bail so we don't add duplicate anonymous directory rows (which are legal
            // but bloat the intermediate and ultimately make the linker do "busy work").
            if (null == id)
            {
                id = this.CreateIdentifier("dir", parentId, name, shortName, sourceName, shortSourceName);

                if (!this.activeSectionInlinedDirectoryIds.Add(id.Id))
                {
                    return id;
                }
            }

            Row row = this.CreateRow(sourceLineNumbers, "Directory", id);
            row[1] = parentId;
            row[2] = defaultDir;
            return id;
        }

        /// <summary>
        /// Gets the attribute value as inline directory syntax.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="attribute">Attribute containing the value to get.</param>
        /// <param name="resultUsedToCreateReference">Flag indicates whether the inline directory syntax should be processed to create a directory row or to create a directory reference.</param>
        /// <returns>Inline directory syntax split into array of strings or null if the syntax did not parse.</returns>
        internal string[] GetAttributeInlineDirectorySyntax(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool resultUsedToCreateReference = false)
        {
            string[] result = null;
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(value))
            {
                int pathStartsAt = 0;
                result = value.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (result[0].EndsWith(":", StringComparison.Ordinal))
                {
                    string id = result[0].TrimEnd(':');
                    if (1 == result.Length)
                    {
                        this.OnMessage(WixErrors.InlineDirectorySyntaxRequiresPath(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, id));
                        return null;
                    }
                    else if (!this.IsValidIdentifier(id))
                    {
                        this.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, id));
                        return null;
                    }

                    pathStartsAt = 1;
                }
                else if (resultUsedToCreateReference && 1 == result.Length)
                {
                    if (value.EndsWith("\\"))
                    {
                        if (!this.IsValidLongFilename(result[0]))
                        {
                            this.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, result[0]));
                            return null;
                        }
                    }
                    else if (!this.IsValidIdentifier(result[0]))
                    {
                        this.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, result[0]));
                        return null;
                    }

                    return result; // return early to avoid additional checks below.
                }

                // Check each part of the relative path to ensure that it is a valid directory name.
                for (int i = pathStartsAt; i < result.Length; ++i)
                {
                    if (!this.IsValidLongFilename(result[i]))
                    {
                        this.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, result[i]));
                        return null;
                    }
                }

                if (1 < result.Length && !value.EndsWith("\\"))
                {
                    this.OnMessage(WixWarnings.BackslashTerminateInlineDirectorySyntax(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return result;
        }

        /// <summary>
        /// Finds a compiler extension by namespace URI.
        /// </summary>
        /// <param name="ns">Namespace the extension supports.</param>
        /// <returns>True if found compiler extension or false if nothing matches namespace URI.</returns>
        private bool TryFindExtension(XNamespace ns, out ICompilerExtension extension)
        {
            return this.extensions.TryGetValue(ns, out extension);
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
