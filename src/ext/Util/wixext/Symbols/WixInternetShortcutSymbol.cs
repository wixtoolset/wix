// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixInternetShortcut = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixInternetShortcut.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixInternetShortcutSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutSymbolFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutSymbolFields.IconFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutSymbolFields.IconIndex), IntermediateFieldType.Number),
            },
            typeof(WixInternetShortcutSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum WixInternetShortcutSymbolFields
    {
        ComponentRef,
        DirectoryRef,
        Name,
        Target,
        Attributes,
        IconFile,
        IconIndex,
    }

    public class WixInternetShortcutSymbol : IntermediateSymbol
    {
        public WixInternetShortcutSymbol() : base(UtilSymbolDefinitions.WixInternetShortcut, null, null)
        {
        }

        public WixInternetShortcutSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixInternetShortcut, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixInternetShortcutSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixInternetShortcutSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixInternetShortcutSymbolFields.ComponentRef, value);
        }

        public string DirectoryRef
        {
            get => this.Fields[(int)WixInternetShortcutSymbolFields.DirectoryRef].AsString();
            set => this.Set((int)WixInternetShortcutSymbolFields.DirectoryRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)WixInternetShortcutSymbolFields.Name].AsString();
            set => this.Set((int)WixInternetShortcutSymbolFields.Name, value);
        }

        public string Target
        {
            get => this.Fields[(int)WixInternetShortcutSymbolFields.Target].AsString();
            set => this.Set((int)WixInternetShortcutSymbolFields.Target, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixInternetShortcutSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixInternetShortcutSymbolFields.Attributes, value);
        }

        public string IconFile
        {
            get => this.Fields[(int)WixInternetShortcutSymbolFields.IconFile].AsString();
            set => this.Set((int)WixInternetShortcutSymbolFields.IconFile, value);
        }

        public int? IconIndex
        {
            get => this.Fields[(int)WixInternetShortcutSymbolFields.IconIndex].AsNullableNumber();
            set => this.Set((int)WixInternetShortcutSymbolFields.IconIndex, value);
        }
    }
}