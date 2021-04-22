// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixUpdateRegistration = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixUpdateRegistration,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationSymbolFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationSymbolFields.Department), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationSymbolFields.ProductFamily), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationSymbolFields.Classification), IntermediateFieldType.String),
            },
            typeof(WixUpdateRegistrationSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixUpdateRegistrationSymbolFields
    {
        Manufacturer,
        Department,
        ProductFamily,
        Name,
        Classification,
    }

    public class WixUpdateRegistrationSymbol : IntermediateSymbol
    {
        public WixUpdateRegistrationSymbol() : base(SymbolDefinitions.WixUpdateRegistration, null, null)
        {
        }

        public WixUpdateRegistrationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixUpdateRegistration, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixUpdateRegistrationSymbolFields index] => this.Fields[(int)index];

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationSymbolFields.Manufacturer];
            set => this.Set((int)WixUpdateRegistrationSymbolFields.Manufacturer, value);
        }

        public string Department
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationSymbolFields.Department];
            set => this.Set((int)WixUpdateRegistrationSymbolFields.Department, value);
        }

        public string ProductFamily
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationSymbolFields.ProductFamily];
            set => this.Set((int)WixUpdateRegistrationSymbolFields.ProductFamily, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationSymbolFields.Name];
            set => this.Set((int)WixUpdateRegistrationSymbolFields.Name, value);
        }

        public string Classification
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationSymbolFields.Classification];
            set => this.Set((int)WixUpdateRegistrationSymbolFields.Classification, value);
        }
    }
}