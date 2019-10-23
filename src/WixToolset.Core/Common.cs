// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Common Wix utility methods and types.
    /// </summary>
    public static class Common
    {
        // TODO: Find a place to put all of these so they doesn't have to be public and exposed by WixToolset.Core.dll
        public const string UpgradeDetectedProperty = "WIX_UPGRADE_DETECTED";
        public const string UpgradePreventedCondition = "NOT WIX_UPGRADE_DETECTED";
        public const string DowngradeDetectedProperty = "WIX_DOWNGRADE_DETECTED";
        public const string DowngradePreventedCondition = "NOT WIX_DOWNGRADE_DETECTED";

        //-------------------------------------------------------------------------------------------------
        // Layout of an Access Mask (from http://technet.microsoft.com/en-us/library/cc783530(WS.10).aspx)
        //
        //  -------------------------------------------------------------------------------------------------
        //  |31|30|29|28|27|26|25|24|23|22|21|20|19|18|17|16|15|14|13|12|11|10|09|08|07|06|05|04|03|02|01|00|
        //  -------------------------------------------------------------------------------------------------
        //  |GR|GW|GE|GA| Reserved  |AS|StandardAccessRights|        Object-Specific Access Rights          |
        //
        //  Key
        //  GR = Generic Read
        //  GW = Generic Write
        //  GE = Generic Execute
        //  GA = Generic All
        //  AS = Right to access SACL
        //
        // TODO: what is the expected decompile behavior if a bit is found that is not explicitly enumerated
        //
        //-------------------------------------------------------------------------------------------------
        // Generic Access Rights (per WinNT.h)
        // ---------------------
        // GENERIC_ALL                      (0x10000000L)
        // GENERIC_EXECUTE                  (0x20000000L)
        // GENERIC_WRITE                    (0x40000000L)
        // GENERIC_READ                     (0x80000000L)
        // TODO: Find a place to put this that it doesn't have to be public and exposed by WixToolset.Core.dll
        public static readonly string[] GenericPermissions = { "GenericAll", "GenericExecute", "GenericWrite", "GenericRead" };

        // Standard Access Rights (per WinNT.h)
        // ----------------------
        // DELETE                           (0x00010000L)
        // READ_CONTROL                     (0x00020000L)
        // WRITE_DAC                        (0x00040000L)
        // WRITE_OWNER                      (0x00080000L)
        // SYNCHRONIZE                      (0x00100000L)
        // TODO: Find a place to put this that it doesn't have to be public and exposed by WixToolset.Core.dll
        public static readonly string[] StandardPermissions = { "Delete", "ReadPermission", "ChangePermission", "TakeOwnership", "Synchronize" };

        // Object-Specific Access Rights
        // =============================
        // Directory Access Rights (per WinNT.h)
        // -----------------------
        // FILE_LIST_DIRECTORY       ( 0x0001 )
        // FILE_ADD_FILE             ( 0x0002 )
        // FILE_ADD_SUBDIRECTORY     ( 0x0004 )
        // FILE_READ_EA              ( 0x0008 )
        // FILE_WRITE_EA             ( 0x0010 )
        // FILE_TRAVERSE             ( 0x0020 )
        // FILE_DELETE_CHILD         ( 0x0040 )
        // FILE_READ_ATTRIBUTES      ( 0x0080 )
        // FILE_WRITE_ATTRIBUTES     ( 0x0100 )
        // TODO: Find a place to put this that it doesn't have to be public and exposed by WixToolset.Core.dll
        public static readonly string[] FolderPermissions = { "Read", "CreateFile", "CreateChild", "ReadExtendedAttributes", "WriteExtendedAttributes", "Traverse", "DeleteChild", "ReadAttributes", "WriteAttributes" };

        // Registry Access Rights (per TODO)
        // ----------------------
        // TODO: Find a place to put this that it doesn't have to be public and exposed by WixToolset.Core.dll
        public static readonly string[] RegistryPermissions = { "Read", "Write", "CreateSubkeys", "EnumerateSubkeys", "Notify", "CreateLink" };

        // File Access Rights (per WinNT.h)
        // ------------------
        // FILE_READ_DATA            ( 0x0001 )
        // FILE_WRITE_DATA           ( 0x0002 )
        // FILE_APPEND_DATA          ( 0x0004 )
        // FILE_READ_EA              ( 0x0008 )
        // FILE_WRITE_EA             ( 0x0010 )
        // FILE_EXECUTE              ( 0x0020 )
        // via mask FILE_ALL_ACCESS  ( 0x0040 )
        // FILE_READ_ATTRIBUTES      ( 0x0080 )
        // FILE_WRITE_ATTRIBUTES     ( 0x0100 )
        //
        // STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
        // FILE_ALL_ACCESS           (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF)
        // TODO: Find a place to put this that it doesn't have to be public and exposed by WixToolset.Core.dll
        public static readonly string[] FilePermissions = { "Read", "Write", "Append", "ReadExtendedAttributes", "WriteExtendedAttributes", "Execute", "FileAllRights", "ReadAttributes", "WriteAttributes" };

        public static readonly Regex WixVariableRegex = new Regex(@"(\!|\$)\((?<namespace>loc|wix|bind|bindpath)\.(?<fullname>(?<name>[_A-Za-z][0-9A-Za-z_]+)(\.(?<scope>[_A-Za-z][0-9A-Za-z_\.]*))?)(\=(?<value>.+?))?\)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        private static readonly Regex PropertySearch = new Regex(@"\[[#$!]?[a-zA-Z_][a-zA-Z0-9_\.]*]", RegexOptions.Singleline);
        private static readonly Regex AddPrefix = new Regex(@"^[^a-zA-Z_]", RegexOptions.Compiled);
        private static readonly Regex LegalIdentifierCharacters = new Regex(@"^[_A-Za-z][0-9A-Za-z_\.]*$", RegexOptions.Compiled);
        private static readonly Regex IllegalIdentifierCharacters = new Regex(@"[^A-Za-z0-9_\.]|\.{2,}", RegexOptions.Compiled); // non 'words' and assorted valid characters

        private const string LegalShortFilenameCharacters = @"[^\\\?|><:/\*""\+,;=\[\]\. ]"; // illegal: \ ? | > < : / * " + , ; = [ ] . (space)
        private static readonly Regex LegalShortFilename = new Regex(String.Concat("^", LegalShortFilenameCharacters, @"{1,8}(\.", LegalShortFilenameCharacters, "{0,3})?$"), RegexOptions.Compiled);

        private const string LegalWildcardShortFilenameCharacters = @"[^\\|><:/""\+,;=\[\]\. ]"; // illegal: \ | > < : / " + , ; = [ ] . (space)
        private static readonly Regex LegalWildcardShortFilename = new Regex(String.Concat("^", LegalWildcardShortFilenameCharacters, @"{1,16}(\.", LegalWildcardShortFilenameCharacters, "{0,6})?$"));

        /// <summary>
        /// Gets a valid code page from the given web name or integer value.
        /// </summary>
        /// <param name="value">A code page web name or integer value as a string.</param>
        /// <param name="allowNoChange">Whether to allow -1 which does not change the database code pages. This may be the case with wxl files.</param>
        /// <param name="onlyAnsi">Whether to allow Unicode (UCS) or UTF code pages.</param>
        /// <param name="sourceLineNumbers">Source line information for the current authoring.</param>
        /// <returns>A valid code page number.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is an integer less than 0 or greater than 65535.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="NotSupportedException">The value doesn't not represent a valid code page name or integer value.</exception>
        /// <exception cref="WixException">The code page is invalid for summary information.</exception>
        public static int GetValidCodePage(string value, bool allowNoChange = false, bool onlyAnsi = false, SourceLineNumber sourceLineNumbers = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            try
            {
                Encoding encoding;

                // check if a integer as a string was passed
                if (Int32.TryParse(value, out int codePage))
                {
                    if (0 == codePage)
                    {
                        // 0 represents a neutral database
                        return 0;
                    }
                    else if (allowNoChange && -1 == codePage)
                    {
                        // -1 means no change to the database code page
                        return -1;
                    }

                    encoding = Encoding.GetEncoding(codePage);
                }
                else
                {
                    encoding = Encoding.GetEncoding(value);
                }

                // Windows Installer parses some code page references
                // as unsigned shorts which fail to open the database.
                if (onlyAnsi)
                {
                    codePage = encoding.CodePage;
                    if (0 > codePage || Int16.MaxValue < codePage)
                    {
                        throw new WixException(ErrorMessages.InvalidSummaryInfoCodePage(sourceLineNumbers, codePage));
                    }
                }

                if (encoding == null)
                {
                    throw new WixException(ErrorMessages.IllegalCodepage(sourceLineNumbers, codePage));
                }

                return encoding.CodePage;
            }
            catch (ArgumentException ex)
            {
                // rethrow as NotSupportedException since either can be thrown
                // if the system does not support the specified code page
                throw new NotSupportedException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Verifies if a filename is a valid short filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>True if the filename is a valid short filename</returns>
        internal static bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            if (String.IsNullOrEmpty(filename))
            {
                return false;
            }

            if (allowWildcards)
            {
                if (Common.LegalWildcardShortFilename.IsMatch(filename))
                {
                    bool foundPeriod = false;
                    int beforePeriod = 0;
                    int afterPeriod = 0;

                    // count the number of characters before and after the period
                    // '*' is not counted because it may represent zero characters
                    foreach (char character in filename)
                    {
                        if ('.' == character)
                        {
                            foundPeriod = true;
                        }
                        else if ('*' != character)
                        {
                            if (foundPeriod)
                            {
                                afterPeriod++;
                            }
                            else
                            {
                                beforePeriod++;
                            }
                        }
                    }

                    if (8 >= beforePeriod && 3 >= afterPeriod)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return Common.LegalShortFilename.IsMatch(filename);
            }
        }

        /// <summary>
        /// Verifies if an identifier is a valid binder variable name.
        /// </summary>
        /// <param name="name">Binder variable name to verify.</param>
        /// <returns>True if the identifier is a valid binder variable name.</returns>
        public static bool IsValidBinderVariable(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return false;
            }

            Match match = Common.WixVariableRegex.Match(name);

            return (match.Success && ("bind" == match.Groups["namespace"].Value || "wix" == match.Groups["namespace"].Value) && 0 == match.Index && name.Length == match.Length);
        }

        /// <summary>
        /// Verifies if a string contains a valid binder variable name.
        /// </summary>
        /// <param name="name">String to verify.</param>
        /// <returns>True if the string contains a valid binder variable name.</returns>
        public static bool ContainsValidBinderVariable(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return false;
            }

            Match match = Common.WixVariableRegex.Match(name);

            return match.Success && ("bind" == match.Groups["namespace"].Value || "wix" == match.Groups["namespace"].Value);
        }

        /// <summary>
        /// Verifies the given string is a valid module or bundle version.
        /// </summary>
        /// <param name="version">The version to verify.</param>
        /// <returns>True if version is a valid module or bundle version.</returns>
        public static bool IsValidModuleOrBundleVersion(string version)
        {
            if (!Common.IsValidBinderVariable(version))
            {
                Version ver = null;
                
                try
                {
                    ver = new Version(version);
                }
                catch (ArgumentException)
                {
                    return false;
                }

                if (65535 < ver.Major || 65535 < ver.Minor || 65535 < ver.Build || 65535 < ver.Revision)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Generate a new Windows Installer-friendly guid.
        /// </summary>
        /// <returns>A new guid.</returns>
        public static string GenerateGuid()
        {
            return Guid.NewGuid().ToString("B").ToUpperInvariant();
        }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        public static string GenerateIdentifier(string prefix, params string[] args)
        {
            string stringData = String.Join("|", args);
            byte[] data = Encoding.UTF8.GetBytes(stringData);

            // hash the data
            byte[] hash;
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                hash = sha1.ComputeHash(data);
            }

            // Build up the identifier.
            StringBuilder identifier = new StringBuilder(35, 35);
            identifier.Append(prefix);
            identifier.Append(Convert.ToBase64String(hash).TrimEnd('='));
            identifier.Replace('+', '.').Replace('/', '_');

            return identifier.ToString();
        }

        /// <summary>
        /// Return an identifier based on provided file or directory name
        /// </summary>
        /// <param name="name">File/directory name to generate identifer from</param>
        /// <returns>A version of the name that is a legal identifier.</returns>
        internal static string GetIdentifierFromName(string name)
        {
            string result = IllegalIdentifierCharacters.Replace(name, "_"); // replace illegal characters with "_".

            // MSI identifiers must begin with an alphabetic character or an
            // underscore. Prefix all other values with an underscore.
            if (AddPrefix.IsMatch(name))
            {
                result = String.Concat("_", result);
            }

            return result;
        }

        /// <summary>
        /// Checks if the string contains a property (i.e. "foo[Property]bar")
        /// </summary>
        /// <param name="possibleProperty">String to evaluate for properties.</param>
        /// <returns>True if a property is found in the string.</returns>
        internal static bool ContainsProperty(string possibleProperty)
        {
            return PropertySearch.IsMatch(possibleProperty);
        }

        /// <summary>
        /// Recursively loops through a directory, changing an attribute on all of the underlying files.
        /// An example is to add/remove the ReadOnly flag from each file.
        /// </summary>
        /// <param name="path">The directory path to start deleting from.</param>
        /// <param name="fileAttribute">The FileAttribute to change on each file.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="markAttribute">If true, add the attribute to each file. If false, remove it.</param>
        private static void RecursiveFileAttributes(string path, FileAttributes fileAttribute, bool markAttribute, IMessaging messageHandler)
        {
            foreach (string subDirectory in Directory.GetDirectories(path))
            {
                RecursiveFileAttributes(subDirectory, fileAttribute, markAttribute, messageHandler);
            }

            foreach (string filePath in Directory.GetFiles(path))
            {
                FileAttributes attributes = File.GetAttributes(filePath);
                if (markAttribute)
                {
                    attributes = attributes | fileAttribute; // add to list of attributes
                }
                else if (fileAttribute == (attributes & fileAttribute)) // if attribute set
                {
                    attributes = attributes ^ fileAttribute; // remove from list of attributes
                }

                try
                {
                    File.SetAttributes(filePath, attributes);
                }
                catch (UnauthorizedAccessException)
                {
                    messageHandler.Write(WarningMessages.AccessDeniedForSettingAttributes(null, filePath));
                }
            }
        }

        /// <summary>
        /// Takes an id, and demodularizes it (if possible).
        /// </summary>
        /// <remarks>
        /// If the output type is a module, returns a demodularized version of an id. Otherwise, returns the id.
        /// </remarks>
        /// <param name="outputType">The type of the output to bind.</param>
        /// <param name="modularizationGuid">The modularization GUID.</param>
        /// <param name="id">The id to demodularize.</param>
        /// <returns>The demodularized id.</returns>
        public static string Demodularize(OutputType outputType, string modularizationGuid, string id)
        {
            if (OutputType.Module == outputType && id.EndsWith(String.Concat(".", modularizationGuid), StringComparison.Ordinal))
            {
                id = id.Substring(0, id.Length - 37);
            }

            return id;
        }

        /// <summary>
        /// Get the source/target and short/long file names from an MSI Filename column.
        /// </summary>
        /// <param name="value">The Filename value.</param>
        /// <returns>An array of strings of length 4.  The contents are: short target, long target, short source, and long source.</returns>
        /// <remarks>
        /// If any particular file name part is not parsed, its set to null in the appropriate location of the returned array of strings.
        /// However, the returned array will always be of length 4.
        /// </remarks>
        public static string[] GetNames(string value)
        {
            string[] names = new string[4];
            int targetSeparator = value.IndexOf(":", StringComparison.Ordinal);

            // split source and target
            string sourceName = null;
            string targetName = value;
            if (0 <= targetSeparator)
            {
                sourceName = value.Substring(targetSeparator + 1);
                targetName = value.Substring(0, targetSeparator);
            }

            // split the source short and long names
            string sourceLongName = null;
            if (null != sourceName)
            {
                int sourceLongNameSeparator = sourceName.IndexOf("|", StringComparison.Ordinal);
                if (0 <= sourceLongNameSeparator)
                {
                    sourceLongName = sourceName.Substring(sourceLongNameSeparator + 1);
                    sourceName = sourceName.Substring(0, sourceLongNameSeparator);
                }
            }

            // split the target short and long names
            int targetLongNameSeparator = targetName.IndexOf("|", StringComparison.Ordinal);
            string targetLongName = null;
            if (0 <= targetLongNameSeparator)
            {
                targetLongName = targetName.Substring(targetLongNameSeparator + 1);
                targetName = targetName.Substring(0, targetLongNameSeparator);
            }

            // remove the long source name when its identical to the long source name
            if (null != sourceName && sourceName == sourceLongName)
            {
                sourceLongName = null;
            }

            // remove the long target name when its identical to the long target name
            if (null != targetName && targetName == targetLongName)
            {
                targetLongName = null;
            }

            // remove the source names when they are identical to the target names
            if (sourceName == targetName && sourceLongName == targetLongName)
            {
                sourceName = null;
                sourceLongName = null;
            }

            // target name(s)
            if ("." != targetName)
            {
                names[0] = targetName;
            }

            if (null != targetLongName && "." != targetLongName)
            {
                names[1] = targetLongName;
            }

            // source name(s)
            if (null != sourceName)
            {
                names[2] = sourceName;
            }

            if (null != sourceLongName && "." != sourceLongName)
            {
                names[3] = sourceLongName;
            }

            return names;
        }

        /// <summary>
        /// Get a source/target and short/long file name from an MSI Filename column.
        /// </summary>
        /// <param name="value">The Filename value.</param>
        /// <param name="source">true to get a source name; false to get a target name</param>
        /// <param name="longName">true to get a long name; false to get a short name</param>
        /// <returns>The name.</returns>
        public static string GetName(string value, bool source, bool longName)
        {
            string[] names = GetNames(value);

            if (source)
            {
                if (longName && null != names[3])
                {
                    return names[3];
                }
                else if (null != names[2])
                {
                    return names[2];
                }
            }

            if (longName && null != names[1])
            {
                return names[1];
            }
            else
            {
                return names[0];
            }
        }

        /// <summary>
        /// Get an attribute value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="emptyRule">A rule for the contents of the value. If the contents do not follow the rule, an error is thrown.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's value.</returns>
        internal static string GetAttributeValue(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute, EmptyRule emptyRule)
        {
            string value = attribute.Value;

            if ((emptyRule == EmptyRule.MustHaveNonWhitespaceCharacters && String.IsNullOrEmpty(value.Trim())) ||
                (emptyRule == EmptyRule.CanBeWhitespaceOnly && String.IsNullOrEmpty(value)))
            {
                messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
                return String.Empty;
            }

            return value;
        }

        /// <summary>
        /// Verifies that a value is a legal identifier.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <returns>true if the value is an identifier; false otherwise.</returns>
        public static bool IsIdentifier(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                if (LegalIdentifierCharacters.IsMatch(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        internal static string GetAttributeIdentifierValue(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = Common.GetAttributeValue(messaging, sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly);

            if (Common.IsIdentifier(value))
            {
                if (72 < value.Length)
                {
                    messaging.Write(WarningMessages.IdentifierTooLong(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }

                return value;
            }
            else
            {
                if (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal))
                {
                    messaging.Write(ErrorMessages.IllegalIdentifierLooksLikeFormatted(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                else
                {
                    messaging.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }

                return String.Empty;
            }
        }

        /// <summary>
        /// Get an integer attribute value and displays an error for an illegal integer value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's integer value or a special value if an error occurred during conversion.</returns>
        public static int GetAttributeIntegerValue(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute, int minimum, int maximum)
        {
            Debug.Assert(minimum > CompilerConstants.IntegerNotSet && minimum > CompilerConstants.IllegalInteger, "The legal values for this attribute collide with at least one sentinel used during parsing.");

            string value = Common.GetAttributeValue(messaging, sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly);
            int integer = CompilerConstants.IllegalInteger;

            if (0 < value.Length)
            {
                if (Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out integer))
                {
                    if (CompilerConstants.IntegerNotSet == integer || CompilerConstants.IllegalInteger == integer)
                    {
                        messaging.Write(ErrorMessages.IntegralValueSentinelCollision(sourceLineNumbers, integer));
                    }
                    else if (minimum > integer || maximum < integer)
                    {
                        messaging.Write(ErrorMessages.IntegralValueOutOfRange(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, integer, minimum, maximum));
                        integer = CompilerConstants.IllegalInteger;
                    }
                }
                else
                {
                    messaging.Write(ErrorMessages.IllegalIntegerValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return integer;
        }

        /// <summary>
        /// Gets a yes/no value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        internal static YesNoType GetAttributeYesNoValue(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            string value = Common.GetAttributeValue(messaging, sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly);
            YesNoType yesNo = YesNoType.IllegalValue;

            if ("yes".Equals(value) || "true".Equals(value))
            {
                yesNo = YesNoType.Yes;
            }
            else if ("no".Equals(value) || "false".Equals(value))
            {
                yesNo = YesNoType.No;
            }
            else
            {
                messaging.Write(ErrorMessages.IllegalYesNoValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
            }

            return yesNo;
        }

        /// <summary>
        /// Gets the text of an XElement.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        internal static string GetInnerText(XElement node)
        {
            var text = node.Nodes().Where(n => XmlNodeType.Text == n.NodeType || XmlNodeType.CDATA == n.NodeType).Cast<XText>().FirstOrDefault();
            return text?.Value;
        }

        /// <summary>
        /// Display an unexpected attribute error.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute.</param>
        public static void UnexpectedAttribute(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            // Ignore elements defined by the W3C because we'll assume they are always right.
            if (!((String.IsNullOrEmpty(attribute.Name.NamespaceName) && attribute.Name.LocalName.Equals("xmlns", StringComparison.Ordinal)) ||
                 attribute.Name.NamespaceName.StartsWith(CompilerCore.W3SchemaPrefix.NamespaceName, StringComparison.Ordinal)))
            {
                messaging.Write(ErrorMessages.UnexpectedAttribute(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }
        }

        /// <summary>
        /// Display an unsupported extension attribute error.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="extensionAttribute">The extension attribute.</param>
        internal static void UnsupportedExtensionAttribute(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute extensionAttribute)
        {
            // Ignore elements defined by the W3C because we'll assume they are always right.
            if (!((String.IsNullOrEmpty(extensionAttribute.Name.NamespaceName) && extensionAttribute.Name.LocalName.Equals("xmlns", StringComparison.Ordinal)) ||
                   extensionAttribute.Name.NamespaceName.StartsWith(CompilerCore.W3SchemaPrefix.NamespaceName, StringComparison.Ordinal)))
            {
                messaging.Write(ErrorMessages.UnsupportedExtensionAttribute(sourceLineNumbers, extensionAttribute.Parent.Name.LocalName, extensionAttribute.Name.LocalName));
            }
        }
    }
}
