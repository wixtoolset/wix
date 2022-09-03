// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface provided to help backend extensions.
    /// </summary>
    public interface IBackendHelper : ILayoutServices
    {
        /// <summary>
        /// Creates a MSI compatible GUID.
        /// </summary>
        /// <returns>Creates an uppercase GUID with braces.</returns>
        string CreateGuid();

        /// <summary>
        /// Creates a version 3 name-based UUID.
        /// </summary>
        /// <param name="namespaceGuid">The namespace UUID.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generated GUID for the given namespace and value.</returns>
        string CreateGuid(Guid namespaceGuid, string value);

        /// <summary>
        /// Creates a resolved directory.
        /// </summary>
        /// <param name="directoryParent">Directory parent identifier.</param>
        /// <param name="name">Name of directory.</param>
        /// <returns>Resolved directory.</returns>
        IResolvedDirectory CreateResolvedDirectory(string directoryParent, string name);

        /// <summary>
        /// Extracts embedded files.
        /// </summary>
        /// <param name="embeddedFiles">Embedded files to extract.</param>
        /// <returns><c>ITrackedFile</c> for each embedded file extracted.</returns>
        IReadOnlyList<ITrackedFile> ExtractEmbeddedFiles(IEnumerable<IExpectedExtractFile> embeddedFiles);

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        string GenerateIdentifier(string prefix, params string[] args);

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
        int GetValidCodePage(string value, bool allowNoChange = false, bool onlyAnsi = false, SourceLineNumber sourceLineNumbers = null);

        /// <summary>
        /// Get a source/target and short/long file name from an MSI Filename column.
        /// </summary>
        /// <param name="value">The Filename value.</param>
        /// <param name="source">true to get a source name; false to get a target name</param>
        /// <param name="longName">true to get a long name; false to get a short name</param>
        /// <returns>The requesed file name.</returns>
        string GetMsiFileName(string value, bool source, bool longName);

        /// <summary>
        /// Verifies if an identifier is a valid binder variable name.
        /// </summary>
        /// <param name="variable">Binder variable name to verify.</param>
        /// <returns>True if the identifier is a valid binder variable name.</returns>
        bool IsValidBinderVariable(string variable);

        /// <summary>
        /// Verifies the given string is a valid 4-part version.
        /// </summary>
        /// <param name="version">The version to verify.</param>
        /// <returns>True if version is a valid 4-part version.</returns>
        bool IsValidFourPartVersion(string version);

        /// <summary>
        /// Verifies the given string is a valid MSI product version.
        /// </summary>
        /// <param name="version">The MSI product version to verify.</param>
        /// <returns>True if version is a valid MSI product version</returns>
        bool IsValidMsiProductVersion(string version);

        /// <summary>
        /// Verifies the given string is a valid WiX version.
        /// </summary>
        /// <param name="version">The version to verify.</param>
        /// <returns>True if version is a valid WiX version.</returns>
        bool IsValidWixVersion(string version);

        /// <summary>
        /// Determines if value is a valid identifier.
        /// </summary>
        /// <param name="id">Identifier to validate.</param>
        /// <returns>True if valid identifier, otherwise false.</returns>
        bool IsValidIdentifier(string id);

        /// <summary>
        /// Verifies the given string is a valid long filename.
        /// </summary>
        /// <param name="filename">The filename to verify.</param>
        /// <param name="allowWildcards">Allow wildcards in the filename.</param>
        /// <param name="allowRelative">Allow long file name to be a relative path.</param>
        /// <returns>True if filename is a valid long filename.</returns>
        bool IsValidLongFilename(string filename, bool allowWildcards, bool allowRelative);

        /// <summary>
        /// Verifies the given string is a valid short filename.
        /// </summary>
        /// <param name="filename">The filename to verify.</param>
        /// <param name="allowWildcards">Allow wildcards in the filename.</param>
        /// <returns>True if filename is a valid short filename.</returns>
        bool IsValidShortFilename(string filename, bool allowWildcards);

        /// <summary>
        /// Resolve delayed fields.
        /// </summary>
        /// <param name="delayedFields">The fields which had resolution delayed.</param>
        /// <param name="variableCache">The cached variable values used when resolving delayed fields.</param>
        void ResolveDelayedFields(IEnumerable<IDelayedField> delayedFields, Dictionary<string, string> variableCache);

        /// <summary>
        /// Get the source/target and short/long file names from an MSI Filename column.
        /// </summary>
        /// <param name="value">The Filename value.</param>
        /// <returns>An array of strings of length 4. The contents are: short target, long target, short source, and long source.</returns>
        /// <remarks>
        /// If any particular file name part is not parsed, its set to null in the appropriate location of the returned array of strings.
        /// Thus the returned array will always be of length 4.
        /// </remarks>
        string[] SplitMsiFileName(string value);

        /// <summary>
        /// Tries to parse a version from the provided version string.
        /// </summary>
        /// <param name="version">The version to verify and parse.</param>
        /// <param name="parsedVersion">The parsed result if possible, otherwise null.</param>
        /// <returns>True if the version was able to parsed, otherwise false.</returns>
        bool TryParseFourPartVersion(string version, out string parsedVersion);

        /// <summary>
        /// Tries to parse an MSI product version from the provided version string.
        /// </summary>
        /// <param name="version">The version to verify and parse.</param>
        /// <param name="strict">Indicates whether to return a strict (255.255.65535) product version or any valid product version (255.255.65535.*).</param>
        /// <param name="parsedVersion">The parsed result if possible, otherwise null.</param>
        /// <returns>True if the version was able to parsed as an product version, otherwise false.</returns>
        bool TryParseMsiProductVersion(string version, bool strict, out string parsedVersion);
    }
}
