// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixRemoveFolderEx = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixRemoveFolderEx.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRemoveFolderExSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRemoveFolderExSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRemoveFolderExSymbolFields.InstallMode), IntermediateFieldType.Number),
            },
            typeof(WixRemoveFolderExSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum WixRemoveFolderExSymbolFields
    {
        ComponentRef,
        Property,
        InstallMode,
    }

    public class WixRemoveFolderExSymbol : IntermediateSymbol
    {
        public WixRemoveFolderExSymbol() : base(UtilSymbolDefinitions.WixRemoveFolderEx, null, null)
        {
        }

        public WixRemoveFolderExSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixRemoveFolderEx, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRemoveFolderExSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixRemoveFolderExSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixRemoveFolderExSymbolFields.ComponentRef, value);
        }

        public string Property
        {
            get => this.Fields[(int)WixRemoveFolderExSymbolFields.Property].AsString();
            set => this.Set((int)WixRemoveFolderExSymbolFields.Property, value);
        }

        public int InstallMode
        {
            get => this.Fields[(int)WixRemoveFolderExSymbolFields.InstallMode].AsNumber();
            set => this.Set((int)WixRemoveFolderExSymbolFields.InstallMode, value);
        }
    }
}