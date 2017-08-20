// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;

    /// <summary>
    /// Core interface provided by the compiler.
    /// </summary>
    public interface ICompilerCore : IMessageHandler
    {
        /// <summary>
        /// Gets whether the compiler core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        bool EncounteredError { get; }

        /// <summary>
        /// Gets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        Platform CurrentPlatform { get; }

        /// <summary>
        /// Creates a version 3 name-based UUID.
        /// </summary>
        /// <param name="namespaceGuid">The namespace UUID.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generated GUID for the given namespace and value.</returns>
        string CreateGuid(Guid namespaceGuid, string value);

        /// <summary>
        /// Create an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The new identifier.</returns>
        Identifier CreateIdentifier(string prefix, params string[] args);

        /// <summary>
        /// Create an identifier based on passed file name
        /// </summary>
        /// <param name="name">File name to generate identifer from</param>
        /// <returns></returns>
        Identifier CreateIdentifierFromFilename(string filename);

        /// <summary>
        /// Convert a bit array into an int value.
        /// </summary>
        /// <param name="bits">The bit array to convert.</param>
        /// <returns>The converted int value.</returns>
        int CreateIntegerFromBitArray(BitArray bits);

        /// <summary>
        /// Creates a row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        /// <param name="tableName">Name of table to create row in.</param>
        /// <param name="identifier">Optional identifier for the row.</param>
        /// <returns>New row.</returns>
        Row CreateRow(SourceLineNumber sourceLineNumbers, string tableName, Identifier identifier = null);

        /// <summary>
        /// Creates directories using the inline directory syntax.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="attribute">The attribute to parse.</param>
        /// <param name="parentId">Optional identifier of parent directory.</param>
        /// <returns>Identifier of the leaf directory created.</returns>
        string CreateDirectoryReferenceFromInlineSyntax(SourceLineNumber sourceLineNumbers, XAttribute attribute, string parentId);

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
        Identifier CreateRegistryRow(SourceLineNumber sourceLineNumbers, int root, string key, string name, string value, string componentId, bool escapeLeadingHash = false);

        /// <summary>
        /// Creates a short file/directory name using an identifier and long file/directory name as input.
        /// </summary>
        /// <param name="longName">The long file/directory name.</param>
        /// <param name="keepExtension">The option to keep the extension on generated short names.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="args">Any additional information to include in the hash for the generated short name.</param>
        /// <returns>The generated 8.3-compliant short file/directory name.</returns>
        string CreateShortName(string longName, bool keepExtension, bool allowWildcards, params string[] args);

        /// <summary>
        /// Create a WixSimpleReference row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="tableName">The table name of the simple reference.</param>
        /// <param name="primaryKeys">The primary keys of the simple reference.</param>
        void CreateSimpleReference(SourceLineNumber sourceLineNumbers, string tableName, params string[] primaryKeys);

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
        void CreateComplexReference(SourceLineNumber sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, string parentLanguage, ComplexReferenceChildType childType, string childId, bool isPrimary);

        /// <summary>
        /// Creates a patch resource reference to the list of resoures to be filtered when producing a patch. This method should only be used when processing children of a patch family.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        /// <param name="tableName">Name of table to create row in.</param>
        /// <param name="primaryKeys">Array of keys that make up the primary key of the table.</param>
        /// <returns>New row.</returns>
        void CreatePatchFamilyChildReference(SourceLineNumber sourceLineNumbers, string tableName, params string[] primaryKeys);

        /// <summary>
        /// Checks if the string contains a property (i.e. "foo[Property]bar")
        /// </summary>
        /// <param name="possibleProperty">String to evaluate for properties.</param>
        /// <returns>True if a property is found in the string.</returns>
        bool ContainsProperty(string possibleProperty);

        /// <summary>
        /// Add the appropriate rows to make sure that the given table shows up in the resulting output.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="tableName">Name of the table to ensure existance of.</param>
        void EnsureTable(SourceLineNumber sourceLineNumbers, string tableName);

        /// <summary>
        /// Get an attribute value and displays an error if the value is empty by default.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="emptyRule">A rule for the contents of the value. If the contents do not follow the rule, an error is thrown.</param>
        /// <returns>The attribute's value.</returns>
        string GetAttributeValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, EmptyRule emptyRule = EmptyRule.CanBeWhitespaceOnly);

        /// <summary>
        /// Gets a Bundle variable value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's value.</returns>
        string GetAttributeBundleVariableValue(SourceLineNumber sourceLineNumbers, XAttribute attribute);

        /// <summary>
        /// Get a guid attribute value and displays an error for an illegal guid value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="generatable">Determines whether the guid can be automatically generated.</param>
        /// <param name="canBeEmpty">If true, no error is raised on empty value. If false, an error is raised.</param>
        /// <returns>The attribute's guid value or a special value if an error occurred.</returns>
        string GetAttributeGuidValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool generatable = false, bool canBeEmpty = false);

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        Identifier GetAttributeIdentifier(SourceLineNumber sourceLineNumbers, XAttribute attribute);

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        string GetAttributeIdentifierValue(SourceLineNumber sourceLineNumbers, XAttribute attribute);

        /// <summary>
        /// Get an integer attribute value and displays an error for an illegal integer value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's integer value or a special value if an error occurred during conversion.</returns>
        int GetAttributeIntegerValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, int minimum, int maximum);

        /// <summary>
        /// Get a long integral attribute value and displays an error for an illegal long value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's long value or a special value if an error occurred during conversion.</returns>
        long GetAttributeLongValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, long minimum, long maximum);

        /// <summary>
        /// Gets a long filename value and displays an error for an illegal long filename value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="allowRelative">true if relative paths are allowed in the filename.</param>
        /// <returns>The attribute's long filename value.</returns>
        string GetAttributeLongFilename(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowWildcards = false, bool allowRelative = false);

        /// <summary>
        /// Gets a RegistryRoot as a MsiInterop.MsidbRegistryRoot value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowHkmu">Whether HKMU is returned as -1 (true), or treated as an error (false).</param>
        /// <returns>The attribute's RegisitryRootType value.</returns>
        int GetAttributeMsidbRegistryRootValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowHkmu);

        /// <summary>
        /// Gets a version value or possibly a binder variable and displays an error for an illegal version value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's version value.</returns>
        string GetAttributeVersionValue(SourceLineNumber sourceLineNumbers, XAttribute attribute);

        /// <summary>
        /// Gets a yes/no value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        YesNoType GetAttributeYesNoValue(SourceLineNumber sourceLineNumbers, XAttribute attribute);

        /// <summary>
        /// Gets a yes/no/default value and displays an error for an illegal yes/no/default value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        YesNoDefaultType GetAttributeYesNoDefaultValue(SourceLineNumber sourceLineNumbers, XAttribute attribute);

        /// <summary>
        /// Gets node's inner text and ensure's it is safe for use in a condition by trimming any extra whitespace.
        /// </summary>
        /// <param name="node">The node to ensure inner text is a condition.</param>
        /// <returns>The value converted into a safe condition.</returns>
        string GetConditionInnerText(XElement node);

        /// <summary>
        /// Get an element's inner text and trims any extra whitespace.
        /// </summary>
        /// <param name="element">The element with inner text to be trimmed.</param>
        /// <returns>The node's inner text trimmed.</returns>
        string GetTrimmedInnerText(XElement element);

        /// <summary>
        /// Verifies that a value is a legal identifier.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <returns>true if the value is an identifier; false otherwise.</returns>
        bool IsValidIdentifier(string value);

        /// <summary>
        /// Verifies if an identifier is a valid loc identifier.
        /// </summary>
        /// <param name="identifier">Identifier to verify.</param>
        /// <returns>True if the identifier is a valid loc identifier.</returns>
        bool IsValidLocIdentifier(string identifier);

        /// <summary>
        /// Verifies if a filename is a valid long filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="allowRelative">true if relative paths are allowed in the filename.</param>
        /// <returns>True if the filename is a valid long filename</returns>
        bool IsValidLongFilename(string filename, bool allowWildcards = false, bool allowRelative = false);

        /// <summary>
        /// Verifies if a filename is a valid short filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>True if the filename is a valid short filename</returns>
        bool IsValidShortFilename(string filename, bool allowWildcards = false);

        /// <summary>
        /// Attempts to use an extension to parse the attribute.
        /// </summary>
        /// <param name="element">Element containing attribute to be parsed.</param>
        /// <param name="attribute">Attribute to be parsed.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        void ParseExtensionAttribute(XElement element, XAttribute attribute, IDictionary<string, string> context = null);

        /// <summary>
        /// Attempts to use an extension to parse the element.
        /// </summary>
        /// <param name="parentElement">Element containing element to be parsed.</param>
        /// <param name="element">Element to be parsed.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        void ParseExtensionElement(XElement parentElement, XElement element, IDictionary<string, string> context = null);

        /// <summary>
        /// Process all children of the element looking for extensions and erroring on the unexpected.
        /// </summary>
        /// <param name="element">Element to parse children.</param>
        void ParseForExtensionElements(XElement element);

        /// <summary>
        /// Sets a bit in a bit array based on the index at which an attribute name was found in a string array.
        /// </summary>
        /// <param name="attributeNames">Array of attributes that map to bits.</param>
        /// <param name="attributeName">Name of attribute to check.</param>
        /// <param name="attributeValue">Value of attribute to check.</param>
        /// <param name="bits">The bit array in which the bit will be set if found.</param>
        /// <param name="offset">The offset into the bit array.</param>
        /// <returns>true if the bit was set; false otherwise.</returns>
        bool TrySetBitFromName(string[] attributeNames, string attributeName, YesNoType attributeValue, BitArray bits, int offset);

        /// <summary>
        /// Called when the compiler encounters an unexpected attribute.
        /// </summary>
        /// <param name="parentElement">Parent element that found unexpected attribute.</param>
        /// <param name="attribute">Unexpected attribute.</param>
        void UnexpectedAttribute(XElement element, XAttribute attribute);

        /// <summary>
        /// Called when the compiler encounters an unexpected child element.
        /// </summary>
        /// <param name="parentElement">Parent element that found unexpected child.</param>
        /// <param name="childElement">Unexpected child element.</param>
        void UnexpectedElement(XElement parentElement, XElement childElement);
    }
}
