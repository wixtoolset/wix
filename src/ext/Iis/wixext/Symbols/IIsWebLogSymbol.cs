// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebLog = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebLog.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebLogSymbolFields.Format), IntermediateFieldType.String),
            },
            typeof(IIsWebLogSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebLogSymbolFields
    {
        Format,
    }

    public class IIsWebLogSymbol : IntermediateSymbol
    {
        public IIsWebLogSymbol() : base(IisSymbolDefinitions.IIsWebLog, null, null)
        {
        }

        public IIsWebLogSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebLog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebLogSymbolFields index] => this.Fields[(int)index];

        public string Format
        {
            get => this.Fields[(int)IIsWebLogSymbolFields.Format].AsString();
            set => this.Set((int)IIsWebLogSymbolFields.Format, value);
        }
    }
}