// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Data;

    public interface IVariableResolver
    {
        /// <summary>
        /// Gets the codepage.
        /// </summary>
        int Codepage { get; }

        /// <summary>
        /// Gets the count of variables added to the resolver.
        /// </summary>
        int VariableCount { get; }

        void AddLocalization(Localization localization);

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        /// <param name="overridable">Indicates whether the variable can be overridden by an existing variable.</param>
        void AddVariable(SourceLineNumber sourceLineNumber, string name, string value, bool overridable);

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <returns>The resolved result.</returns>
        IVariableResolution ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly);

        /// <summary>
        /// Try to find localization information for dialog and (optional) control.
        /// </summary>
        /// <param name="dialog">Dialog identifier.</param>
        /// <param name="control">Optional control identifier.</param>
        /// <param name="localizedControl">Found localization information.</param>
        /// <returns>True if localized control was found, otherwise false.</returns>
        bool TryGetLocalizedControl(string dialog, string control, out LocalizedControl localizedControl);
    }
}
