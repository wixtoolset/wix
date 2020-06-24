// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public interface ISymbolDefinitionCreator
    {
        void AddCustomSymbolDefinition(IntermediateSymbolDefinition definition);

        bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition);
    }
}
