// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition PatchPackage = new IntermediateSymbolDefinition(
            SymbolDefinitionType.PatchPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PatchPackageSymbolFields.PatchId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchPackageSymbolFields.MediaDiskIdRef), IntermediateFieldType.Number),
            },
            typeof(PatchPackageSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum PatchPackageSymbolFields
    {
        PatchId,
        MediaDiskIdRef,
    }

    public class PatchPackageSymbol : IntermediateSymbol
    {
        public PatchPackageSymbol() : base(SymbolDefinitions.PatchPackage, null, null)
        {
        }

        public PatchPackageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.PatchPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PatchPackageSymbolFields index] => this.Fields[(int)index];

        public string PatchId
        {
            get => (string)this.Fields[(int)PatchPackageSymbolFields.PatchId];
            set => this.Set((int)PatchPackageSymbolFields.PatchId, value);
        }

        public int MediaDiskIdRef
        {
            get => (int)this.Fields[(int)PatchPackageSymbolFields.MediaDiskIdRef];
            set => this.Set((int)PatchPackageSymbolFields.MediaDiskIdRef, value);
        }
    }
}