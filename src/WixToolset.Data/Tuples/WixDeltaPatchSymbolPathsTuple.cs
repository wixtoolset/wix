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
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsTupleFields.Id), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsTupleFields.SymbolPaths), IntermediateFieldType.String),
            },
            typeof(WixDeltaPatchSymbolPathsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixDeltaPatchSymbolPathsTupleFields
    {
        Id,
        Type,
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

        public WixDeltaPatchSymbolPathsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixDeltaPatchSymbolPaths, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDeltaPatchSymbolPathsTupleFields index] => this.Fields[(int)index];

        public string Id
        {
            get => (string)this.Fields[(int)WixDeltaPatchSymbolPathsTupleFields.Id]?.Value;
            set => this.Set((int)WixDeltaPatchSymbolPathsTupleFields.Id, value);
        }

        public SymbolPathType Type
        {
            get => (SymbolPathType)Enum.Parse(typeof(SymbolPathType), (string)this.Fields[(int)WixDeltaPatchSymbolPathsTupleFields.Type]?.Value, true);
            set => this.Set((int)WixDeltaPatchSymbolPathsTupleFields.Type, value.ToString());
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)WixDeltaPatchSymbolPathsTupleFields.SymbolPaths]?.Value;
            set => this.Set((int)WixDeltaPatchSymbolPathsTupleFields.SymbolPaths, value);
        }
    }
}