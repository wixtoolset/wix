// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiAssemblyName = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiAssemblyName,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiAssemblyNameSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyNameSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyNameSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(MsiAssemblyNameSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiAssemblyNameSymbolFields
    {
        ComponentRef,
        Name,
        Value,
    }

    public class MsiAssemblyNameSymbol : IntermediateSymbol
    {
        public MsiAssemblyNameSymbol() : base(SymbolDefinitions.MsiAssemblyName, null, null)
        {
        }

        public MsiAssemblyNameSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiAssemblyName, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiAssemblyNameSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)MsiAssemblyNameSymbolFields.ComponentRef];
            set => this.Set((int)MsiAssemblyNameSymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)MsiAssemblyNameSymbolFields.Name];
            set => this.Set((int)MsiAssemblyNameSymbolFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)MsiAssemblyNameSymbolFields.Value];
            set => this.Set((int)MsiAssemblyNameSymbolFields.Value, value);
        }
    }
}