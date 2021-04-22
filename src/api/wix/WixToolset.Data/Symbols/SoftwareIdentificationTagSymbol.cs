// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition SoftwareIdentificationTag = new IntermediateSymbolDefinition(
            SymbolDefinitionType.SoftwareIdentificationTag,
            new[]
            {
                new IntermediateFieldDefinition(nameof(SoftwareIdentificationTagSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SoftwareIdentificationTagSymbolFields.Regid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SoftwareIdentificationTagSymbolFields.UniqueId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SoftwareIdentificationTagSymbolFields.PersistentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SoftwareIdentificationTagSymbolFields.Alias), IntermediateFieldType.String),
            },
            typeof(SoftwareIdentificationTagSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum SoftwareIdentificationTagSymbolFields
    {
        FileRef,
        Regid,
        UniqueId,
        PersistentId,
        Alias,
    }

    public class SoftwareIdentificationTagSymbol : IntermediateSymbol
    {
        public SoftwareIdentificationTagSymbol() : base(SymbolDefinitions.SoftwareIdentificationTag, null, null)
        {
        }

        public SoftwareIdentificationTagSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.SoftwareIdentificationTag, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SoftwareIdentificationTagSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => this.Fields[(int)SoftwareIdentificationTagSymbolFields.FileRef].AsString();
            set => this.Set((int)SoftwareIdentificationTagSymbolFields.FileRef, value);
        }

        public string Regid
        {
            get => this.Fields[(int)SoftwareIdentificationTagSymbolFields.Regid].AsString();
            set => this.Set((int)SoftwareIdentificationTagSymbolFields.Regid, value);
        }

        public string TagId
        {
            get => this.Fields[(int)SoftwareIdentificationTagSymbolFields.UniqueId].AsString();
            set => this.Set((int)SoftwareIdentificationTagSymbolFields.UniqueId, value);
        }

        public string PersistentId
        {
            get => this.Fields[(int)SoftwareIdentificationTagSymbolFields.PersistentId].AsString();
            set => this.Set((int)SoftwareIdentificationTagSymbolFields.PersistentId, value);
        }

        public string Alias
        {
            get => this.Fields[(int)SoftwareIdentificationTagSymbolFields.Alias].AsString();
            set => this.Set((int)SoftwareIdentificationTagSymbolFields.Alias, value);
        }
    }
}
