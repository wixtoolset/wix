// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBuildInfo = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBuildInfo,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBuildInfoSymbolFields.WixVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBuildInfoSymbolFields.WixOutputFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBuildInfoSymbolFields.WixProjectFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBuildInfoSymbolFields.WixPdbFile), IntermediateFieldType.String),
            },
            typeof(WixBuildInfoSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBuildInfoSymbolFields
    {
        WixVersion,
        WixOutputFile,
        WixProjectFile,
        WixPdbFile,
    }

    public class WixBuildInfoSymbol : IntermediateSymbol
    {
        public WixBuildInfoSymbol() : base(SymbolDefinitions.WixBuildInfo, null, null)
        {
        }

        public WixBuildInfoSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBuildInfo, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBuildInfoSymbolFields index] => this.Fields[(int)index];

        public string WixVersion
        {
            get => (string)this.Fields[(int)WixBuildInfoSymbolFields.WixVersion];
            set => this.Set((int)WixBuildInfoSymbolFields.WixVersion, value);
        }

        public string WixOutputFile
        {
            get => (string)this.Fields[(int)WixBuildInfoSymbolFields.WixOutputFile];
            set => this.Set((int)WixBuildInfoSymbolFields.WixOutputFile, value);
        }

        public string WixProjectFile
        {
            get => (string)this.Fields[(int)WixBuildInfoSymbolFields.WixProjectFile];
            set => this.Set((int)WixBuildInfoSymbolFields.WixProjectFile, value);
        }

        public string WixPdbFile
        {
            get => (string)this.Fields[(int)WixBuildInfoSymbolFields.WixPdbFile];
            set => this.Set((int)WixBuildInfoSymbolFields.WixPdbFile, value);
        }
    }
}