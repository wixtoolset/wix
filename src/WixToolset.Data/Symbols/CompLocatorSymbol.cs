// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition CompLocator = new IntermediateSymbolDefinition(
            SymbolDefinitionType.CompLocator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CompLocatorSymbolFields.SignatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CompLocatorSymbolFields.ComponentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CompLocatorSymbolFields.Type), IntermediateFieldType.Number),
            },
            typeof(CompLocatorSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum CompLocatorSymbolFields
    {
        SignatureRef,
        ComponentId,
        Type,
    }

    public class CompLocatorSymbol : IntermediateSymbol
    {
        public CompLocatorSymbol() : base(SymbolDefinitions.CompLocator, null, null)
        {
        }

        public CompLocatorSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.CompLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CompLocatorSymbolFields index] => this.Fields[(int)index];

        public string SignatureRef
        {
            get => (string)this.Fields[(int)CompLocatorSymbolFields.SignatureRef];
            set => this.Set((int)CompLocatorSymbolFields.SignatureRef, value);
        }

        public string ComponentId
        {
            get => (string)this.Fields[(int)CompLocatorSymbolFields.ComponentId];
            set => this.Set((int)CompLocatorSymbolFields.ComponentId, value);
        }

        public LocatorType Type
        {
            get => (LocatorType)this.Fields[(int)CompLocatorSymbolFields.Type].AsNumber();
            set => this.Set((int)CompLocatorSymbolFields.Type, (int)value);
        }
    }
}