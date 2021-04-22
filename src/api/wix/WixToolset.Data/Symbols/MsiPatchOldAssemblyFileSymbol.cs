// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiPatchOldAssemblyFile = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiPatchOldAssemblyFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyFileSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyFileSymbolFields.AssemblyRef), IntermediateFieldType.String),
            },
            typeof(MsiPatchOldAssemblyFileSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiPatchOldAssemblyFileSymbolFields
    {
        FileRef,
        AssemblyRef,
    }

    public class MsiPatchOldAssemblyFileSymbol : IntermediateSymbol
    {
        public MsiPatchOldAssemblyFileSymbol() : base(SymbolDefinitions.MsiPatchOldAssemblyFile, null, null)
        {
        }

        public MsiPatchOldAssemblyFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiPatchOldAssemblyFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchOldAssemblyFileSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyFileSymbolFields.FileRef];
            set => this.Set((int)MsiPatchOldAssemblyFileSymbolFields.FileRef, value);
        }

        public string AssemblyRef
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyFileSymbolFields.AssemblyRef];
            set => this.Set((int)MsiPatchOldAssemblyFileSymbolFields.AssemblyRef, value);
        }
    }
}