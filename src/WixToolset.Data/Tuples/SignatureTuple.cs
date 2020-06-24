// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Signature = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Signature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(SignatureSymbolFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SignatureSymbolFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SignatureSymbolFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SignatureSymbolFields.MinSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SignatureSymbolFields.MaxSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SignatureSymbolFields.MinDate), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SignatureSymbolFields.MaxDate), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SignatureSymbolFields.Languages), IntermediateFieldType.String),
            },
            typeof(SignatureSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum SignatureSymbolFields
    {
        FileName,
        MinVersion,
        MaxVersion,
        MinSize,
        MaxSize,
        MinDate,
        MaxDate,
        Languages,
    }

    public class SignatureSymbol : IntermediateSymbol
    {
        public SignatureSymbol() : base(SymbolDefinitions.Signature, null, null)
        {
        }

        public SignatureSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Signature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SignatureSymbolFields index] => this.Fields[(int)index];

        public string FileName
        {
            get => (string)this.Fields[(int)SignatureSymbolFields.FileName];
            set => this.Set((int)SignatureSymbolFields.FileName, value);
        }

        public string MinVersion
        {
            get => (string)this.Fields[(int)SignatureSymbolFields.MinVersion];
            set => this.Set((int)SignatureSymbolFields.MinVersion, value);
        }

        public string MaxVersion
        {
            get => (string)this.Fields[(int)SignatureSymbolFields.MaxVersion];
            set => this.Set((int)SignatureSymbolFields.MaxVersion, value);
        }

        public int? MinSize
        {
            get => (int?)this.Fields[(int)SignatureSymbolFields.MinSize];
            set => this.Set((int)SignatureSymbolFields.MinSize, value);
        }

        public int? MaxSize
        {
            get => (int?)this.Fields[(int)SignatureSymbolFields.MaxSize];
            set => this.Set((int)SignatureSymbolFields.MaxSize, value);
        }

        public int? MinDate
        {
            get => (int?)this.Fields[(int)SignatureSymbolFields.MinDate];
            set => this.Set((int)SignatureSymbolFields.MinDate, value);
        }

        public int? MaxDate
        {
            get => (int?)this.Fields[(int)SignatureSymbolFields.MaxDate];
            set => this.Set((int)SignatureSymbolFields.MaxDate, value);
        }

        public string Languages
        {
            get => (string)this.Fields[(int)SignatureSymbolFields.Languages];
            set => this.Set((int)SignatureSymbolFields.Languages, value);
        }
    }
}