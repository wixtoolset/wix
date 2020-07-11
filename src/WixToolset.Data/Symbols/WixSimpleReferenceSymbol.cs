// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixSimpleReference = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixSimpleReference,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixSimpleReferenceSymbolFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixSimpleReferenceSymbolFields.PrimaryKeys), IntermediateFieldType.String),
            },
            typeof(WixSimpleReferenceSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;
    using System.Diagnostics;

    public enum WixSimpleReferenceSymbolFields
    {
        Table,
        PrimaryKeys,
    }

    [DebuggerDisplay("{SymbolicName,nq}")]
    public class WixSimpleReferenceSymbol : IntermediateSymbol
    {
        public WixSimpleReferenceSymbol() : base(SymbolDefinitions.WixSimpleReference, null, null)
        {
        }

        public WixSimpleReferenceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixSimpleReference, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixSimpleReferenceSymbolFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)WixSimpleReferenceSymbolFields.Table];
            set => this.Set((int)WixSimpleReferenceSymbolFields.Table, value);
        }

        public string PrimaryKeys
        {
            get => (string)this.Fields[(int)WixSimpleReferenceSymbolFields.PrimaryKeys];
            set => this.Set((int)WixSimpleReferenceSymbolFields.PrimaryKeys, value);
        }

        /// <summary>
        /// Gets the symbolic name.
        /// </summary>
        /// <value>Symbolic name.</value>
        public string SymbolicName => String.Concat("Ref ",  this.Table, ":", this.PrimaryKeys);
    }
}