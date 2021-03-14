// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPatchId = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixPatchId,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchIdSymbolFields.ClientPatchId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPatchIdSymbolFields.OptimizePatchSizeForLargeFiles), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixPatchIdSymbolFields.ApiPatchingSymbolFlags), IntermediateFieldType.Number),
            },
            typeof(WixPatchIdSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixPatchIdSymbolFields
    {
        ClientPatchId,
        OptimizePatchSizeForLargeFiles,
        ApiPatchingSymbolFlags,
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

    public class WixPatchIdSymbol : IntermediateSymbol
    {
        public WixPatchIdSymbol() : base(SymbolDefinitions.WixPatchId, null, null)
        {
        }

        public WixPatchIdSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixPatchId, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchIdSymbolFields index] => this.Fields[(int)index];

        public string ClientPatchId
        {
            get => (string)this.Fields[(int)WixPatchIdSymbolFields.ClientPatchId];
            set => this.Set((int)WixPatchIdSymbolFields.ClientPatchId, value);
        }

        public bool? OptimizePatchSizeForLargeFiles
        {
            get => (bool?)this.Fields[(int)WixPatchIdSymbolFields.OptimizePatchSizeForLargeFiles];
            set => this.Set((int)WixPatchIdSymbolFields.OptimizePatchSizeForLargeFiles, value);
        }

        public int? ApiPatchingSymbolFlags
        {
            get => (int?)this.Fields[(int)WixPatchIdSymbolFields.ApiPatchingSymbolFlags];
            set => this.Set((int)WixPatchIdSymbolFields.ApiPatchingSymbolFlags, value);
        }
    }
}