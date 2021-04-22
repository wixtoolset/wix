// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPatchId = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixPatch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchSymbolFields.ClientPatchId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPatchSymbolFields.OptimizePatchSizeForLargeFiles), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixPatchSymbolFields.ApiPatchingSymbolFlags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPatchSymbolFields.Codepage), IntermediateFieldType.String),
            },
            typeof(WixPatchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixPatchSymbolFields
    {
        ClientPatchId,
        OptimizePatchSizeForLargeFiles,
        ApiPatchingSymbolFlags,
        Codepage,
    }

    /// <summary>
    /// The following flags are used with PATCH_OPTION_DATA SymbolOptionFlags:
    /// </summary>
    [Flags]
    [CLSCompliant(false)]
    public enum PatchSymbolFlags : uint
    {
        /// <summary>
        /// Don't use imagehlp.dll
        /// </summary>
        PatchSymbolNoImagehlp = 0x00000001,

        /// <summary>
        /// Don't fail patch due to imagehlp failures.
        /// </summary>
        PatchSymbolNoFailures = 0x00000002,

        /// <summary>
        /// After matching decorated symbols, try to match remaining by undecorated names.
        /// </summary>
        PatchSymbolUndecoratedToo = 0x00000004,

        /// <summary>
        /// (used internally)
        /// </summary>
        PatchSymbolReserved = 0x80000000,
    }

    public class WixPatchSymbol : IntermediateSymbol
    {
        public WixPatchSymbol() : base(SymbolDefinitions.WixPatchId, null, null)
        {
        }

        public WixPatchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixPatchId, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchSymbolFields index] => this.Fields[(int)index];

        public string ClientPatchId
        {
            get => (string)this.Fields[(int)WixPatchSymbolFields.ClientPatchId];
            set => this.Set((int)WixPatchSymbolFields.ClientPatchId, value);
        }

        public bool? OptimizePatchSizeForLargeFiles
        {
            get => (bool?)this.Fields[(int)WixPatchSymbolFields.OptimizePatchSizeForLargeFiles];
            set => this.Set((int)WixPatchSymbolFields.OptimizePatchSizeForLargeFiles, value);
        }

        public int? ApiPatchingSymbolFlags
        {
            get => (int?)this.Fields[(int)WixPatchSymbolFields.ApiPatchingSymbolFlags];
            set => this.Set((int)WixPatchSymbolFields.ApiPatchingSymbolFlags, value);
        }

        public string Codepage
        {
            get => (string)this.Fields[(int)WixPatchSymbolFields.Codepage];
            set => this.Set((int)WixPatchSymbolFields.Codepage, value);
        }
    }
}