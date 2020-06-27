// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebAddress = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebAddress.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebAddressSymbolFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebAddressSymbolFields.IP), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebAddressSymbolFields.Port), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebAddressSymbolFields.Header), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebAddressSymbolFields.Secure), IntermediateFieldType.Number),
            },
            typeof(IIsWebAddressSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebAddressSymbolFields
    {
        WebRef,
        IP,
        Port,
        Header,
        Secure,
    }

    public class IIsWebAddressSymbol : IntermediateSymbol
    {
        public IIsWebAddressSymbol() : base(IisSymbolDefinitions.IIsWebAddress, null, null)
        {
        }

        public IIsWebAddressSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebAddress, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebAddressSymbolFields index] => this.Fields[(int)index];

        public string WebRef
        {
            get => this.Fields[(int)IIsWebAddressSymbolFields.WebRef].AsString();
            set => this.Set((int)IIsWebAddressSymbolFields.WebRef, value);
        }

        public string IP
        {
            get => this.Fields[(int)IIsWebAddressSymbolFields.IP].AsString();
            set => this.Set((int)IIsWebAddressSymbolFields.IP, value);
        }

        public string Port
        {
            get => this.Fields[(int)IIsWebAddressSymbolFields.Port].AsString();
            set => this.Set((int)IIsWebAddressSymbolFields.Port, value);
        }

        public string Header
        {
            get => this.Fields[(int)IIsWebAddressSymbolFields.Header].AsString();
            set => this.Set((int)IIsWebAddressSymbolFields.Header, value);
        }

        public int? Secure
        {
            get => this.Fields[(int)IIsWebAddressSymbolFields.Secure].AsNullableNumber();
            set => this.Set((int)IIsWebAddressSymbolFields.Secure, value);
        }
    }
}