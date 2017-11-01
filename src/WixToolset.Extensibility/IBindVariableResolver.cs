// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

    public interface IBindVariableResolver
    {
        int VariableCount { get; }

        void AddVariable(string name, string value);

        void AddVariable(WixVariableTuple wixVariableRow);

        string ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly);

        string ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly, bool errorOnUnknown, out bool isDefault, out bool delayedResolve);

        string ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly, out bool isDefault);

        string ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly, out bool isDefault, out bool delayedResolve);

        bool TryGetLocalizedControl(string dialog, string control, out LocalizedControl localizedControl);
    }
}
