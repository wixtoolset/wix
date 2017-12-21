// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Data;

    public interface IBindVariableResolver
    {
        int VariableCount { get; }

        void AddVariable(string name, string value, bool overridable);

        BindVariableResolution ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly);

        bool TryGetLocalizedControl(string dialog, string control, out LocalizedControl localizedControl);
    }
}
