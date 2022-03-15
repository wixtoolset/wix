// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface provided to help with bundle validation.
    /// </summary>
    public interface IBundleValidator
    {

        /// <summary>
        /// Validates path is relative and canonicalizes it.
        /// For example, "a\..\c\.\d.exe" => "c\d.exe".
        /// </summary>
        /// <param name="sourceLineNumbers"></param>
        /// <param name="elementName"></param>
        /// <param name="attributeName"></param>
        /// <param name="relativePath"></param>
        /// <returns>The original value if not relative, otherwise the canonicalized relative path.</returns>
        string GetCanonicalRelativePath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string relativePath);

        /// <summary>
        /// Validates an MsiProperty name value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers"></param>
        /// <param name="elementName"></param>
        /// <param name="attributeName"></param>
        /// <param name="propertyName"></param>
        /// <returns>Whether the name is valid.</returns>
        bool ValidateBundleMsiPropertyName(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string propertyName);

        /// <summary>
        /// Validates a Bundle variable name and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers"></param>
        /// <param name="elementName"></param>
        /// <param name="attributeName"></param>
        /// <param name="variableName"></param>
        /// <returns>Whether the name is valid.</returns>
        bool ValidateBundleVariableName(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string variableName);

        /// <summary>
        /// Validates a bundle condition and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers"></param>
        /// <param name="elementName"></param>
        /// <param name="attributeName"></param>
        /// <param name="condition"></param>
        /// <param name="phase"></param>
        /// <returns>Whether the condition is valid.</returns>
        bool ValidateBundleCondition(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string condition, BundleConditionPhase phase);
    }
}
