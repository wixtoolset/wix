// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDeltaPatchSymbolPaths = new IntermediateTupleDefinition(
            TupleDefinitionType.WixDeltaPatchSymbolPaths,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsTupleFields.SymbolType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsTupleFields.SymbolId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsTupleFields.SymbolPaths), IntermediateFieldType.String),
            },
            typeof(WixDeltaPatchSymbolPathsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixDeltaPatchSymbolPathsTupleFields
    {
        SymbolType,
        SymbolId,
        SymbolPaths,
    }

    /// <summary>
    /// The types that the WixDeltaPatchSymbolPaths table can hold.
    /// </summary>
    /// <remarks>The order of these values is important since WixDeltaPatchSymbolPaths are sorted by this type.</remarks>
    public enum SymbolPathType
    {
        File,
        Component,
        Directory,
        Media,
        Product
    };

    public class WixDeltaPatchSymbolPathsTuple : IntermediateTuple
    {
        public WixDeltaPatchSymbolPathsTuple() : base(TupleDefinitions.WixDeltaPatchSymbolPaths, null, null)
        {
        }

        public WixDeltaPatchSymbolPathsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixDeltaPatchSymbolPaths, sourceLineNumber, null)
        {
        }

        public IntermediateField this[WixDeltaPatchSymbolPathsTupleFields index] => this.Fields[(int)index];

        public SymbolPathType SymbolType
        {
            get => (SymbolPathType)this.Fields[(int)WixDeltaPatchSymbolPathsTupleFields.SymbolType].AsNumber();
            set => this.Set((int)WixDeltaPatchSymbolPathsTupleFields.SymbolType, (int)value);
        }

        public string SymbolId
        {
            get => (string)this.Fields[(int)WixDeltaPatchSymbolPathsTupleFields.SymbolId];
            set => this.Set((int)WixDeltaPatchSymbolPathsTupleFields.SymbolId, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)WixDeltaPatchSymbolPathsTupleFields.SymbolPaths];
            set => this.Set((int)WixDeltaPatchSymbolPathsTupleFields.SymbolPaths, value);
        }
    }
}