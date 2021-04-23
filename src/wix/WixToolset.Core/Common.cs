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
    using System.Xml;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Common Wix utility methods and types.
    /// </summary>
    internal static class Common
    {
        private static readonly char[] IllegalShortFilenameCharacters = new[] { '\\', '?', '|', '>', '<', ':', '/', '*', '\"', '+', ',', ';', '=', '[', ']', '.', ' ' };
        private static readonly char[] IllegalWildcardShortFilenameCharacters = new[] { '\\', '|', '>', '<', ':', '/', '\"', '+', ',', ';', '=', '[', ']', '.', ' ' };

        internal static readonly char[] IllegalLongFilenameCharacters = new[] { '\\', '/', '?', '*', '|', '>', '<', ':', '\"' }; // illegal: \ / ? | > < : / * "
        internal static readonly char[] IllegalRelativeLongFilenameCharacters = new[] { '?', '*', '|', '>', '<', ':', '\"' }; // like illegal, but we allow '\' and '/'
        internal static readonly char[] IllegalWildcardLongFilenameCharacters = new[] { '\\', '/', '|', '>', '<', ':', '\"' };   // like illegal: but we allow '*' and '?'

        public static string GetCanonicalRelativePath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string relativePath, IMessaging messageHandler)
        {
            const string root = @"C:\";
            if (!Path.IsPathRooted(relativePath))
            {
                var normalizedPath = Path.GetFullPath(root + relativePath);
                if (normalizedPath.StartsWith(root))
                {
                    var canonicalizedPath = normalizedPath.Substring(root.Length);
                    if (canonicalizedPath != relativePath)
                    {
                        messageHandler.Write(WarningMessages.PathCanonicalized(sourceLineNumbers, elementName, attributeName, relativePath, canonicalizedPath));
                    }
                    return canonicalizedPath;
                }
            }

            messageHandler.Write(ErrorMessages.PayloadMustBeRelativeToCache(sourceLineNumbers, elementName, attributeName, relativePath));
            return relativePath;
        }

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

                // Check if a integer as a string was passed.
                if (Int32.TryParse(value, out var codePage))
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
                // Rethrow as NotSupportedException since either can be thrown
                // if the system does not support the specified code page.
                throw new NotSupportedException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Verifies if an identifier is a valid binder variable name.
        /// </summary>
        /// <param name="variable">Binder variable name to verify.</param>
        /// <returns>True if the identifier is a valid binder variable name.</returns>
        public static bool IsValidBinderVariable(string variable)
        {
            return TryParseWixVariable(variable, 0, out var parsed) && parsed.Index == 0 && parsed.Length == variable.Length && (parsed.Namespace == "bind" || parsed.Namespace == "wix");
        }

        /// <summary>
        /// Verifies if a string contains a valid binder variable name.
        /// </summary>
        /// <param name="verify">String to verify.</param>
        /// <returns>True if the string contains a valid binder variable name.</returns>
        public static bool ContainsValidBinderVariable(string verify)
        {
            return TryParseWixVariable(verify, 0, out var parsed) && (parsed.Namespace == "bind" || parsed.Namespace == "wix");
        }

        /// <summary>
        /// Verifies the given string is a valid 4-part version module or bundle version.
        /// </summary>
        /// <param name="version">The version to verify.</param>
        /// <returns>True if version is a valid module or bundle version.</returns>
        public static bool IsValidFourPartVersion(string version)
        {
            if (!Common.IsValidBinderVariable(version))
            {
                if (!Version.TryParse(version, out var ver) || 65535 < ver.Major || 65535 < ver.Minor || 65535 < ver.Build || 65535 < ver.Revision)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidLongFilename(string filename, bool allowWildcards, bool allowRelative)
        {
            if (String.IsNullOrEmpty(filename))
            {
                return false;
            }
            else if (filename.Length > 259)
            {
                return false;
            }

            // Check for a non-period character (all periods is not legal)
            var allPeriods = true;
            foreach (var character in filename)
            {
                if ('.' != character)
                {
                    allPeriods = false;
                    break;
                }
            }

            if (allPeriods)
            {
                return false;
            }

            if (allowWildcards)
            {
                return filename.IndexOfAny(Common.IllegalWildcardLongFilenameCharacters) == -1;
            }
            else if (allowRelative)
            {
                return filename.IndexOfAny(Common.IllegalRelativeLongFilenameCharacters) == -1;
            }
            else
            {
                return filename.IndexOfAny(Common.IllegalLongFilenameCharacters) == -1;
            }
        }

        public static bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            if (String.IsNullOrEmpty(filename))
            {
                return false;
            }

            if (allowWildcards)
            {
                var expectedDot = filename.IndexOfAny(IllegalWildcardShortFilenameCharacters);
                if (expectedDot == -1)
                {
                }
                else if (filename[expectedDot] != '.')
                {
                    return false;
                }
                else if (expectedDot < filename.Length)
                {
                    var extensionInvalids = filename.IndexOfAny(IllegalWildcardShortFilenameCharacters, expectedDot + 1);
                    if (extensionInvalids != -1)
                    {
                        return false;
                    }
                }

                var foundPeriod = false;
                var beforePeriod = 0;
                var afterPeriod = 0;

                // count the number of characters before and after the period
                // '*' is not counted because it may represent zero characters
                foreach (var character in filename)
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

                return false;
            }
            else
            {
                if (filename.Length > 12)
                {
                    return false;
                }

                var expectedDot = filename.IndexOfAny(IllegalShortFilenameCharacters);
                if (expectedDot == -1)
                {
                    return filename.Length < 9;
                }
                else if (expectedDot > 8 || filename[expectedDot] != '.' || expectedDot + 4 < filename.Length)
                {
                    return false;
                }

                var validExtension = filename.IndexOfAny(IllegalShortFilenameCharacters, expectedDot + 1);
                return validExtension == -1;
            }
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
            string base64;

            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var combined = String.Join("|", args);
                var data = Encoding.UTF8.GetBytes(combined);
                var hash = sha1.ComputeHash(data);
                base64 = Convert.ToBase64String(hash);
            }

            var identifier = new StringBuilder(32);
            identifier.Append(prefix);
            identifier.Append(base64);
            identifier.Length -= 1; // removes the trailing '=' from base64
            identifier.Replace('+', '.');
            identifier.Replace('/', '_');

            return identifier.ToString();
        }

        /// <summary>
        /// Return an identifier based on provided file or directory name
        /// </summary>
        /// <param name="name">File/directory name to generate identifer from</param>
        /// <returns>A version of the name that is a legal identifier.</returns>
        internal static string GetIdentifierFromName(string name)
        {
            StringBuilder sb = null;
            var offset = 0;

            // MSI identifiers must begin with an alphabetic character or an
            // underscore. Prefix all other values with an underscore.
            if (!ValidIdentifierChar(name[0], true))
            {
                sb = new StringBuilder("_" + name);
                offset = 1;
            }

            for (var i = 0; i < name.Length; ++i)
            {
                if (!ValidIdentifierChar(name[i], false))
                {
                    if (sb == null)
                    {
                        sb = new StringBuilder(name);
                    }

                    sb[i + offset] = '_';
                }
            }

            return sb?.ToString() ?? name;
        }

        /// <summary>
        /// Checks if the string contains a property (i.e. "foo[Property]bar")
        /// </summary>
        /// <param name="possibleProperty">String to evaluate for properties.</param>
        /// <returns>True if a property is found in the string.</returns>
        internal static bool ContainsProperty(string possibleProperty)
        {
            var start = possibleProperty.IndexOf('[');
            if (start != -1 && start < possibleProperty.Length - 2)
            {
                var end = possibleProperty.IndexOf(']', start + 1);
                if (end > start + 1)
                {
                    // Skip supported property modifiers.
                    if (possibleProperty[start + 1] == '#' || possibleProperty[start + 1] == '$' || possibleProperty[start + 1] == '!')
                    {
                        ++start;
                    }

                    var id = possibleProperty.Substring(start + 1, end - 1);

                    if (Common.IsIdentifier(id))
                    {
                        return true;
                    }
                }
            }

            return false;
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
            foreach (var subDirectory in Directory.GetDirectories(path))
            {
                RecursiveFileAttributes(subDirectory, fileAttribute, markAttribute, messageHandler);
            }

            foreach (var filePath in Directory.GetFiles(path))
            {
                var attributes = File.GetAttributes(filePath);
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
        /// Thus the returned array will always be of length 4.
        /// </remarks>
        public static string[] GetNames(string value)
        {
            var targetSeparator = value.IndexOf(':');

            // split source and target
            string sourceName = null;
            var targetName = value;
            if (0 <= targetSeparator)
            {
                sourceName = value.Substring(targetSeparator + 1);
                targetName = value.Substring(0, targetSeparator);
            }

            // split the source short and long names
            string sourceLongName = null;
            if (null != sourceName)
            {
                var sourceLongNameSeparator = sourceName.IndexOf('|');
                if (0 <= sourceLongNameSeparator)
                {
                    sourceLongName = sourceName.Substring(sourceLongNameSeparator + 1);
                    sourceName = sourceName.Substring(0, sourceLongNameSeparator);
                }
            }

            // split the target short and long names
            var targetLongNameSeparator = targetName.IndexOf('|');
            string targetLongName = null;
            if (0 <= targetLongNameSeparator)
            {
                targetLongName = targetName.Substring(targetLongNameSeparator + 1);
                targetName = targetName.Substring(0, targetLongNameSeparator);
            }

            // Remove the long source name when its identical to the short source name.
            if (null != sourceName && sourceName == sourceLongName)
            {
                sourceLongName = null;
            }

            // Remove the long target name when its identical to the long target name.
            if (null != targetName && targetName == targetLongName)
            {
                targetLongName = null;
            }

            // Remove the source names when they are identical to the target names.
            if (sourceName == targetName && sourceLongName == targetLongName)
            {
                sourceName = null;
                sourceLongName = null;
            }

            // target name(s)
            if ("." == targetName)
            {
                targetName = null;
            }

            if ("." == targetLongName)
            {
                targetLongName = null;
            }

            // source name(s)
            if ("." == sourceName)
            {
                sourceName = null;
            }

            if ("." == sourceLongName)
            {
                sourceLongName = null;
            }

            return new[] { targetName, targetLongName, sourceName, sourceLongName };
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
            var names = GetNames(value);

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
        /// <param name="messaging"></param>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="emptyRule">A rule for the contents of the value. If the contents do not follow the rule, an error is thrown.</param>
        /// <returns>The attribute's value.</returns>
        internal static string GetAttributeValue(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute, EmptyRule emptyRule)
        {
            var value = attribute.Value;

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
            if (String.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var i = 0; i < value.Length; ++i)
            {
                if (!ValidIdentifierChar(value[i], i == 0))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        internal static string GetAttributeIdentifierValue(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            var value = Common.GetAttributeValue(messaging, sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly);

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
        /// <param name="messaging"></param>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's integer value or a special value if an error occurred during conversion.</returns>
        public static int GetAttributeIntegerValue(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute, int minimum, int maximum)
        {
            Debug.Assert(minimum > CompilerConstants.IntegerNotSet && minimum > CompilerConstants.IllegalInteger, "The legal values for this attribute collide with at least one sentinel used during parsing.");

            var value = Common.GetAttributeValue(messaging, sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly);
            var integer = CompilerConstants.IllegalInteger;

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
        /// <param name="messaging"></param>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        internal static YesNoType GetAttributeYesNoValue(IMessaging messaging, SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            var value = Common.GetAttributeValue(messaging, sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly);
            var yesNo = YesNoType.IllegalValue;

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
        /// <param name="node">Element to get text.</param>
        /// <returns>The element's text.</returns>
        internal static string GetInnerText(XElement node)
        {
            var text = node.Nodes().Where(n => XmlNodeType.Text == n.NodeType || XmlNodeType.CDATA == n.NodeType).Cast<XText>().FirstOrDefault();
            return text?.Value;
        }

        internal static bool TryParseWixVariable(string value, int start, out ParsedWixVariable parsedVariable)
        {
            parsedVariable = null;

            if (String.IsNullOrEmpty(value) || start >= value.Length)
            {
                return false;
            }

            var startWixVariable = value.IndexOf("!(", start, StringComparison.Ordinal);
            if (startWixVariable == -1)
            {
                return false;
            }

            var firstDot = value.IndexOf('.', startWixVariable + 1);
            if (firstDot == -1)
            {
                return false;
            }

            var ns = value.Substring(startWixVariable + 2, firstDot - startWixVariable - 2);
            if (ns != "loc" && ns != "bind" && ns != "wix")
            {
                return false;
            }

            var closeParen = value.IndexOf(')', firstDot);
            if (closeParen == -1)
            {
                return false;
            }

            string name;
            string scope = null;
            string defaultValue = null;

            var equalsDefaultValue = value.IndexOf('=', firstDot + 1, closeParen - firstDot);
            var end = equalsDefaultValue == -1 ? closeParen : equalsDefaultValue;
            var secondDot = value.IndexOf('.', firstDot + 1, end - firstDot);

            if (secondDot == -1)
            {
                name = value.Substring(firstDot + 1, end - firstDot - 1);
            }
            else
            {
                name = value.Substring(firstDot + 1, secondDot - firstDot - 1);
                scope = value.Substring(secondDot + 1, end - secondDot - 1);

                if (!Common.IsIdentifier(scope))
                {
                    return false;
                }
            }

            if (!Common.IsIdentifier(name))
            {
                return false;
            }

            if (equalsDefaultValue != -1 && equalsDefaultValue < closeParen)
            {
                defaultValue = value.Substring(equalsDefaultValue + 1, closeParen - equalsDefaultValue - 1);
            }

            parsedVariable = new ParsedWixVariable
            {
                Index = startWixVariable,
                Length = closeParen - startWixVariable + 1,
                Namespace = ns,
                Name = name,
                Scope = scope,
                DefaultValue = defaultValue
            };

            return true;
        }

        /// <summary>
        /// Display an unexpected attribute error.
        /// </summary>
        /// <param name="messaging"></param>
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
        /// <param name="messaging"></param>
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

        private static bool ValidIdentifierChar(char c, bool firstChar)
        {
            return ('A' <= c && 'Z' >= c) || ('a' <= c && 'z' >= c) || '_' == c ||
                   (!firstChar && (Char.IsDigit(c) || '.' == c));
        }
    }
}
