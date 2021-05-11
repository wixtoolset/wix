// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusComponent = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusComponent.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusComponentSymbolFields.AssemblyRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusComponentSymbolFields.CLSID), IntermediateFieldType.String),
            },
            typeof(ComPlusComponentSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusComponentSymbolFields
    {
        AssemblyRef,
        CLSID,
    }

    public class ComPlusComponentSymbol : IntermediateSymbol
    {
        public ComPlusComponentSymbol() : base(ComPlusSymbolDefinitions.ComPlusComponent, null, null)
        {
        }

        public ComPlusComponentSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusComponentSymbolFields index] => this.Fields[(int)index];

        public string AssemblyRef
        {
            get => this.Fields[(int)ComPlusComponentSymbolFields.AssemblyRef].AsString();
            set => this.Set((int)ComPlusComponentSymbolFields.AssemblyRef, value);
        }

        public string CLSID
        {
            get => this.Fields[(int)ComPlusComponentSymbolFields.CLSID].AsString();
            set => this.Set((int)ComPlusComponentSymbolFields.CLSID, value);
        }
    }
}