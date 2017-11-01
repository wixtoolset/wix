// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixSimpleReference = new IntermediateTupleDefinition(
            TupleDefinitionType.WixSimpleReference,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSimpleReferenceTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSimpleReferenceTupleFields.PrimaryKeys), IntermediateFieldType.String),
            },
            typeof(WixSimpleReferenceTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixSimpleReferenceTupleFields
    {
        Table,
        PrimaryKeys,
    }

    public class WixSimpleReferenceTuple : IntermediateTuple
    {
        public WixSimpleReferenceTuple() : base(TupleDefinitions.WixSimpleReference, null, null)
        {
        }

        public WixSimpleReferenceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixSimpleReference, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSimpleReferenceTupleFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)WixSimpleReferenceTupleFields.Table]?.Value;
            set => this.Set((int)WixSimpleReferenceTupleFields.Table, value);
        }

        public string PrimaryKeys
        {
            get => (string)this.Fields[(int)WixSimpleReferenceTupleFields.PrimaryKeys]?.Value;
            set => this.Set((int)WixSimpleReferenceTupleFields.PrimaryKeys, value);
        }

        /// <summary>
        /// Gets the symbolic name.
        /// </summary>
        /// <value>Symbolic name.</value>
        public string SymbolicName => String.Concat(this.Table, ":", this.PrimaryKeys);
    }
}