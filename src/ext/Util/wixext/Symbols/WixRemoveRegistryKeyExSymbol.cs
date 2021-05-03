// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixRemoveRegistryKeyEx = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixRemoveRegistryKeyEx.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRemoveRegistryKeyExSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRemoveRegistryKeyExSymbolFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixRemoveRegistryKeyExSymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRemoveRegistryKeyExSymbolFields.InstallMode), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixRemoveRegistryKeyExSymbolFields.Condition), IntermediateFieldType.String),
            },
            typeof(WixRemoveRegistryKeyExSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    public enum WixRemoveRegistryKeyExSymbolFields
    {
        ComponentRef,
        Root,
        Key,
        InstallMode,
        Condition,
    }

    public enum WixRemoveRegistryKeyExInstallMode
    {
        Install = 1,
        Uninstall = 2,
    }

    public class WixRemoveRegistryKeyExSymbol : IntermediateSymbol
    {
        public WixRemoveRegistryKeyExSymbol() : base(UtilSymbolDefinitions.WixRemoveRegistryKeyEx, null, null)
        {
        }

        public WixRemoveRegistryKeyExSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixRemoveRegistryKeyEx, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRemoveRegistryKeyExSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixRemoveRegistryKeyExSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixRemoveRegistryKeyExSymbolFields.ComponentRef, value);
        }

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)WixRemoveRegistryKeyExSymbolFields.Root].AsNumber();
            set => this.Set((int)WixRemoveRegistryKeyExSymbolFields.Root, (int)value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)WixRemoveRegistryKeyExSymbolFields.Key];
            set => this.Set((int)WixRemoveRegistryKeyExSymbolFields.Key, value);
        }

        public WixRemoveRegistryKeyExInstallMode InstallMode
        {
            get => (WixRemoveRegistryKeyExInstallMode)this.Fields[(int)WixRemoveRegistryKeyExSymbolFields.InstallMode].AsNumber();
            set => this.Set((int)WixRemoveRegistryKeyExSymbolFields.InstallMode, (int)value);
        }

        public string Condition
        {
            get => this.Fields[(int)WixRemoveRegistryKeyExSymbolFields.Condition].AsString();
            set => this.Set((int)WixRemoveRegistryKeyExSymbolFields.Condition, value);
        }
    }
}
