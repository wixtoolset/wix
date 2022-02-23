// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPackageTag = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixPackageTag,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPackageTagSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageTagSymbolFields.Regid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageTagSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPackageTagSymbolFields.Attributes), IntermediateFieldType.Number)
            },
            typeof(WixPackageTagSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixPackageTagSymbolFields
    {
        FileRef,
        Regid,
        Name,
        Attributes
    }

    public class WixPackageTagSymbol : IntermediateSymbol
    {
        public WixPackageTagSymbol() : base(SymbolDefinitions.WixPackageTag, null, null)
        {
        }

        public WixPackageTagSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixPackageTag, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPackageTagSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => this.Fields[(int)WixPackageTagSymbolFields.FileRef].AsString();
            set => this.Set((int)WixPackageTagSymbolFields.FileRef, value);
        }

        public string Regid
        {
            get => this.Fields[(int)WixPackageTagSymbolFields.Regid].AsString();
            set => this.Set((int)WixPackageTagSymbolFields.Regid, value);
        }

        public string Name
        {
            get => this.Fields[(int)WixPackageTagSymbolFields.Name].AsString();
            set => this.Set((int)WixPackageTagSymbolFields.Name, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixPackageTagSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixPackageTagSymbolFields.Attributes, value);
        }
    }
}
