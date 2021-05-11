// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiPatchOldAssemblyName = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiPatchOldAssemblyName,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyNameSymbolFields.Assembly), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyNameSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyNameSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(MsiPatchOldAssemblyNameSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiPatchOldAssemblyNameSymbolFields
    {
        Assembly,
        Name,
        Value,
    }

    public class MsiPatchOldAssemblyNameSymbol : IntermediateSymbol
    {
        public MsiPatchOldAssemblyNameSymbol() : base(SymbolDefinitions.MsiPatchOldAssemblyName, null, null)
        {
        }

        public MsiPatchOldAssemblyNameSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiPatchOldAssemblyName, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchOldAssemblyNameSymbolFields index] => this.Fields[(int)index];

        public string Assembly
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyNameSymbolFields.Assembly];
            set => this.Set((int)MsiPatchOldAssemblyNameSymbolFields.Assembly, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyNameSymbolFields.Name];
            set => this.Set((int)MsiPatchOldAssemblyNameSymbolFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyNameSymbolFields.Value];
            set => this.Set((int)MsiPatchOldAssemblyNameSymbolFields.Value, value);
        }
    }
}