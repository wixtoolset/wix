// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IsolatedComponent = new IntermediateSymbolDefinition(
            SymbolDefinitionType.IsolatedComponent,
            new[]
            {
                new IntermediateFieldDefinition(nameof(IsolatedComponentSymbolFields.SharedComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IsolatedComponentSymbolFields.ApplicationComponentRef), IntermediateFieldType.String),
            },
            typeof(IsolatedComponentSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum IsolatedComponentSymbolFields
    {
        SharedComponentRef,
        ApplicationComponentRef,
    }

    public class IsolatedComponentSymbol : IntermediateSymbol
    {
        public IsolatedComponentSymbol() : base(SymbolDefinitions.IsolatedComponent, null, null)
        {
        }

        public IsolatedComponentSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.IsolatedComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IsolatedComponentSymbolFields index] => this.Fields[(int)index];

        public string SharedComponentRef
        {
            get => (string)this.Fields[(int)IsolatedComponentSymbolFields.SharedComponentRef];
            set => this.Set((int)IsolatedComponentSymbolFields.SharedComponentRef, value);
        }

        public string ApplicationComponentRef
        {
            get => (string)this.Fields[(int)IsolatedComponentSymbolFields.ApplicationComponentRef];
            set => this.Set((int)IsolatedComponentSymbolFields.ApplicationComponentRef, value);
        }
    }
}
