// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleTag = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleTag,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleTagSymbolFields.Filename), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTagSymbolFields.Regid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTagSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTagSymbolFields.InstallPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleTagSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleTagSymbolFields.Xml), IntermediateFieldType.String),
            },
            typeof(WixBundleTagSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleTagSymbolFields
    {
        Filename,
        Regid,
        Name,
        InstallPath,
        Attributes,
        Xml,
    }

    public class WixBundleTagSymbol : IntermediateSymbol
    {
        public WixBundleTagSymbol() : base(SymbolDefinitions.WixBundleTag, null, null)
        {
        }

        public WixBundleTagSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleTag, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleTagSymbolFields index] => this.Fields[(int)index];

        public string Filename
        {
            get => this.Fields[(int)WixBundleTagSymbolFields.Filename].AsString();
            set => this.Set((int)WixBundleTagSymbolFields.Filename, value);
        }

        public string Regid
        {
            get => this.Fields[(int)WixBundleTagSymbolFields.Regid].AsString();
            set => this.Set((int)WixBundleTagSymbolFields.Regid, value);
        }

        public string Name
        {
            get => this.Fields[(int)WixBundleTagSymbolFields.Name].AsString();
            set => this.Set((int)WixBundleTagSymbolFields.Name, value);
        }

        public string InstallPath
        {
            get => this.Fields[(int)WixBundleTagSymbolFields.InstallPath].AsString();
            set => this.Set((int)WixBundleTagSymbolFields.InstallPath, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixBundleTagSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixBundleTagSymbolFields.Attributes, value);
        }

        public string Xml
        {
            get => this.Fields[(int)WixBundleTagSymbolFields.Xml].AsString();
            set => this.Set((int)WixBundleTagSymbolFields.Xml, value);
        }
    }
}
