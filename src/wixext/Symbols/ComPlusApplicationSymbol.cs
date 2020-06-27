// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusApplication = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusApplication.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusApplicationSymbolFields.PartitionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationSymbolFields.ApplicationId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationSymbolFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusApplicationSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusApplicationSymbolFields
    {
        PartitionRef,
        ComponentRef,
        ApplicationId,
        Name,
    }

    public class ComPlusApplicationSymbol : IntermediateSymbol
    {
        public ComPlusApplicationSymbol() : base(ComPlusSymbolDefinitions.ComPlusApplication, null, null)
        {
        }

        public ComPlusApplicationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusApplicationSymbolFields index] => this.Fields[(int)index];

        public string PartitionRef
        {
            get => this.Fields[(int)ComPlusApplicationSymbolFields.PartitionRef].AsString();
            set => this.Set((int)ComPlusApplicationSymbolFields.PartitionRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusApplicationSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusApplicationSymbolFields.ComponentRef, value);
        }

        public string ApplicationId
        {
            get => this.Fields[(int)ComPlusApplicationSymbolFields.ApplicationId].AsString();
            set => this.Set((int)ComPlusApplicationSymbolFields.ApplicationId, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationSymbolFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationSymbolFields.Name, value);
        }
    }
}