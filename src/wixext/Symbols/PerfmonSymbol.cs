// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Perfmon = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.Perfmon.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(PerfmonSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerfmonSymbolFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerfmonSymbolFields.Name), IntermediateFieldType.String),
            },
            typeof(PerfmonSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum PerfmonSymbolFields
    {
        ComponentRef,
        File,
        Name,
    }

    public class PerfmonSymbol : IntermediateSymbol
    {
        public PerfmonSymbol() : base(UtilSymbolDefinitions.Perfmon, null, null)
        {
        }

        public PerfmonSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.Perfmon, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PerfmonSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)PerfmonSymbolFields.ComponentRef].AsString();
            set => this.Set((int)PerfmonSymbolFields.ComponentRef, value);
        }

        public string File
        {
            get => this.Fields[(int)PerfmonSymbolFields.File].AsString();
            set => this.Set((int)PerfmonSymbolFields.File, value);
        }

        public string Name
        {
            get => this.Fields[(int)PerfmonSymbolFields.Name].AsString();
            set => this.Set((int)PerfmonSymbolFields.Name, value);
        }
    }
}