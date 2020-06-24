// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MIME = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MIME,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MIMESymbolFields.ContentType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MIMESymbolFields.ExtensionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MIMESymbolFields.CLSID), IntermediateFieldType.String),
            },
            typeof(MIMESymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MIMESymbolFields
    {
        ContentType,
        ExtensionRef,
        CLSID,
    }

    public class MIMESymbol : IntermediateSymbol
    {
        public MIMESymbol() : base(SymbolDefinitions.MIME, null, null)
        {
        }

        public MIMESymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MIME, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MIMESymbolFields index] => this.Fields[(int)index];

        public string ContentType
        {
            get => (string)this.Fields[(int)MIMESymbolFields.ContentType];
            set => this.Set((int)MIMESymbolFields.ContentType, value);
        }

        public string ExtensionRef
        {
            get => (string)this.Fields[(int)MIMESymbolFields.ExtensionRef];
            set => this.Set((int)MIMESymbolFields.ExtensionRef, value);
        }

        public string CLSID
        {
            get => (string)this.Fields[(int)MIMESymbolFields.CLSID];
            set => this.Set((int)MIMESymbolFields.CLSID, value);
        }
    }
}