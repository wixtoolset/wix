// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixDeltaPatchSymbolPaths = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixDeltaPatchSymbolPaths,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsSymbolFields.SymbolType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsSymbolFields.SymbolId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDeltaPatchSymbolPathsSymbolFields.SymbolPaths), IntermediateFieldType.String),
            },
            typeof(WixDeltaPatchSymbolPathsSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixDeltaPatchSymbolPathsSymbolFields
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

    public class WixDeltaPatchSymbolPathsSymbol : IntermediateSymbol
    {
        public WixDeltaPatchSymbolPathsSymbol() : base(SymbolDefinitions.WixDeltaPatchSymbolPaths, null, null)
        {
        }

        public WixDeltaPatchSymbolPathsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixDeltaPatchSymbolPaths, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDeltaPatchSymbolPathsSymbolFields index] => this.Fields[(int)index];

        public SymbolPathType SymbolType
        {
            get => (SymbolPathType)this.Fields[(int)WixDeltaPatchSymbolPathsSymbolFields.SymbolType].AsNumber();
            set => this.Set((int)WixDeltaPatchSymbolPathsSymbolFields.SymbolType, (int)value);
        }

        public string SymbolId
        {
            get => (string)this.Fields[(int)WixDeltaPatchSymbolPathsSymbolFields.SymbolId];
            set => this.Set((int)WixDeltaPatchSymbolPathsSymbolFields.SymbolId, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)WixDeltaPatchSymbolPathsSymbolFields.SymbolPaths];
            set => this.Set((int)WixDeltaPatchSymbolPathsSymbolFields.SymbolPaths, value);
        }
    }
}