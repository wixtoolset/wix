// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.VisualStudio.Symbols;

    public static partial class VSSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition HelpFile = new IntermediateSymbolDefinition(
            VSSymbolDefinitionType.HelpFile.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(HelpFileSymbolFields.HelpFileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileSymbolFields.LangID), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(HelpFileSymbolFields.HxSFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileSymbolFields.HxIFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileSymbolFields.HxQFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileSymbolFields.HxRFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(HelpFileSymbolFields.SamplesFileRef), IntermediateFieldType.String),
            },
            typeof(HelpFileSymbol));
    }
}

namespace WixToolset.VisualStudio.Symbols
{
    using WixToolset.Data;

    public enum HelpFileSymbolFields
    {
        HelpFileName,
        LangID,
        HxSFileRef,
        HxIFileRef,
        HxQFileRef,
        HxRFileRef,
        SamplesFileRef,
    }

    public class HelpFileSymbol : IntermediateSymbol
    {
        public HelpFileSymbol() : base(VSSymbolDefinitions.HelpFile, null, null)
        {
        }

        public HelpFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(VSSymbolDefinitions.HelpFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[HelpFileSymbolFields index] => this.Fields[(int)index];

        public string HelpFileName
        {
            get => this.Fields[(int)HelpFileSymbolFields.HelpFileName].AsString();
            set => this.Set((int)HelpFileSymbolFields.HelpFileName, value);
        }

        public int? LangID
        {
            get => this.Fields[(int)HelpFileSymbolFields.LangID].AsNullableNumber();
            set => this.Set((int)HelpFileSymbolFields.LangID, value);
        }

        public string HxSFileRef
        {
            get => this.Fields[(int)HelpFileSymbolFields.HxSFileRef].AsString();
            set => this.Set((int)HelpFileSymbolFields.HxSFileRef, value);
        }

        public string HxIFileRef
        {
            get => this.Fields[(int)HelpFileSymbolFields.HxIFileRef].AsString();
            set => this.Set((int)HelpFileSymbolFields.HxIFileRef, value);
        }

        public string HxQFileRef
        {
            get => this.Fields[(int)HelpFileSymbolFields.HxQFileRef].AsString();
            set => this.Set((int)HelpFileSymbolFields.HxQFileRef, value);
        }

        public string HxRFileRef
        {
            get => this.Fields[(int)HelpFileSymbolFields.HxRFileRef].AsString();
            set => this.Set((int)HelpFileSymbolFields.HxRFileRef, value);
        }

        public string SamplesFileRef
        {
            get => this.Fields[(int)HelpFileSymbolFields.SamplesFileRef].AsString();
            set => this.Set((int)HelpFileSymbolFields.SamplesFileRef, value);
        }
    }
}